using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Collections;

namespace ChattingServer.Server
{
    /* 클라이언트에 대한 정보를 저장하는 구조체 */
    public class ClientInfo
    {
        public Socket Sct { get; set; }
        public Thread Thread_Recv_Msg { get; set; }
        public FTP ftp;

        public string ClientIP;
        public byte[] Recvbuff_from_Client;
        public byte[] Sendbuff_to_Client;

        public void resetData()
        {
            Recvbuff_from_Client = null;
            Sendbuff_to_Client = null;
            ClientIP = null;

            Sct.Close();
            Sct = null;
        }

        //[https://stackoverflow.com/questions/9977393/how-do-i-pass-an-object-into-a-timer-event]
    }

    public class MainServer
    {
        private const int MAX_CLIENT_COUNT = 5;
        private const int MAX_BUFF_SIZE = 65536;
        public static Hashtable Hash_Connected_Clients;

        private PublicFunction pf;
        private Socket SctServer;
        private Queue<Socket> Que_Connected_Clients;

        private string ServerIP;
        public int PORT;
        public bool isOpen;

        public MainServer(int PORT)
        {
            this.pf = new PublicFunction();
            this.PORT = PORT;
            this.isOpen = false;
        }

        /* Server의 Socket 초기화하는 함수 */
        public void InitServer()
        {
            /// Ipv4 버전, 스트림 형식으로 데이터를 주고 받기, TCP 통신 프로토콜로 초기화.
            this.SctServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.SctServer.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            /// IPAddress.Any 모든 IP 허용.
            IPEndPoint _IPEF = new IPEndPoint(IPAddress.Any, PORT);

            /// IP와 PORT를 Socket과 Bind.
            SctServer.Bind(_IPEF);
            SctServer.Listen(MAX_CLIENT_COUNT);

            /// Server에 연결하는 Client에 대한 정보를 저장하는 구조체 선언.
            Hash_Connected_Clients = new Hashtable();
            Que_Connected_Clients = new Queue<Socket>();

            pf.printLog("InitServer() -> Complete to Server Initialization");

        }

