using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using System.Threading;
using System.IO;
using System.Windows.Threading;
using System.Timers;
using Microsoft.Win32;
using System.Net.Sockets;

namespace ChattingClient
{
    public partial class MainWindow : Window
    {
        private Client client;
        private PublicFunction pf;
        private System.Timers.Timer Timer_ConnectionStatus;

        public static Image img_Static_isConnected;

        public static TextBlock txt_Static_IP;
        public static TextBlock txt_Static_PORT;
        public static TextBox txt_Static_Append_Msg;
        public static ListBox lst_Static_Accepted_client;

        public static Button btn_Static_Send_File;
        public static Button btn_Static_Connection;

        public MainWindow()
        {
            InitializeComponent();
            Initialization();
        }

        /* 멤버변수 초기화 */
        private void Initialization()
        {
            client = new Client();

            pf = new PublicFunction();


            txt_Static_IP = txt_ShowIP as TextBlock;
            txt_Static_PORT = txt_ShowPORT as TextBlock;
            txt_Static_Append_Msg = txt_ShowMsg as TextBox;

            btn_Static_Send_File = btn_SendFile as Button;
            btn_Static_Connection = btn_Connection as Button;

            img_Static_isConnected = img_isConnected as Image;
            lst_Static_Accepted_client = lst_ShowClientIP as ListBox;

            Timer_ConnectionStatus = new System.Timers.Timer();
            Timer_ConnectionStatus.Interval = 1000 / 100;
            Timer_ConnectionStatus.Elapsed += new ElapsedEventHandler(Change_ConnectionStatus_Image);
        }

        /* Server에게 메시지를 보내는 Click 이벤트 */
        private void btn_SendMsg_Click(object sender, RoutedEventArgs e)
        {
            if (client.isConnected == true)
            {
                SendMsg_to_Server();
            }
        }

        /* Server에게 메시지를 보내는 KeyDown 이벤트 */
        private void txt_SendMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (client.isConnected == true && e.Key == Key.Enter)
            {
                SendMsg_to_Server();
            }
        }

        /* Server에게 메시지를 보내는 함수 */
        private void SendMsg_to_Server()
        {
            string _msg = txt_InputMsg.Text.ToString();
            string _Receiver = lst_Get_Connected_ClientIP();

            client.Init_and_Send_MsgPacket($"Server#{_Receiver}#{_msg}");

            txt_ShowMsg.AppendText($"[{_Receiver} 에게] : {_msg} {Environment.NewLine}");

            txt_ShowMsg.ScrollToEnd();
            txt_InputMsg.Clear();
        }

        private string lst_Get_Connected_ClientIP()
        {
            object _clientIP = lst_ShowClientIP.SelectedItem;

            return _clientIP as string;
        }

        /* ConnectionWindow를 여는 Click 이벤트 */
        private void btn_Connection_Click(object sender, RoutedEventArgs e)
        {
            if (client.isConnected == false)
            {
                ConnectionWindow connection = new ConnectionWindow(pf);
                connection.InfoSendEvent += new InfoSendEventHandler(Get_Info_to_ConnectionWindow);
                connection.Show();
            }
            else
            {
                Packet Single_Use_Data = new Packet();
                Single_Use_Data.Init_SendMsg(3, client.MyIP, "All", client.MyIP);
                client.SendBuff_to_Server = pf.StructureToByte(Single_Use_Data);
                client.Sct.Send(client.SendBuff_to_Server, 0, client.SendBuff_to_Server.Length, SocketFlags.None);

                client.CloseSocket();

                DisConnect_to_Server();
            }
        }

        /* ConnectionWindow에서 전달받은 정보로 Server에 연결하는 함수 */
        private void Get_Info_to_ConnectionWindow(PublicFunction pf)
        {
            this.pf = pf;

            client.InitSocket(pf.TEMPORARY_STORAGE_SERVERIP, pf.TEMPORARY_STORAGE_PORT);
            client.Connect_to_Server();

            txt_ShowIP.Text = pf.TEMPORARY_STORAGE_SERVERIP;
            txt_ShowPORT.Text = pf.TEMPORARY_STORAGE_PORT.ToString();

            img_isConnected.Source = new BitmapImage(new Uri(Directory.GetParent(Environment.CurrentDirectory).Parent.FullName + "/Picture/On.png", UriKind.RelativeOrAbsolute));
            Timer_ConnectionStatus.Start();

            txt_ShowMsg.AppendText($"[서버와 연결이 되었습니다]{Environment.NewLine}");

            lst_Static_Accepted_client.Items.Add("All");
            btn_Connection.Content = "해제";
        }

        /* Server와의 연결상태에 따라 이미지UI를 제어하는 타이머 함수 */
        private void Change_ConnectionStatus_Image(object sender, ElapsedEventArgs e)
        {
            if (client.isConnected == false)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    DisConnect_to_Server();
                }));
            }
        }

        private void btn_FileSend_Click(object sender, RoutedEventArgs e)
        {
            if (client.isConnected == true)
            {

                OpenFileDialog dlg = new OpenFileDialog();

                dlg.Filter = "All files(*.*)|*.*";

                if (dlg.ShowDialog().ToString() == "True")
                {
                    btn_SendFile.IsEnabled = false;

                    txt_ShowMsg.AppendText($"서버에게 [{pf.GetFileName(dlg.FileName)}]을 전송합니다.{Environment.NewLine}");

                    client.Init_and_Send_FilePacket($"{client.MyIP}#Server#{dlg.FileName}");
                }
            }
        }

        /* Server와의 연결을 끊는 함수 */
        private void DisConnect_to_Server()
        {
            lst_Static_Accepted_client.Items.Clear();

            Timer_ConnectionStatus.Stop();

            txt_ShowMsg.AppendText($"[서버와 연결이 끊겼습니다.] {Environment.NewLine}");

            txt_ShowMsg.ScrollToEnd();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (client.isConnected == true)
            {
                DisConnect_to_Server();

                client.CloseSocket();
            }

            pf.printLog("Closing Client...");

            Thread.Sleep(1000);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            pf.printLog("Closed Client");
        }
    }
}
