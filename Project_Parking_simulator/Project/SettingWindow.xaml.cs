using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project
{
    /* delegate 핸들러 */
    public delegate void SettingCoinHanlder(int Price30, int PriceAdd, int MaxCarCount, int EventMAXTime, int EventMinTime);

    public partial class SettingWindow : Window
    {
        public SettingCoinHanlder Setting;

        private int Price30;
        private int PriceAdd;
        private int MaxCount;
        private int MaxTime;
        private int MinTime;

        public SettingWindow()
        {
            InitializeComponent();

            this.Price30 = DataManager.Instance.Price30;
            this.PriceAdd = DataManager.Instance.PriceAdd;
            this.MaxCount = DataManager.Instance.simulator_car_count;
            this.MaxTime = DataManager.Instance.simulator_event_time_max;
            this.MinTime = DataManager.Instance.simulator_event_time_min;

            initialize();
        }//설정 값 초기화

        private void initialize()
        {
            MinEventBox.Text = MinTime.ToString();
            MaxEventBox.Text = MaxTime.ToString();
            MaxCarCountBox.Text = MaxCount.ToString();
            Price30mBox.Text = Price30.ToString();
            PriceAddBox.Text = PriceAdd.ToString();
        }//설정 값들을 해당 각 TextBox.Text에 저장

        private void Seting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.MaxCount = Int32.Parse(MaxCarCountBox.Text);
                this.MaxTime = Int32.Parse(MaxEventBox.Text);
                this.MinTime = Int32.Parse(MinEventBox.Text);
                this.Price30 = Int32.Parse(Price30mBox.Text);
                this.PriceAdd = Int32.Parse(PriceAddBox.Text);

                if (Check())//예외처리
                {
                    Setting(this.Price30, this.PriceAdd, this.MaxCount, this.MaxTime, this.MinTime);
                    System.GC.Collect();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("잘못된 설정입니다.");
                }
            }
            catch
            {
                MessageBox.Show("잘못된 값입니다.");
            }
        }//확인 버튼 누를 시 동작 MainWindow로 데이터를 전달

        private bool Check()
        {
            bool Event = (MaxTime > MinTime) && (MaxTime > 0 && MinTime > 0);
            bool Price = (Price30 >= 0) && (PriceAdd >= 0);
            bool Count = (MaxCount >= 0);

            return Event && Price && Count;
        } // 이상 값 체크


        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.GC.Collect();
            this.Close();
        }// 취소버튼

    }
}
