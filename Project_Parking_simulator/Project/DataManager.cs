using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project
{
    class DataManager
    {

        private static DataManager DM;
        private static object singlelock = new object();

        private DataManager() { } // 생성자 방지

        public static DataManager Instance
        {
            get
            {
                lock (singlelock)
                {
                    if (DM == null)
                        DM = new DataManager();
                }
                return DM;
            }
        }

        /*------------------- 1. 수정금지 -------------------*/

        /* 차 번호판의 가운데 문자 데이터 */
        public string[] car_number_center_format =
            { "가", "나", "다", "라", "마",
            "거", "너", "더", "러", "머", "버", "서", "어", "저",
            "고", "노", "도", "로", "모", "보", "소", "오", "조",
            "구", "누", "두", "루", "무", "부", "수", "우", "주",
            "아", "바", "사", "자",
            "배",
            "하", "허", "호" };//40개
        // https://brunch.co.kr/@caroute/4

        /* 차 번호판의 앞 번호(차종) 범위 1~999 */
        public int car_number_front_min = 1;
        public int car_number_front_max = 999;

        /* 차 번호판의 가운데 문자(용도) 범위 0~39 (번호로 car_number_center_data 배열에 index 함) */
        public int car_number_center_min = 0;
        public int car_number_center_max = 39;

        /* 차 번호판의 뒷 번호(차량등록번호) 범위 101~9999 */
        public int car_number_back_min = 101;
        public int car_number_back_max = 9999;

        /* 입/출차 고유 번호 */
        public int state_join_car = 1;
        public int state_exit_car = 2;

        /* 층마다 주차가능 대수 */
        public int floor_1 = 52;
        public int floor_2 = 55;

        public int First_Section_FullCount = 0;
        public int First_Section_EmptyCount = 52;

        public int Second_Section_FullCount = 0;
        public int Second_Section_EmptyCount = 55;

    
        
        /*------------------- ********* -------------------*/


        /*------------------- 2. 데이터 저장 -------------------*/
        public int[] car_number_front_data { get; set; } = null; // 랜덤 값 저장 변수(차량 앞 번호)
        public int[] car_number_center_data_index { get; set; } = null; // 랜덤 값 저장 변수(차량 가운데 번호) (정수)
        public string[] car_number_center_data { get; set; } = null; // 랜덤 값 저장 변수(차량 가운데 번호) (문자)
        public int[] car_number_back_data { get; set; } = null; // 랜덤 값 저장 변수(차량 뒷 번호)

        public int simulator_car_count { get; set; } = 107; // 시뮬레이터에서 생성할 차량 대수 설정(default=50)
        public int simulator_event_time_interval { get; set; } = 1000; // 시뮬레이터에서 발생되는 이벤트 시간 데이터를 저장함(default=1000)(1초)
        public int simulator_event_time_min { get; set; } = 100; // 시뮬레이터 이벤트 발생 시간 최소 범위 설정(default=100)(0.1초)
        public int simulator_event_time_max { get; set; } = 200; // 시뮬레이터 이벤트 발생 시간 최대 범위 설정(default=100000)(1분40초)

        public int[] simulator_car_state { get; set; } = null; // 각각 차량에 대한 이벤트 상태 테이블을 저장함(1, 2)

        public int[] simulator_parking_section_state { get; set; } = null;
        // 1층, 2층 주차구역에 대한 상태 테이블을 저장함(1:주차중, 그외 자리비움)

        public int Price30 { get; set; }= 1000;
        public int PriceAdd { get; set; } = 200;

        public struct Parking_car
        {
            public string car_number { get; set; }

            public string visit_time { get; set; }
            public string visit_date { get; set; }

            public int parking_floor { get; set; }
            public int parking_section { get; set; } // 주차된 자리의 번호를 저장
            public int event_mode { get; set; } // 1 : 입차, 2 : 출차

            public string exit_date { get; set; }
            public int charge { get; set; }

            public void set_parking_car(string t_car_number, string t_visit_date,string t_visit_time, int t_event_mode,int t_parking_floor,
                int t_parking_section, string t_exit_date, int t_charge)
            {
                this.car_number = t_car_number;
                this.visit_date = t_visit_date;
                this.visit_time = t_visit_time;
                this.event_mode = t_event_mode;

                this.parking_floor = t_parking_floor;
                this.parking_section = t_parking_section;
                this.exit_date = t_exit_date;
                this.charge = t_charge;
            }

            public void Clear_parking_car()
            {
                this.car_number = null;
                this.visit_date = null;
                this.visit_time = null;
                this.exit_date = null;
                this.parking_section = -1;
                this.parking_floor = -1;
                this.event_mode = -1;
                this.charge = -1;

            }
        }
        /*------------------- *. ****** **** -------------------*/



    }
}
