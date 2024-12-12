using System;
using System.Threading;

using System.Net.Sockets;
using System.Net;

using ChattingClient.Class;
using System.Windows.Media.Imaging;
using System.IO;

namespace ChattingClient
{
    public class Client
    {
        private PublicFunction pf;
        private Thread Recv_Thread_GetMsg;
        private FTP ftp;
        public Socket Sct;

        private const int MAX_BUFF_SIZE = 65536;

        private string ServerIP;
        public string MyIP;
        private int PORT;

        public bool isConnected;
        public byte[] SendBuff_to_Server;
        public byte[] RecvBuff_from_Server;

        public Client()
        {
            pf = new PublicFunction();
            ftp = new FTP(this);
        }

        /* 클라이언트가 접속할 Server의 IP와 PORT를 초기화하는 함수 */
        public void InitSocket(string ServerIP, int PORT)
        {
            this.PORT = PORT;
            this.ServerIP = ServerIP;

            /// Server와 연결여부를 나타내는 isConnected를 초기화.
            this.isConnected = false;
        }

        /* 저장된 ServerIP와 PORT로 Socket를 초기화하고 Server에 접속하는 함수 */
        public void Connect_to_Server()
        {
            try
            {
                /// IPv4, Stream 형식 데이터 송/수신, TCP 프로토콜으로 Socket을 초기화하고,
                /// 서버와 연결한다.
                Sct = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Sct.Connect(new IPEndPoint(IPAddress.Parse(this.ServerIP), this.PORT));

                MyIP = pf.Get_External_Host_IP();
                /// 연결에 성공될 시 isConnected에 true 저장하고, 서버에서 오는 메시지를 받는 Thread_Recv_MSg를 실행한다.
                /// 연결에 실패할 시 예외처리문가 수행된다.
                this.isConnected = true;

                Recv_Thread_GetMsg = new Thread(new ThreadStart(Thread_Recv_Packet_from_Server));
                Recv_Thread_GetMsg.Start();

                /// 연결에 성공될 시 Client의 외부 IP를 저장하고, 로그를 출력한다.
                pf.printLog("Connect_to_Server() -> Connection to server complete");
            }
            catch (FormatException FmtExp)
            {
                /// SctClient의 Connect를 수행할 때, 잘못된 IP와 연결될 시 발생하는 오류를 예외 처리한다.
                pf.printLog($"Connect_to_Server() -> Invalid Parameters : [ServerIP] {ServerIP} [PORT] {PORT}");
                pf.printLog($"{FmtExp.ToString()}");
                CloseSocket();

            }
            catch (SocketException SctExp)
            {
                /// SctClient의 Connect를 수행할 때, Server와 연결이 되지 않았을 때 발생하는 오류를 예외 처리한다.
                pf.printLog("Connect_to_Server() -> Server not opened");
                pf.printLog($"{SctExp.ToString()}");
                CloseSocket();
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
                Packet Sigle_Use_Data = new Packet();

                ftp.FtpStart(_FilePath);

                Sigle_Use_Data.Init_SendFile(5, _Sender, _Receiver, 1, ftp.FileLength, FileName);

                Send_Packet_to_Server(Sigle_Use_Data);
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

                Send_Packet_to_Server(Sigle_Use_Data);
            }
            else
            {
                pf.printLog($"Init_and_Send_MsgPacket() -> Packet Send failed [Cause] Receiver : {_Receiver}");
            }
        }

        /* Server에게 메시지를 보내는 함수 */
        public void Send_Packet_to_Server(Packet Data)
        {
            if (isConnected == true)
            {
                SendBuff_to_Server = pf.StructureToByte(Data);

                Sct.Send(SendBuff_to_Server, 0, SendBuff_to_Server.Length, SocketFlags.None);

                pf.printLog($"Send_Packet_to_Server() -> Packet Sent : {Data.Show_All_Data()}");
            }
        }

        /* Server에서 메시지를 받는 함수 */
        public void Thread_Recv_Packet_from_Server()
        {
            /// isConnected가 true일 경우에만 메시지를 받을 수 있다.

            while (isConnected == true)
            {
                try
                {
                    Recv_Packet_from_Server();
                }
                catch (SocketException SctExp)
                {
                    /// 서버와의 연결이 끊겼을 경우 발생하는 오류를 예외 처리한다.
                    pf.printLog($"Thread_Recv_Msg_from_Server() -> Lost connection with server");
                    pf.printLog($"Thread_Recv_Msg_from_Server() -> {SctExp.ToString()}");
                    CloseSocket();
                }
            }
        }

