using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Management;

using System.IO.Ports;
using System.Threading;
using Hyperwsn.Comm;


namespace Hyperwsn.SerialPortLibrary
{
    public class SerialPortHelper
    {
        private SerialPort port;

        bool isTimeout = false;                     // 读写的超时控制标志位
        System.Timers.Timer timer;                  // 读写超时控制的计时器

        bool AfterReceivedTimeout = false;          // 计时器超时标志位
        System.Timers.Timer AfterReceivedTimer;     // 收到数据后，开始计时，若是一定时间内没有再收到新的数据，则退出接收；

        bool BeforeReceivedTimeout = false;          // 计时器超时标志位
        System.Timers.Timer BeforeReceivedTimer;     // 在进入接收后开始计时，若一段时间内没有收到任何内容，则超时退出；

        int isGetResult = 0;                        // 是否获得反馈信息的标志位

        string RxExpStr;                            // 接收过程中，希望收到的字符串
        bool ReceivedExpStr = false;                // 是否已经收到了期望的字符串了

        string RxByteStr;                           // 从串口接收到的数据字节数组的字符串形式，可累计
        byte[] receivedBytes;                       // 从串口接收到的数据字节数组

        private int _BaudRate = 115200;             // 通信速率初始值
        private int _ReadBufferSize = 16384;        // 写缓冲器大小
        private int _WriteBufferSize = 16384;       // 读缓冲器大小

        public bool IsLogger { get; set; }

        public string Name { get; set; }
        /// <summary>
        /// 通信速率属性，默认115200
        /// </summary>
        public int BaudRate
        {
            get
            {
                return _BaudRate;
            }

            set
            {
                _BaudRate = value;
            }
        }

        /// <summary>
        /// 读缓冲器大小，默认8192
        /// </summary>
        public int ReadBufferSize
        {
            get
            {
                return _ReadBufferSize;
            }

            set
            {
                _ReadBufferSize = value;
            }
        }

        /// <summary>
        /// 些缓冲器大小，默认8192
        /// </summary>

        public int WriteBufferSize
        {
            get
            {
                return _WriteBufferSize;
            }

            set
            {
                _WriteBufferSize = value;
            }
        }

        public static String[] GetSerialPorts()
        {
            String[] ss = MulGetHardwareInfo(HardwareEnum.Win32_SerialPort, "Name");    //调用方式通过WMI获取COM端口 
            return ss;
        }



        /// <summary>
        /// 初始化SerialPort对象方法.PortName为COM口名称,例如"COM1","COM2"等,注意是string类型
        /// </summary>
        /// <param name="PortName"></param>
        public void InitCOM(string PortName)
        {
            //释放以前的端口
            if (port != null)
            {
                port.Dispose();
                Thread.Sleep(50);

            }

            //port Name 有2中可能  COMX  和  Silicon Labs CP210x USB to UART Bridge (COM11)
            if (PortName.Substring(0, 3) != "COM")
            {
                //获得新的名称
                PortName = GetSerialPortName(PortName);
            }

            port = new SerialPort(PortName);
            port.BaudRate = BaudRate;
            port.Parity = Parity.None;//无奇偶校验位
            port.StopBits = StopBits.One;//两个停止位
            //port1.Handshake = Handshake.RequestToSend;//控制协议
            port.ReadBufferSize = ReadBufferSize;
            port.WriteBufferSize = WriteBufferSize;

            port.DataReceived += Port_DataReceived; ; //Port1_DataReceived;//DataReceived事件委托
        }

