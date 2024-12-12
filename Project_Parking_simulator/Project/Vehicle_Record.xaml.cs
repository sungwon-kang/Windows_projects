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
    /// <summary>
    /// Parking_vehicle_history.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Vehicle_Record : Window
    {

        private DataManager.Parking_car[] current_parking_car; // static 제거
        private List<string> CarNumberList = new List<string>();

        public Vehicle_Record()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            current_parking_car = (DataManager.Parking_car[])MainWindow.ParkingHistoryInstance;
            if (current_parking_car != null)
            {
                ListBox_vechicle.Items.Clear();
                ComBoBox_vechicle.Items.Clear();
                CarNumberList.Clear();
                SortList();

                foreach (string Number in CarNumberList)
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Content = Number;

                    ComBoBox_vechicle.Items.Add(Number);
                    ListBox_vechicle.Items.Add(item);
                }
            }
        }// 가상 차량 데이터를 리스트 박스에 추가하는 메소드


        private void SortList()
        {
            for (int i = 0; i < current_parking_car.Length; i++)
            {
                if (current_parking_car[i].parking_section != -1)
                    CarNumberList.Add(current_parking_car[i].car_number);
            }
            CarNumberList.Sort();
        }// CarNumberList에 차량 데이터를 Add한 후 오름차순 정렬하는 메소드


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Initialize();

            ListBox_vechicle.SelectedIndex = -1;
            ComBoBox_vechicle.SelectedIndex = -1;

            CarNumber_lb.Content = null;
            VisitDate_lb.Content = null;
            ExitDate_lb.Content = null;
        } // 새로고침 버튼 


        private void ComBoBox_vechicle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = ComBoBox_vechicle.SelectedIndex;
            if (i != -1)
            {
                Show_CarInformation(ComBoBox_vechicle.Items[i].ToString());
            }
        } //콤보박스에서 차량이 선택될 시 동작하는 메소드 


        private void ListBox_vechicle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = ListBox_vechicle.SelectedIndex;
            if (i != -1)
            {
                ListBoxItem temp_ListBox = ListBox_vechicle.SelectedItem as ListBoxItem;
                if (temp_ListBox.Content != null)
                    Show_CarInformation(temp_ListBox.Content.ToString());
            }
        } //리스트박스에서 차량이 선택될 시 동작하는 메소드 


        private void Show_CarInformation(string number)
        {
            int index = -1;

            for (int i = 0; i < DataManager.Instance.simulator_car_count; i++)
            {
                try
                {
                    if (current_parking_car[i].car_number == number)
                    {
                        index = i;
                        break;
                    }
                }
                catch { }
            }

            if (index != -1)
            {
                CarNumber_lb.Content = current_parking_car[index].car_number;
                VisitDate_lb.Content = current_parking_car[index].visit_date;
                ExitDate_lb.Content = current_parking_car[index].exit_date;
            }
        }// 전달받은 차량번호를 저장돤 차량 데이터에서 조회한 후 각 label에 저장

    }
}