        private void Recv_Packet_from_Server()
        {
            RecvBuff_from_Server = new byte[MAX_BUFF_SIZE];
            int _ReceivedBuff_Length = Sct.Receive(RecvBuff_from_Server);

            if (_ReceivedBuff_Length != 0)
            {
                Packet Data = (Packet)pf.ByteToStructure(RecvBuff_from_Server,typeof(Packet));

                switch (Data.Func)
                {
                    case 1:
                        MainWindow.txt_Static_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            MainWindow.txt_Static_Append_Msg.AppendText($"[{Data.Sender}]이/가 [{Data.Receiver}]에게 : {Data.Message} {Environment.NewLine}");
                        }));
                        break;

                    case 2:
                        if (MainWindow.lst_Static_Accepted_client.Items.Contains(Data.Message) == false)
                        {
                            MainWindow.txt_Static_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                            {
                                MainWindow.lst_Static_Accepted_client.Items.Add(Data.Message);
                                MainWindow.txt_Static_Append_Msg.AppendText($"[{Data.Message}]이/가 입장하였습니다. {Environment.NewLine}");
                            }));

                            pf.printLog($"RecvMsg_From_Server() -> {Data.Message} has been added to the ListBox");
                        }
                        break;

                    case 3:
                        if (MainWindow.lst_Static_Accepted_client.Items.Contains(Data.Message) == true)
                        {
                            MainWindow.txt_Static_Append_Msg.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                            {
                                MainWindow.lst_Static_Accepted_client.Items.Remove(Data.Message);
                                MainWindow.txt_Static_Append_Msg.AppendText($"[{Data.Message}]이/가 퇴장하였습니다.{ Environment.NewLine}");
                            }));

                            pf.printLog($"RecvMsg_From_Server() -> {Data.Message} has been removed to the ListBox");
                        }
                        break;

                    //case 4:
                    //    if (ftp.isWaiting == false && ftp.isWorking == false)
                    //    {
                    //        ftp.FtpStart_wtih_Client(_msg, _signal);
                    //    }
                    //    break;

                    case 5: // ftpAns
                        if (ftp.isWorking == false)
                        {
                            ftp.FtpAns(Data);

                            if (Data.Sigl == 1)
                            {
                                Data.Init_SendFile(5, Data.Receiver, Data.Sender, 2, Data.FileLength, Data.FileName);

                                Send_Packet_to_Server(Data);
                            }
                        }
                        break;

                    case 6: // ftpRecvAndWrite
                        if (ftp.isWorking == true)
                        {
                            Data = ftp.FileRecv(Data);

                            Send_Packet_to_Server(Data);
                        }
                        break;

                    case 7: // ftpReadAndSend
                        if (ftp.isWorking == true)
                        {
                            Data = ftp.FileSend(Data);

                            Thread.Sleep(100);

                            Send_Packet_to_Server(Data);
                        }
                        break;

                    case 8: // ftpClose

                        ftp.FileClose();

                        MainWindow.btn_Static_Send_File.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            MainWindow.btn_Static_Send_File.IsEnabled = true;
                        }));

                        break;
                }
            }
        }

        /* Client의 모든 데이터들을 초기화하는 함수 */
        public void CloseSocket()
        {
            if (isConnected == true)
            {
                MainWindow.btn_Static_Send_File.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    MainWindow.btn_Static_Send_File.IsEnabled = true;
                    MainWindow.txt_Static_IP.Text = string.Empty;
                    MainWindow.txt_Static_PORT.Text = string.Empty;
                    MainWindow.btn_Static_Connection.Content = "접속";
                    MainWindow.img_Static_isConnected.Source = new BitmapImage(new Uri(Directory.GetParent(Environment.CurrentDirectory).Parent.FullName + "/Picture/Off.png", UriKind.RelativeOrAbsolute));
                }));

                this.RecvBuff_from_Server = null;
                this.SendBuff_to_Server = null;
                this.isConnected = false;

                Sct.Close();
                Sct = null;

                Recv_Thread_GetMsg.Abort();
                ftp.FileClose();

                pf.printLog("CloseSocket() -> Complete to clear Data");
            }
        }
    }
}
