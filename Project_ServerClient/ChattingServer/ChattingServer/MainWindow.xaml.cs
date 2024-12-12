using ChattingServer.Server;
using Microsoft.Win32;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChattingServer
{

    public partial class MainWindow : Window
    {

        private MainServer server;
        private PublicFunction pf;
        private Thread Thread_Open_Server;

        private string ServerIP;

        public static Button btn_Send_File;
        public static TextBox txt_Append_Msg;
        public static ListBox lst_Accepted_client;

        public MainWindow()
        {
            InitializeComponent();
            Initialization();
        }
        /* 멤버변수 초기화 */
        private void Initialization()
        {
            pf = new PublicFunction();
            server = new MainServer(9999);

            btn_Send_File = btn_SendFile as Button;

            txt_Append_Msg = txt_ShowMsg as TextBox;

            lst_Accepted_client = lst_ShowClientIP as ListBox;

            Thread_Open_Server = new Thread(new ThreadStart(server.OpenServer));
        }

        /* Server를 여는 Click 이벤트 */
        private void btn_ServerOpen_Click(object sender, RoutedEventArgs e)
        {
            if (server.isOpen == false)
            {
                server.InitServer();

                Thread_Open_Server = new Thread(new ThreadStart(server.OpenServer));
                Thread_Open_Server.Start();

                btn_OpenServer.Content = "Off";
                lst_Accepted_client.Items.Add("All");
                lst_Accepted_client.SelectedIndex = 0;

                ServerIP = pf.Get_External_Host_IP();

                txt_ShowIP.Text = ServerIP.ToString();
                txt_ShowPORT.Text = server.PORT.ToString();

                txt_ShowMsg.AppendText($"[서버가 열렸습니다.] {Environment.NewLine}");
            }
            else
            {
                Close_Server();
            }

            txt_ShowMsg.ScrollToEnd();
        }

        /* Client에게 메시지를 보내는 Click 이벤트 */
        private void btn_SendMsg_Click(object sender, RoutedEventArgs e)
        {
            if (server.isOpen == true)
            {
                SendMsg_to_Client();
            }
        }

        /* Client에게 메시지를 보내는 KeyDown 이벤트 */
        private void txt_SendMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (server.isOpen == true && e.Key == Key.Enter)
            {
                SendMsg_to_Client();
            }
        }

        private void btn_SendFile_Click(object sender, RoutedEventArgs e)
        {
            if (server.isOpen == true)
            {
                OpenFileDialog dlg = new OpenFileDialog();

                dlg.Filter = "All files(*.*)|*.*";

                if (dlg.ShowDialog().ToString() == "True")
                {
                    btn_SendFile.IsEnabled = false;

                    string _clientIP = lst_Get_Connected_ClientIP();

                    txt_ShowMsg.AppendText($"{_clientIP}에게 [{pf.GetFileName(dlg.FileName)}]을 전송합니다.{Environment.NewLine}");

                    server.Init_and_Send_FilePacket($"Server#{_clientIP}#{dlg.FileName}");
                }
            }
        }

        private string lst_Get_Connected_ClientIP()
        {
            object _clientIP = lst_ShowClientIP.SelectedItem;

            return _clientIP as string;
        }

        /* Client에게 메시지를 보내는 함수 */
        private void SendMsg_to_Client()
        {
            string _msg = txt_InputMsg.Text.ToString();
            string _Receiver = lst_Get_Connected_ClientIP();

            server.Init_and_Send_MsgPacket($"Server#{_Receiver}#{_msg}");

            txt_ShowMsg.AppendText($"[{_Receiver} 에게] : {_msg} {Environment.NewLine}");

            txt_ShowMsg.ScrollToEnd();
            txt_InputMsg.Clear();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Close_Server();

            pf.printLog("Closing Server...");

            Thread.Sleep(1000);
        }

        private void Close_Server()
        {
            server.ShutDown();

            Thread_Open_Server.Abort();

            lst_Accepted_client.Items.Clear();

            btn_OpenServer.Content = "On";
            txt_ShowIP.Text = null;
            txt_ShowPORT.Text = null;

            txt_ShowMsg.AppendText($"[서버가 닫혔습니다.] {Environment.NewLine}");
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            pf.printLog("Closed Server");

            System.Environment.Exit(0);
        }
    }
}
