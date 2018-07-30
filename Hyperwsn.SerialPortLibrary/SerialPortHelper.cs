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
        //TODO : 1 Name 控制，如何连续操作


        private SerialPort port;
        bool isTimeout = false; //读写的超时控制标志位
        System.Timers.Timer timer; // 读写超时控制的计时器
        int isGetResult = 0;   //是否获得反馈信息的标志位
        byte[] receivedBytes;  //从串口接收到的数据字节数组


        private int _BaudRate = 115200; //通信速率初始值
        private int _ReadBufferSize = 8192; //写缓冲器大小
        private int _WriteBufferSize = 8192; //读缓冲器大小

       
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

            String[] ss = MulGetHardwareInfo(HardwareEnum.Win32_SerialPort, "Name");//调用方式通过WMI获取COM端口 
            return ss;
        }



        /// <summary>
        /// 初始化SerialPort对象方法.PortName为COM口名称,例如"COM1","COM2"等,注意是string类型
        /// </summary>
        /// <param name="PortName"></param>
        public void InitCOM(string PortName)
        {
            //释放以前的端口
            if (port!=null)
            {
                port.Dispose();
                Thread.Sleep(50);
                
            }
            //port Name 有2中可能  COMX  和  Silicon Labs CP210x USB to UART Bridge (COM11)
            if (PortName.Substring(0,3) !="COM")
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
            //port1.ReceivedBytesThreshold = 4;//设置 DataReceived 事件发生前内部输入缓冲区中的字节数
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
                throw new ApplicationException("Port not initial.");
            }

            if (port.IsOpen)
            {
                return true;
            }

            try
            {

                port.Open(); //打开串口
                

                return true;
            }
            catch (Exception ex)
            {

                throw ex ;
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
            timer.Enabled = true;
            timer.Elapsed += Timer_Elapsed;
            receivedBytes = null;  //测试用

            port.Write(request, 0, request.Length); //发送数据


            while (!isTimeout)
            {
                //TODO 这里有可能>1 , 存在风险
                if (isGetResult == 1)
                {
                    isTimeout = true;
                    timer.Enabled = false;
                    timer.Stop();
                    timer.Dispose();
                    //return commandResult;
                    //返回收到的字节数组
                    //isGetResult = 0;
                    /*
                    if(initStatus ==1)
                    {
                        //port.Close();
                        initStatus = 0;

                    }
                    */
                    isGetResult = 0;
                    isTimeout = false;
                    timer.Enabled = false;
                    timer.Stop();
                    timer.Dispose();

                    return receivedBytes;
                }

                System.Threading.Thread.Sleep(25);


            }
            isGetResult = 0;
            isTimeout = false;
            timer.Enabled = false;
            timer.Stop();
            timer.Dispose();

            return null;

        }
        /// <summary>
        /// 触发超时反馈
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            isTimeout = true;
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //分为不同的状态，包括
            //1：发送Request 后的Response 信息
            StringBuilder currentline = new StringBuilder();
            System.Threading.Thread.Sleep(70); //尝试不要断开接收数据，在SurfaceBook 上，断开的时间大约为22ms

            //20180418 非常重要  isOpen的判断是临时的
            while (port.IsOpen && port.BytesToRead > 0)
            {
                byte ch = (byte)port.ReadByte();
                currentline.Append(ch.ToString("X2"));
            }

            //补丁，防止收到单个字符
            if (currentline.Length >= 2)
            {
                receivedBytes = CommArithmetic.HexStringToByteArray(currentline.ToString());
                isGetResult++;
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
            { strs = null; }
        }


    }
}
