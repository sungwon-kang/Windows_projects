using System;
using System.Windows;
using System.Windows.Input;

namespace ChattingClient
{

    public delegate void InfoSendEventHandler(PublicFunction Call);
    public partial class ConnectionWindow : Window
    {

        private PublicFunction pf;
        public InfoSendEventHandler InfoSendEvent;

        /* ConnectionWindow의 생성자 */
        public ConnectionWindow(PublicFunction pf)
        {
            InitializeComponent();

            this.pf = pf;

            Initialization();
        }

        /* ConnectionWindow UI 초기화 */
        private void Initialization()
        {
            /// 임시로 저장된 ServerIP와 PORT를 TextBox에 출력한다.
            txt_IP.Text = pf.TEMPORARY_STORAGE_SERVERIP.ToString();
            txt_PORT.Text = pf.TEMPORARY_STORAGE_PORT.ToString();
        }

        /* ConnectionWindow UI 초기화 */
        private void btn_Connection_Click(object sender, RoutedEventArgs e)
        {
            Send_InputData_to_MainWindow();
        }

        private void txt_IP_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Send_InputData_to_MainWindow();
            }
        }

        private void txt_PORT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send_InputData_to_MainWindow();
            }
        }

        private void Send_InputData_to_MainWindow()
        {
            pf.TEMPORARY_STORAGE_SERVERIP = txt_IP.Text.ToString();
            pf.TEMPORARY_STORAGE_PORT = Int32.Parse(txt_PORT.Text);

            /* MainWindow에 call을 전달 */
            InfoSendEvent(pf);

            this.Close();
        }
    }
}
