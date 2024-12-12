using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Project
{
    public class WorkShop
    {
        public string RealDate;
        public string RealTime;

        public string SendRealTime()
        {
            return RealDate+" "+RealTime;
        } //오늘 요일, 시간을 반환

        public string SetRealTime()
        {
            DateTime date = new DateTime(); // 시간, 날짜를 반환
            var day = date.DayOfWeek; // 오늘 요일을 반환

            string week = string.Empty;
            string Mid = string.Empty;

            RealDate = System.DateTime.Now.ToString("yyyy-MM-dd");
            RealTime = System.DateTime.Now.ToString("HH:mm:ss");

            string subClock = RealTime.Substring(2, 6);//:mm:ss
            int hour = Int32.Parse(RealTime.Substring(0, 2));//HH

            if (hour>=12)
            {
                hour -= 12;
                Mid = "PM";
            }
            else
            {
                //hour = 00;
                Mid = "AM";
            }

            switch (day)
            {
                case DayOfWeek.Monday:
                    week = "월";
                    break;
                case DayOfWeek.Tuesday:
                    week = "화";
                    break;
                case DayOfWeek.Wednesday:
                    week = "수";
                    break;
                case DayOfWeek.Thursday:
                    week = "목";
                    break;
                case DayOfWeek.Friday:
                    week = "금";
                    break;
                case DayOfWeek.Saturday:
                    week = "토";
                    break;
                case DayOfWeek.Sunday:
                    week = "일";
                    break;
                default:
                    break;
            }

            return RealDate + " " + week + "요일\n" + Mid + " " + hour.ToString()+subClock;
        }//오늘 요일, 시간을 저장

        public int GetSubTime(string Time) // 시,분을 초로 만들기
        {
            int sec = 0;

            sec += (Time[0] - '0') * 36000;
            sec += (Time[1] - '0') * 3600;
            sec += (Time[3] - '0') * 600;
            sec += (Time[4] - '0') * 60;
            sec += (Time[6] - '0') * 10;
            sec += (Time[7] - '0');

            return sec;
        }

        public int Charge(string CarNumber,int sec)
        {

            int coin = 0;
            int cost = DataManager.Instance.Price30;

            if (!CarNumber.Contains("배"))
            {
                for (int i = 0; i < sec / 60 /*3600*/; i++) // 한시간씩 시간당 추가요금 붙음
                {
                    coin += (cost * 6);
                    cost += DataManager.Instance.PriceAdd;
                }
                coin += (cost * (sec % 60));
            }

            //coin += coin * (cost / 600); //남은 초 계산하기
            //coin += coin * (cost % 600) / 60;

            return coin;
        }//요금을 계산하는 메소드 현재 1초마다 요금계산

    }


}
