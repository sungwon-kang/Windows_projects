using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace ChattingClient
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct Packet
    {
        [MarshalAs(UnmanagedType.I4)]
        public int Func;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Sender;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Receiver;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string Message;

        [MarshalAs(UnmanagedType.I4)]
        public int Sigl;
        [MarshalAs(UnmanagedType.I8)]
        public long FileLength;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string FileName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32768)]
        public byte[] FileData;

        public void Init_SendMsg([MarshalAs(UnmanagedType.I4)] int func, [MarshalAs(UnmanagedType.LPWStr)] string Sender, [MarshalAs(UnmanagedType.LPWStr)] string Receiver, [MarshalAs(UnmanagedType.LPWStr)] string Msg)
        {
            this.Func = func;
            this.Sender = Sender;
            this.Receiver = Receiver;
            this.Message = Msg;
        }

        public void Init_SendFile([MarshalAs(UnmanagedType.I4)]int func, [MarshalAs(UnmanagedType.LPWStr)] string Sender, [MarshalAs(UnmanagedType.LPWStr)] string Receiver, [MarshalAs(UnmanagedType.I4)] int sigl, [MarshalAs(UnmanagedType.I8)] long FileLength, [MarshalAs(UnmanagedType.LPWStr)] string FileName)
        {
            this.Func = func;
            this.Sender = Sender;
            this.Receiver = Receiver;
            this.Sigl = sigl;

            this.FileLength = FileLength;
            this.FileName = FileName;
        }

        public string Show_All_Data()
        {
            return $"Sender : {Sender}, Receiver : {Receiver}, Msg : {Message}, sigl : {Sigl}, FileLength : {FileLength}, FileName : {FileName}";
        }
    }

    public class PublicFunction
    {
        public string TEMPORARY_STORAGE_SERVERIP { get; set; }
        public int TEMPORARY_STORAGE_PORT { get; set; }

        /* 임시로 저장될 ServerIP, PORT를 초기화하는 생성자 */
        public PublicFunction()
        {
            TEMPORARY_STORAGE_SERVERIP = "Ipv4";
            TEMPORARY_STORAGE_PORT = 0;
        }

        /// https://devjaya.tistory.com/1 Marshal
        /// https://docs.microsoft.com/ko-kr/dotnet/api/system.runtime.interopservices.gchandle?view=net-5.0 GCHandle

        public object ByteToStructure(byte[] data, Type type)
        {
            /// https://devjaya.tistory.com/1 Marshal
            IntPtr buff = Marshal.AllocHGlobal(data.Length); // 배열의 크기만큼 비관리 메모리 영역에 메모리를 할당한다.

            Marshal.Copy(data, 0, buff, data.Length); // 배열에 저장된 데이터를 위에서 할당한 메모리 영역에 복사한다.
            object obj = Marshal.PtrToStructure(buff, type); // 복사된 데이터를 구조체 객체로 변환한다.
            Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함

            return obj; // 구조체 리턴
        }

        public byte[] StructureToByte(object obj)
        {
            int datasize = Marshal.SizeOf(obj);//((PACKET_DATA)obj).TotalBytes; // 구조체에 할당된 메모리의 크기를 구한다.

            IntPtr buff = Marshal.AllocHGlobal(datasize); // 비관리 메모리 영역에 구조체 크기만큼의 메모리를 할당한다.

            Marshal.StructureToPtr(obj, buff, false); // 할당된 구조체 객체의 주소를 구한다.

            byte[] data = new byte[datasize]; // 구조체가 복사될 배열

            Marshal.Copy(buff, data, 0, datasize); // 구조체 객체를 배열에 복사

            Marshal.FreeHGlobal(buff); // 비관리 메모리 영역에 할당했던 메모리를 해제함

            return data; // 배열을 리턴
        }

        public string GetFileName(string path)
        {
            string[] _split_path = path.Split('\\');

            return _split_path[_split_path.Length - 1];
        }
        
        /* 자신의 외부 IP를 얻는 함수 */
        public string Get_External_Host_IP()
        {
            string _MyIP = string.Empty;

            try
            {
                /// [http://checkip.dyndns.org/]에서 HTML을 가져온다.
                /// HTML에 IP 부분만을 SubString하여 반환한다.

                string _check_IP_URL = "http://checkip.dyndns.org/";

                WebClient _wc = new WebClient();

                UTF8Encoding _utf8 = new UTF8Encoding();

                string _Request_HTML = "";

                /// 사이트에서 얻은 Html의 다음과 같이 저장된다.
                /// ex) "<html><head><title>Current IP Check</title></head><body>Current IP Address: 111.222.333.444</body></html>\r\n"
                _Request_HTML = _utf8.GetString(_wc.DownloadData(_check_IP_URL));

                _Request_HTML = _Request_HTML.Substring(_Request_HTML.IndexOf("Current IP Address:"));

                _Request_HTML = _Request_HTML.Substring(0, _Request_HTML.IndexOf("</body>"));

                _Request_HTML = _Request_HTML.Split(':')[1].Trim();

                IPAddress _External_Ip = null;

                _External_Ip = IPAddress.Parse(_Request_HTML);

                _MyIP = _External_Ip.ToString();

                /// 출처 : [https://m.blog.naver.com/PostView.nhn?blogId=goldrushing&logNo=130183695846&proxyReferer=https:%2F%2Fwww.google.co.kr%2F]
            }
            catch (Exception ex)
            {
                this.printLog($"{ex.ToString()}");
            }

            return _MyIP;
        }

        /* 콘솔에 로그를 출력하는 함수 */
        public void printLog(string msg)
        {
            Console.WriteLine($"[Client] {msg}\n");
        }

    }
}
