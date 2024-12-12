using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading;
using System.Drawing;
using System.Collections.Specialized;
using Excel = Microsoft.Office.Interop.Excel;
using System.Resources;

namespace Project
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private static DataManager.Parking_car[] current_parking_car;
        private DispatcherTimer myTimer = new DispatcherTimer();

        private System.Timers.Timer SimulatorEventThread;
        private bool btn_simulator_state = true;

        private WorkShop Work = new WorkShop();
        private bool P1_P2_flag = false;
        private int floor = 1;

        public static object ParkingHistoryInstance;

        public MainWindow()
        {
            InitializeComponent();

            myTimer.Interval = new TimeSpan(0, 0, 1); //1초마다 동작
            myTimer.Tick += myTimer_Tick; // 해당 메소드 동작
            myTimer.Start();
        }

        private void myTimer_Tick(object sender, EventArgs e)
        {
            lbRealTime.Content = Work.SetRealTime();
        } // 실시간 시간 출력

        private ImageBrush Set_Image(ImageBrush ib, string filename, Stretch stretch)
        {
            ib.ImageSource = new BitmapImage(new Uri(@"src\" + filename, UriKind.RelativeOrAbsolute));
            ib.Stretch = stretch;

            return ib;
        } // 배경을 이미지로 바꾸는 메소드

        private void Erase_Click(object sender, RoutedEventArgs e)
        {
            Message.Text = String.Empty;
        } // 메세지박스 지우기

        /*------------------- 1, 2층 전환 메소드 -------------------*/
        private void Trans_Screen_Click(object sender, RoutedEventArgs e)
        {
            string[] image_path = new string[2];

            if (P1_P2_flag == true)
            {
                image_path[0] = "P1.png";
                image_path[1] = "FunctionShow1.png";

                //버튼 숨김
                ParkSection_53.Visibility = Visibility.Hidden;
                ParkSection_54.Visibility = Visibility.Hidden;
                ParkSection_55.Visibility = Visibility.Hidden;

                //텍스트
                TextOutPath.Content = "OUT";
                TextInPath.Content = "IN";
                TextP.Content = "P1";
                TextP1.Visibility = Visibility.Visible;
                TextP2.Visibility = Visibility.Visible;

                floor = 1;
            }
            else
            {
                image_path[0] = "P2.png";//@"C:\Users\Soric\source\repos\WinsProject_0512(ing)\Project\Resource\P2.png";
                image_path[1] = "FunctionShow2.png";//@"C:\Users\Soric\source\repos\WinsProject_0512(ing)\Project\Resource\FunctionShow2.png";

                //추가버튼 출력
                ParkSection_53.Visibility = Visibility.Visible;
                ParkSection_54.Visibility = Visibility.Visible;
                ParkSection_55.Visibility = Visibility.Visible;

                //화살표 텍스트
                TextOutPath.Content = "P1";
                TextInPath.Content = "P2";
                TextP.Content = "P2";
                TextP1.Visibility = Visibility.Hidden;
                TextP2.Visibility = Visibility.Hidden;

                floor = 2;
            }

            Clear_Park_Screen(floor); // 이동 시 화면초기화기능 후에 다시 층 옮길시 해당 층에 있는 차량들을 배치해야함

            if (btn_simulator_state == false)
            {
                display_LED_control_UI(floor);
            }

            ImageBrush[] ib = new ImageBrush[2];

            for (int i = 0; i < ib.Length; i++)
                ib[i] = Set_Image(new ImageBrush(), image_path[i], Stretch.Fill);

            Screen.Background = ib[0];
            FunctionShow.Background = ib[1];

            P1_P2_flag = !P1_P2_flag;

        } // 화면전환 이벤트

        private void Show_P_Car_Click(object sender, RoutedEventArgs e) // 현재 층에서 주차된 차량 출력
        {
            if (current_parking_car != null)
            {
                StringCollection lines = new StringCollection();

                int count = 0;
                lines.Add("======= 현재 " + floor + "층 주차된 차량 =======\n");

                for (int i = 0; i < current_parking_car.Length; i++)
                {
                    if (current_parking_car[i].parking_floor == floor)
                    {
                        lines.Add("[" + current_parking_car[i].parking_section + "]구역 차량 : " + current_parking_car[i].car_number + "\n");
                        count++;
                    }
                }

                lines.Add("======= 현재 " + floor + "층 주차된 차량 =======\n");

                foreach (string txt in lines)
                    Message.AppendText(txt);

                Message.AppendText(count.ToString() + "\n");

                Message.ScrollToEnd();
            }
            else
                Message.AppendText("시뮬레이션을 작동해주세요.\n");
        }


        /*------------------- 시뮬레이션 동작 메소드 -------------------*/
        private void Btn_start_simulator_Click(object sender, RoutedEventArgs e)
        {
            if (btn_simulator_state == true)
            {
                /* 시뮬레이션 시작전 초기화 */
                Initilize();

                /* 이벤트 발생하는 쓰레드 생성 */
                SimulatorEventThread = new System.Timers.Timer();

                /* 저장되어 있는 데이터들 초기화 */
                Clear_parking_all_car();

                /* 시뮬레이터에 사용할 차량에 대한 row 데이터를 랜덤생성하는 함수 */
                generate_simulator_car_row_data();

                /* 각각 차량에 대한 row 데이터를 가공하여 구조체화 정의 */
                make_simulator_car_inf();

                /* 주차차량관제 시뮬레이터 시작 */
                start_parking_manager_simulator();
            }
            else
            {
                /* 시뮬레이터 버튼 초기화*/
                btn_start_simulator.Background = null;

                /* 데이터 초기화*/
                Clear_parking_all_car();

                /* 시뮬레이터 종료*/
                stop_parking_manager_simulator();
            }

            btn_simulator_state = !btn_simulator_state;

        } // 버튼 누를 시 시뮬레이션 동작

        private void Initilize()
        {
            /* 차량번호 앞, 가운데(index), 뒤 int 배열 데이터 길이 초기화 */
            DataManager.Instance.car_number_front_data = new int[DataManager.Instance.simulator_car_count];
            DataManager.Instance.car_number_center_data_index = new int[DataManager.Instance.simulator_car_count];
            DataManager.Instance.car_number_back_data = new int[DataManager.Instance.simulator_car_count];
            DataManager.Instance.simulator_car_state = new int[DataManager.Instance.simulator_car_count];
            DataManager.Instance.simulator_parking_section_state = new int[DataManager.Instance.simulator_car_count];
            /* 차량번호 가운데 문자 배열 데이터 길이 초기화 */
            DataManager.Instance.car_number_center_data = new string[DataManager.Instance.simulator_car_count];

            /* 시뮬레이터의 주차 차량에 대한 정보(구조체) */
            current_parking_car = new DataManager.Parking_car[DataManager.Instance.simulator_car_count];
            ParkingHistoryInstance = current_parking_car;

            /* 시뮬레이터 시작/정지에 따른 버튼 이미지 및 카운트 초기화*/
            btn_start_simulator.Background = Set_Image
                (new ImageBrush(), "Stop.png", Stretch.Fill);
            Clear_Park_Screen(2);

            EmptyCount_P1.Content = "52";
            EmptyCount_P2.Content = "55";
            FullCount_P1.Content = "0";
            FullCount_P2.Content = "0";

        }// 가상 차량 데이터 메모리공간 생성

        private void generate_simulator_car_row_data()
        {
            /* (차량번호생성) */
            DataManager.Instance.car_number_front_data =
                plural_Random_value(DataManager.Instance.car_number_front_min, DataManager.Instance.car_number_front_max, DataManager.Instance.simulator_car_count);

            DataManager.Instance.car_number_center_data_index =
                plural_Random_value(DataManager.Instance.car_number_center_min, DataManager.Instance.car_number_center_max, DataManager.Instance.simulator_car_count);

            get_car_number_center_data_char();

            DataManager.Instance.car_number_back_data =
                plural_Random_value(DataManager.Instance.car_number_back_min, DataManager.Instance.car_number_back_max, DataManager.Instance.simulator_car_count);
        } // 차량 번호를 생성

        private void get_car_number_center_data_char()// 차량 가운데 번호 생성
        {
            int[] temp_array_index = (int[])(DataManager.Instance.car_number_center_data_index).Clone();
            string[] temp_data_format = (string[])(DataManager.Instance.car_number_center_format).Clone();
            string[] temp_data_char = new string[temp_array_index.Length];

            for (int i = 0; i < temp_array_index.Length; i++)
            {
                int temp_index = temp_array_index[i];

                temp_data_char[i] = temp_data_format[temp_index];
            }
            DataManager.Instance.car_number_center_data = (string[])temp_data_char.Clone();
        }

        private void make_simulator_car_inf()
        {
            for (int i = 0; i < DataManager.Instance.simulator_car_count; i++)
            {
                string temp_car_number = DataManager.Instance.car_number_front_data[i] +
                    DataManager.Instance.car_number_center_data[i] + DataManager.Instance.car_number_back_data[i];

                DataManager.Instance.simulator_car_state[i] = DataManager.Instance.state_exit_car;
                DataManager.Instance.simulator_parking_section_state[i] = 0;
                current_parking_car[i].set_parking_car(temp_car_number, null, null, -1, -1, -1, null, -1);
                // 차량번호, 방문일자(null), 이벤트상태(2:출차/idle), 주차구역위치(-1:null), 출차일자(null), 요금(0)
            }
        } // 차량 번호 조합 후 데이터 초기화

        private void start_parking_manager_simulator()
        {
            DataManager.Instance.simulator_event_time_interval = singular_Random_value
                (DataManager.Instance.simulator_event_time_min, DataManager.Instance.simulator_event_time_max);

            System.Console.Write("interval : {0}, ", DataManager.Instance.simulator_event_time_interval);

            SimulatorEventThread.Interval = DataManager.Instance.simulator_event_time_interval;
            SimulatorEventThread.Elapsed += simulatorEvent;
            SimulatorEventThread.Start();
        } // 시뮬레이션 쓰레드 동작

        private void simulatorEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            int event_mode = singular_Random_value(1, 3); // 1 : 입차, 2 : 출차
            int rnd_index;

            rnd_index = singular_Random_value(0, DataManager.Instance.simulator_car_count);

            if (DataManager.Instance.First_Section_FullCount == 52 && DataManager.Instance.Second_Section_FullCount == 55)
                event_mode = 2;
            try
            {
                if (DataManager.Instance.simulator_car_state[rnd_index] != event_mode)
                {
                    switch (event_mode)
                    {
                        case 1:
                            current_parking_car[rnd_index].visit_date = Work.SendRealTime();
                            current_parking_car[rnd_index].visit_time = Work.RealTime;
                            current_parking_car[rnd_index].exit_date = "\0";
                            set_park_car(rnd_index);
                            break;

                        case 2:
                            set_exit_car(rnd_index);
                            current_parking_car[rnd_index].exit_date = Work.SendRealTime();
                            break;
                    }

                    Console.WriteLine("차량번호 : {0}, 방문일자 : {1}, 출차일자 : {2}, 이벤트상태 : {3}, 인덱스 : {4} ",
                                current_parking_car[rnd_index].car_number,
                                current_parking_car[rnd_index].visit_date,
                                current_parking_car[rnd_index].exit_date,
                                current_parking_car[rnd_index].event_mode,
                                rnd_index);

                }
            }
            catch
            {
                //Console.WriteLine("차량 카운트 인덱스 범위 벗어남");
            }

            DataManager.Instance.simulator_event_time_interval = singular_Random_value
                (DataManager.Instance.simulator_event_time_min, DataManager.Instance.simulator_event_time_max);

            SimulatorEventThread.Interval = DataManager.Instance.simulator_event_time_interval;

            Console.Write("interval : {0} \n", DataManager.Instance.simulator_event_time_interval);
        } // 입차/출차 동작

        private void stop_parking_manager_simulator()
        {
            if (SimulatorEventThread.Enabled == true)
            {
                SimulatorEventThread.Close();
                SimulatorEventThread.Dispose();
            }

            DataManager.Instance.car_number_front_data = null;
            DataManager.Instance.car_number_center_data_index = null;
            DataManager.Instance.car_number_center_data = null;
            DataManager.Instance.car_number_back_data = null;
            DataManager.Instance.simulator_car_state = null;
            DataManager.Instance.simulator_parking_section_state = null;

            DataManager.Instance.First_Section_FullCount = 0;
            DataManager.Instance.Second_Section_FullCount = 0;
            DataManager.Instance.First_Section_EmptyCount = 52;
            DataManager.Instance.Second_Section_EmptyCount = 55;

        } //시뮬레이션 쓰레드 중지

        private void Clear_parking_all_car()
        {
            for (int i = 0; i < current_parking_car.Length; i++)
            {
                current_parking_car[i].Clear_parking_car();
            }

        }//가상 차량 데이터 초기화

        private void set_park_car(int car_index)
        {
            if (current_parking_car[car_index].event_mode != 1)
            {
                int parking_floor = singular_Random_value(1, 3); // 1~2층 랜덤
                int add_index = 0;
                int parking_max_count = 0;
                int parking_number = 0;

                Button LED_button = null;
                bool loop_flag = true;

                // 1층 자리가 없을 경우 2층 주차
                if (DataManager.Instance.First_Section_FullCount >= 52)
                    parking_floor = 2;


                switch (parking_floor)
                {
                    case 1:
                        Console.Write("층 : {0}", parking_floor);

                        parking_max_count = DataManager.Instance.floor_1;//52
                        add_index = 0;
                        break;
                    case 2:

                        Console.Write("층 : {0}", parking_floor);

                        parking_max_count = DataManager.Instance.floor_2;//55
                        add_index = (DataManager.Instance.floor_1);//52
                        break;
                }

                while (loop_flag)
                {

                    parking_number = singular_Random_value(1, parking_max_count + 1);//1층 1~ 52버튼 // 2층 1~55 버튼

                    if (DataManager.Instance.simulator_parking_section_state[add_index + parking_number - 1] != 1) // 1:주차중
                    {
                        DataManager.Instance.simulator_parking_section_state[add_index + parking_number - 1] = 1;// 0 ~ 51 1층 , 52 ~ 106 2층

                        DataManager.Instance.simulator_car_state[car_index] = parking_floor;

                        current_parking_car[car_index].parking_section = parking_number;

                        LED_button = get_LED_control_UI_inf(parking_number.ToString());

                        current_parking_car[car_index].parking_floor = parking_floor;

                        current_parking_car[car_index].event_mode = 1;


                        switch (parking_floor)
                        {
                            case 1:

                                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate //카운트 변경부분
                                {
                                    DataManager.Instance.First_Section_EmptyCount -= 1;
                                    DataManager.Instance.First_Section_FullCount += 1;

                                    EmptyCount_P1.Content = (DataManager.Instance.First_Section_EmptyCount).ToString();
                                    FullCount_P1.Content = (DataManager.Instance.First_Section_FullCount).ToString();

                                //Console.WriteLine("1층 주차");//temp
                            }));
                                break;

                            case 2:

                                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    DataManager.Instance.Second_Section_EmptyCount -= 1;
                                    DataManager.Instance.Second_Section_FullCount += 1;

                                    EmptyCount_P2.Content = (DataManager.Instance.Second_Section_EmptyCount).ToString();
                                    FullCount_P2.Content = (DataManager.Instance.Second_Section_FullCount).ToString();

                                //Console.WriteLine("2층 주차");//temp
                            }));
                                break;
                        }


                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            if (floor == current_parking_car[car_index].parking_floor)
                            {
                                LED_button.Background = Set_Image(new ImageBrush(),"Park_ing.png", Stretch.Uniform);
                            }

                            Message.AppendText(Work.RealTime + " " + current_parking_car[car_index].car_number + " 차량 주차\n");
                            Message.ScrollToEnd();
                        }));

                        loop_flag = false;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }//입차 동작 메소드

        private void set_exit_car(int car_index)
        {
            if (current_parking_car[car_index].event_mode == 1)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    Button LED_button = null;

                    string visit_time = current_parking_car[car_index].visit_time;
                    int Time = Work.GetSubTime(Work.RealTime) - Work.GetSubTime(visit_time);
                    current_parking_car[car_index].charge = Work.Charge(current_parking_car[car_index].car_number, Time);

                    int Section = current_parking_car[car_index].parking_floor;

                    if (Section == floor)
                    {
                        LED_button = get_LED_control_UI_inf(current_parking_car[car_index].parking_section.ToString());
                        LED_button.Background = Set_Image(new ImageBrush(), "Park_not.png", Stretch.Uniform);
                    }

                    if (Section == 1)
                    {
                        DataManager.Instance.First_Section_EmptyCount += 1;
                        DataManager.Instance.First_Section_FullCount -= 1;

                        EmptyCount_P1.Content = (DataManager.Instance.First_Section_EmptyCount).ToString();
                        FullCount_P1.Content = (DataManager.Instance.First_Section_FullCount).ToString();

                        DataManager.Instance.simulator_parking_section_state[current_parking_car[car_index].parking_section - 1] = 2;
                        DataManager.Instance.simulator_car_state[car_index] = 0;
                    }
                    else
                    {
                        int index = 51;

                        DataManager.Instance.Second_Section_EmptyCount += 1;
                        DataManager.Instance.Second_Section_FullCount -= 1;

                        EmptyCount_P1.Content = (DataManager.Instance.Second_Section_EmptyCount).ToString();
                        FullCount_P1.Content = (DataManager.Instance.Second_Section_FullCount).ToString();

                        DataManager.Instance.simulator_parking_section_state[current_parking_car[car_index].parking_section + index] = 2;
                        DataManager.Instance.simulator_car_state[car_index + index] = 0;
                    }

                    Message.AppendText(Work.RealTime + " " + current_parking_car[car_index].car_number + " 차량 출차 " + current_parking_car[car_index].charge + " 요금 지불\n");

                    //DataManager.Instance.simulator_car_state[car_index] = 0;
                    current_parking_car[car_index].parking_section = -1;
                    current_parking_car[car_index].parking_floor = -1;
                    current_parking_car[car_index].event_mode = 2;
                }));

            }
        }//출차 동작 메소드

        /*------------------- 주차 LED 메소드 -------------------*/
        private void Clear_Park_Screen(int P)
        {
            int Section = 0;

            switch (P)
            {
                case 1:
                    Section = DataManager.Instance.floor_1;//52
                    break;

                case 2:
                    Section = DataManager.Instance.floor_2;//55
                    break;

                default:
                    break;
            }

            for (int i = 0; i < Section; i++) // 모든 버튼 이미지 초기화 =>55개
            {
                Button btn = FindName("ParkSection_" + (i + 1)) as Button;

                btn.Background = Set_Image(new ImageBrush(), "Park_not.png", Stretch.Uniform);
            }
        } // 모든 LED 초기화

        private void display_LED_control_UI(int t_floor)
        {
            int floor_parking_section_start_index = 0;
            int floor_parking_section_end_index = 0;
            Button LED_btn = null;

            switch (t_floor)
            {
                case 1:
                    floor_parking_section_start_index = 0;
                    floor_parking_section_end_index = (DataManager.Instance.floor_1);//52

                    break;
                case 2:
                    floor_parking_section_start_index = DataManager.Instance.floor_1;//52
                    floor_parking_section_end_index = DataManager.Instance.simulator_car_count;//107
                    break;
            }

            for (int i = floor_parking_section_start_index; i < floor_parking_section_end_index; i++)
            {
                int LED_btn_index = 0;

                switch (t_floor)
                {
                    case 1:
                        LED_btn_index = i;
                        break;
                    case 2:
                        LED_btn_index = i - (DataManager.Instance.floor_1);
                        break;
                }

                LED_btn = get_LED_control_UI_inf((LED_btn_index + 1).ToString());

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    string image_path;
                    if (i < 107)
                    {
                        if (DataManager.Instance.simulator_parking_section_state[i] == 1)
                        {
                            image_path = @"C:\Users\Soric\source\repos\WinsProject_0512(ing)\Project\Resource\Park_ing.png";
                        }
                        else
                        {
                            image_path = @"C:\Users\Soric\source\repos\WinsProject_0512(ing)\Project\Resource\Park_not.png";
                        }

                        LED_btn.Background = Set_Image(new ImageBrush(), image_path, Stretch.Uniform);
                    }
                }));
            }
        } // 층 변환 시 해당 층에 주차된 가상 차량 데이터에 맞게 LED 출력

        private Button get_LED_control_UI_inf(string t_ui_name)
        {
            string LED_name_front = "ParkSection_" + t_ui_name;
            Button LED_btn = null;

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                object temp_LED_btn = FindName(LED_name_front);
                LED_btn = temp_LED_btn as Button;
            }));

            return LED_btn;
        } //LED 버튼 찾아 반환 메소드

        /*------------------- 랜덤 반환 메소드 -------------------*/
        private int singular_Random_value(int rnd_min, int rnd_max) // 단일(1개)
        {
            Random r = new Random(DateTime.Now.Millisecond);

            return r.Next(rnd_min, rnd_max);
        }

        private int[] plural_Random_value(int rnd_min, int rnd_max, int rnd_count) // 복수(2개 이상)
        {
            Random r = new Random(DateTime.Now.Millisecond);

            int[] plural_number = new int[rnd_count];

            for (int i = 0; i < rnd_count; i++)
            {
                plural_number[i] = r.Next(rnd_min, rnd_max);
            }

            return plural_number;
        }



        /*------------------- 데이터 저장(txt, xlsx) 메소드 -------------------*/
        private void DataSave_Click(object sender, RoutedEventArgs e) // 로그 및 엑셀 저장
        {
            StringCollection lines = new StringCollection();
            string filename = Work.RealDate + "-" + Work.RealTime.Replace(":", "-");
            string savepath = @"src\txt\" + filename + ".txt";

            int lineCount = Message.LineCount;
            if (lineCount > 0)
            {
                for (int line = 0; line < lineCount; line++)
                    lines.Add(Message.GetLineText(line));

                foreach (string txt in lines)
                    System.IO.File.AppendAllText(savepath, txt, Encoding.Default);

                Message.AppendText(filename + ".txt 로그가 저장되었습니다.\n");
            }
            else
            {
                MessageBox.Show("저장할 데이터가 없습니다.");
            }

            Thread t1 = new Thread(new ThreadStart(MakeNewExcel));
            t1.Start();
        }

        public void MakeNewExcel()
        {
            Excel.Application xlapp;
            Excel.Workbook xlwb;
            Excel.Worksheet xlst;
            xlapp = new Excel.Application(); // 엑셀 기능 
            xlwb = null; // 워크 북 기능
            xlst = null;// 워크 시트 기능

            try
            {
                xlwb = xlapp.Workbooks.Add();// 엑셀 기본 생성
                xlst = xlwb.Worksheets.get_Item(1) as Excel.Worksheet;

                xlst.Cells[1, 1] = "차량번호";
                xlst.Cells[1, 2] = "차량상태";
                xlst.Cells[1, 3] = "최근지불액";
                xlst.Cells[1, 4] = "최근방문날짜";
                xlst.Cells[1, 5] = "최근퇴장날짜";
                xlst.Cells[1, 6] = "현재 층";

                int FristRow = 2;
                for (int i = 0; i < current_parking_car.Length; i++)
                {
                    //Cells[행,열] 행 : 1 2 3 4  / 열 A B C D
                    xlst.Cells[i + FristRow, 1] = current_parking_car[i].car_number;
                    xlst.Cells[i + FristRow, 2] = current_parking_car[i].event_mode;
                    xlst.Cells[i + FristRow, 3] = current_parking_car[i].charge;
                    xlst.Cells[i + FristRow, 4] = current_parking_car[i].visit_date;
                    xlst.Cells[i + FristRow, 5] = current_parking_car[i].exit_date;
                    xlst.Cells[i + FristRow, 6] = current_parking_car[i].parking_floor;

                }
                /* 파일 경로 지정*/
                string filename = Work.RealDate + "-" + Work.RealTime.Replace(":", "-");
                string savepath = Environment.CurrentDirectory+@"\src\xlsx\"+filename+".xlsx";

                xlst.Columns.AutoFit();
                xlwb.SaveAs(savepath, Excel.XlFileFormat.xlWorkbookDefault);
                xlwb.Close(true);
                xlapp.Quit();

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate //카운트 변경부분
                {
                    Message.AppendText(filename + ".xlsx 파일이 저장되었습니다.\n");
                }));

            }
            catch
            {
                MessageBox.Show("해당 파일이 없습니다");
            }

            System.GC.Collect();
        }



        /*------------------- 설정 폼 메소드 -------------------*/
        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow RunSettingWin = new SettingWindow();
            RunSettingWin.Setting += new SettingCoinHanlder(this.GetSettingData);
            RunSettingWin.ShowDialog();
        } // 설정 폼 실행

        private void GetSettingData(int GetPrice30, int GetPriceAdd, int GetMaxCount, int GetMaxEvent, int GetMinEvent)
        {
            DataManager.Instance.Price30 = GetPrice30;
            DataManager.Instance.PriceAdd = GetPriceAdd;

            DataManager.Instance.simulator_car_count = GetMaxCount;
            DataManager.Instance.simulator_event_time_min = GetMinEvent;
            DataManager.Instance.simulator_event_time_max = GetMaxEvent;

            Message.AppendText("설정이 완료되었습니다.\n");
        }
        // 설정 폼에서 전달받은 데이터 저장

        /*------------------- 주차 이력 폼 메소드 -------------------*/
        private void Vehicle_Record_run(object sender, RoutedEventArgs e)
        {
            Vehicle_Record HistoryFrom = new Vehicle_Record();
            HistoryFrom.Show();
        }

    }
}