        /* Server를 여는 함수 */
        public void OpenServer()
        {
            pf.printLog("OpenServer() -> Server Start");

            ServerIP = pf.Get_External_Host_IP();

            isOpen = true;

            while (isOpen == true)
            {
                try
                {
                    pf.printLog("OpenServer() -> Waiting for client...");
                    /// Client가 연결될 때까지 대기.
                    string _Accpeted_Client_IP = Accept_Client();

                    pf.printLog($"OpenServer() -> Accpetion Client { _Accpeted_Client_IP }");

                    MainWindow.txt_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                           delegate ()
                           {
                               MainWindow.txt_Append_Msg.AppendText($"[{_Accpeted_Client_IP} 클라이언트가 접속하였습니다.] {Environment.NewLine}");
                               MainWindow.lst_Accepted_client.Items.Add(_Accpeted_Client_IP);
                           }));

                    Packet Sigle_Use_Data = new Packet();
                    Sigle_Use_Data.Init_SendMsg(2, _Accpeted_Client_IP, "All", _Accpeted_Client_IP);
                    Thread t1 = new Thread(new ParameterizedThreadStart(Send_Msg_to_All_Clients));
                    t1.Start(Sigle_Use_Data);
                }
                catch (ArgumentException ArgExp)
                {
                    Socket Rejected_Client = Que_Connected_Clients.Dequeue();

                    pf.printLog($"OpenServer() -> Invalid IP {pf.GetClientIP(Rejected_Client)} has already been accessed.");

                    pf.printLog($"{ ArgExp.ToString()}");

                    Rejected_Client.Close();
                }
                catch (SocketException SctExp)
                {
                    pf.printLog($"OpenServer() -> Socket Accpetion Error");
                    pf.printLog($"OpenServer() -> {SctExp.ToString()}");
                }
            }
        }

        private string Accept_Client()
        {
            Que_Connected_Clients.Enqueue(SctServer.Accept());

            string _Accpeted_Client_IP = pf.GetClientIP(Que_Connected_Clients.Peek());
            Hash_Connected_Clients.Add(_Accpeted_Client_IP, new ClientInfo());

            ClientInfo _Accpeted_Client_Info = (ClientInfo)Hash_Connected_Clients[_Accpeted_Client_IP];
            _Accpeted_Client_Info.Sct = Que_Connected_Clients.Dequeue();
            _Accpeted_Client_Info.ftp = new FTP(_Accpeted_Client_Info.Sct);
            _Accpeted_Client_Info.ClientIP = _Accpeted_Client_IP;

            pf.printLog($"Accept_Client() -> ClientInfo of {_Accpeted_Client_IP} Initialization");

            _Accpeted_Client_Info.Thread_Recv_Msg = new Thread(new ParameterizedThreadStart(Thread_Recv_Packet_from_Clients));
            _Accpeted_Client_Info.Thread_Recv_Msg.Start(_Accpeted_Client_Info);

            pf.printLog($"Accept_Client() -> {_Accpeted_Client_IP} Start receiving messages");

            return _Accpeted_Client_IP;
        }

        private void Send_Msg_to_All_Clients(object packet)
        {
            ICollection _Hash_keys = Hash_Connected_Clients.Keys;
            Packet Data = (Packet)packet;

            if (Hash_Connected_Clients.Count != 0)
            {
                foreach (string A_Receiver in _Hash_keys)
                {
                    ClientInfo _client = (ClientInfo)Hash_Connected_Clients[A_Receiver];
                    string _client_IP = pf.GetClientIP(_client.Sct);

                    if (_client_IP.Equals(Data.Sender) == false)
                    {
                        Data.Receiver = A_Receiver;
                        Send_Packet_to_one_client(Data);
                    }
                }
            }
            else
            {
                MainWindow.txt_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    MainWindow.txt_Append_Msg.AppendText($"연결된 클라이언트가 없어, 메세지가 전송되지 않았습니다. {Environment.NewLine}");
                }));

                pf.printLog($"Send_Msg_to_All_Clients() -> No clients connected");
            }
        }

        private void Send_Packet_to_one_client(Packet Data)
        {
            ClientInfo _client = (ClientInfo)Hash_Connected_Clients[Data.Receiver];

            _client.Sendbuff_to_Client = pf.StructureToByte(Data);

            _client.Sct.Send(_client.Sendbuff_to_Client, 0, _client.Sendbuff_to_Client.Length, SocketFlags.None);

            pf.printLog($"Send_Packet_to_one_client() -> Packet Sent : {Data.Show_All_Data()}");
        }

        /* Server가 Client에게 메시지를 보내는 함수 */
        public void Send_Packet_to_Client(Packet Data)
        {
            int _function = Data.Func;   // 기능
            string _Receiver = Data.Receiver;   // 메시지를 받을 IP

            try
            {
                switch (_function)
                {
                    case 1: // 모든 클라이언트에게 전송
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        if (_Receiver.Equals("All") == true)
                        {
                            Thread t1 = new Thread(new ParameterizedThreadStart(Send_Msg_to_All_Clients));
                            t1.Start(Data);
                        }
                        else
                        {
                            Send_Packet_to_one_client(Data);
                        }
                        break;

                }
            }
            catch (SocketException e)
            {
                pf.printLog($"Send_Packet_to_Client() -> Packet Send failed : {Data.Show_All_Data()}");
                pf.printLog($"Send_Packet_to_Client() -> {e.ToString()}");
            }
        }

        public void Init_and_Send_FilePacket(string msg)
        {
            string[] _split_msg = msg.Split('#');
            string _Sender = _split_msg[0];       // 메시지를 받을 IP
            string _Receiver = _split_msg[1];       // 메시지를 받을 IP
            string _FilePath = _split_msg[2];       // 파일명

            string FileName = pf.GetFileName(_FilePath);

            if (_Receiver.Equals("null") == false && _Receiver.Equals("All") == false)
            {
                ClientInfo _Info = Hash_Connected_Clients[_Receiver] as ClientInfo;

                Packet Sigle_Use_Data = new Packet();

                _Info.ftp.FtpStart(_FilePath);

                Sigle_Use_Data.Init_SendFile(5, _Sender, _Receiver, 1, _Info.ftp.FileLength, FileName);

                Send_Packet_to_Client(Sigle_Use_Data);
            }
            else
            {
                pf.printLog($"Init_and_Send_FilePacket() -> Packet Send failed [Cause] Receiver : {_Receiver}");
            }
        }

        public void Init_and_Send_MsgPacket(string msg)
        {
            string[] _split_msg = msg.Split('#');
            string _Sender = _split_msg[0];       // 메시지를 받을 IP
            string _Receiver = _split_msg[1];       // 메시지를 받을 IP
            string _msg = _split_msg[2];       // 메시지를 받을 IP

            if (_Receiver.Equals("null") == false)
            {
                Packet Sigle_Use_Data = new Packet();

                Sigle_Use_Data.Init_SendMsg(1, _Sender, _Receiver, _msg);

                Send_Packet_to_Client(Sigle_Use_Data);
            }
            else
            {
                pf.printLog($"Init_and_Send_MsgPacket() -> Packet Send failed [Cause] Receiver : {_Receiver}");
            }
        }

        /* Client에서 오는 메시지를 Server가 받는 쓰레드 함수 */
        public void Thread_Recv_Packet_from_Clients(object Client)
        {
            /// Client 정보가 저장된 구조체를 지역변수 _Info에 초기화
            ClientInfo _Info = Client as ClientInfo;

            while (isOpen == true)
            {
                try
                {
                    Recv_Packet_from_Clients(_Info);
                }
                catch (SocketException SctExp)
                {
                    pf.printLog($"Thread_Recv_Msg_from_Clients() -> Complete to clear Data of disconnected {_Info.ClientIP}");
                    pf.printLog($"Thread_Recv_Msg_from_Clients() -> {SctExp.ToString()}");
                    break;
                }
            }
        }


        private void Recv_Packet_from_Clients(ClientInfo Info)
        {

            Info.Recvbuff_from_Client = new byte[MAX_BUFF_SIZE];
            int _Received_Byte_Length = Info.Sct.Receive(Info.Recvbuff_from_Client);

            /// _RecvBuff_Length가 0이 아닐 경우 수행한다.
            if (_Received_Byte_Length != 0)
            {
                /// Recvbuff_from_Client에 저장된 Bytes를 Unicode로 인코딩하고, string형으로 변환하여 _RecvMsg에 저장한다.
                Packet Data = (Packet)pf.ByteToStructure(Info.Recvbuff_from_Client,typeof(Packet));

                pf.printLog($"Recv_Packet_from_Clients() -> Packet received {Data.Show_All_Data()}");

                switch (Data.Func)
                {
                    case 1: // msg

                        Send_Packet_to_Client(Data);

                        MainWindow.txt_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            MainWindow.txt_Append_Msg.AppendText($"[{Data.Sender}이/가 [{Data.Receiver}]에게 : {Data.Message} {Environment.NewLine}");
                        }));

                        break;
                    case 2: //lstadd
                        if (MainWindow.lst_Accepted_client.Items.Contains(Data.Message) == false)
                        {
                            Send_Packet_to_Client(Data);

                            MainWindow.txt_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                            {
                                MainWindow.lst_Accepted_client.Items.Add(Data.Message);
                                MainWindow.txt_Append_Msg.AppendText($"[{Data.Message}] 이/가 입장하였습니다.{ Environment.NewLine}");
                            }));

                            pf.printLog($"Recv_Packet_from_Clients() -> {Data.Message} has been added to the ListBox");
                        }
                        break;
                    case 3: //lstdel
                        if (MainWindow.lst_Accepted_client.Items.Contains(Data.Message) == true)
                        {
                            Send_Packet_to_Client(Data);

                            string DisConnected_ClientIP = Data.Sender;
                            pf.printLog($"Recv_Packet_from_Clients() -> {DisConnected_ClientIP} is disconnected");

                            ClientInfo DisConnected_Client = MainServer.Hash_Connected_Clients[Data.Sender] as ClientInfo;

                            DisConnected_Client.resetData();
                            DisConnected_Client.Thread_Recv_Msg.Abort();

                            MainServer.Hash_Connected_Clients.Remove(DisConnected_ClientIP);
                            MainWindow.lst_Accepted_client.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                              delegate ()
                              {
                                  MainWindow.lst_Accepted_client.Items.Remove(DisConnected_ClientIP);
                              }));

                            MainWindow.txt_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                            {
                                MainWindow.lst_Accepted_client.Items.Remove(Data.Message);
                                MainWindow.txt_Append_Msg.AppendText($"[{Data.Message}] 이/가 퇴장하였습니다.{ Environment.NewLine}");
                            }));

                            pf.printLog($"Recv_Packet_from_Clients() -> {Data.Message} has been removed to the ListBox");
                        }
                        break;
                    //case 4: // ftpStart
                        //if (Info.ftp.isWorking == false)
                        //{
                        //    Info.ftp.FtpStart_wtih_Client(Data.FileName);

                        //    Data.Init_SendFile(5, Data.Sender, Data.Receiver, 1, Data.FileLength, Data.FileName);

                        //    Send_Pakcet_to_Client(Data);
                        //}
                      //  break;

                    case 5: // ftpAns
                        if (Info.ftp.isWorking==false)
                        {
                            Info.ftp.FtpAns(Data);

                            if (Data.Sigl == 1)
                            {
                                Data.Init_SendFile(5, Data.Receiver, Data.Sender, 2, Data.FileLength, Data.FileName);

                                Send_Packet_to_Client(Data);
                            }

                        }
                        break;

                    case 6: // ftpRecvAndWrite
                        if (Info.ftp.isWorking == true)
                        {
                            Data = Info.ftp.FileRecv(Data);

                            Send_Packet_to_Client(Data);
                        }
                        break;

                    case 7: // ftpReadAndSend
                        if (Info.ftp.isWorking == true)
                        {
                            Data = Info.ftp.FileSend(Data);

                            Thread.Sleep(100);

                            Send_Packet_to_Client(Data);
                        }
                        break;

                    case 8: // ftpClose

                        Info.ftp.FileClose();

                       
                        
                        break;
                }
            }
        }
        /* Server를 닫는 함수 */
        public void ShutDown()
        {
            if (isOpen == true)
            {
                isOpen = false;

                ICollection _Hash_keys = Hash_Connected_Clients.Keys;
                if (_Hash_keys.Count != 0)
                {
                    foreach (object key in _Hash_keys)
                    {
                        ClientInfo _Info = (ClientInfo)Hash_Connected_Clients[key];

                        _Info.resetData();
                        _Info.Thread_Recv_Msg.Abort();

                        pf.printLog($"ShutDown() -> Complete to Clear all Data of {key}");
                    }
                }

                Que_Connected_Clients.Clear();
                Hash_Connected_Clients.Clear();
                SctServer.Close();

                pf.printLog("ShutDown() -> Complete to clear all data on the Server");

                Thread.Sleep(500);

                pf.printLog("ShutDown() -> ShutDown Server");
                pf.printLog("Bye Bye :)");
            }
        }
    }
}





