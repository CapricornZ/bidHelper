using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

namespace tobid.util {

    public struct SystemTime {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;

        /// <summary>  
        /// 从System.DateTime转换。  
        /// </summary>  
        /// <param name="time">System.DateTime类型的时间。</param>  
        public void FromDateTime(DateTime time) {
            wYear = (ushort)time.Year;
            wMonth = (ushort)time.Month;
            wDayOfWeek = (ushort)time.DayOfWeek;
            wDay = (ushort)time.Day;
            wHour = (ushort)time.Hour;
            wMinute = (ushort)time.Minute;
            wSecond = (ushort)time.Second;
            wMilliseconds = (ushort)time.Millisecond;
        }
        /// <summary>  
        /// 转换为System.DateTime类型。  
        /// </summary>  
        /// <returns></returns>  
        public DateTime ToDateTime() {
            return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds);
        }
        /// <summary>  
        /// 静态方法。转换为System.DateTime类型。  
        /// </summary>  
        /// <param name="time">SYSTEMTIME类型的时间。</param>  
        /// <returns></returns>  
        public static DateTime ToDateTime(SystemTime time) {
            return time.ToDateTime();
        }
    }

    public class SystemTimeUtil {

        [DllImport("Kernel32.dll")]
        public static extern bool SetLocalTime(ref SystemTime Time);
        [DllImport("Kernel32.dll")]
        public static extern void GetLocalTime(ref SystemTime Time);

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SystemTimeUtil));

        static public void addSecond(int second) {

            DateTime startDT = DateTime.Now;

            SystemTime st = new SystemTime();
            st.wYear = (ushort)startDT.Year;
            st.wMonth = (ushort)startDT.Month;
            st.wDay = (ushort)startDT.Day;
            st.wHour = (ushort)startDT.Hour;
            st.wMinute = (ushort)startDT.Minute;
            st.wSecond = (ushort)(startDT.Second + second);
            SystemTimeUtil.SetLocalTime(ref st);
        }

        static public void SetInternetTime() {

            // 记录开始的时间  
            DateTime startDT = DateTime.Now;

            //建立IPAddress对象与端口，创建IPEndPoint节点:  
            int port = 13;
            string[] whost = { "time-nw.nist.gov", "time-a.nist.gov", "time-b.nist.gov", "tick.mit.edu", "time.windows.com", "clock.sgi.com", "5time.nist.gov" };

            IPHostEntry iphostinfo;
            IPAddress ip;
            IPEndPoint ipe;
            Socket c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建Socket  

            c.ReceiveTimeout = 5 * 1000;//设置超时时间  

            string sEX = "";// 接受错误信息  

            // 遍历时间服务器列表  
            foreach (string strHost in whost) {
                try {
                    logger.Debug("Connecting to " + strHost);
                    iphostinfo = Dns.GetHostEntry(strHost);
                    ip = iphostinfo.AddressList[0];
                    ipe = new IPEndPoint(ip, port);

                    c.Connect(ipe);//连接到服务器  
                    if (c.Connected) break;// 如果连接到服务器就跳出  
                } catch (Exception ex) {
                    logger.Warn(ex.Message);
                    sEX = ex.Message;
                }
            }

            if (!c.Connected) {
                logger.Error("时间服务器连接失败！错误信息：" + sEX);
                return;
            }

            //SOCKET同步接受数据
            try {
                StringBuilder sb = new StringBuilder();
            
                byte[] RecvBuffer = new byte[1024];
                int nBytes, nTotalBytes = 0;
                System.Text.Encoding myE = Encoding.UTF8;

                while ((nBytes = c.Receive(RecvBuffer, 0, 1024, SocketFlags.None)) > 0) {
                    nTotalBytes += nBytes;
                    sb.Append(myE.GetString(RecvBuffer, 0, nBytes));
                }

                string[] o = sb.ToString().Split(' '); // 打断字符串

                TimeSpan k = new TimeSpan();
                k = (TimeSpan)(DateTime.Now - startDT);// 得到开始到现在所消耗的时间  

                DateTime SetDT = Convert.ToDateTime(o[1] + " " + o[2]).Subtract(-k);// 减去中途消耗的时间

                //处置北京时间 +8时
                SetDT = SetDT.AddHours(8);

                //转换System.DateTime到SystemTime
                SystemTime st = new SystemTime();
                st.FromDateTime(SetDT);
                logger.Info("SET TIME " + SetDT);

                //调用Win32 API设置系统时间  
                SystemTimeUtil.SetLocalTime(ref st);
                logger.Info("时间已同步");
            } catch (Exception ex) {
                logger.Warn(ex.Message);
            } finally {
                //关闭连接  
                c.Close();
            }
        }  
    }  
}