        /// <summary>
        /// 打开指定的端口
        /// </summary>
        /// <returns></returns>
        public bool OpenPort()
        {
            if (port == null)
            {
                return false;
            }

            if (port.IsOpen)
            {
                return true;
            }

            try
            {
                port.Open();
            }
            catch 
            {
                return false;
            }

            if (port.IsOpen)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public void Close()
        {
            if (port == null)
            {
                return;
            }

            if (port.IsOpen)
            {
                port.Close();
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="tip"></param>
        /// <param name="Buf"></param>
        /// <param name="StartIndex"></param>
        /// <param name="Len"></param>
        private void AddLog(string tip, byte[] Buf, int StartIndex, int Len)
        {
            Logger.AddLogAutoTime(tip + ":\t\t" + CommArithmetic.ByteArrayToHexString(Buf, (UInt16)StartIndex, (UInt16)Len));
        }

        private void AddLog(string tip, byte[] Buf)
        {
            if (IsLogger == true)
            {
                if (Buf == null)
                {
                    AddLog(tip, null, 0, 0);
                }
                else
                {
                    AddLog(tip, Buf, 0, Buf.Length);
                }
            }
        }

        /// <summary>
        /// 在规定超时时间内，获得反馈，否则返回null
        /// </summary>
        /// <param name="request"></param>
        /// <param name="Timeout"></param>
        /// <returns></returns>
        public byte[] Send(byte[] request, int Timeout)
        {
            isTimeout = false;
            timer = new System.Timers.Timer();
            timer.Interval = Timeout;            
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;

            AfterReceivedTimeout = false;
            AfterReceivedTimer = null;

            RxExpStr = null;
            ReceivedExpStr = false;
            RxByteStr = null;
            receivedBytes = null;
            isGetResult = 0;

            if (request != null)
            {
                port.Write(request, 0, request.Length); //发送数据

                AddLog("TX", request);
            }            

            while (isTimeout == false && isGetResult == 0)
            {
                System.Threading.Thread.Sleep(25);
            }

            isTimeout = false;
            timer.Enabled = false;
            timer.Stop();
            timer.Dispose();

            AddLog("RX", receivedBytes);

            return receivedBytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TxBuf"></param>
        /// <param name="IndexOfStart"></param>
        /// <param name="TxLen"></param>
        /// <param name="TimeoutMs">超时时间，单位：毫秒；</param>
        /// <param name="WrLog">true = 记录日志</param>
        /// <returns></returns>
        public byte[] Send(byte[] TxBuf, UInt16 IndexOfStart, UInt16 TxLen, UInt16 TimeoutMs)
        {
            isTimeout = false;
            timer = new System.Timers.Timer();
            timer.Interval = TimeoutMs;            
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;

            AfterReceivedTimeout = false;
            AfterReceivedTimer = null;

            RxExpStr = null;
            ReceivedExpStr = false;
            RxByteStr = null;
            receivedBytes = null;
            isGetResult = 0;

            if (TxBuf != null)
            {
                port.Write(TxBuf, IndexOfStart, TxLen); //发送数据

                AddLog("TX", TxBuf, IndexOfStart, TxLen);
            }

            while (isTimeout == false && isGetResult == 0)
            {
                System.Threading.Thread.Sleep(25);
            }

            isTimeout = false;
            timer.Enabled = false;
            timer.Stop();
            timer.Dispose();

            AddLog("RX", receivedBytes);

            return receivedBytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TxBuf"></param>
        /// <param name="IndexOfStart"></param>
        /// <param name="TxLen"></param>
        /// <param name="TimeoutMs"></param>
        /// <param name="WrLog"></param>
        /// <param name="ExpStr">若是存在字符串ExpStr，则退出接收</param>
        /// <returns></returns>
        public byte[] Send(byte[] TxBuf, UInt16 IndexOfStart, UInt16 TxLen, UInt16 TimeoutMs, string ExpStr)
        {
            isTimeout = false;
            timer = new System.Timers.Timer();
            timer.Interval = TimeoutMs;            
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;

            AfterReceivedTimeout = false;
            AfterReceivedTimer = null;

            RxExpStr = ExpStr;
            RxExpStr = RxExpStr.Replace(" ", "");
            ReceivedExpStr = false;
            RxByteStr = null;
            receivedBytes = null;
            isGetResult = 0;

            if (TxBuf != null)
            {
                port.Write(TxBuf, IndexOfStart, TxLen);             // 发送数据

                AddLog("TX", TxBuf, IndexOfStart, TxLen);
            }

            while (isTimeout == false && ReceivedExpStr == false)
            {
                System.Threading.Thread.Sleep(25);
            }

            isTimeout = false;
            timer.Enabled = false;
            timer.Stop();
            timer.Dispose();

            // 记录到日志
            AddLog("RX", receivedBytes);

            return receivedBytes;
        }

        /// <summary>
        /// 发送数据，并接收数据；
        /// </summary>
        /// <param name="TxBuf"></param>
        /// <param name="IndexOfStart"></param>
        /// <param name="TxLen"></param>
        /// <param name="RxTimeoutMs">整个接收过程的最大等待时间，单位：ms</param>
        /// <param name="WrLog"></param>
        /// <param name="AfterReceivedTimeoutMs">收到数据后，开始计时，若是一定时间内没有再收到新的数据，则退出接收；</param>
        /// <returns></returns>
        public byte[] Send(byte[] TxBuf, UInt16 IndexOfStart, UInt16 TxLen, UInt16 RxTimeoutMs, UInt16 AfterReceivedTimeoutMs)
        {
            isTimeout = false;
            timer = new System.Timers.Timer();
            timer.Interval = RxTimeoutMs;            
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;

            AfterReceivedTimeout = false;
            AfterReceivedTimer = new System.Timers.Timer();
            AfterReceivedTimer.Interval = AfterReceivedTimeoutMs;
            AfterReceivedTimer.Elapsed += AfterReceivedTimer_Elapsed;
            AfterReceivedTimer.Enabled = true;

            RxExpStr = null;
            ReceivedExpStr = false;
            RxByteStr = null;
            receivedBytes = null;
            isGetResult = 0;

            if (TxBuf != null)
            {
                port.Write(TxBuf, IndexOfStart, TxLen);             // 发送数据

                AddLog("TX", TxBuf, IndexOfStart, TxLen);
            }

            while (isTimeout == false && AfterReceivedTimeout == false)
            {
                System.Threading.Thread.Sleep(25);
            }

            isTimeout = false;
            timer.Enabled = false;
            timer.Stop();
            timer.Dispose();

            AfterReceivedTimeout = false;
            AfterReceivedTimer.Enabled = false;
            AfterReceivedTimer.Stop();
            AfterReceivedTimer.Dispose();

            // 记录到日志
            AddLog("RX", receivedBytes);

            return receivedBytes;
        }

        public byte[] Send(byte[] TxBuf, UInt16 IndexOfStart, UInt16 TxLen, UInt16 TotalRxTimeoutMs, UInt16 BeforeRxTimeoutMs,  UInt16 AfterReceivedTimeoutMs)
        {
            isTimeout = false;
            timer = new System.Timers.Timer();
            timer.Interval = TotalRxTimeoutMs;            
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;

            BeforeReceivedTimeout = false;
            BeforeReceivedTimer = new System.Timers.Timer();
            BeforeReceivedTimer.Interval = BeforeRxTimeoutMs;
            BeforeReceivedTimer.Elapsed += BeforeReceivedTimer_Elapsed;
            BeforeReceivedTimer.Enabled = true;

            AfterReceivedTimeout = false;
            AfterReceivedTimer = new System.Timers.Timer();
            AfterReceivedTimer.Interval = AfterReceivedTimeoutMs;
            AfterReceivedTimer.Elapsed += AfterReceivedTimer_Elapsed;
            AfterReceivedTimer.Enabled = true;

            RxExpStr = null;
            ReceivedExpStr = false;
            RxByteStr = null;
            receivedBytes = null;
            isGetResult = 0;

            if (TxBuf != null)
            {
                port.Write(TxBuf, IndexOfStart, TxLen);             // 发送数据

                AddLog("TX", TxBuf, IndexOfStart, TxLen);
            }

            while (isTimeout == false && AfterReceivedTimeout == false)
            {
                if (BeforeReceivedTimeout == true && receivedBytes == null)
                {
                    break;
                }

                System.Threading.Thread.Sleep(25);
            }

            isTimeout = false;
            timer.Enabled = false;
            timer.Stop();
            timer.Dispose();

            BeforeReceivedTimeout = false;
            BeforeReceivedTimer.Enabled = false;
            BeforeReceivedTimer.Stop();
            BeforeReceivedTimer.Dispose();

            AfterReceivedTimeout = false;
            AfterReceivedTimer.Enabled = false;
            AfterReceivedTimer.Stop();
            AfterReceivedTimer.Dispose();

            // 记录到日志
            AddLog("RX", receivedBytes);

            return receivedBytes;
        }

        /// <summary>
        /// 计时器超时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            isTimeout = true;
        }

        private void AfterReceivedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            AfterReceivedTimeout = true;
        }

        private void BeforeReceivedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BeforeReceivedTimeout = true;
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //分为不同的状态，包括
                //1：发送Request 后的Response 信息
                StringBuilder currentline = new StringBuilder();
                System.Threading.Thread.Sleep(70);                  // 尝试不要断开接收数据，在SurfaceBook 上，断开的时间大约为22ms

                //20180418 非常重要  isOpen的判断是临时的
                while (port.IsOpen && port.BytesToRead > 0)
                {
                    byte ch = (byte)port.ReadByte();
                    currentline.Append(ch.ToString("X2"));
                }

                //补丁，防止收到单个字符
                if (currentline.Length >= 2)
                {
                    RxByteStr += currentline.ToString();
                    receivedBytes = CommArithmetic.HexStringToByteArray(RxByteStr);
                    isGetResult++;

                    if (RxExpStr != null && RxExpStr != "")
                    {
                        if (RxByteStr.Contains(RxExpStr) == true)
                        {
                            ReceivedExpStr = true;
                        }
                    }

                    // 刷新计时器，重新计时
                    if (AfterReceivedTimer != null && AfterReceivedTimer.Enabled == true)
                    {
                        AfterReceivedTimer.Stop();
                        AfterReceivedTimer.Start();
                    }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 从完整名称中，截取COM名称
        /// </summary>
        /// <param name="DeviceName"></param>
        /// <returns></returns>
        public static string GetSerialPortName(string DeviceName)
        {
            int first = DeviceName.IndexOf("(COM");
            if (first == -1)
            {
                return null;
            }

            int last = DeviceName.IndexOf(')');
            if (last == -1)
            {
                return null;
            }

            if (first >= last)
            {
                return null;
            }

            return DeviceName.Substring(first + 1, last - first - 1);
        }

        private static string[] MulGetHardwareInfo(HardwareEnum hardType, string propKey)
        {
            List<string> strs = new List<string>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + hardType))
                {
                    var hardInfos = searcher.Get();
                    foreach (var hardInfo in hardInfos)
                    {
                        if (hardInfo.Properties[propKey].Value != null && hardInfo.Properties[propKey].Value.ToString().Contains("(COM"))
                        {
                            strs.Add(hardInfo.Properties[propKey].Value.ToString());
                        }
                    }
                    searcher.Dispose();
                }
                return strs.ToArray();
            }
            catch
            {
                return null;
            }

            finally
            {
                strs = null;
            }
        }
    }
}
