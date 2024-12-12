using System;
using System.IO;
using System.Net.Sockets;

namespace ChattingClient.Class
{
    public class FTP
    {
        private FileStream fs;
        private PublicFunction pf;
        private Client client;

        private string FileName;
        private long Num_Bytes_To_Read;

        private int Buff_Length_Total = 0;
        private int DataLength = 32768;
        private int ReadBuff_Length;
        private int TEMP_SIGNAL;

        private byte[] FTP_SendBuff;

        public bool isWorking = false;
        public long FileLength;

        public FTP(Client client)
        {
            this.pf = new PublicFunction();
            this.client = client;
        }

        public void FtpStart(string FilePath)
        {
            FTP_SendBuff = new byte[DataLength];

            FileName = pf.GetFileName(FilePath);

            fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);

            FileLength = fs.Length;

            pf.printLog($"FtpStart() -> Try connecting to Send files to Server");
        }

        public void FtpAns(Packet Data)
        {
            switch (Data.Sigl)
            {
                case 1: // 송신쪽에서 파일전송을 수신쪽이 대답하는 
                    if (fs == null) // // isWorking이 false일 때만 동작
                    {
                        isWorking = true;

                        FileName = Data.FileName;
                        FileLength = Data.FileLength;

                        FTP_SendBuff = new byte[DataLength];

                        string FilePath = $"{Environment.CurrentDirectory}\\{FileName}";

                        fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write);

                        pf.printLog($"FtpAns() -> Authorize sender to transfer files");
                    }
                    else
                    {
                        // 안될 경우 또 신호 보내야함
                        Data.Func = 8;

                        isWorking = false;

                        pf.printLog($"FtpAns() -> Already receiving file [fs] = {fs.Name}");
                    }
                    break;

                case 2:
                    // Sender와 자신의 아이피 체크도 해야함
                    if (Data.FileLength == FileLength && Data.FileName == FileName)
                    {
                        pf.printLog($"FtpAns() -> Start Sending Files to {Data.Sender}");

                        Num_Bytes_To_Read = FileLength;

                        isWorking = true;

                        TEMP_SIGNAL = 0;
                        Data.Sigl = 0;

                        FTP_SendBuff = pf.StructureToByte(FileSend(Data));
                        client.Sct.Send(FTP_SendBuff, FTP_SendBuff.Length, SocketFlags.None);
                    }
                    break;
            }
        }

        public Packet FileSend(Packet Data)
        {
            if (Data.Sigl == TEMP_SIGNAL && Num_Bytes_To_Read > 0)
            {
                ReadBuff_Length = fs.Read(FTP_SendBuff, 0, DataLength);

                Data.FileData = (byte[])FTP_SendBuff.Clone();

                Data.Sigl = ReadBuff_Length;
                TEMP_SIGNAL += ReadBuff_Length;


                Buff_Length_Total += ReadBuff_Length;
                Num_Bytes_To_Read -= ReadBuff_Length;

                pf.printLog($"FileSend() -> Sent {Buff_Length_Total} of {FileLength}");

                Data.Func = 6;
            }
            else
            {
                Data.Func = 8;
                Data.Sigl = -1;

                if (Buff_Length_Total == fs.Length)
                {
                    pf.printLog($"FileSend() -> All file data transferred successfully !!");
                }
                FileClose();
            }

            Data.Sender = client.MyIP;
            Data.Receiver = "Server";

            return Data;
        }

        public Packet FileRecv(Packet Data)
        {
            if (FileName == Data.FileName)
            {
                Buff_Length_Total += Data.Sigl;

                fs.Write(Data.FileData, 0, Data.Sigl);

                Data.Sigl = Buff_Length_Total;

                Data.Func = 7;

                pf.printLog($"FileRecv() -> Received {Buff_Length_Total} of {Data.FileLength}");
            }
            else
            {

                pf.printLog($"FileRecv() -> Invalid Packet Arrival");
                pf.printLog($"FileRecv() -> {Data.Show_All_Data()}");

                if(Buff_Length_Total == Data.FileLength)
                {
                    pf.printLog($"FileRecv() -> All file data received successfully !!");
                }

                Data.Func = 8;
                FileClose();
            }

            Data.Sender = client.MyIP;
            Data.Receiver = "Server";

            return Data;
        }

        public void FileClose()
        {
            FileName = string.Empty;

            Num_Bytes_To_Read = 0;
            Buff_Length_Total = 0;

            ReadBuff_Length = 0;

            FTP_SendBuff = null;
            isWorking = false;

            TEMP_SIGNAL = -1;
            FileLength = 0;

            if (fs != null)
            {
                fs.Close();
                fs = null;
            }

            pf.printLog("FileClose() -> Close File");

            MainWindow.btn_Static_Send_File.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                MainWindow.btn_Static_Send_File.IsEnabled = true;
            }));
        }
    }
}
