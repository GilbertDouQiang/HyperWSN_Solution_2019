using Hyperwsn.Comm;
using Hyperwsn.Protocol;
using Hyperwsn.SerialPortLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using System.Collections.ObjectModel;
using System.ComponentModel;
using BootLoaderLibrary;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Microsoft.Win32;
using System.Media;

namespace DeviceConfigTools2018
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate Int16 TxAndRxHandler(object sender, RoutedEventArgs e, SerialPortHelper helper);
        private delegate Int16 TxAndRxHandler_inThread(SerialPortHelper helper);                            // 适合在子线程中调用

        string ComText = "";                    // 串行设备的COM端口名称

        Gateway aGateway;
        byte[] RxBuf = null;                   // 用来存储串口发来的数据

        InternalSensor aIntervalSensor;

        // USB导出数据
        DateTime StartTimeOfExport;             // 开始时间
        DateTime EndTimeOfExport;               // 结束时间
        UInt32 ExpTotalOfExport = 0;            // 查询到设备里有多少有效数据/用户计划读取多少条有效数据
        ObservableCollection<M1> DataOfExport = new ObservableCollection<M1>();         // 用来存储导出的数据
        int GridLineOfExport = 1;                                                       // M1表格的行编号  
        UInt32 ReadBase = 0;                    // 希望读取的数据的起始位置       
        UInt32 ReadTotal = 0;                   // 希望读取的数据的总数量
        UInt32 ReadCnt = 0;                     // 读取数据的累计单元
        UInt32 GroupCnt = 0;                    // 读取时的组序号，从0开始计数
        UInt32 IndexOfStart = 0;                // 这次读取时的起始位置
        UInt32 UnitOfRead = 100;                // 这次读取时的读取数量；理论上，一次读取的最大数量最好不要大于200；
        UInt16 ExportTimoutMs = 2000;           // 一次导出操作的最大超时时间，单位：ms；
        bool ForceExist = false;                // 是否需要强制退出导出过程
        DateTime Start = System.DateTime.Now;   // 记录导出数据的开始时间

        // 绑定命令
        byte BindCmd = 0;

        SoundPlayer okPlayer;
        SoundPlayer ngPlayer;

        bool ShowErMsg = true;                 // 是否显示错误弹窗

        public MainWindow()
        {
            InitializeComponent();

            DataGridOfExport.ItemsSource = DataOfExport;

            //M1 排序用
            ICollectionView v = CollectionViewSource.GetDefaultView(DataGridOfExport.ItemsSource);
            v.SortDescriptions.Clear();
            ListSortDirection d = ListSortDirection.Descending;
            v.SortDescriptions.Add(new SortDescription("DisplayID", d));
            v.Refresh();

            // 从配置文件里读取
            UnitOfRead = Convert.ToUInt32(ConfigurationManager.AppSettings["ExportUnit"]);
            ExportTimoutMs = Convert.ToUInt16(ConfigurationManager.AppSettings["ExportTimeoutMs"]);

            tbkResultOfBind.Foreground = new SolidColorBrush(Colors.ForestGreen);
            tbkResultOfBind.FontWeight = FontWeights.Bold;

            // 从配置文件里读取
            bool Disable = Convert.ToBoolean(ConfigurationManager.AppSettings["DisableConfig"]);
            if(Disable == true)
            {
                RadTabItemOfCfg.Visibility = Visibility.Hidden;
            }

            Disable = Convert.ToBoolean(ConfigurationManager.AppSettings["DisableInternalSensor"]);
            if (Disable == true)
            {
                RadTabItemOfInternalSensor.Visibility = Visibility.Hidden;
            }

            Disable = Convert.ToBoolean(ConfigurationManager.AppSettings["DisableCC1101"]);
            if (Disable == true)
            {
                RadTabItemOfSG6XCC1101.Visibility = Visibility.Hidden;
            }

            Disable = Convert.ToBoolean(ConfigurationManager.AppSettings["DisableLayout"]);
            if (Disable == true)
            {
                RadTabItemOfLayout.Visibility = Visibility.Hidden;
            }

            Disable = Convert.ToBoolean(ConfigurationManager.AppSettings["DisableExport"]);
            if (Disable == true)
            {
                RadTabItemOfExport.Visibility = Visibility.Hidden;
            }

            Disable = Convert.ToBoolean(ConfigurationManager.AppSettings["DisableBind"]);
            if (Disable == true)
            {
                RadTabItemOfBind.Visibility = Visibility.Hidden;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Config基础信息
            // 1. 读取版本号和标题； 2. 写入计算机名称； 3. 确定授权级别；
            this.Title = ConfigurationManager.AppSettings["Title"] + " v" +
              System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            //根据Key读取元素的Value
            UserInfo user = new UserInfo();
            string userAuth = user.GetUserInfo();

            //写入元素的Value
            config.AppSettings.Settings["LiceseName"].Value = userAuth;

            string liceseKey = Base64.base64encode(userAuth);
            string liceseKeyApp = ConfigurationManager.AppSettings["LiceseKey"];

            if (liceseKeyApp.Length > 10 && liceseKey == liceseKeyApp)
            {
                btnUpdateFactory.Visibility = Visibility.Visible;
            }
            else
            {
                btnUpdateFactory.Visibility = Visibility.Hidden;
            }

            // 一定要记得保存，写不带参数的config.Save()也可以
            config.Save(ConfigurationSaveMode.Modified);

            string SoundPath = Directory.GetCurrentDirectory() + "\\Sound";

            okPlayer = new SoundPlayer();
            okPlayer.SoundLocation = SoundPath + "\\1.Normal.WAV";
            okPlayer.Load(); 

            ngPlayer = new SoundPlayer();
            ngPlayer.SoundLocation = SoundPath + "\\2.Error.WAV";
            ngPlayer.Load(); 

            UpdateDeviceList();
        }

        /// <summary>
        /// 打开串口，发送命令，接收反馈，关闭串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="TxRxEvent"></param>
        private void OpenTxRxClose(object sender, RoutedEventArgs e, TxAndRxHandler aTxAndRxHandler)
        {
            // 判断是否有串口设备
            if (cbDeviceList.SelectedIndex < 0)
            {
                return;
            }

            SerialPortHelper helper = new SerialPortHelper();
            helper.IsLogger = true;

            try
            {
                // 打开串口
                helper.InitCOM(cbDeviceList.Text);
                helper.OpenPort();
            }
            catch (Exception ex)
            {
                if (ShowErMsg == true)
                {
                    MessageBox.Show(" Open 过程异常：" + ex.Message);
                }                
            }

            try
            {
                // 发送命令，接收反馈
                if (aTxAndRxHandler != null)
                {
                    aTxAndRxHandler(sender, e, helper);
                }
            }
            catch (Exception ex)
            {
                if (ShowErMsg == true)
                {
                    MessageBox.Show(" TX | RX 过程异常：" + ex.Message);
                }                
            }

            try
            {
                // 延时，然后关闭串口
                System.Threading.Thread.Sleep(100);
                helper.Close();
            }
            catch (Exception ex)
            {
                if (ShowErMsg == true)
                {
                    MessageBox.Show(" Close 过程异常：" + ex.Message);
                }               
            }
        }

        /// <summary>
        /// 适合在子线程中调用：打开串口，发送命令，接收反馈，关闭串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="TxRxEvent"></param>
        private void OpenTxRxClose_inThread(string ComText, TxAndRxHandler_inThread aTxAndRxHandler)
        {
            try
            {
                // 打开串口
                SerialPortHelper helper = new SerialPortHelper();
                helper.IsLogger = true;
                helper.InitCOM(ComText);
                helper.OpenPort();

                // 发送命令，接收反馈
                if (aTxAndRxHandler != null)
                {
                    aTxAndRxHandler(helper);
                }

                // 延时，然后关闭串口
                System.Threading.Thread.Sleep(100);
                helper.Close();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("正在中止线程") == false)
                {
                    if (ShowErMsg == true)
                    {
                        MessageBox.Show("Open | TX | RX | Close 过程异常：" + ex.Message);
                    }                    
                }
            }
        }

        private void UpdateDeviceList()
        {
            string[] strDeviceList = SerialPortHelper.GetSerialPorts();
            cbDeviceList.Items.Clear();

            // 先添加CP2102

            foreach (string item in strDeviceList)
            {
                if(item.Contains("Silicon Labs CP210x USB to UART Bridge") == true)
                {
                    cbDeviceList.Items.Add(item);
                }                
            }

            // 再添加其他
            foreach (string item in strDeviceList)
            {
                if (item.Contains("Silicon Labs CP210x USB to UART Bridge") == false)
                {
                    cbDeviceList.Items.Add(item);
                }
            }

            if (strDeviceList != null && strDeviceList.Length > 0)
            {
                cbDeviceList.SelectedIndex = 0;
            }
        }


        private void btnRefersh_Click(object sender, RoutedEventArgs e)
        {
            UpdateDeviceList();
        }

        /// <summary>
        /// 读取网关的基础配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ReadBase(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[10];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x01;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 2000, "CB CB");

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {                
                if (ShowErMsg == true)
                {
                    MessageBox.Show("读取网关的基础配置失败:" + error.ToString("G"));
                }
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// 读取网关的详细配置信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ReadCfg(object sender, RoutedEventArgs e, SerialPortHelper helper, byte Protocol)
        {
            byte[] TxBuf = new byte[10];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x64;

            // 协议版本
            TxBuf[TxLen++] = Protocol;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 2000, "CB CB");

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                if (ShowErMsg == true)
                {
                    MessageBox.Show("读取网关的详细配置失败:" + error.ToString("G"));
                }                
                return -1;
            }

            return 0;
        }

        private Int16 TxAndRx_Connect(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            aGateway = null;

            Int16 error = TxAndRx_ReadBase(sender, e, helper);
            if (error < 0)
            {
                return -1;
            }

            System.Threading.Thread.Sleep(200);

            if (aGateway == null)
            {
                error = TxAndRx_ReadCfg(sender, e, helper, 2);
            }
            else
            {
                if ((aGateway.DeviceTypeV == (byte)Device.DeviceType.SG5 && aGateway.SwRevisionV <= 0xB827 && (0 != (aGateway.SwRevisionV & 0xFF00))) || (aGateway.DeviceTypeV == (byte)Device.DeviceType.SG6 && aGateway.SwRevisionV <= 0xBD0A && (0 != (aGateway.SwRevisionV & 0xFF00))))
                {   // 也就是说，在2018年11月07日之前的网关，都只支持V2的查询指令            
                    error = TxAndRx_ReadCfg(sender, e, helper, 1);
                }
                else
                {   // 也就是说，在2018年11月07日之后的网关，都支持V2的查询指令
                    error = TxAndRx_ReadCfg(sender, e, helper, 2);
                }
            }           

            if (error < 0)
            {
                return -2;
            }

            GatewayDetail.DataContext = aGateway;

            if (aGateway.DeviceTypeV == (byte)Device.DeviceType.SG6E || aGateway.DeviceTypeV == (byte)Device.DeviceType.M44)
            {
                RadTabItemOfEthernet.Visibility = Visibility.Visible;
            }
            else
            {
                RadTabItemOfEthernet.Visibility = Visibility.Hidden;
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnectDevice_Click(object sender, RoutedEventArgs e)
        {
            aRadTabControl.SelectedIndex = 0;
            OpenTxRxClose(sender, e, TxAndRx_Connect);
        }

        /// <summary>
        /// 修改应用配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 _TxAndRx_SetAppCfg(object sender, RoutedEventArgs e, SerialPortHelper helper, byte Protocol)
        {
            byte[] TxBuf = new byte[240];
            UInt16 TxLen = 0;

            byte[] ByteBuf = null;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x65;

            // 协议版本
            TxBuf[TxLen++] = Protocol;

            // 客户码
            ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewCustomer.Text);
            if (ByteBuf == null || ByteBuf.Length < 2)
            {                
                if (ShowErMsg == true)
                {                    
                    if (ShowErMsg == true)
                    {
                        MessageBox.Show("新客户码错误!");
                    }
                }
                return -1;
            }
            TxBuf[TxLen++] = ByteBuf[0];
            TxBuf[TxLen++] = ByteBuf[1];

            // Debug
            ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewDebug.Text);
            if (ByteBuf == null || ByteBuf.Length < 2)
            {            
                if (ShowErMsg == true)
                {
                    MessageBox.Show("新Debug错误!");
                }
                return -2;
            }
            TxBuf[TxLen++] = ByteBuf[0];
            TxBuf[TxLen++] = ByteBuf[1];

            // Category
            TxBuf[TxLen++] = Convert.ToByte(tbxNewCategory.Text);

            if (Protocol == 1)
            {
                // 网关状态数据包的采集间隔
                UInt16 Interval = Convert.ToUInt16(tbxNewInterval.Text);
                TxBuf[TxLen++] = (byte)((Interval & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((Interval & 0x00FF) >> 0);

                // 当前时间
                ByteBuf = CommArithmetic.EncodeDateTime(System.DateTime.Now);
                TxBuf[TxLen++] = ByteBuf[0];
                TxBuf[TxLen++] = ByteBuf[1];
                TxBuf[TxLen++] = ByteBuf[2];
                TxBuf[TxLen++] = ByteBuf[3];
                TxBuf[TxLen++] = ByteBuf[4];
                TxBuf[TxLen++] = ByteBuf[5];

                // Pattern
                TxBuf[TxLen++] = Convert.ToByte(tbxNewPattern.Text);

                // bps
                TxBuf[TxLen++] = Convert.ToByte(tbxNewBps.Text);

                // channel
                TxBuf[TxLen++] = Convert.ToByte(tbxNewChannel.Text);

                // Carousel
                UInt16 Carousel = Convert.ToUInt16(tbxNewCarousel.Text);
                TxBuf[TxLen++] = (byte)((Carousel & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((Carousel & 0x00FF) >> 0);

                // IntervalOfAlert
                UInt16 IntervalOfAlert = Convert.ToUInt16(tbxNewIntervalOfAlert.Text);
                TxBuf[TxLen++] = (byte)((IntervalOfAlert & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((IntervalOfAlert & 0x00FF) >> 0);

                // 转发策略
                TxBuf[TxLen++] = Convert.ToByte(tbxNewTransPolicy.Text);

                // 时间来源
                TxBuf[TxLen++] = Convert.ToByte(tbxNewTimeSrc.Text);

                // AlertWay
                TxBuf[TxLen++] = Convert.ToByte(tbxNewAlertWay.Text);

                // 判据来源
                TxBuf[TxLen++] = Convert.ToByte(tbxNewCriSrc.Text);

                // 屏限
                TxBuf[TxLen++] = Convert.ToByte(tbxNewDisplayStyle.Text);

                // 亮屏策略
                TxBuf[TxLen++] = Convert.ToByte(tbxNewBrightMode.Text);

                // 启用
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewUse.Text);
                if (ByteBuf == null || ByteBuf.Length < 1)
                {
                    MessageBox.Show("新启用错误!");
                    return -4;
                }
                TxBuf[TxLen++] = ByteBuf[0];

                // TxPower
                Int16 TxPower = Convert.ToInt16(tbxNewTxPower.Text);
                TxBuf[TxLen++] = (byte)((TxPower & 0x00FF) >> 0);

                // 配置保留位                
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewReserved.Text);
                if (ByteBuf == null || ByteBuf.Length < 3)
                {
                    MessageBox.Show("新配置保留位错误!");
                    return -5;
                }
                TxBuf[TxLen++] = ByteBuf[0];
                TxBuf[TxLen++] = ByteBuf[1];
                TxBuf[TxLen++] = ByteBuf[2];

                // 主服务器

                // 域名
                TxBuf[TxLen++] = (byte)tbxNewServerDomainM.Text.Length;

                if (tbxNewServerDomainM.Text == "")
                {
                    MessageBox.Show("新的主服务器域名错误!");
                    return -6;
                }

                try
                {
                    ByteBuf = Encoding.UTF8.GetBytes(tbxNewServerDomainM.Text);
                }
                catch
                {
                    MessageBox.Show("新的主服务器域名错误!");
                    return -7;
                }

                if (ByteBuf != null && ByteBuf.Length > 0)
                {
                    if (ByteBuf.Length > 63)
                    {
                        MessageBox.Show("新的主服务器域名长度超出了限制!");
                        return -8;
                    }

                    for (UInt16 iX = 0; iX < (byte)ByteBuf.Length; iX++)
                    {
                        TxBuf[TxLen++] = ByteBuf[iX];
                    }
                }

                // 端口
                UInt16 ServerPort = Convert.ToUInt16(tbxNewServerPortM.Text);
                TxBuf[TxLen++] = (byte)((ServerPort & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((ServerPort & 0x00FF) >> 0);

                // 副服务器
                // 域名
                TxBuf[TxLen++] = (byte)tbxNewServerDomainS.Text.Length;

                if (tbxNewServerDomainS.Text != "")
                {
                    try
                    {
                        ByteBuf = Encoding.UTF8.GetBytes(tbxNewServerDomainS.Text);
                    }
                    catch
                    {
                        MessageBox.Show("第二服务器域名错误!");
                        return -9;
                    }

                    if (ByteBuf != null && ByteBuf.Length > 0)
                    {
                        if (ByteBuf.Length > 63)
                        {
                            MessageBox.Show("第二服务器域名长度超出了限制!");
                            return -10;
                        }

                        for (UInt16 iX = 0; iX < (byte)ByteBuf.Length; iX++)
                        {
                            TxBuf[TxLen++] = ByteBuf[iX];
                        }
                    }
                }

                // 端口
                if (tbxNewServerPortS.Text == "")
                {
                    ServerPort = 0;
                }
                else
                {
                    ServerPort = Convert.ToUInt16(tbxNewServerPortS.Text);
                }
                TxBuf[TxLen++] = (byte)((ServerPort & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((ServerPort & 0x00FF) >> 0);
            }
            else
            {   // Protocol == 2
                // 网关状态数据包的采集间隔
                UInt16 Interval = Convert.ToUInt16(tbxNewInterval.Text);
                TxBuf[TxLen++] = (byte)((Interval & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((Interval & 0x00FF) >> 0);

                // 定位间隔
                UInt16 IntervalOfLocate = Convert.ToUInt16(tbxNewIntervalOfLocate.Text);
                TxBuf[TxLen++] = (byte)((IntervalOfLocate & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((IntervalOfLocate & 0x00FF) >> 0);

                // 当前时间
                ByteBuf = CommArithmetic.EncodeDateTime(System.DateTime.Now);
                TxBuf[TxLen++] = ByteBuf[0];
                TxBuf[TxLen++] = ByteBuf[1];
                TxBuf[TxLen++] = ByteBuf[2];
                TxBuf[TxLen++] = ByteBuf[3];
                TxBuf[TxLen++] = ByteBuf[4];
                TxBuf[TxLen++] = ByteBuf[5];

                // Pattern
                TxBuf[TxLen++] = Convert.ToByte(tbxNewPattern.Text);

                // bps
                TxBuf[TxLen++] = Convert.ToByte(tbxNewBps.Text);

                // channel
                TxBuf[TxLen++] = Convert.ToByte(tbxNewChannel.Text);

                // TxPower
                Int16 TxPower = Convert.ToInt16(tbxNewTxPower.Text);
                TxBuf[TxLen++] = (byte)((TxPower & 0x00FF) >> 0);

                // Carousel
                UInt16 Carousel = Convert.ToUInt16(tbxNewCarousel.Text);
                TxBuf[TxLen++] = (byte)((Carousel & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((Carousel & 0x00FF) >> 0);

                // IntervalOfAlert
                UInt16 IntervalOfAlert = Convert.ToUInt16(tbxNewIntervalOfAlert.Text);
                TxBuf[TxLen++] = (byte)((IntervalOfAlert & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((IntervalOfAlert & 0x00FF) >> 0);

                // 转发策略
                TxBuf[TxLen++] = Convert.ToByte(tbxNewTransPolicy.Text);

                // 时间来源
                TxBuf[TxLen++] = Convert.ToByte(tbxNewTimeSrc.Text);

                // AlertWay
                TxBuf[TxLen++] = Convert.ToByte(tbxNewAlertWay.Text);

                // 判据来源
                TxBuf[TxLen++] = Convert.ToByte(tbxNewCriSrc.Text);

                // 屏限
                TxBuf[TxLen++] = Convert.ToByte(tbxNewDisplayStyle.Text);

                // 亮屏策略
                TxBuf[TxLen++] = Convert.ToByte(tbxNewBrightMode.Text);

                // 启用
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewUse.Text);
                if (ByteBuf == null || ByteBuf.Length < 1)
                {
                    MessageBox.Show("新启用错误!");
                    return -4;
                }
                TxBuf[TxLen++] = ByteBuf[0];

                // 配置保留位                
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewReserved.Text);
                if (ByteBuf == null || ByteBuf.Length < 3)
                {
                    MessageBox.Show("新配置保留位错误!");
                    return -5;
                }
                TxBuf[TxLen++] = 0x00;          // 该位未用到，但不可缺少。
                TxBuf[TxLen++] = ByteBuf[0];
                TxBuf[TxLen++] = ByteBuf[1];
                TxBuf[TxLen++] = ByteBuf[2];

                // 主服务器

                // 域名
                TxBuf[TxLen++] = (byte)tbxNewServerDomainM.Text.Length;

                if (tbxNewServerDomainM.Text == "")
                {
                    MessageBox.Show("新的主服务器域名错误!");
                    return -6;
                }

                try
                {
                    ByteBuf = Encoding.UTF8.GetBytes(tbxNewServerDomainM.Text);
                }
                catch
                {
                    MessageBox.Show("新的主服务器域名错误!");
                    return -7;
                }

                if (ByteBuf != null && ByteBuf.Length > 0)
                {
                    if (ByteBuf.Length > 63)
                    {
                        MessageBox.Show("新的主服务器域名长度超出了限制!");
                        return -8;
                    }

                    for (UInt16 iX = 0; iX < (byte)ByteBuf.Length; iX++)
                    {
                        TxBuf[TxLen++] = ByteBuf[iX];
                    }
                }

                // 端口
                UInt16 ServerPort = Convert.ToUInt16(tbxNewServerPortM.Text);
                TxBuf[TxLen++] = (byte)((ServerPort & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((ServerPort & 0x00FF) >> 0);

                // 副服务器
                // 域名
                TxBuf[TxLen++] = (byte)tbxNewServerDomainS.Text.Length;

                if (tbxNewServerDomainS.Text != "")
                {
                    try
                    {
                        ByteBuf = Encoding.UTF8.GetBytes(tbxNewServerDomainS.Text);
                    }
                    catch
                    {
                        MessageBox.Show("第二服务器域名错误!");
                        return -9;
                    }

                    if (ByteBuf != null && ByteBuf.Length > 0)
                    {
                        if (ByteBuf.Length > 63)
                        {
                            MessageBox.Show("第二服务器域名长度超出了限制!");
                            return -10;
                        }

                        for (UInt16 iX = 0; iX < (byte)ByteBuf.Length; iX++)
                        {
                            TxBuf[TxLen++] = ByteBuf[iX];
                        }
                    }
                }

                // 端口
                if (tbxNewServerPortS.Text == "")
                {
                    ServerPort = 0;
                }
                else
                {
                    ServerPort = Convert.ToUInt16(tbxNewServerPortS.Text);
                }

                TxBuf[TxLen++] = (byte)((ServerPort & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((ServerPort & 0x00FF) >> 0);
            }

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 2000, "CB CB");

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {                
                if (ShowErMsg == true)
                {
                    MessageBox.Show("修改应用配置失败:" + error.ToString("G"));
                }
                return -1;
            }

            return 0;
        }

        private Int16 TxAndRx_SetAppCfg(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte Protocol = 0;

            if(tbxProtocol.Text == "")
            {
                Protocol = 2;
            }else
            {
                Protocol = Convert.ToByte(tbxProtocol.Text);
            }

            Int16 Error = _TxAndRx_SetAppCfg(sender, e, helper, Protocol);

            TxAndRx_Connect(sender, e, helper);

            return 0;
        }

        /// <summary>
        /// 修改应用配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateGateway_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_SetAppCfg);
        }

        /// <summary>
        /// 修改出厂配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <param name="Protocol"></param>
        /// <returns></returns>
        private Int16 TxAndRx_SetFactoryCfg(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[18];
            UInt16 TxLen = 0;

            byte[] ByteBuf = null;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x68;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // New Device MAC
            ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewDeviceMac.Text);
            if (ByteBuf == null || ByteBuf.Length < 4)
            {
                MessageBox.Show("新Device MAC有错误!");
                return -1;
            }
            TxBuf[TxLen++] = ByteBuf[0];
            TxBuf[TxLen++] = ByteBuf[1];
            TxBuf[TxLen++] = ByteBuf[2];
            TxBuf[TxLen++] = ByteBuf[3];

            // 新硬件版本
            ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewHwRevision.Text);
            if (ByteBuf == null || ByteBuf.Length < 4)
            {
                MessageBox.Show("新硬件版本有错误!");
                return -2;
            }
            TxBuf[TxLen++] = ByteBuf[0];
            TxBuf[TxLen++] = ByteBuf[1];
            TxBuf[TxLen++] = ByteBuf[2];
            TxBuf[TxLen++] = ByteBuf[3];

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 2000, "CB CB");

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("修改出厂配置失败:" + error.ToString("G"));
                return -1;
            }

            // 完成后，再执行一次读取操作
            TxAndRx_Connect(sender, e, helper);

            return 0;
        }

        /// <summary>
        /// 控制网关的工厂信息，包括MAC和硬件版本号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateFactory_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_SetFactoryCfg);
        }

        private Int16 TxAndRx_DeleteHistory(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[18];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x69;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 控制位
            TxBuf[TxLen++] = 0x00;      // 0x00: 所有的队列

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 10000, "CB CB");

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("删除全部数据失败:" + error.ToString("G"));
                return -1;
            }

            // 完成后，再执行一次读取操作
            TxAndRx_Connect(sender, e, helper);

            return 0;
        }

        /// <summary>
        /// 删除队列中的信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteFlash_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_DeleteHistory);
        }

        /// <summary>
        /// 读取绑定设备列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ReadBind(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[14];
            UInt16 TxLen = 0;

            // Start
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // Length
            TxBuf[TxLen++] = 0x00;

            // Cmd
            TxBuf[TxLen++] = 0x6A;

            // Protocol
            TxBuf[TxLen++] = 0x01;

            // 强制读取绑定设备列表的原始数据
            if(cbxBindRaw.IsChecked == true)
            {
                TxBuf[TxLen++] = 0x01;
            }else
            {
                TxBuf[TxLen++] = 0x00;
            }

            // 保留            
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // End
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 8000, 1000, 200);

            if (RxBuf == null)
            {
                MessageBox.Show("读取绑定设备列表失败，无反馈！");
                return -1;
            }

            BindingAnalyse analyse = new BindingAnalyse();
            RadGridView1.ItemsSource = analyse.GatewayBinding(RxBuf);

            tbkResultOfBind.Text = "读取完毕！";

            return 0;
        }

        /// <summary>
        /// 读取绑定设备列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadGatewayBinding_Click(object sender, RoutedEventArgs e)
        {
            RadGridView1.ItemsSource = null;

            tbkResultOfBind.Text = "";
            OpenTxRxClose(sender, e, TxAndRx_ReadBind);
        }

        /// <summary>
        /// 删除一个绑定设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_DeleteOneBind(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[20];
            UInt16 TxLen = 0;

            byte[] ByteBuf = null;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x6B;

            // 协议版本
            TxBuf[TxLen++] = 0x02;

            // Cmd
            BindCmd = TxBuf[TxLen++] = 0x01;

            // Sensor MAC
            ByteBuf = MyCustomFxn.HexStringToByteArray(tbxMacOfBind.Text);
            if (ByteBuf == null || ByteBuf.Length < 4)
            {
                MessageBox.Show("请输入\"设备MAC号\"!");
                return -1;
            }
            TxBuf[TxLen++] = ByteBuf[0];
            TxBuf[TxLen++] = ByteBuf[1];
            TxBuf[TxLen++] = ByteBuf[2];
            TxBuf[TxLen++] = ByteBuf[3];

            // 配置长度
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("删除一个失败:" + error.ToString("G"));
                return -1;
            }

            TxAndRx_ReadBind(sender, e, helper);

            return 0;
        }

        /// <summary>
        /// 删除一个绑定设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteGatewayBinding_Click(object sender, RoutedEventArgs e)
        {
            tbkResultOfBind.Text = "";
            OpenTxRxClose(sender, e, TxAndRx_DeleteOneBind);
        }

        /// <summary>
        /// 判断收到的数据包是否符合格式要求
        /// </summary>
        /// <param name="SrcBuf"></param>
        /// <returns></returns>
        private Int16 RxPkt_IsRight(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 15)
            {
                return -1;
            }

            // 起始位
            if (SrcData[IndexOfStart + 0] != 0xBC || SrcData[IndexOfStart + 1] != 0xBC)
            {
                return -2;
            }

            // 长度位
            byte pktLen = SrcData[IndexOfStart + 2];
            if (pktLen + 7 > SrcLen)
            {
                return -3;
            }

            if (SrcData[IndexOfStart + 3 + pktLen + 2] != 0xCB || SrcData[IndexOfStart + 3 + pktLen + 3] != 0xCB)
            {
                return -4;
            }

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, SrcData, (UInt16)(IndexOfStart + 3), pktLen);
            UInt16 crc_chk = (UInt16)(SrcData[IndexOfStart + 3 + pktLen + 0] * 256 + SrcData[IndexOfStart + 3 + pktLen + 1]);
            if (crc_chk != crc && crc_chk != 0)
            {
                return -5;
            }

            return (Int16)(pktLen + 7);
        }

        /// <summary>
        /// 读取绑定
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadBind(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 22)
            {
                return -1;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1)
            {
                return -2;
            }

            return 0;
        }

        /// <summary>
        /// 添加/删除绑定设备
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_AddOrDeleteBind(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 16)
            {
                return -1;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 2)
            {
                return -2;
            }

            // Error
            byte Error = SrcData[IndexOfStart + 10];
            if (Error == 1)
            {
                if (BindCmd == 0)
                {
                    MessageBox.Show("绑定设备失败，有反馈(" + Error.ToString() + ")!");
                }
                else if (BindCmd == 1)
                {
                    MessageBox.Show("删除设备失败，有反馈(" + Error.ToString() + ")!");
                }
                else if (BindCmd == 3)
                {
                    MessageBox.Show("删除所有设备失败，有反馈(" + Error.ToString() + ")!");
                }
                else
                {
                    MessageBox.Show("未知操作，有反馈(" + Error.ToString() + ")!");
                }

                return -3;
            }

            // 绑定设备的数量
            byte Total = SrcData[IndexOfStart + 11];

            if (BindCmd == 0)
            {
                tbkResultOfBind.Text = "绑定成功(" + Total.ToString() + ")!";
            }
            else if (BindCmd == 1)
            {
                tbkResultOfBind.Text = "删除成功(" + Total.ToString() + ")!";
            }
            else if (BindCmd == 3)
            {
                tbkResultOfBind.Text = "删除所有设备成功(" + Total.ToString() + ")!";
            }
            else
            {
                tbkResultOfBind.Text = "未知操作(" + Total.ToString() + ")!";
            }

            return 0;
        }

        /// <summary>
        /// 查询CC110的配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadCfgOfCC1101(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol == 1)
            {
                if (SrcLen < 21)
                {
                    return -2;
                }
            }
            else if (Protocol == 2)
            {
                if (SrcLen < 26)
                {
                    return -3;
                }
            }
            else
            {
                return -1;
            }

            UInt16 iCnt = (UInt16)(IndexOfStart + 10);

            tbxOnOff.Text = SrcData[iCnt].ToString("D");
            iCnt += 1;

            tbxBpsOfCC1101.Text = SrcData[iCnt].ToString("X2");
            iCnt += 1;

            Int16 txPower = SrcData[iCnt];
            if (txPower >= 128)
            {
                txPower -= 256;
            }
            tbxTxPowerOfCC1101.Text = txPower.ToString("D");
            iCnt += 1;

            tbxCustomerOfCC1101.Text = SrcData[iCnt].ToString("X2") + " " + SrcData[iCnt + 1].ToString("X2");
            iCnt += 2;

            if (Protocol == 2)
            {
                tbxFreqOfCC1101.Text = (SrcData[iCnt] * 256 * 256 * 256 + SrcData[iCnt + 1] * 256 * 256 + SrcData[iCnt + 2] * 256 + SrcData[iCnt + 3]).ToString("D");
                iCnt += 4;

                tbxXtOfCC1101.Text = SrcData[iCnt].ToString("D");
                iCnt += 1;
            }

            tbxReservedOfCC1101.Text = SrcData[iCnt].ToString("X2") + " " + SrcData[iCnt + 1].ToString("X2");
            iCnt += 2;

            return 0;
        }

        /// <summary>
        /// 修改CC110的配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_SetCfgOfCC1101(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            if (SrcLen < 17)
            {
                return -1;
            }

            byte Error = SrcData[IndexOfStart + 10];
            if (Error != 0)
            {
                return -2;
            }

            return 0;
        }

        /// <summary>
        /// 读取APN
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadApn(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 19)
            {
                return -2;
            }

            // APN的长度
            byte LenOfApn = SrcData[IndexOfStart + 10];
            if (LenOfApn > 63)
            {
                return -3;
            }

            // APN的内容
            UInt16 StartOfApn = (UInt16)(IndexOfStart + 11);
            string StrOfApn = System.Text.Encoding.UTF8.GetString(SrcData, StartOfApn, LenOfApn);

            // Username的长度
            byte LenOfUsername = SrcData[StartOfApn + LenOfApn];
            if (LenOfUsername > 63)
            {
                return -4;
            }

            // Username的内容
            UInt16 StartOfUsername = (UInt16)(StartOfApn + LenOfApn + 1);
            string StrOfUsername = System.Text.Encoding.UTF8.GetString(SrcData, StartOfUsername, LenOfUsername);

            // Password的长度
            byte LenOfPassword = SrcData[StartOfUsername + LenOfUsername];
            if (LenOfPassword > 63)
            {
                return -4;
            }

            // Password的内容
            UInt16 StartOfPassword = (UInt16)(StartOfUsername + LenOfUsername + 1);
            string StrOfPassword = System.Text.Encoding.UTF8.GetString(SrcData, StartOfPassword, LenOfPassword);


            // 显示
            tbxApn.Text = StrOfApn;
            tbxUserName.Text = StrOfUsername;
            tbxPassword.Text = StrOfPassword;

            return 0;
        }

        /// <summary>
        /// 设置APN
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_SetApn(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 17)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error < 0)
            {
                return -3;
            }

            return 0;
        }

        private Int16 RxPkt_Ntp(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 17)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error < 0)
            {
                return -3;
            }

            return 0;
        }

        /// <summary>
        /// 读取DeptCode
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadDeptCode(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 27)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error < 0)
            {
                return -3;
            }

            // DeptCode
            string DeptCode = System.Text.Encoding.UTF8.GetString(SrcData, IndexOfStart + 11, 10);
            tbxDeptCode.Text = DeptCode;

            return 0;
        }

        /// <summary>
        /// 设置DeptCode
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_SetDeptCode(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 17)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error < 0)
            {
                return -3;
            }

            return 0;
        }

        /// <summary>
        /// 读取/设置/还原开始时间
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadWriteResetStartCalendar(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 22)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error < 0)
            {
                return -3;
            }

            // Cmd
            byte Cmd = SrcData[IndexOfStart + 11];
            if (Cmd > 3)
            {
                return -4;
            }

            DateTime st = CommArithmetic.DecodeDateTime(SrcData, (UInt16)(IndexOfStart + 12));

            tbxNewStartCalendar.Text = tbxStartCalendar.Text = st.ToString("yyyy-MM-dd HH:mm:ss");

            return 0;
        }

        /// <summary>
        /// 读取设备的基础信息
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadBase(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 32)
            {
                return -1;
            }

            // 数据包的长度
            byte PktLen = SrcData[IndexOfStart + 2];
            if (PktLen > SrcLen - 7)
            {
                return -2;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1)
            {
                return -3;
            }
            /*
            aGateway = new Gateway();

            // 设备类型
            aGateway.SetDeviceName(SrcData[IndexOfStart + 5]);

            // Primary Mac
            aGateway.SetDevicePrimaryMac(SrcData, (UInt16)(IndexOfStart + 6));

            // MAC
            aGateway.SetDeviceMac(SrcData, (UInt16)(IndexOfStart + 10));

            // 硬件版本
            aGateway.SetHardwareRevision(SrcData, (UInt16)(IndexOfStart + 14));

            // 软件版本
            aGateway.SetSoftwareRevision(SrcData, (UInt16)(IndexOfStart + 18));
            */

            return 0;
        }

        /// <summary>
        /// 读取网关的详细配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadCfgDetail(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 32)
            {
                return -1;
            }

            // 数据包的长度
            byte PktLen = SrcData[IndexOfStart + 2];
            if (PktLen > SrcLen - 7)
            {
                return -2;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1 && Protocol != 2)
            {
                return -3;
            }

            aGateway = new Gateway();

            if (Protocol == 1)
            {
                // 协议版本
                aGateway.Protocol = Protocol;

                // 设备类型
                aGateway.SetDeviceName(SrcData[IndexOfStart + 5]);

                // MAC
                aGateway.SetDeviceMac(SrcData, (UInt16)(IndexOfStart + 6));

                // 硬件版本
                aGateway.SetHardwareRevision(SrcData, (UInt16)(IndexOfStart + 10));

                // 软件版本
                aGateway.SetSoftwareRevision(SrcData, (UInt16)(IndexOfStart + 14));

                // CC1310/CC1352P软件版本
                aGateway.cSwRevisionS = CommArithmetic.ByteArrayToHexString(SrcData, (UInt16)(IndexOfStart + 16), 2);

                // 客户码
                aGateway.SetDeviceCustomer(SrcData, (UInt16)(IndexOfStart + 18));

                // Debug
                aGateway.SetDebug(SrcData, (UInt16)(IndexOfStart + 20));

                // Category
                aGateway.Category = SrcData[IndexOfStart + 22];

                // Interval
                aGateway.Interval = (UInt16)(((UInt16)SrcData[IndexOfStart + 23] << 8) | ((UInt16)SrcData[IndexOfStart + 24] << 0));

                // 日期和时间
                aGateway.Current = CommArithmetic.DecodeDateTime(SrcData, (UInt16)(IndexOfStart + 25));

                // 工作模式
                aGateway.Pattern = SrcData[IndexOfStart + 31];

                // bps
                aGateway.bps = SrcData[IndexOfStart + 32];

                // Channel
                aGateway.channel = SrcData[IndexOfStart + 33];

                // 轮播间隔
                aGateway.Carousel = (UInt16)(((UInt16)SrcData[IndexOfStart + 34] << 8) | ((UInt16)SrcData[IndexOfStart + 35] << 0));

                // 报警间隔
                aGateway.IntervalOfAlert = (UInt16)(((UInt16)SrcData[IndexOfStart + 36] << 8) | ((UInt16)SrcData[IndexOfStart + 37] << 0));

                // 转发策略
                aGateway.TransPolicy = SrcData[IndexOfStart + 38];

                // 时间来源
                aGateway.TimeSrc = SrcData[IndexOfStart + 39];

                // 报警方式
                aGateway.AlertWay = SrcData[IndexOfStart + 40];

                // 判据来源
                aGateway.criSrc = SrcData[IndexOfStart + 41];

                // 显示容量
                aGateway.DisplayStyle = SrcData[IndexOfStart + 42];

                // 亮屏策略
                aGateway.brightMode = SrcData[IndexOfStart + 43];

                // 启用
                aGateway.use = SrcData[IndexOfStart + 44];

                // TxPower
                aGateway.TxPower = SrcData[IndexOfStart + 45];
                if (aGateway.TxPower >= 0x80)
                {
                    aGateway.TxPower -= 0x100;
                }

                // 配置保留位
                aGateway.SetReserved(SrcData, (UInt16)(IndexOfStart + 46), 3);

                // 复位原因
                aGateway.RstSrc = SrcData[IndexOfStart + 49];

                // RAM存储
                aGateway.RAMCountHigh = SrcData[IndexOfStart + 50];
                aGateway.RAMCountLow = SrcData[IndexOfStart + 51];

                // 主服务器：Flash存储
                aGateway.FlashToSend_SSH_M = (UInt32)(((UInt32)SrcData[IndexOfStart + 52] << 16) | ((UInt32)SrcData[IndexOfStart + 53] << 8) | ((UInt32)SrcData[IndexOfStart + 54] << 0));
                aGateway.FlashToSend_SSL_M = (UInt32)(((UInt32)SrcData[IndexOfStart + 55] << 16) | ((UInt32)SrcData[IndexOfStart + 56] << 8) | ((UInt32)SrcData[IndexOfStart + 57] << 0));

                // 主服务器的域名和端口
                byte DomainLen = SrcData[IndexOfStart + 58];        // 域名的长度
                if (DomainLen == 0 || DomainLen > 63)
                {
                    return -4;                                      // 域名长度错误
                }

                UInt16 DomainStart = (UInt16)(IndexOfStart + 59);   // 域名的起始位置

                aGateway.TargetDomain = Encoding.Default.GetString(SrcData, DomainStart, DomainLen);    // 域名

                UInt16 PortStart = (UInt16)(DomainStart + DomainLen);                                   // 端口的起始位置

                aGateway.TargetPort = (UInt16)(((UInt16)SrcData[PortStart] << 8) | ((UInt16)SrcData[PortStart + 1] << 0));

                if (SrcData[PortStart + 4] != 0xCB || SrcData[PortStart + 5] != 0xCB)
                {
                    return -5;              // 域名的长度有问题
                }
            }
            else if (Protocol == 2)
            {
                // 协议版本
                aGateway.Protocol = Protocol;

                // 设备类型
                aGateway.SetDeviceName(SrcData[IndexOfStart + 5]);

                // Primary Mac
                aGateway.SetDevicePrimaryMac(SrcData, (UInt16)(IndexOfStart + 6));

                // MAC
                aGateway.SetDeviceMac(SrcData, (UInt16)(IndexOfStart + 10));

                // 硬件版本
                aGateway.SetHardwareRevision(SrcData, (UInt16)(IndexOfStart + 14));

                // 软件版本
                aGateway.SetSoftwareRevision(SrcData, (UInt16)(IndexOfStart + 18));

                // CC1310/CC1352P软件版本
                aGateway.cSwRevisionS = CommArithmetic.ByteArrayToHexString(SrcData, (UInt16)(IndexOfStart + 20), 2);

                // 客户码
                aGateway.SetDeviceCustomer(SrcData, (UInt16)(IndexOfStart + 22));

                // Debug
                aGateway.SetDebug(SrcData, (UInt16)(IndexOfStart + 24));

                // Category
                aGateway.Category = SrcData[IndexOfStart + 26];

                // Interval
                aGateway.Interval = (UInt16)(((UInt16)SrcData[IndexOfStart + 27] << 8) | ((UInt16)SrcData[IndexOfStart + 28] << 0));

                // 定位间隔
                aGateway.IntervalOfLocate = (UInt16)(((UInt16)SrcData[IndexOfStart + 29] << 8) | ((UInt16)SrcData[IndexOfStart + 30] << 0));

                // 日期和时间
                aGateway.Current = CommArithmetic.DecodeDateTime(SrcData, (UInt16)(IndexOfStart + 31));

                // 工作模式
                aGateway.Pattern = SrcData[IndexOfStart + 37];

                // bps
                aGateway.bps = SrcData[IndexOfStart + 38];

                // Channel
                aGateway.channel = SrcData[IndexOfStart + 39];

                // TxPower
                aGateway.TxPower = SrcData[IndexOfStart + 40];
                if (aGateway.TxPower >= 0x80)
                {
                    aGateway.TxPower -= 0x100;
                }

                // 轮播间隔
                aGateway.Carousel = (UInt16)(((UInt16)SrcData[IndexOfStart + 41] << 8) | ((UInt16)SrcData[IndexOfStart + 42] << 0));

                // 报警间隔
                aGateway.IntervalOfAlert = (UInt16)(((UInt16)SrcData[IndexOfStart + 43] << 8) | ((UInt16)SrcData[IndexOfStart + 44] << 0));

                // 转发策略
                aGateway.TransPolicy = SrcData[IndexOfStart + 45];

                // 时间来源
                aGateway.TimeSrc = SrcData[IndexOfStart + 46];

                // 报警方式
                aGateway.AlertWay = SrcData[IndexOfStart + 47];

                // 判据来源
                aGateway.criSrc = SrcData[IndexOfStart + 48];

                // 显示容量
                aGateway.DisplayStyle = SrcData[IndexOfStart + 49];

                // 亮屏策略
                aGateway.brightMode = SrcData[IndexOfStart + 50];

                // 启用
                aGateway.use = SrcData[IndexOfStart + 51];

                // 配置保留位
                aGateway.SetReserved(SrcData, (UInt16)(IndexOfStart + 53), 3);

                // 复位原因
                aGateway.RstSrc = SrcData[IndexOfStart + 56];

                // RAM存储
                aGateway.RAMCountHigh = SrcData[IndexOfStart + 57];
                aGateway.RAMCountLow = SrcData[IndexOfStart + 58];

                // 主服务器：Flash存储
                aGateway.FlashToSend_SSH_M = (UInt32)(((UInt32)SrcData[IndexOfStart + 59] << 16) | ((UInt32)SrcData[IndexOfStart + 60] << 8) | ((UInt32)SrcData[IndexOfStart + 61] << 0));
                aGateway.FlashToSend_SSL_M = (UInt32)(((UInt32)SrcData[IndexOfStart + 62] << 16) | ((UInt32)SrcData[IndexOfStart + 63] << 8) | ((UInt32)SrcData[IndexOfStart + 64] << 0));
                aGateway.FlashToSend_Status_M = (UInt32)(((UInt32)SrcData[IndexOfStart + 65] << 16) | ((UInt32)SrcData[IndexOfStart + 66] << 8) | ((UInt32)SrcData[IndexOfStart + 67] << 0));
                aGateway.FlashToSend_Locate_M = (UInt32)(((UInt32)SrcData[IndexOfStart + 68] << 16) | ((UInt32)SrcData[IndexOfStart + 69] << 8) | ((UInt32)SrcData[IndexOfStart + 70] << 0));

                // 副服务器：Flash存储
                aGateway.FlashToSend_SSH_S = (UInt32)(((UInt32)SrcData[IndexOfStart + 71] << 16) | ((UInt32)SrcData[IndexOfStart + 72] << 8) | ((UInt32)SrcData[IndexOfStart + 73] << 0));
                aGateway.FlashToSend_SSL_S = (UInt32)(((UInt32)SrcData[IndexOfStart + 74] << 16) | ((UInt32)SrcData[IndexOfStart + 75] << 8) | ((UInt32)SrcData[IndexOfStart + 76] << 0));
                aGateway.FlashToSend_Status_S = (UInt32)(((UInt32)SrcData[IndexOfStart + 77] << 16) | ((UInt32)SrcData[IndexOfStart + 78] << 8) | ((UInt32)SrcData[IndexOfStart + 79] << 0));
                aGateway.FlashToSend_Locate_S = (UInt32)(((UInt32)SrcData[IndexOfStart + 80] << 16) | ((UInt32)SrcData[IndexOfStart + 81] << 8) | ((UInt32)SrcData[IndexOfStart + 82] << 0));

                // 主服务器的域名和端口
                byte DomainLen = SrcData[IndexOfStart + 83];        // 域名的长度
                if (DomainLen == 0 || DomainLen > 63)
                {
                    return -4;                                      // 域名长度错误
                }

                UInt16 DomainStart = (UInt16)(IndexOfStart + 84);   // 域名的起始位置

                aGateway.TargetDomain = Encoding.Default.GetString(SrcData, DomainStart, DomainLen);    // 域名

                UInt16 PortStart = (UInt16)(DomainStart + DomainLen);                                   // 端口的起始位置

                aGateway.TargetPort = (UInt16)(((UInt16)SrcData[PortStart] << 8) | ((UInt16)SrcData[PortStart + 1] << 0));

                // 副服务器的域名和端口
                DomainLen = SrcData[PortStart + 2];                 // 域名的长度
                DomainStart = (UInt16)(PortStart + 3);              // 域名的起始位置
                if (DomainLen > 0 && DomainLen <= 63)
                {
                    aGateway.TargetDomain2 = Encoding.Default.GetString(SrcData, DomainStart, DomainLen);   // 域名   
                }
                else
                {
                    aGateway.TargetDomain2 = "";                    // 域名   
                }

                PortStart = (UInt16)(DomainStart + DomainLen);      // 端口的起始位置

                aGateway.TargetPort2 = (UInt16)(((UInt16)SrcData[PortStart] << 8) | ((UInt16)SrcData[PortStart + 1] << 0));

                if (SrcData[PortStart + 4] != 0xCB || SrcData[PortStart + 5] != 0xCB)
                {
                    return -5;              // 域名的长度有问题
                }
            }

            return 0;
        }

        /// <summary>
        /// 读取本地传感器的配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadCfgOfLsx(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 72)
            {
                return -1;
            }

            // 数据包的长度
            byte PktLen = SrcData[IndexOfStart + 2];
            if (PktLen > SrcLen - 7)
            {
                return -2;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1)
            {
                return -3;
            }

            aIntervalSensor = new InternalSensor();

            // 协议版本
            aIntervalSensor.Protocol = Protocol;

            // 传感器的编号
            aIntervalSensor.iX = SrcData[IndexOfStart + 6];

            // MAC
            aIntervalSensor.SetDeviceMac(SrcData, (UInt16)(IndexOfStart + 7));

            // 传感器类型
            aIntervalSensor.SensorType = SrcData[IndexOfStart + 11];

            // 传感器是否在线
            aIntervalSensor.Online = SrcData[IndexOfStart + 12];

            // ON/OFF
            aIntervalSensor.OnOff = SrcData[IndexOfStart + 13];

            // Debug
            aIntervalSensor.SetDebug(SrcData, (UInt16)(IndexOfStart + 14));

            // Category
            aIntervalSensor.Category = SrcData[IndexOfStart + 16];

            // 工作模式
            aIntervalSensor.Pattern = SrcData[IndexOfStart + 17];

            // 存储容量
            aIntervalSensor.MaxLength = SrcData[IndexOfStart + 18];

            // SampleSend
            aIntervalSensor.SampleSend = SrcData[IndexOfStart + 19];

            // Interval
            aIntervalSensor.Interval = (UInt16)(((UInt16)SrcData[IndexOfStart + 20] << 8) | ((UInt16)SrcData[IndexOfStart + 21] << 0));

            // 正常间隔
            aIntervalSensor.IntervalOfNormal = (UInt16)(((UInt16)SrcData[IndexOfStart + 22] << 8) | ((UInt16)SrcData[IndexOfStart + 23] << 0));

            // 预警间隔
            aIntervalSensor.IntervalOfWarn = (UInt16)(((UInt16)SrcData[IndexOfStart + 24] << 8) | ((UInt16)SrcData[IndexOfStart + 25] << 0));

            // 报警间隔
            aIntervalSensor.IntervalOfAlert = (UInt16)(((UInt16)SrcData[IndexOfStart + 26] << 8) | ((UInt16)SrcData[IndexOfStart + 27] << 0));

            // 温度补偿
            Int16 CompensationV = (Int16)(((UInt16)SrcData[IndexOfStart + 28] << 8) | ((UInt16)SrcData[IndexOfStart + 29] << 0));
            aIntervalSensor.TempCompensation = (double)CompensationV / 100.0f;

            // 湿度补偿
            CompensationV = (Int16)(((UInt16)SrcData[IndexOfStart + 30] << 8) | ((UInt16)SrcData[IndexOfStart + 31] << 0));
            aIntervalSensor.HumCompensation = (double)CompensationV / 100.0f;

            // 温度阈值上限
            Int16 temp = (Int16)(((UInt16)SrcData[IndexOfStart + 40] << 8) | ((UInt16)SrcData[IndexOfStart + 41] << 0));
            aIntervalSensor.TempThrHigh = (double)temp / 100.0f;

            // 温度阈值下限
            temp = (Int16)(((UInt16)SrcData[IndexOfStart + 42] << 8) | ((UInt16)SrcData[IndexOfStart + 43] << 0));
            aIntervalSensor.TempThrLow = (double)temp / 100.0f;

            // 湿度阈值上限
            UInt16 hum = (UInt16)(((UInt16)SrcData[IndexOfStart + 44] << 8) | ((UInt16)SrcData[IndexOfStart + 45] << 0));
            aIntervalSensor.HumThrHigh = (double)hum / 100.0f;

            // 湿度阈值下限
            hum = (UInt16)(((UInt16)SrcData[IndexOfStart + 46] << 8) | ((UInt16)SrcData[IndexOfStart + 47] << 0));
            aIntervalSensor.HumThrLow = (double)hum / 100.0f;

            // 温度
            temp = (Int16)(((UInt16)SrcData[IndexOfStart + 64] << 8) | ((UInt16)SrcData[IndexOfStart + 65] << 0));
            aIntervalSensor.Temp = (double)temp / 100.0f;

            // 湿度
            hum = (UInt16)(((UInt16)SrcData[IndexOfStart + 66] << 8) | ((UInt16)SrcData[IndexOfStart + 67] << 0));
            aIntervalSensor.Hum = (double)hum / 100.0f;

            return 0;
        }

        /// <summary>
        /// 修改本地传感器的配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_SetCfgOfLsx(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 54)
            {
                return -1;
            }

            // 数据包的长度
            byte PktLen = SrcData[IndexOfStart + 2];
            if (PktLen > SrcLen - 7)
            {
                return -2;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1)
            {
                return -3;
            }

            return 0;
        }

        /// <summary>
        /// 查询是否支持BootLoader
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_BootLoader(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 19)
            {
                return -2;
            }

            // State
            if (SrcData[IndexOfStart + 10] == 1)
            {
                     // 支持BootLoader
            }

            return 0;
        }

        /// <summary>
        /// 读取布局
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadLayout(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 17)
            {
                return -2;
            }

            // 数据包的长度
            byte LenOfPkt = SrcData[IndexOfStart + 2];
            if (LenOfPkt < 10)
            {
                return -3;
            }

            // 布局的长度
            byte LenOfLayout = SrcData[IndexOfStart + 10];
            if (LenOfLayout != LenOfPkt - 10 || LenOfLayout > 48 * 2)
            {
                return -4;
            }

            // 布局内容
            tbxLayout.Text = "";
            UInt16 StartIndexOfLayout = (UInt16)(IndexOfStart + 11);
            if (LenOfLayout == 0)
            {   // 布局为空

            }
            else
            {
                byte LayoutType = 0;        // 布局类型
                byte LayoutParam = 0;       // 布局参数

                for (byte iX = 0; iX < LenOfLayout; iX += 2)
                {
                    LayoutType = SrcData[StartIndexOfLayout + iX];
                    LayoutParam = SrcData[StartIndexOfLayout + iX + 1];

                    switch (LayoutType)
                    {
                        case 0x00:              // 无
                            {
                                tbxLayout.Text += "无";
                                break;
                            }
                        case 0x01:              // 空一行
                            {
                                tbxLayout.Text += "\n";
                                break;
                            }
                        case 0x02:              // 标题
                            {
                                tbxLayout.Text += "[标题]\n";
                                break;
                            }
                        case 0x03:              // 车辆信息
                            {
                                tbxLayout.Text += "[车辆信息]\n";
                                break;
                            }
                        case 0x04:              // 网关MAC号和运输码
                            {
                                tbxLayout.Text += "[网关MAC号和运输码]\n";
                                break;
                            }
                        case 0x05:              // 开始时间
                            {
                                tbxLayout.Text += "[开始时间]\n";
                                break;
                            }
                        case 0x06:              // 结束时间
                            {
                                tbxLayout.Text += "[结束时间]\n";
                                break;
                            }
                        case 0x07:              // 无意义
                            {
                                break;
                            }
                        case 0x08:              // 无意义
                            {
                                break;
                            }
                        case 0x09:              // Sensor MAC和名称
                            {
                                tbxLayout.Text += "[Sensor MAC和名称]\n";
                                break;
                            }
                        case 0x0A:              // 温度列表
                            {
                                tbxLayout.Text += "[温度列表]\n";
                                break;
                            }
                        case 0x0B:              // 送货人
                            {
                                tbxLayout.Text += "[送货人]\n";
                                break;
                            }
                        case 0x0C:              // 收货人
                            {
                                tbxLayout.Text += "[收货人]\n";
                                break;
                            }
                        case 0x0D:              // 送货单位
                            {
                                tbxLayout.Text += "[送货单位]\n";
                                break;
                            }
                        case 0x0E:              // 当前时间
                            {
                                tbxLayout.Text += "[当前时间]\n";
                                break;
                            }
                        case 0x0F:              // 固定字符串
                            {
                                tbxLayout.Text += "[固定字符串][" + LayoutParam.ToString() + "]\n";
                                break;
                            }
                        default:                // 未定义
                            {
                                tbxLayout.Text += "未定义\n";
                                break;
                            }
                    }   // switch
                }   // for

                // 去除末尾的回车换行
                tbxLayout.Text = tbxLayout.Text.TrimEnd('\r');
                tbxLayout.Text = tbxLayout.Text.TrimEnd('\n');

            }   // else

            return 0;
        }

        /// <summary>
        /// 读取布局内容
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadLayoutUnit(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 17)
            {
                return -2;
            }

            // 数据包的长度
            byte LenOfPkt = SrcData[IndexOfStart + 2];
            if (LenOfPkt < 10)
            {
                return -3;
            }

            // 总数量
            byte total = SrcData[IndexOfStart + 10];
            if (total == 0)
            {
                return 1;
            }

            byte current = SrcData[IndexOfStart + 11];
            byte LayoutType = SrcData[IndexOfStart + 12];               // 布局类型
            byte LayoutParam = SrcData[IndexOfStart + 13];              // 布局参数
            byte LenOfTxt = SrcData[IndexOfStart + 14];                 // 内容长度

            UInt16 StartIndexOfLayoutTxt = (UInt16)(IndexOfStart + 15);

            string LayoutTxt = Encoding.Default.GetString(SrcData, StartIndexOfLayoutTxt, LenOfTxt);

            switch (LayoutType)
            {
                case 0x00:              // 无
                    {
                        break;
                    }
                case 0x01:              // 空一行
                    {
                        break;
                    }
                case 0x02:              // 标题
                    {
                        tbxLayout.Text = tbxLayout.Text.Replace("[标题]", "[标题] = " + LayoutTxt);
                        break;
                    }
                case 0x03:              // 车辆信息
                    {
                        tbxLayout.Text = tbxLayout.Text.Replace("[车辆信息]", "[车辆信息] = " + LayoutTxt);
                        break;
                    }
                case 0x04:              // 网关MAC号和运输码
                    {
                        break;
                    }
                case 0x05:              // 开始时间
                    {
                        break;
                    }
                case 0x06:              // 结束时间
                    {
                        break;
                    }
                case 0x07:              // 无意义
                    {
                        break;
                    }
                case 0x08:              // 无意义
                    {
                        break;
                    }
                case 0x09:              // Sensor MAC和名称
                    {
                        break;
                    }
                case 0x0A:              // 温度列表
                    {
                        break;
                    }
                case 0x0B:              // 送货人
                    {
                        tbxLayout.Text = tbxLayout.Text.Replace("\n[送货人]", "\n[送货人] = " + LayoutTxt);
                        break;
                    }
                case 0x0C:              // 收货人
                    {
                        tbxLayout.Text = tbxLayout.Text.Replace("\n[收货人]", "\n[收货人] = " + LayoutTxt);
                        break;
                    }
                case 0x0D:              // 送货单位
                    {
                        tbxLayout.Text = tbxLayout.Text.Replace("\n[送货单位]", "\n[送货单位] = " + LayoutTxt);
                        break;
                    }
                case 0x0E:              // 当前时间
                    {
                        break;
                    }
                case 0x0F:              // 固定字符串
                    {
                        string item = "\n[固定字符串][" + LayoutParam.ToString() + "]";
                        tbxLayout.Text = tbxLayout.Text.Replace(item, item + " = " + LayoutTxt);
                        break;
                    }
                default:                // 未定义
                    {
                        tbxLayout.Text += "[未定义]\n";
                        break;
                    }
            }   // switch

            return 0;
        }

        /// <summary>
        /// 导出数据时的预操作
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadAbstract(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 24)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error != 0)
            {
                tbxStartTimeOfData.Text = "";
                tbkTotalOfData.Text = "查询失败=" + Error.ToString("G");
                tbkTotalOfData.Foreground = new SolidColorBrush(Colors.Red);
                tbkTotalOfData.FontWeight = FontWeights.Bold;
                return -3;
            }

            // 开始时间
            StartTimeOfExport = CommArithmetic.DecodeDateTime(SrcData, (UInt16)(IndexOfStart + 11));

            // 有效数据的总量
            ExpTotalOfExport = ((UInt32)SrcData[IndexOfStart + 17] << 16) | ((UInt32)SrcData[IndexOfStart + 18] << 8) | ((UInt32)SrcData[IndexOfStart + 19] << 0);

            // 主界面显示
            tbxStartTimeOfData.Text = StartTimeOfExport.ToString("yyyy-MM-dd HH:mm:ss");
            tbkTotalOfData.Text = ExpTotalOfExport.ToString();
            tbkTotalOfData.Foreground = new SolidColorBrush(Colors.Black);
            tbkTotalOfData.FontWeight = FontWeights.Normal;

            return 0;
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_Export(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 16)
            {
                return -2;
            }

            // 过程
            byte Step = SrcData[IndexOfStart + 10];

            // Error
            Int16 Error = SrcData[IndexOfStart + 11];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Step == 2 && Error == 1)
            {
                // 已读到尽头
                return 3;
            }
            else
            {
                if (Error != 0)
                {
                    return -3;
                }
            }

            switch (Step)
            {
                case 0:     // 导出开始
                    {
                        return 1;           // 继续接收
                    }
                case 1:     // 正在导出数据
                    {
                        M1 aM1 = new M1(SrcData, IndexOfStart);
                        if (aM1 == null)
                        {
                            return -5;
                        }

                        // 添加到显示列表中

                        ReadCnt++;

                        if(IsMyExpSensorData(aM1.DeviceMacV, aM1.SensorCollectTime) == false)
                        {
                            break;
                        }

                        aM1.DisplayID = GridLineOfExport;
                        if (++GridLineOfExport == 0)
                        {
                            GridLineOfExport++;
                        }                       

                        aM1.Group = GroupCnt + 1;
                        aM1.GroupCapacity = UnitOfRead;
                        DataOfExport.Add((M1)aM1);

                        break;
                    }
                case 2:     // 导出结束
                    {
                        return 2;           // 正常结束接收
                    }
                default:
                    {
                        return -5;          // 结束接收
                    }
            }

            return 0;
        }

        /// <summary>
        /// 修改出厂配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_SetFactoryCfg(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 18)
            {
                return -1;
            }

            // 数据包的长度
            byte PktLen = SrcData[IndexOfStart + 2];
            if (PktLen > SrcLen - 7)
            {
                return -2;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1)
            {
                return -3;
            }

            aGateway = new Gateway();

            // 设备类型
            aGateway.SetDeviceName(SrcData[IndexOfStart + 5]);

            // MAC
            aGateway.SetDeviceMac(SrcData, (UInt16)(IndexOfStart + 6));

            // 硬件版本
            aGateway.SetHardwareRevision(SrcData, (UInt16)(IndexOfStart + 10));

            return 0;
        }

        /// <summary>
        /// 删除全部数据
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_DeleteAllData(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 25)
            {
                return -1;
            }

            // 数据包的长度
            byte PktLen = SrcData[IndexOfStart + 2];
            if (PktLen > SrcLen - 7)
            {
                return -2;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1)
            {
                return -3;
            }

            byte Error = SrcData[IndexOfStart + 11];
            if (Error != 0)
            {
                return -4;
            }

            return 0;
        }

        /// <summary>
        /// 修改应用配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_SetAppCfg(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);
            if (SrcLen < 15)
            {
                return -1;
            }

            // 数据包的长度
            byte PktLen = SrcData[IndexOfStart + 2];
            if (PktLen > SrcLen - 7)
            {
                return -2;
            }

            byte Protocol = SrcData[IndexOfStart + 4];
            if (Protocol != 1 && Protocol != 2)
            {
                return -3;
            }

            if (Protocol == 1)
            {

            }
            else
            {
                byte Error = SrcData[IndexOfStart + 10];
                if (Error != 0)
                {
                    return -4;
                }
            }

            return 0;
        }

        private Int16 RxPkt_WriteKeyOfMd5(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 17)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error < 0)
            {
                return -3;
            }

            return 0;
        }

        /// <summary>
        /// 读取以太网的配置
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 RxPkt_ReadEthernet(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的总长度
            UInt16 SrcLen = (UInt16)(SrcData.Length - IndexOfStart);

            byte Protocol = SrcData[IndexOfStart + 4];

            if (Protocol != 1)
            {
                return -1;
            }

            // 长度
            if (SrcLen < 34)
            {
                return -2;
            }

            // Error
            Int16 Error = SrcData[IndexOfStart + 10];
            if (Error > 0x80)
            {
                Error -= 0x100;
            }

            if (Error < 0)
            {
                return -3;
            }

            // DHCP
            tbxDhcp.Text = SrcData[IndexOfStart + 11].ToString();

            // IP 地址
            tbxIp.Text = SrcData[IndexOfStart + 12].ToString() + "." + SrcData[IndexOfStart + 13].ToString() + "." + SrcData[IndexOfStart + 14].ToString() + "." + SrcData[IndexOfStart + 15].ToString();

            // 子网掩码
            tbxSubnet.Text = SrcData[IndexOfStart + 16].ToString() + "." + SrcData[IndexOfStart + 17].ToString() + "." + SrcData[IndexOfStart + 18].ToString() + "." + SrcData[IndexOfStart + 19].ToString();

            // 默认网关
            tbxGateway.Text = SrcData[IndexOfStart + 20].ToString() + "." + SrcData[IndexOfStart + 21].ToString() + "." + SrcData[IndexOfStart + 22].ToString() + "." + SrcData[IndexOfStart + 23].ToString();

            // 物理地址
            tbxPhyMac.Text = SrcData[IndexOfStart + 24].ToString("X2") + "-" + SrcData[IndexOfStart + 25].ToString("X2") + "-" + SrcData[IndexOfStart + 26].ToString("X2") + "-" + SrcData[IndexOfStart + 27].ToString("X2") + "-" + SrcData[IndexOfStart + 28].ToString("X2") + "-" + SrcData[IndexOfStart + 29].ToString("X2");

            return 0;
        }

        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        /// <param name="SrcData"></param>
        /// <returns></returns>
        private Int16 RxPkt_Handle(byte[] SrcData)
        {
            if (SrcData == null)
            {
                return -1;
            }

            UInt16 SrcLen = (UInt16)SrcData.Length;

            Int16 HandleLen = 0;
            Int16 ExeError = 0;

            for (UInt16 iCnt = 0; iCnt < SrcLen; iCnt++)
            {
                try
                {
                    HandleLen = RxPkt_IsRight(SrcData, iCnt);
                    if (HandleLen < 0)
                    {
                        continue;
                    }

                    switch (SrcData[iCnt + 3])
                    {
                        case 0x01:
                            {
                                ExeError = RxPkt_ReadBase(SrcData, iCnt);
                                break;
                            }
                        case 0x02:
                            {
                                ExeError = RxPkt_BootLoader(SrcData, iCnt);
                                break;
                            }
                        case 0x61:
                            {
                                ExeError = RxPkt_ReadAbstract(SrcData, iCnt);
                                break;
                            }
                        case 0x62:
                            {
                                ExeError = RxPkt_Export(SrcData, iCnt);
                                break;
                            }
                        case 0x64:
                            {
                                ExeError = RxPkt_ReadCfgDetail(SrcData, iCnt);
                                break;
                            }
                        case 0x65:
                            {
                                ExeError = RxPkt_SetAppCfg(SrcData, iCnt);
                                break;
                            }
                        case 0x66:
                            {
                                ExeError = RxPkt_ReadCfgOfLsx(SrcData, iCnt);
                                break;
                            }
                        case 0x67:
                            {
                                ExeError = RxPkt_SetCfgOfLsx(SrcData, iCnt);
                                break;
                            }
                        case 0x68:
                            {
                                ExeError = RxPkt_SetFactoryCfg(SrcData, iCnt);
                                break;
                            }
                        case 0x69:
                            {
                                ExeError = RxPkt_DeleteAllData(SrcData, iCnt);
                                break;
                            }
                        case 0x6A:
                            {
                                ExeError = RxPkt_ReadBind(SrcData, iCnt);
                                break;
                            }
                        case 0x6B:
                            {
                                ExeError = RxPkt_AddOrDeleteBind(SrcData, iCnt);
                                break;
                            }
                        case 0x70:
                            {
                                ExeError = RxPkt_ReadCfgOfCC1101(SrcData, iCnt);
                                break;
                            }
                        case 0x71:
                            {
                                ExeError = RxPkt_SetCfgOfCC1101(SrcData, iCnt);
                                break;
                            }
                        case 0x72:
                            {
                                ExeError = RxPkt_ReadApn(SrcData, iCnt);
                                break;
                            }
                        case 0x73:
                            {
                                ExeError = RxPkt_SetApn(SrcData, iCnt);
                                break;
                            }
                        case 0x74:
                            {
                                ExeError = RxPkt_Ntp(SrcData, iCnt);
                                break;
                            }
                        case 0x75:
                            {
                                ExeError = RxPkt_ReadLayout(SrcData, iCnt);
                                break;
                            }
                        case 0x76:
                            {
                                ExeError = RxPkt_ReadLayoutUnit(SrcData, iCnt);
                                break;
                            }
                        case 0x77:
                            {
                                ExeError = RxPkt_ReadDeptCode(SrcData, iCnt);
                                break;
                            }
                        case 0x78:
                            {
                                ExeError = RxPkt_SetDeptCode(SrcData, iCnt);
                                break;
                            }
                        case 0x79:
                            {
                                ExeError = RxPkt_ReadWriteResetStartCalendar(SrcData, iCnt);
                                break;
                            }
                        case 0x7A:      // 恢复默认布局
                            {
                                ExeError = RxPkt_SetDeptCode(SrcData, iCnt);        // 此数据包与设置DeptCode的反馈数据包的格式一样
                                break;
                            }
                        case 0x7B:
                            {
                                ExeError = RxPkt_WriteKeyOfMd5(SrcData, iCnt);
                                break;
                            }
                        case 0x7C:
                            {
                                ExeError = RxPkt_ReadEthernet(SrcData, iCnt);
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                    if (ExeError < 0)
                    {
                        continue;
                    }

                    if (HandleLen > 0)
                    {
                        HandleLen--;        // 因为马上就要执行iCnt++
                    }

                    iCnt = (UInt16)(iCnt + HandleLen);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("处理接收数据包错误" + ex.Message);
                }
            }
            return 0;
        }

        private byte[] ReadCfgOfCC1101(SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[14];
            UInt16 TxLen = 0;

            // Start
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // Length
            TxBuf[TxLen++] = 0x00;

            // Cmd
            TxBuf[TxLen++] = 0x70;

            // Protocol
            TxBuf[TxLen++] = 0x02;

            // 保留
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // End
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            return helper.Send(TxBuf, 0, TxLen, 500);
        }

        /// <summary>
        /// 读取CC1101的配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadCfgOfCC1101_Click(object sender, RoutedEventArgs e)
        {
            if (cbDeviceList.SelectedIndex >= 0)
            {
                //目标：测试串口的打开，发送数据，在超时时间内接收数据
                SerialPortHelper helper = new SerialPortHelper();
                helper.IsLogger = true;
                helper.InitCOM(cbDeviceList.Text);

                try
                {
                    helper.OpenPort();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("错误：" + ex.Message);
                    return;
                }

                DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();

                try
                {
                    RxBuf = ReadCfgOfCC1101(helper);

                    RxPkt_Handle(RxBuf);
                }
                catch (Exception)
                {
                    MessageBox.Show("更新网关信息失败！");
                }

                System.Threading.Thread.Sleep(200);

                helper.Close();
            }
        }

        /// <summary>
        /// 修改CC1101的配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetCfgOfCC1101_Click(object sender, RoutedEventArgs e)
        {
            if (cbDeviceList.SelectedIndex >= 0)
            {
                //目标：测试串口的打开，发送数据，在超时时间内接收数据
                SerialPortHelper helper = new SerialPortHelper();
                helper.IsLogger = true;
                helper.InitCOM(cbDeviceList.Text);

                try
                {
                    helper.OpenPort();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("错误：" + ex.Message);
                    return;
                }

                DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();

                try
                {
                    byte[] TxBuf = new byte[24];
                    UInt16 TxLen = 0;

                    // Start
                    TxBuf[TxLen++] = 0xCB;
                    TxBuf[TxLen++] = 0xCB;

                    // Length
                    TxBuf[TxLen++] = 0x00;

                    // Cmd
                    TxBuf[TxLen++] = 0x71;

                    // Protocol
                    TxBuf[TxLen++] = 0x02;

                    // ON/OFF
                    TxBuf[TxLen++] = Convert.ToByte(tbxNewOnOff.Text);

                    // Bps
                    byte[] ByteBufTmp = MyCustomFxn.HexStringToByteArray(tbxNewBpsOfCC1101.Text);
                    if (ByteBufTmp == null || ByteBufTmp.Length < 1)
                    {
                        return;
                    }
                    TxBuf[TxLen++] = ByteBufTmp[0];

                    // Tx Power
                    TxBuf[TxLen++] = (byte)Convert.ToInt16(tbxNewTxPowerOfCC1101.Text);

                    // Customer
                    ByteBufTmp = MyCustomFxn.HexStringToByteArray(tbxNewCustomerOfCC1101.Text);
                    if (ByteBufTmp == null || ByteBufTmp.Length < 2)
                    {
                        return;
                    }
                    TxBuf[TxLen++] = ByteBufTmp[0];
                    TxBuf[TxLen++] = ByteBufTmp[1];

                    // Freq
                    UInt32 freq = Convert.ToUInt32(tbxNewFreqOfCC1101.Text);
                    TxBuf[TxLen++] = (byte)((freq & 0xFF000000) >> 24);
                    TxBuf[TxLen++] = (byte)((freq & 0x00FF0000) >> 16);
                    TxBuf[TxLen++] = (byte)((freq & 0x0000FF00) >> 8);
                    TxBuf[TxLen++] = (byte)((freq & 0x000000FF) >> 0);

                    // XT
                    TxBuf[TxLen++] = Convert.ToByte(tbxNewXtOfCC1101.Text);

                    // Reserved
                    ByteBufTmp = MyCustomFxn.HexStringToByteArray(tbxNewReservedOfCC1101.Text);
                    if (ByteBufTmp == null || ByteBufTmp.Length < 2)
                    {
                        return;
                    }
                    TxBuf[TxLen++] = ByteBufTmp[0];
                    TxBuf[TxLen++] = ByteBufTmp[1];

                    // CRC16
                    UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
                    TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
                    TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

                    // End
                    TxBuf[TxLen++] = 0xBC;
                    TxBuf[TxLen++] = 0xBC;

                    // 重写长度位
                    TxBuf[2] = (byte)(TxLen - 7);

                    RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

                    RxPkt_Handle(RxBuf);

                    // 修改之后，再读取一次

                    RxBuf = ReadCfgOfCC1101(helper);

                    RxPkt_Handle(RxBuf);
                }
                catch (Exception)
                {
                    MessageBox.Show("更新网关信息失败！");
                }

                System.Threading.Thread.Sleep(200);

                helper.Close();
            }
        }

        private void btnCopyCfg_Click(object sender, RoutedEventArgs e)
        {
            tbxNewOnOff.Text = tbxOnOff.Text;
            tbxNewBpsOfCC1101.Text = tbxBpsOfCC1101.Text;
            tbxNewTxPowerOfCC1101.Text = tbxTxPowerOfCC1101.Text;
            tbxNewCustomerOfCC1101.Text = tbxCustomerOfCC1101.Text;
            tbxNewFreqOfCC1101.Text = tbxFreqOfCC1101.Text;
            tbxNewXtOfCC1101.Text = tbxXtOfCC1101.Text;
            tbxNewReservedOfCC1101.Text = tbxReservedOfCC1101.Text;
        }

        private void btnClearCfg_Click(object sender, RoutedEventArgs e)
        {
            tbxOnOff.Text = "";
            tbxBpsOfCC1101.Text = "";
            tbxTxPowerOfCC1101.Text = "";
            tbxCustomerOfCC1101.Text = "";
            tbxFreqOfCC1101.Text = "";
            tbxXtOfCC1101.Text = "";
            tbxReservedOfCC1101.Text = "";
        }

        /// <summary>
        /// 绑定一个设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_AddOneBind(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[128];
            UInt16 TxLen = 0;

            byte[] ByteBuf = null;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x6B;

            // 协议版本
            TxBuf[TxLen++] = 0x02;

            // Cmd
            BindCmd = TxBuf[TxLen++] = 0x00;

            // Sensor MAC
            ByteBuf = MyCustomFxn.HexStringToByteArray(tbxMacOfBind.Text);
            if (ByteBuf == null || ByteBuf.Length < 4)
            {
                MessageBox.Show("请输入\"设备MAC号\"!");
                return -1;
            }
            TxBuf[TxLen++] = ByteBuf[0];
            TxBuf[TxLen++] = ByteBuf[1];
            TxBuf[TxLen++] = ByteBuf[2];
            TxBuf[TxLen++] = ByteBuf[3];

            // 配置长度
            TxBuf[TxLen++] = 0x00;

            double tempF = 0.0f;
            Int16 tempI = 0;

            // 温度阈值下限
            if (tbxTempThrLowOfBind.Text != "")
            {
                try
                {
                    tempF = Convert.ToDouble(tbxTempThrLowOfBind.Text);
                }
                catch
                {
                    MessageBox.Show("温度范围的\"下限\"有错误!");
                    return -2;
                }

                if (tempF < -273.14 || tempF > 327.67)
                {
                    MessageBox.Show("温度范围的\"下限\"有错误，不可超出-273.14~327.67的范围!");
                    return -12;
                }

                tempI = (Int16)(tempF * 100.0f);

                TxBuf[TxLen++] = 0x65;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = (byte)((tempI & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((tempI & 0x00FF) >> 0);
            }

            // 温度阈值上限
            if (tbxTempThrHighOfBind.Text != "")
            {
                try
                {
                    tempF = Convert.ToDouble(tbxTempThrHighOfBind.Text);
                }
                catch
                {
                    MessageBox.Show("温度范围的\"上限\"有错误!");
                    return -3;
                }

                if (tempF < -273.14 || tempF > 327.67)
                {
                    MessageBox.Show("温度范围的\"下限\"有错误，不可超出-273.14~327.67的范围!");
                    return -13;
                }

                tempI = (Int16)(tempF * 100.0f);

                TxBuf[TxLen++] = 0x65;
                TxBuf[TxLen++] = 0x01;
                TxBuf[TxLen++] = (byte)((tempI & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((tempI & 0x00FF) >> 0);
            }

            double humF = 0.0f;
            UInt16 humI = 0;

            // 湿度阈值下限
            if (tbxHumThrLowOfBind.Text != "")
            {
                try
                {
                    humF = Convert.ToDouble(tbxHumThrLowOfBind.Text);
                }
                catch
                {
                    MessageBox.Show("湿度范围的\"下限\"有错误!");
                    return -4;
                }

                if (humF < 0.0f || humF > 100.0f)
                {
                    MessageBox.Show("湿度范围的\"下限\"有错误，不可超出0.00~100.00的范围!");
                    return -14;
                }

                humI = (UInt16)(humF * 100.0f);

                TxBuf[TxLen++] = 0x66;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = (byte)((humI & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((humI & 0x00FF) >> 0);
            }

            // 湿度阈值上限
            if (tbxHumThrHighOfBind.Text != "")
            {
                try
                {
                    humF = Convert.ToDouble(tbxHumThrHighOfBind.Text);
                }
                catch
                {
                    MessageBox.Show("湿度范围的\"上限\"有错误!");
                    return -5;
                }

                if (humF < 0.0f || humF > 100.0f)
                {
                    MessageBox.Show("湿度范围的\"下限\"有错误，不可超出0.00~100.00的范围!");
                    return -15;
                }

                humI = (UInt16)(humF * 100.0f);

                TxBuf[TxLen++] = 0x66;
                TxBuf[TxLen++] = 0x01;
                TxBuf[TxLen++] = (byte)((humI & 0xFF00) >> 8);
                TxBuf[TxLen++] = (byte)((humI & 0x00FF) >> 0);
            }

            // 设备名称
            if (tbxNameOfBind.Text != "")
            {
                try
                {
                    ByteBuf = Encoding.GetEncoding("GB18030").GetBytes(tbxNameOfBind.Text);
                }
                catch
                {
                    MessageBox.Show("设备名称有错误!");
                    return -6;
                }

                if (ByteBuf != null && ByteBuf.Length > 0)
                {
                    if (ByteBuf.Length > 30)
                    {
                        MessageBox.Show("设备名称长度超出了限制!");
                        return -7;
                    }

                    TxBuf[TxLen++] = 0x73;
                    TxBuf[TxLen++] = (byte)ByteBuf.Length;
                    for (UInt16 iX = 0; iX < (byte)ByteBuf.Length; iX++)
                    {
                        TxBuf[TxLen++] = ByteBuf[iX];
                    }
                }
            }

            // 重写配置长度位
            TxBuf[10] = (byte)(TxLen - 11);

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("绑定一个失败:" + error.ToString("G"));
                return -1;
            }

            TxAndRx_ReadBind(sender, e, helper);

            return 0;
        }

        private void btnAddBind_Click(object sender, RoutedEventArgs e)
        {
            tbkResultOfBind.Text = "";
            OpenTxRxClose(sender, e, TxAndRx_AddOneBind);
        }

        /// <summary>
        /// 删除所有设备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_DeleteAllBind(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            TxAndRx_ReadBind(sender, e, helper);

            byte[] TxBuf = new byte[20];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x6B;

            // 协议版本
            TxBuf[TxLen++] = 0x02;

            // Cmd
            BindCmd = TxBuf[TxLen++] = 0x03;

            // Sensor MAC
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // 配置长度
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("删除所有设备失败:" + error.ToString("G"));
                return -1;
            }

            TxAndRx_ReadBind(sender, e, helper);

            return 0;
        }

        private void btnDeleteAllBind_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult dr = MessageBox.Show("确定删除全部绑定关系？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (dr == MessageBoxResult.OK)
            {
                tbkResultOfBind.Text = "";
                OpenTxRxClose(sender, e, TxAndRx_DeleteAllBind);
            }
        }

        /// <summary>
        /// 读取DeptCode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 ReadDeptCode(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[14];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x77;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("读取DeptCode失败:" + error.ToString("G"));
                return -1;
            }

            return 0;
        }

        private void btnReadDeptCode_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, ReadDeptCode);
        }

        /// <summary>
        /// 设置DeptCode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 SetDeptCode(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[20];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x78;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // DeptCode
            byte[] deptCode = System.Text.Encoding.Default.GetBytes(tbxNewDeptCode.Text);
            if (deptCode == null || deptCode.Length == 0)
            {
                for (UInt16 ix = 0; ix < 10; ix++)
                {
                    TxBuf[TxLen++] = 0x00;
                }
            }
            else if (deptCode.Length >= 10)
            {
                for (UInt16 ix = 0; ix < 10; ix++)
                {
                    TxBuf[TxLen++] = deptCode[ix];
                }
            }
            else
            {
                for (UInt16 ix = 0; ix < 10; ix++)
                {
                    if (ix < deptCode.Length)
                    {
                        TxBuf[TxLen++] = deptCode[ix];
                    }
                    else
                    {
                        TxBuf[TxLen++] = 0x00;
                    }
                }
            }         

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("设置DeptCode失败:" + error.ToString("G"));
                return -2;
            }

            return 0;
        }

        private Int16 TxAndRx_SetDeptCode(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {

            if (SetDeptCode(sender, e, helper) < 0)
            {

            }
            else
            {
                System.Threading.Thread.Sleep(200);
                ReadDeptCode(sender, e, helper);
            }

            return 0;
        }

        /// <summary>
        /// 修改DeptCode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateDeptCode_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_SetDeptCode);
        }


        private Int16 TxAndRx_ReadApn(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();

            byte[] TxBuf = deviceHelper.ReadApn();

            RxBuf = helper.Send(TxBuf, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("读取APN失败:" + error.ToString("G"));
                return -2;
            }

            return 0;
        }


        private void btnReadApn_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_ReadApn);
        }

        /// <summary>
        /// 设置Apn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 SetApn(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[210];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x73;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // APN
            byte[] ByteBufTmp = System.Text.Encoding.Default.GetBytes(tbxNewApn.Text);
            if (ByteBufTmp == null || ByteBufTmp.Length == 0)
            {
                MessageBox.Show("APN不可为空！");
                return -1;
            }

            if (ByteBufTmp.Length > 63)
            {
                MessageBox.Show("APN的长度超限！");
                return -2;
            }

            TxBuf[TxLen++] = (byte)ByteBufTmp.Length;

            for (UInt16 ix = 0; ix < ByteBufTmp.Length; ix++)
            {
                TxBuf[TxLen++] = ByteBufTmp[ix];
            }

            // Username
            ByteBufTmp = System.Text.Encoding.Default.GetBytes(tbxNewUserName.Text);
            if (ByteBufTmp.Length > 63)
            {
                MessageBox.Show("用户名的长度超限！");
                return -3;
            }

            if (ByteBufTmp == null || ByteBufTmp.Length == 0)
            {
                TxBuf[TxLen++] = 0;
            }
            else
            {
                TxBuf[TxLen++] = (byte)ByteBufTmp.Length;

                for (UInt16 ix = 0; ix < ByteBufTmp.Length; ix++)
                {
                    TxBuf[TxLen++] = ByteBufTmp[ix];
                }
            }

            // Password
            ByteBufTmp = System.Text.Encoding.Default.GetBytes(tbxNewPassword.Text);
            if (ByteBufTmp.Length > 63)
            {
                MessageBox.Show("密码的长度超限！");
                return -3;
            }

            if (ByteBufTmp == null || ByteBufTmp.Length == 0)
            {
                TxBuf[TxLen++] = 0;
            }
            else
            {
                TxBuf[TxLen++] = (byte)ByteBufTmp.Length;

                for (UInt16 ix = 0; ix < ByteBufTmp.Length; ix++)
                {
                    TxBuf[TxLen++] = ByteBufTmp[ix];
                }
            }

            // 协议保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("设置APN失败:" + error.ToString("G"));
                return -2;
            }

            return 0;
        }

        private Int16 TxAndRx_SetApn(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            if (SetApn(sender, e, helper) < 0)
            {

            }
            else
            {
                System.Threading.Thread.Sleep(200);
                TxAndRx_ReadApn(sender, e, helper);
            }

            return 0;
        }

        private void btnSetApn_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_SetApn);
        }     

        /// <summary>
        /// 读取布局
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ReadLayout(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[14];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x75;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 200);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("读取布局失败:" + error.ToString("G"));
                return -1;
            }

            SucReadLayout = true;

            return 0;
        }

        /// <summary>
        /// 读取布局元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ReadLayoutUnit(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {       
            byte[] TxBuf = new byte[14];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x76;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("读取布局内容失败:" + error.ToString("G"));
                return -1;
            }

            return 0;
        }

        bool SucReadLayout = false;             // 读取布局成功

        /// <summary>
        /// 读取布局
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadLayout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tbxLayout.Text = "";

                SucReadLayout = false;
                OpenTxRxClose(sender, e, TxAndRx_ReadLayout);

                if (SucReadLayout == true)
                {
                    OpenTxRxClose(sender, e, TxAndRx_ReadLayoutUnit);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnClearLayout_Click(object sender, RoutedEventArgs e)
        {
            tbxLayout.Text = "";
        }

        private void btnCopyLayout_Click(object sender, RoutedEventArgs e)
        {
            tbxNewLayout.Text = tbxLayout.Text;
        }

        /// <summary>
        /// 恢复默认布局
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ResetLayout(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[14];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x7A;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 200);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("恢复默认布局失败:" + error.ToString("G"));
                return -1;
            }

            SucReadLayout = true;

            return 0;
        }

        private void btnResetLayout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenTxRxClose(sender, e, TxAndRx_ResetLayout);

                btnReadLayout_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 导出数据时的预操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ReadAbstract(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[12];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x61;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 存储区域           
            TxBuf[TxLen++] = (byte) cbxSensorChannel.SelectedIndex;

            // 保留位
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 1000);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("查询有效数据失败:" + error.ToString("G"));
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// 导出数据时的预操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadAbstract_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_ReadAbstract);
        }

        /// <summary>
        /// 创建导出数据时的命令数据包
        /// </summary>
        /// <param name="IndexOfStart"></param>
        /// <param name="NbrOfRead"></param>
        /// <returns></returns>
        private byte[] NewExportTxBuf(UInt32 IndexOfStart, UInt32 NbrOfRead)
        {
            byte[] TxBuf = new byte[16];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x62;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 存储区域
            TxBuf[TxLen++] = DataArea;

            // 起始位置
            TxBuf[TxLen++] = (byte)((IndexOfStart & 0x00FF0000) >> 16);
            TxBuf[TxLen++] = (byte)((IndexOfStart & 0x0000FF00) >> 8);
            TxBuf[TxLen++] = (byte)((IndexOfStart & 0x000000FF) >> 0);

            // 读取数量
            TxBuf[TxLen++] = (byte)((NbrOfRead & 0x00FF0000) >> 16);
            TxBuf[TxLen++] = (byte)((NbrOfRead & 0x0000FF00) >> 8);
            TxBuf[TxLen++] = (byte)((NbrOfRead & 0x000000FF) >> 0);

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            return TxBuf;
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_Export(SerialPortHelper helper)
        {
            byte[] TxBuf;
            Int16 error;

            ForceExist = false;

            ReadCnt = 0;                                             // 已导出的数量

            UInt32 GroupCntThr = 0;

            if (ReadTotal < UnitOfRead)
            {
                GroupCntThr = 1;
                UnitOfRead = ReadTotal;
            }
            else if (0 == (ReadTotal % UnitOfRead))
            {
                GroupCntThr = ReadTotal / UnitOfRead;
            }
            else
            {
                GroupCntThr = (ReadTotal / UnitOfRead) + 1;
            }

            for (UInt32 iX = 0; iX < GroupCntThr; iX++)
            {
                if (ForceExist == true)
                {
                    break;
                }

                IndexOfStart = ReadBase + iX * UnitOfRead;

                TxBuf = NewExportTxBuf(IndexOfStart, UnitOfRead);

                RxBuf = helper.Send(TxBuf, 0, (UInt16)TxBuf.Length, ExportTimoutMs, "BC BC 09 62 01");

                GroupCnt = iX;      // 记录当前是导出的第几组，显示数据时会用到
                error = ThisThreadOutputOfExport(RxBuf);
                if (error < 0)
                {
                    MessageBox.Show("导出数据失败:" + error.ToString("G"));
                    return -1;
                }

                System.Threading.Thread.Sleep(8);
            }

            if (ForceExist == true)
            {
                ThisThreadStatusOfExport("终止导出：");
            }
            else
            {
                ThisThreadStatusOfExport("导出结束：");
            }

            return 0;
        }

        /// <summary>
        /// 为了保证数据导出的正常执行，将一些有影响的控件禁用，等执行完后，再解禁；
        /// </summary>
        void ProtectExport()
        {
            cbDeviceList.IsEnabled = false;
            btnConnectDevice.IsEnabled = false;

            btnReadAbstract.IsEnabled = false;
            tbxStartTimeOfData.IsEnabled = false;
            btnExport.IsEnabled = false;
            btnClearExport.IsEnabled = false;
            tbxStartIndexOfData.IsEnabled = false;
            tbxNumberOfData.IsEnabled = false;
            btnExportPart.IsEnabled = false;

            RadTabItemOfCfg.IsEnabled = false;
            RadTabItemOfInternalSensor.IsEnabled = false;
            RadTabItemOfSG6XCC1101.IsEnabled = false;
            RadTabItemOfBind.IsEnabled = false;
            RadTabItemOfLayout.IsEnabled = false;
        }

        /// <summary>
        /// 解禁
        /// </summary>
        void UnProtectExport()
        {
            cbDeviceList.IsEnabled = true;
            btnConnectDevice.IsEnabled = true;

            btnReadAbstract.IsEnabled = true;
            tbxStartTimeOfData.IsEnabled = true;
            btnExport.IsEnabled = true;
            btnClearExport.IsEnabled = true;
            tbxStartIndexOfData.IsEnabled = true;
            tbxNumberOfData.IsEnabled = true;
            btnExportPart.IsEnabled = true;

            RadTabItemOfCfg.IsEnabled = true;
            RadTabItemOfInternalSensor.IsEnabled = true;
            RadTabItemOfSG6XCC1101.IsEnabled = true;
            RadTabItemOfBind.IsEnabled = true;
            RadTabItemOfLayout.IsEnabled = true;
        }

        /// <summary>
        /// 子线程执行时，不断地将读取的数据显示主线程里的控件上
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate Int16 ThreadOutputEventHander(byte[] SrcData);
        public event ThreadOutputEventHander ThreadOutputEvent;

        public delegate void ThreadStatusEventHander(string text);
        public event ThreadStatusEventHander ThreadStatusEvent;

        public delegate void ThreadEndEventHander();
        public event ThreadEndEventHander ThreadEndEventOfExport;           // 线程结束后，需要解禁主线程某些控件的使用权          

        Thread ThisThreadOfExport;

        byte DataArea = 0;              // 数据的存储区域

        void ThisThreadEndOfExport()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                UnProtectExport();                                          // 需要解禁主线程某些控件的使用权
            }));
        }

        Int16 ThisThreadOutputOfExport(byte[] SrcData)
        {
            Int16 error = 0;

            Dispatcher.BeginInvoke(new Action(delegate
            {
                error = RxPkt_Handle(SrcData);

                tbkExportResult.Text = "正在导出：" + ReadCnt.ToString() + "/" + ReadTotal.ToString();
            }));

            return error;
        }

        void ThisThreadStatusOfExport(string text)
        {
            // 计算烧录所用的时间
            DateTime End = System.DateTime.Now;
            TimeSpan Diff = End.Subtract(Start);

            Dispatcher.BeginInvoke(new Action(delegate
            {
                tbkExportResult.Text = text + ReadCnt.ToString() + "/" + ReadTotal.ToString() + ", " + Diff.TotalSeconds.ToString("F0") + "秒";
            }));
        }

        /// <summary>
        /// 子线程的具体执行内容
        /// </summary>
        void ThisThreadOfExport_Start()
        {
            ThreadEndEventOfExport += ThisThreadEndOfExport;
            ThreadOutputEvent += ThisThreadOutputOfExport;
            ThreadStatusEvent += ThisThreadStatusOfExport;

            Start = System.DateTime.Now;           // 记录导出数据的开始时间

            // 从配置文件里读取
            UnitOfRead = Convert.ToUInt32(ConfigurationManager.AppSettings["ExportUnit"]);
            ExportTimoutMs = Convert.ToUInt16(ConfigurationManager.AppSettings["ExportTimeoutMs"]);

            OpenTxRxClose_inThread(ComText, TxAndRx_Export);

            // 通知主线程，子线程执行完毕
            if (ThreadEndEventOfExport != null)
            {
                ThreadEndEventOfExport();
            }
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveListAndSampleTime();

            // 判断是否有串口设备
            if (cbDeviceList.SelectedIndex < 0)
            {
                return;
            }

            if (ExpTotalOfExport == 0)
            {
                OpenTxRxClose(sender, e, TxAndRx_ReadAbstract);
            }

            // 读取的起始位置
            ReadBase = 0;

            // 需要读取的数量
            ReadTotal = ExpTotalOfExport;

            // 存储区域
            DataArea = (byte)cbxSensorChannel.SelectedIndex;

            tbkExportResult.Text = "开始导出数据";
            tbkExportResult.Foreground = new SolidColorBrush(Colors.Green);
            tbkExportResult.FontWeight = FontWeights.Bold;

            // 串行设备的COM端口名称，在子线程里会用到
            ComText = cbDeviceList.Text;

            // 保护子线程不被打断
            ProtectExport();

            // 创建线程
            ThisThreadOfExport = new Thread(ThisThreadOfExport_Start);
            ThisThreadOfExport.Name = "导出全部数据";

            ThisThreadOfExport.Start();
        }

        private void btnClearExport_Click(object sender, RoutedEventArgs e)
        {
            tbxStartTimeOfData.Text = "";
            tbkTotalOfData.Text = "";
            GridLineOfExport = 1;
            DataOfExport.Clear();
            tbkExportResult.Text = "";
        }

        public UInt32[] ExSensorIdBuf = new UInt32[16]; // 期望的Sensor ID，允许是多个
        UInt16 ExSensorIdLen = 0;                       // 期望的Sensor ID的数量

        DateTime SampleStart;                           // 采集时间的开始时间
        DateTime SampleEnd;                             // 采集时间的结束时间

        /// <summary>
        /// 保存Sensor MAC列表和采集时间段
        /// </summary>
        private void SaveListAndSampleTime()
        {
            // 更新期望的Sensor ID序列
            Array.Clear(ExSensorIdBuf, 0, ExSensorIdBuf.Length);
            ExSensorIdLen = 0;

            if (tbxSensorMacList.Text.Length > 0)
            {
                // 按照逗号分隔符将字符串拆分为多个子字符串
                tbxSensorMacList.Text = tbxSensorMacList.Text.Replace(" ", "");
                tbxSensorMacList.Text = tbxSensorMacList.Text.Replace("\r", "");
                tbxSensorMacList.Text = tbxSensorMacList.Text.Replace("\n", "");
                tbxSensorMacList.Text = tbxSensorMacList.Text.Replace("\t", "");
                string[] SID = tbxSensorMacList.Text.Split(',');

                foreach (string sid in SID)
                {
                    byte[] ExSensorIdByte = CommArithmetic.HexStringToByteArray(sid);
                    if (ExSensorIdByte != null && ExSensorIdByte.Length >= 4)
                    {
                        ExSensorIdBuf[ExSensorIdLen++] = (UInt32)(ExSensorIdByte[0] * 256 * 256 * 256 + ExSensorIdByte[1] * 256 * 256 + ExSensorIdByte[2] * 256 + ExSensorIdByte[3]);
                    }
                }
            }

            tbkListNum.Text = ExSensorIdLen.ToString("G");

            // 采集时间
            try
            {
                if (tbxSampleStart.Text == "")
                {
                    SampleStart = new DateTime(2017, 4, 1, 23, 59, 59);
                }
                else
                {
                    SampleStart = Convert.ToDateTime(tbxSampleStart.Text);
                }

                if (tbxSampleEnd.Text == "")
                {
                    SampleEnd = new DateTime(2099, 4, 1, 23, 59, 59);
                }
                else
                {
                    SampleEnd = Convert.ToDateTime(tbxSampleEnd.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        bool IsMyExpSensorId(UInt32 Id)
        {
            if (ExSensorIdLen == 0)
            {
                return true;
            }

            for (int i = 0; i < ExSensorIdLen; i++)
            {
                if (Id == ExSensorIdBuf[i])
                {
                    return true;
                }
            }

            return false;
        }

        bool IsMyExpSensorData(UInt32 Id, DateTime Sample)
        {
            bool IsExpId = IsMyExpSensorId(Id);
            if(IsExpId == false)
            {
                return false;
            }

            int dif = DateTime.Compare(Sample, SampleStart);
            if(dif < 0)
            {
                return false;
            }

            dif = DateTime.Compare(SampleEnd, Sample);
            if (dif < 0)
            {
                return false;
            }

            return true;
        }

        private void btnExportPart_Click(object sender, RoutedEventArgs e)
        {
            SaveListAndSampleTime();

            // 判断是否有串口设备
            if (cbDeviceList.SelectedIndex < 0)
            {
                return;
            }

            // 读取的起始位置
            ReadBase = Convert.ToUInt32(tbxStartIndexOfData.Text);

            // 需要读取的数量
            ReadTotal = Convert.ToUInt32(tbxNumberOfData.Text);

            // 存储区域
            DataArea = (byte)cbxSensorChannel.SelectedIndex;

            if (ReadTotal == 0)
            {
                return;
            }

            if (ExpTotalOfExport == 0)
            {
                OpenTxRxClose(sender, e, TxAndRx_ReadAbstract);
            }

            tbkExportResult.Text = "开始导出数据";
            tbkExportResult.Foreground = new SolidColorBrush(Colors.Green);
            tbkExportResult.FontWeight = FontWeights.Bold;

            // 串行设备的COM端口名称，在子线程里会用到
            ComText = cbDeviceList.Text;

            // 保护子线程不被打断
            ProtectExport();

            // 创建线程
            ThisThreadOfExport = new Thread(ThisThreadOfExport_Start);
            ThisThreadOfExport.Name = "导出部分数据";

            ThisThreadOfExport.Start();
        }

        private void btnTerminateExport_Click(object sender, RoutedEventArgs e)
        {
            ForceExist = true;
        }

        /// <summary>
        /// 在关闭主线程之前，先将所有的子线程终止掉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (ThisThreadOfExport != null)
            {
                ThisThreadOfExport.Abort();
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            tbxNewDeviceMac.Text = tbxDeviceMac.Text;
            tbxNewHwRevision.Text = tbxHwRevision.Text;

            tbxNewCustomer.Text = tbxCustomer.Text;
            tbxNewDebug.Text = tbxDebug.Text;
            tbxNewCategory.Text = tbxCategory.Text;
            tbxNewPattern.Text = tbxPattern.Text;
            tbxNewBps.Text = tbxBps.Text;
            tbxNewChannel.Text = tbxChannel.Text;
            tbxNewInterval.Text = tbxInterval.Text;
            tbxNewCarousel.Text = tbxCarousel.Text;
            tbxNewIntervalOfAlert.Text = tbxIntervalOfAlert.Text;
            tbxNewTransPolicy.Text = tbxTransPolicy.Text;
            tbxNewTimeSrc.Text = tbxTimeSrc.Text;
            tbxNewAlertWay.Text = tbxAlertWay.Text;
            tbxNewCriSrc.Text = tbxCriSrc.Text;
            tbxNewDisplayStyle.Text = tbxDisplayStyle.Text;
            tbxNewBrightMode.Text = tbxBrightMode.Text;
            tbxNewUse.Text = tbxUse.Text;
            tbxNewIntervalOfLocate.Text = tbxIntervalOfLocate.Text;
            tbxNewTxPower.Text = tbxTxPower.Text;
            tbxNewReserved.Text = tbxReserved.Text;
            tbxNewServerDomainM.Text = tbxServerDomainM.Text;
            tbxNewServerPortM.Text = tbxServerPortM.Text;
            tbxNewServerDomainS.Text = tbxServerDomainS.Text;
            tbxNewServerPortS.Text = tbxServerPortS.Text;

            tbxNewDeptCode.Text = tbxDeptCode.Text;

            tbxNewApn.Text = tbxApn.Text;
            tbxNewUserName.Text = tbxUserName.Text;
            tbxNewPassword.Text = tbxPassword.Text;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbxDeviceType.Text = "";
            tbxRstSrc.Text = "";
            tbxPrimaryMac.Text = "";
            tbxProtocol.Text = "";
            tbxSwRevisionM.Text = "";
            tbxSwRevisionS.Text = "";
            tbxCurrent.Text = "";
            tbxDeviceMac.Text = "";
            tbxHwRevision.Text = "";

            tbxToSendRam.Text = "";
            tbxToSendFlash_M.Text = "";
            tbxToSendFlash_S.Text = "";

            tbxCustomer.Text = "";
            tbxDebug.Text = "";
            tbxCategory.Text = "";
            tbxPattern.Text = "";
            tbxBps.Text = "";
            tbxChannel.Text = "";
            tbxInterval.Text = "";
            tbxCarousel.Text = "";
            tbxIntervalOfAlert.Text = "";
            tbxTransPolicy.Text = "";
            tbxTimeSrc.Text = "";
            tbxAlertWay.Text = "";
            tbxCriSrc.Text = "";
            tbxDisplayStyle.Text = "";
            tbxBrightMode.Text = "";
            tbxUse.Text = "";
            tbxIntervalOfLocate.Text = "";
            tbxTxPower.Text = "";
            tbxReserved.Text = "";
            tbxServerDomainM.Text = "";
            tbxServerPortM.Text = "";
            tbxServerDomainS.Text = "";
            tbxServerPortS.Text = "";

            tbxDeptCode.Text = "";

            tbxApn.Text = "";
            tbxUserName.Text = "";
            tbxPassword.Text = "";
        }

        byte SensorIx = 0;              // 本地传感器的编号，取值范围：0或1；

        /// <summary>
        /// 读取本地传感器的配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_ReadCfgOfLsx(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[16];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x66;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 传感器
            TxBuf[TxLen++] = SensorIx;

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 2000, "CB CB");

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("读取本地传感器的配置失败:" + error.ToString("G"));
                return -1;
            }

            if (aIntervalSensor.iX == 0)
            {
                InternalSernsor1.DataContext = aIntervalSensor;
            }
            else if (aIntervalSensor.iX == 1)
            {
                InternalSernsor2.DataContext = aIntervalSensor;
            }
            else
            {
                MessageBox.Show("本地传感器编号错误：" + aIntervalSensor.iX.ToString("G"));
            }

            return 0;
        }

        private void btnReadCfgOfLs1_Click(object sender, RoutedEventArgs e)
        {
            SensorIx = 0;
            OpenTxRxClose(sender, e, TxAndRx_ReadCfgOfLsx);
        }

        /// <summary>
        /// 修改本地传感器的配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_SetCfgOfLsx(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[64];
            UInt16 TxLen = 0;

            byte[] ByteBuf = null;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x67;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 传感器
            TxBuf[TxLen++] = SensorIx;

            // Sensor Mac
            if (SensorIx == 0)
            {
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxDeviceMacOfLs1.Text);
            }
            else
            {
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxDeviceMacOfLs2.Text);
            }
            if (ByteBuf == null || ByteBuf.Length < 4)
            {
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
            }
            else
            {
                TxBuf[TxLen++] = ByteBuf[0];
                TxBuf[TxLen++] = ByteBuf[1];
                TxBuf[TxLen++] = ByteBuf[2];
                TxBuf[TxLen++] = ByteBuf[3];
            }

            // On/Off
            if (SensorIx == 0)
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewRunOfLs1.Text);

            }
            else
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewRunOfLs2.Text);
            }

            // Debug
            if (SensorIx == 0)
            {
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewDebugOfLs1.Text);

            }
            else
            {
                ByteBuf = MyCustomFxn.HexStringToByteArray(tbxNewDebugOfLs2.Text);
            }
            if (ByteBuf == null || ByteBuf.Length < 2)
            {
                MessageBox.Show("Debug错误！");
                return -1;
            }
            TxBuf[TxLen++] = ByteBuf[0];
            TxBuf[TxLen++] = ByteBuf[1];

            // Category
            if (SensorIx == 0)
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewCategoryOfLs1.Text);

            }
            else
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewCategoryOfLs2.Text);
            }

            // Pattern
            if (SensorIx == 0)
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewPatternOfLs1.Text);
            }
            else
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewPatternOfLs2.Text);
            }

            // MaxLength
            if (SensorIx == 0)
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewMaxLengthOfLs1.Text);
            }
            else
            {
                TxBuf[TxLen++] = Convert.ToByte(tbxNewMaxLengthOfLs2.Text);
            }

            // SampleSend
            TxBuf[TxLen++] = 0x00;

            // 采集间隔
            UInt16 uValue = 0;
            if (SensorIx == 0)
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfLs1.Text);
            }
            else
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfLs2.Text);
            }
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 正常间隔
            if (SensorIx == 0)
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfNormalOfLs1.Text);
            }
            else
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfNormalOfLs2.Text);
            }
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 预警间隔
            if (SensorIx == 0)
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfWarnOfLs1.Text);
            }
            else
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfWarnOfLs2.Text);
            }
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 报警间隔
            if (SensorIx == 0)
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfAlertOfLs1.Text);
            }
            else
            {
                uValue = Convert.ToUInt16(tbxNewIntervalOfAlertOfLs2.Text);
            }
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 温度补偿
            double fValue = 0.0f;
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewTempCompensationOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewTempCompensationOfLs2.Text);
            }
            Int16 iValue = (Int16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((iValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((iValue & 0x00FF) >> 0);

            // 湿度补偿
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewHumCompensationOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewHumCompensationOfLs2.Text);
            }
            iValue = (Int16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((iValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((iValue & 0x00FF) >> 0);

            // 温度预警上限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewTempThrHighOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewTempThrHighOfLs2.Text);
            }
            iValue = (Int16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((iValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((iValue & 0x00FF) >> 0);

            // 温度预警下限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewTempThrLowOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewTempThrLowOfLs2.Text);
            }
            iValue = (Int16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((iValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((iValue & 0x00FF) >> 0);

            // 湿度预警上限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewHumThrHighOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewHumThrHighOfLs2.Text);
            }
            uValue = (UInt16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 湿度预警下限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewHumThrLowOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewHumThrLowOfLs2.Text);
            }
            uValue = (UInt16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 温度阈值上限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewTempThrHighOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewTempThrHighOfLs2.Text);
            }
            iValue = (Int16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((iValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((iValue & 0x00FF) >> 0);

            // 温度阈值下限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewTempThrLowOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewTempThrLowOfLs2.Text);
            }
            iValue = (Int16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((iValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((iValue & 0x00FF) >> 0);

            // 湿度阈值上限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewHumThrHighOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewHumThrHighOfLs2.Text);
            }
            uValue = (UInt16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 湿度阈值下限
            if (SensorIx == 0)
            {
                fValue = Convert.ToDouble(tbxNewHumThrLowOfLs1.Text);
            }
            else
            {
                fValue = Convert.ToDouble(tbxNewHumThrLowOfLs2.Text);
            }
            uValue = (UInt16)(fValue * 100.0f);
            TxBuf[TxLen++] = (byte)((uValue & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((uValue & 0x00FF) >> 0);

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 2000, "CB CB");

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("修改本地传感器的配置失败:" + error.ToString("G"));
                return -1;
            }

            return 0;
        }

        private void btnSetCfgOfLs1_Click(object sender, RoutedEventArgs e)
        {
            SensorIx = 0;
            OpenTxRxClose(sender, e, TxAndRx_SetCfgOfLsx);

            btnReadCfgOfLs1_Click(sender, e);
        }

        private void btnCopyOfLs1_Click(object sender, RoutedEventArgs e)
        {
            tbxNewRunOfLs1.Text = tbxRunOfLs1.Text;
            tbxNewDebugOfLs1.Text = tbxDebugOfLs1.Text;
            tbxNewCategoryOfLs1.Text = tbxCategoryOfLs1.Text;
            tbxNewPatternOfLs1.Text = tbxPatternOfLs1.Text;
            tbxNewMaxLengthOfLs1.Text = tbxMaxLengthOfLs1.Text;
            tbxNewIntervalOfLs1.Text = tbxIntervalOfLs1.Text;
            tbxNewIntervalOfNormalOfLs1.Text = tbxIntervalOfNormalOfLs1.Text;
            tbxNewIntervalOfWarnOfLs1.Text = tbxIntervalOfWarnOfLs1.Text;
            tbxNewIntervalOfAlertOfLs1.Text = tbxIntervalOfAlertOfLs1.Text;
            tbxNewTempCompensationOfLs1.Text = tbxTempCompensationOfLs1.Text;
            tbxNewHumCompensationOfLs1.Text = tbxHumCompensationOfLs1.Text;
            tbxNewTempThrLowOfLs1.Text = tbxTempThrLowOfLs1.Text;
            tbxNewTempThrHighOfLs1.Text = tbxTempThrHighOfLs1.Text;
            tbxNewHumThrLowOfLs1.Text = tbxHumThrLowOfLs1.Text;
            tbxNewHumThrHighOfLs1.Text = tbxHumThrHighOfLs1.Text;
        }

        private void btnClearOfLs1_Click(object sender, RoutedEventArgs e)
        {
            tbxDeviceMacOfLs1.Text = "";
            tbxSensorTypeOfLs1.Text = "";
            tbxSensorOnlineOfLs1.Text = "";
            tbxRunOfLs1.Text = "";
            tbxDebugOfLs1.Text = "";
            tbxCategoryOfLs1.Text = "";
            tbxPatternOfLs1.Text = "";
            tbxMaxLengthOfLs1.Text = "";
            tbxIntervalOfLs1.Text = "";
            tbxIntervalOfNormalOfLs1.Text = "";
            tbxIntervalOfWarnOfLs1.Text = "";
            tbxIntervalOfAlertOfLs1.Text = "";
            tbxTempCompensationOfLs1.Text = "";
            tbxHumCompensationOfLs1.Text = "";
            tbxTempOfLs1.Text = "";
            tbxTempThrLowOfLs1.Text = "";
            tbxTempThrHighOfLs1.Text = "";
            tbxHumOfLs1.Text = "";
            tbxHumThrLowOfLs1.Text = "";
            tbxHumThrHighOfLs1.Text = "";
        }

        private void btnReadCfgOfLs2_Click(object sender, RoutedEventArgs e)
        {
            SensorIx = 1;
            OpenTxRxClose(sender, e, TxAndRx_ReadCfgOfLsx);
        }

        private void btnCopyOfLs2_Click(object sender, RoutedEventArgs e)
        {
            tbxNewRunOfLs2.Text = tbxRunOfLs2.Text;
            tbxNewDebugOfLs2.Text = tbxDebugOfLs2.Text;
            tbxNewCategoryOfLs2.Text = tbxCategoryOfLs2.Text;
            tbxNewPatternOfLs2.Text = tbxPatternOfLs2.Text;
            tbxNewMaxLengthOfLs2.Text = tbxMaxLengthOfLs2.Text;
            tbxNewIntervalOfLs2.Text = tbxIntervalOfLs2.Text;
            tbxNewIntervalOfNormalOfLs2.Text = tbxIntervalOfNormalOfLs2.Text;
            tbxNewIntervalOfWarnOfLs2.Text = tbxIntervalOfWarnOfLs2.Text;
            tbxNewIntervalOfAlertOfLs2.Text = tbxIntervalOfAlertOfLs2.Text;
            tbxNewTempCompensationOfLs2.Text = tbxTempCompensationOfLs2.Text;
            tbxNewHumCompensationOfLs2.Text = tbxHumCompensationOfLs2.Text;
            tbxNewTempThrLowOfLs2.Text = tbxTempThrLowOfLs2.Text;
            tbxNewTempThrHighOfLs2.Text = tbxTempThrHighOfLs2.Text;
            tbxNewHumThrLowOfLs2.Text = tbxHumThrLowOfLs2.Text;
            tbxNewHumThrHighOfLs2.Text = tbxHumThrHighOfLs2.Text;
        }

        private void btnClearOfLs2_Click(object sender, RoutedEventArgs e)
        {
            tbxDeviceMacOfLs2.Text = "";
            tbxSensorTypeOfLs2.Text = "";
            tbxSensorOnlineOfLs2.Text = "";
            tbxRunOfLs2.Text = "";
            tbxDebugOfLs2.Text = "";
            tbxCategoryOfLs2.Text = "";
            tbxPatternOfLs2.Text = "";
            tbxMaxLengthOfLs2.Text = "";
            tbxIntervalOfLs2.Text = "";
            tbxIntervalOfNormalOfLs2.Text = "";
            tbxIntervalOfWarnOfLs2.Text = "";
            tbxIntervalOfAlertOfLs2.Text = "";
            tbxTempCompensationOfLs2.Text = "";
            tbxHumCompensationOfLs2.Text = "";
            tbxTempOfLs2.Text = "";
            tbxTempThrLowOfLs2.Text = "";
            tbxTempThrHighOfLs2.Text = "";
            tbxHumOfLs2.Text = "";
            tbxHumThrLowOfLs2.Text = "";
            tbxHumThrHighOfLs2.Text = "";
        }

        private void btnSetCfgOfLs2_Click(object sender, RoutedEventArgs e)
        {
            SensorIx = 1;
            OpenTxRxClose(sender, e, TxAndRx_SetCfgOfLsx);

            btnReadCfgOfLs2_Click(sender, e);
        }

        public byte[] ReadWriteResetStartCalendar(byte Cmd)
        {
            byte[] TxBuf = new byte[16];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x79;

            // 协议版本
            TxBuf[TxLen++] = 0x01;



            if (Cmd == 0)
            {   // 查询

                // Cmd
                TxBuf[TxLen++] = Cmd;

                // 无意义
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
            }
            else if (Cmd == 1 || Cmd == 2)
            {   // 设置
                if (cbxDeleteHistory.IsChecked == true)
                {
                    Cmd = 2;        // 需要立即删除本地的历史数据
                }
                else
                {
                    Cmd = 1;        // 不需要立即删除本地的历史数据
                }

                // Cmd
                TxBuf[TxLen++] = Cmd;

                DateTime ThisCalendar = System.DateTime.Now;

                // 开始时间
                if (cbxUseCurrentTime.IsChecked == true)
                {   // 使用当前时间                   
                    tbxNewStartCalendar.Text = ThisCalendar.ToString("yyyy-MM-dd HH:mm:ss");
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Year - 2000);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Month);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Day);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Hour);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Minute);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Second);
                }
                else
                {
                    ThisCalendar = Convert.ToDateTime(tbxNewStartCalendar.Text);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Year - 2000);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Month);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Day);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Hour);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Minute);
                    TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Second);
                }
            }
            else if (Cmd == 3)
            {   // 还原
                // Cmd
                TxBuf[TxLen++] = Cmd;

                // 无意义
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
            }
            else
            {   // 未定义
                // Cmd
                TxBuf[TxLen++] = Cmd;

                // 无意义
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
                TxBuf[TxLen++] = 0x00;
            }

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            // 判断长度是否正确
            if (TxBuf.Length != TxLen)
            {
                while (true) ;
            }

            return TxBuf;
        }

        private Int16 TxAndRx_ReadStartCalendar(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = ReadWriteResetStartCalendar(0);

            RxBuf = helper.Send(TxBuf, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("读取开始时间失败:" + error.ToString("G"));
                return -2;
            }

            return 0;
        }

        private void btnReadStartCalendar_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_ReadStartCalendar);
        }

        private Int16 TxAndRx_WriteStartCalendar(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = ReadWriteResetStartCalendar(1);

            RxBuf = helper.Send(TxBuf, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("设置开始时间失败:" + error.ToString("G"));
                return -2;
            }

            return 0;
        }

        private void btnSetStartCalendar_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_WriteStartCalendar);
        }

        /// <summary>
        /// 布局内容
        /// </summary>
        public struct T_LayoutTxt
        {
            public byte type;          // 布局类型
            public byte param;         // 布局参数
            public byte[] buf;         // 内容 
        };

        public void SetLayoutTxt(ref T_LayoutTxt lTxt, ref int lTxtLen, byte type, byte param, string iStr)
        {
            if (lTxtLen > 7)
            {
                MessageBox.Show("布局内容的最大数量是7，现在已经超过了限制!");
                return;
            }

            lTxt.type = type;
            lTxt.param = param;
            lTxt.buf = StringToByteBuf(iStr);
            if (lTxt.buf != null)
            {
                lTxtLen++;
            }
        }

        private void btnGenerateScript_Click(object sender, RoutedEventArgs e)
        {
            string[] RowText = tbxNewLayout.Text.Split('\n');
            if (RowText.Length > 32)
            {
                MessageBox.Show("最多可以设置32个布局元素，现在已经超出限制！");
                return;
            }

            // 检索生成布局格式和布局内容

            byte[,] LayoutForm = new byte[RowText.Length, 2];       // 布局元素
            int LayoutFormCnt = 0;                                  // 布局元素的数量

            T_LayoutTxt[] LayoutTxt = new T_LayoutTxt[7];           // 布局内容
            int LayoutTxtLen = 0;                                   // 布局内容的数量

            for (int iX = 0; iX < RowText.Length; iX++)
            {
                if (RowText[iX] == null || RowText[iX] == string.Empty)
                {
                    continue;
                }

                if (RowText[iX].StartsWith("\r") == true)
                {   // 空一行
                    LayoutForm[LayoutFormCnt, 0] = 0x01;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[标题]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x02;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;

                    string txt = GetText(RowText[iX], "[标题]");
                    if (txt != null)
                    {
                        SetLayoutTxt(ref LayoutTxt[LayoutTxtLen], ref LayoutTxtLen, 0x02, 0x00, txt);
                    }
                }
                else if (RowText[iX].StartsWith("[车辆信息]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x03;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;

                    string txt = GetText(RowText[iX], "[车辆信息]");
                    if (txt != null)
                    {
                        SetLayoutTxt(ref LayoutTxt[LayoutTxtLen], ref LayoutTxtLen, 0x03, 0x00, txt);
                    }
                }
                else if (RowText[iX].StartsWith("[网关MAC号和运输码]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x04;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[开始时间]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x05;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[结束时间]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x06;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[Sensor MAC和名称]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x09;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[温度列表") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x0A;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[送货人]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x0B;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[收货人]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x0C;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[送货单位]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x0D;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;

                    string txt = GetText(RowText[iX], "[送货单位]");
                    if (txt != null)
                    {
                        SetLayoutTxt(ref LayoutTxt[LayoutTxtLen], ref LayoutTxtLen, 0x0D, 0x00, txt);
                    }
                }
                else if (RowText[iX].StartsWith("[当前时间]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x0E;
                    LayoutForm[LayoutFormCnt, 1] = 0x00;
                    LayoutFormCnt++;
                }
                else if (RowText[iX].StartsWith("[固定字符串]") == true)
                {   //
                    LayoutForm[LayoutFormCnt, 0] = 0x0F;

                    // 查找参数
                    byte param = 0;
                    if (RowText[iX].StartsWith("[固定字符串][0]") == true)
                    {
                        param = 0;
                    }
                    else if (RowText[iX].StartsWith("[固定字符串][1]") == true)
                    {
                        param = 1;
                    }
                    else if (RowText[iX].StartsWith("[固定字符串][2]") == true)
                    {
                        param = 2;
                    }
                    else if (RowText[iX].StartsWith("[固定字符串][3]") == true)
                    {
                        param = 3;
                    }
                    else
                    {
                        MessageBox.Show("固定字符串的参数的取值范围是：0到3！");
                        return;
                    }

                    LayoutForm[LayoutFormCnt, 1] = param;
                    LayoutFormCnt++;

                    string txt = GetText(RowText[iX], "[固定字符串][" + param.ToString() + "]");
                    if (txt != null)
                    {
                        SetLayoutTxt(ref LayoutTxt[LayoutTxtLen], ref LayoutTxtLen, 0x0F, param, txt);
                    }
                }
                else
                {
                    continue;
                }                
            } // for

            // 显示结果

            // 格式
            tbxLayoutScript.Text = "";
            tbxLayoutScript.Text += "布局格式  ：\t\t\t" + CommArithmetic.ByteArrayToHexString(LayoutForm, 0, (UInt16)LayoutFormCnt);

            // 内容
            byte[] buf = new byte[3];
            for (int iX = 0; iX < LayoutTxtLen; iX++)
            {
                buf[0] = LayoutTxt[iX].type;
                buf[1] = LayoutTxt[iX].param;
                buf[2] = (byte)LayoutTxt[iX].buf.Length;

                tbxLayoutScript.Text += "\n布局内容" + iX.ToString() + "： ";

                if (LayoutTxt[iX].type == 0x02)
                {
                    tbxLayoutScript.Text += "[标题]\t";
                }
                else if (LayoutTxt[iX].type == 0x03)
                {
                    tbxLayoutScript.Text += "[车辆信息]\t";
                }
                else if (LayoutTxt[iX].type == 0x0D)
                {
                    tbxLayoutScript.Text += "[送货单位]\t";
                }
                else if (LayoutTxt[iX].type == 0x0F)
                {
                    tbxLayoutScript.Text += "[固定字符串" + LayoutTxt[iX].param.ToString() + "]";
                }
                else
                {
                    tbxLayoutScript.Text += "[未知]\t";
                }

                tbxLayoutScript.Text += "\t" + CommArithmetic.ByteArrayToHexString(buf) + " " + CommArithmetic.ByteArrayToHexString(LayoutTxt[iX].buf);
            }
        }

        /// <summary>
        /// 获取等号后面的内容，需要去掉第一个等号附近的空格，去掉末尾的"\r"
        /// </summary>
        /// <param name="iStr"></param>
        /// <returns></returns>
        public string GetText(string iStr, string StartStr)
        {
            if (iStr.Contains("=") == false)
            {
                return null;
            }

            // 去掉第一个等号附件的空格
            iStr = iStr.Replace(StartStr + " = ", StartStr + "=");
            iStr = iStr.Replace(StartStr + "= ", StartStr + "=");
            iStr = iStr.Replace(StartStr + " =", StartStr + "=");

            // 去除末尾的"\r"
            iStr = iStr.TrimEnd('\r');

            // 把类型和等号都删掉，留下等号后面的内容            
            return iStr.Replace(StartStr + "=", string.Empty);
        }

        public byte[] StringToByteBuf(string iStr)
        {
            if (iStr == "")
            {
                return null;
            }

            byte[] ByteBuf = null;

            try
            {
                ByteBuf = Encoding.GetEncoding("GB18030").GetBytes(iStr);
            }
            catch
            {
                MessageBox.Show("无法解析\"" + iStr + "\"!");
                return null;
            }

            if (ByteBuf == null || ByteBuf.Length == 0)
            {
                return null;                
            }

            if (ByteBuf.Length > 32)
            {
                MessageBox.Show("\"" + iStr + "\"，其长度超过了最大限制（32个字节或16个汉字）!");
                return null;
            }

            return ByteBuf;
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            if(tbkStartCalendar.Visibility == Visibility.Visible)
            {
                tbkStartCalendar.Visibility = Visibility.Hidden;
                tbxStartCalendar.Visibility = Visibility.Hidden;
                tbxNewStartCalendar.Visibility = Visibility.Hidden; 
                cbxUseCurrentTime.Visibility = Visibility.Hidden;
                cbxDeleteHistory.Visibility = Visibility.Hidden;
                btnReadStartCalendar.Visibility = Visibility.Hidden; 
                btnSetStartCalendar.Visibility = Visibility.Hidden;
            }
            else
            {
                tbkStartCalendar.Visibility = Visibility.Visible;
                tbxStartCalendar.Visibility = Visibility.Visible;
                tbxNewStartCalendar.Visibility = Visibility.Visible;
                cbxUseCurrentTime.Visibility = Visibility.Visible;
                cbxDeleteHistory.Visibility = Visibility.Visible;
                btnReadStartCalendar.Visibility = Visibility.Visible;
                btnSetStartCalendar.Visibility = Visibility.Visible;
            }
        }

        private void btnHideApn_Click(object sender, RoutedEventArgs e)
        {
            if (tbkApn.Visibility == Visibility.Visible)
            {
                tbkApn.Visibility = Visibility.Hidden;
                tbxApn.Visibility = Visibility.Hidden;
                tbxNewApn.Visibility = Visibility.Hidden;

                tbkUserName.Visibility = Visibility.Hidden;
                tbxUserName.Visibility = Visibility.Hidden;
                tbxNewUserName.Visibility = Visibility.Hidden;

                btnReadApn.Visibility = Visibility.Hidden;
                btnSetApn.Visibility = Visibility.Hidden;

                tbkPassword.Visibility = Visibility.Hidden;
                tbxPassword.Visibility = Visibility.Hidden;
                tbxNewPassword.Visibility = Visibility.Hidden;
            }
            else
            {
                tbkApn.Visibility = Visibility.Visible;
                tbxApn.Visibility = Visibility.Visible;
                tbxNewApn.Visibility = Visibility.Visible;

                tbkUserName.Visibility = Visibility.Visible;
                tbxUserName.Visibility = Visibility.Visible;
                tbxNewUserName.Visibility = Visibility.Visible;

                btnReadApn.Visibility = Visibility.Visible;
                btnSetApn.Visibility = Visibility.Visible;

                tbkPassword.Visibility = Visibility.Visible;
                tbxPassword.Visibility = Visibility.Visible;
                tbxNewPassword.Visibility = Visibility.Visible;
            }
        }

        private Int16 TxAndRx_WriteKeyOfMd5(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[28];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x7B;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // Key
            byte[] key = MyCustomFxn.HexStringToByteArray(tbxNewKeyOfMd5.Text);
            if (key == null || key.Length != 16)
            {
                MessageBox.Show("密钥格式错误！");
                return -1;
            }

            for (int ix = 0; ix < key.Length; ix++)
            {
                TxBuf[TxLen++] = key[ix];
            }

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("修改密钥失败:" + error.ToString("G"));
                return -2;
            }

            MessageBox.Show("修改密钥成功!");

            return 0;
        }

        private void btnWriteKeyOfMd5_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_WriteKeyOfMd5);
        }

        private void btnHideKeyOfMd5_Click(object sender, RoutedEventArgs e)
        {
            if (tbkLabeOfKeyOfMd5.Visibility == Visibility.Visible)
            {
                tbkLabeOfKeyOfMd5.Visibility = Visibility.Hidden;
                tbxNewKeyOfMd5.Visibility = Visibility.Hidden;
                btnWriteKeyOfMd5.Visibility = Visibility.Hidden;
            }
            else
            {
                tbkLabeOfKeyOfMd5.Visibility = Visibility.Visible;
                tbxNewKeyOfMd5.Visibility = Visibility.Visible;
                btnWriteKeyOfMd5.Visibility = Visibility.Visible;
            }
        }

        private void RadGridView1_SelectedCellsChanged(object sender, GridViewSelectedCellsChangedEventArgs e)
        {

        }

        private Int16 TxAndRx_ReadEthernet(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[12];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x7C;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("读取以太网的配置失败:" + error.ToString("G"));
                return -2;
            }

            return 0;
        }

        private void btnReadCfgOfEthernet_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_ReadEthernet);
        }

        private void btnCopyEthernet_Click(object sender, RoutedEventArgs e)
        {
            tbxNewDhcp.Text = tbxDhcp.Text;
            tbxNewIp.Text = tbxIp.Text;
            tbxNewSubnet.Text = tbxSubnet.Text;
            tbxNewGateway.Text = tbxGateway.Text;
        }

        private void btnClearEthernet_Click(object sender, RoutedEventArgs e)
        {
            tbxDhcp.Text = "";
            tbxIp.Text = "";
            tbxSubnet.Text = "";
            tbxGateway.Text = "";
            tbxPhyMac.Text = "";
        }

        private Int16 TxAndRx_WriteEthernet(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = new byte[24];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x7D;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            try
            {
                // DHCP
                TxBuf[TxLen++] = Convert.ToByte(tbxNewDhcp.Text);

                // IP
                byte[] byteBuf = MyCustomFxn.IpStr2ByteBuf(tbxNewIp.Text);
                if(byteBuf == null || byteBuf.Length != 4)
                {
                    MessageBox.Show("IP地址错误！");
                    return -1;
                }
                TxBuf[TxLen++] = byteBuf[0];
                TxBuf[TxLen++] = byteBuf[1];
                TxBuf[TxLen++] = byteBuf[2];
                TxBuf[TxLen++] = byteBuf[3];

                // Subnet
                byteBuf = MyCustomFxn.IpStr2ByteBuf(tbxNewSubnet.Text);
                if (byteBuf == null || byteBuf.Length != 4)
                {
                    MessageBox.Show("子网掩码错误！");
                    return -2;
                }
                TxBuf[TxLen++] = byteBuf[0];
                TxBuf[TxLen++] = byteBuf[1];
                TxBuf[TxLen++] = byteBuf[2];
                TxBuf[TxLen++] = byteBuf[3];

                // Gateway
                byteBuf = MyCustomFxn.IpStr2ByteBuf(tbxNewGateway.Text);
                if (byteBuf == null || byteBuf.Length != 4)
                {
                    MessageBox.Show("默认网关错误！");
                    return -3;
                }
                TxBuf[TxLen++] = byteBuf[0];
                TxBuf[TxLen++] = byteBuf[1];
                TxBuf[TxLen++] = byteBuf[2];
                TxBuf[TxLen++] = byteBuf[3];

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "; 输入参数错误！");
                return -100;
            }

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            RxBuf = helper.Send(TxBuf, 0, TxLen, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("修改以太网的配置失败:" + error.ToString("G"));
                return -2;
            }

            TxAndRx_ReadEthernet(sender, e, helper);

            return 0;
        }

        private void btnSetCfgOfEthernet_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_WriteEthernet);
        }

        private void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {         
            
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "XLS文件|*.xls|所有文件|*.*";
            saveDlg.FileName = "Export_" + DateTime.Now.ToString("yyyyMMdd_hhmmss");

            // 生成备注
            string Comment = string.Empty;
            Comment += "Gateway MAC: " + tbxDeviceMac.Text + "\n";
            Comment += "起始时间: " + tbxStartTimeOfData.Text + "\n";

            if (saveDlg.ShowDialog() == true)
            {
                ExportXLS export = new ExportXLS();
                export.ExportWPFDataGrid(DataGridOfExport, saveDlg.FileName, DataOfExport, Comment);
            }
            
        }

        private void btnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            tbxSensorMacList.Text = "";
            tbxSampleStart.Text = "2017-04-01 23:59:59";
            tbxSampleEnd.Text = "2099-04-01 23:59:59";
        }

        UInt16 NtpSerial = 0;           // 授时的序列号

        public byte[] NtpPacket()
        {
            byte[] TxBuf = new byte[19];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x74;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 序列号
            TxBuf[TxLen++] = (byte)((NtpSerial & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((NtpSerial & 0x00FF) >> 0);

            DateTime ThisCalendar = System.DateTime.Now;

            // 开始时间
            if (cbxUseCurrentTimeNtp.IsChecked == true)
            {   // 使用当前时间                   
                tbxNewNtpCalendar.Text = ThisCalendar.ToString("yyyy-MM-dd HH:mm:ss");
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Year - 2000);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Month);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Day);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Hour);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Minute);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Second);
            }
            else
            {
                ThisCalendar = Convert.ToDateTime(tbxNewNtpCalendar.Text);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Year - 2000);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Month);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Day);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Hour);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Minute);
                TxBuf[TxLen++] = MyCustomFxn.DecimalToBcd(ThisCalendar.Second);
            }

            // 协议保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            // 判断长度是否正确
            if (TxBuf.Length != TxLen)
            {
                while (true) ;
            }

            return TxBuf;
        }

        private Int16 TxAndRx_Ntp(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            byte[] TxBuf = NtpPacket();

            RxBuf = helper.Send(TxBuf, 500);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("授时失败:" + error.ToString("G"));
                return -2;
            }

            return 0;
        }

        private void btnSetNtpCalendar_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_Ntp);
        }

        private void btnHideNtp_Click(object sender, RoutedEventArgs e)
        {
            if (tbkNtpCalendar.Visibility == Visibility.Visible)
            {
                tbkNtpCalendar.Visibility = Visibility.Hidden;
                tbxNewNtpCalendar.Visibility = Visibility.Hidden;
                cbxUseCurrentTimeNtp.Visibility = Visibility.Hidden;
                btnSetNtpCalendar.Visibility = Visibility.Hidden;
            }
            else
            {
                tbkNtpCalendar.Visibility = Visibility.Visible;
                tbxNewNtpCalendar.Visibility = Visibility.Visible;
                cbxUseCurrentTimeNtp.Visibility = Visibility.Visible;
                btnSetNtpCalendar.Visibility = Visibility.Visible;
            }
        }

        private void btnHideDeptCode_Click(object sender, RoutedEventArgs e)
        {
            if (tbkLabelOfDeptCode.Visibility == Visibility.Visible)
            {
                tbkLabelOfDeptCode.Visibility = Visibility.Hidden;
                tbxDeptCode.Visibility = Visibility.Hidden;
                tbxNewDeptCode.Visibility = Visibility.Hidden;
                btnReadDeptCode.Visibility = Visibility.Hidden;
                btnUpdateDeptCode.Visibility = Visibility.Hidden;
            }
            else
            {
                tbkLabelOfDeptCode.Visibility = Visibility.Visible;
                tbxDeptCode.Visibility = Visibility.Visible;
                tbxNewDeptCode.Visibility = Visibility.Visible;
                btnReadDeptCode.Visibility = Visibility.Visible;
                btnUpdateDeptCode.Visibility = Visibility.Visible;
            }
        }

        private void btnHideFactory_Click(object sender, RoutedEventArgs e)
        {
            if (btnUpdateFactory.Visibility == Visibility.Visible)
            {
                btnUpdateFactory.Visibility = Visibility.Hidden;
            }
            else
            {
                btnUpdateFactory.Visibility = Visibility.Visible;
            }
        }

        bool NeedStopCheck = false;         // 是否需要停止检查

        private int GatewayCfg_isRight()
        {
            if (tbxSwRevisionM.Text != tbxNewSwRevisionM.Text)
            {
                return -1;
            }

            if (tbxSwRevisionS.Text != tbxNewSwRevisionS.Text)
            {
                return -2;
            }

            if (tbxHwRevision.Text != tbxNewHwRevision.Text)
            {
                return -3;
            }

            if (tbxCustomer.Text != tbxNewCustomer.Text)
            {
                return -4;
            }

            if (tbxDebug.Text != tbxNewDebug.Text)
            {
                return -5;
            }

            if (tbxCategory.Text != tbxNewCategory.Text)
            {
                return -6;
            }

            if (tbxPattern.Text != tbxNewPattern.Text)
            {
                return -7;
            }

            if (tbxBps.Text != tbxNewBps.Text)
            {
                return -8;
            }

            if (tbxChannel.Text != tbxNewChannel.Text)
            {
                return -9;
            }

            if (tbxInterval.Text != tbxNewInterval.Text)
            {
                return -10;
            }

            if (tbxCarousel.Text != tbxNewCarousel.Text)
            {
                return -11;
            }

            if (tbxIntervalOfAlert.Text != tbxNewIntervalOfAlert.Text)
            {
                return -12;
            }

            if (tbxTransPolicy.Text != tbxNewTransPolicy.Text)
            {
                return -13;
            }

            if (tbxTimeSrc.Text != tbxNewTimeSrc.Text)
            {
                return -14;
            }

            if (tbxAlertWay.Text != tbxNewAlertWay.Text)
            {
                return -15;
            }

            if (tbxCriSrc.Text != tbxNewCriSrc.Text)
            {
                return -16;
            }

            if (tbxDisplayStyle.Text != tbxNewDisplayStyle.Text)
            {
                return -17;
            }

            if (tbxBrightMode.Text != tbxNewBrightMode.Text)
            {
                return -18;
            }

            if (tbxUse.Text != tbxNewUse.Text)
            {
                return -19;
            }

            if (tbxIntervalOfLocate.Text != tbxNewIntervalOfLocate.Text)
            {
                return -20;
            }

            if (tbxTxPower.Text != tbxNewTxPower.Text)
            {
                return -21;
            }

            if (tbxReserved.Text != tbxNewReserved.Text)
            {
                return -22;
            }

            if (tbxServerDomainM.Text != tbxNewServerDomainM.Text)
            {
                return -23;
            }

            if (tbxServerPortM.Text != tbxNewServerPortM.Text)
            {
                return -24;
            }

            if (tbxServerDomainS.Text != tbxNewServerDomainS.Text)
            {
                return -25;
            }

            if (tbxServerPortS.Text != tbxNewServerPortS.Text)
            {
                return -26;
            }            

            return 0;
        }

        private void btnStartCheck_Click(object sender, RoutedEventArgs e)
        {
            btnStartCheck.IsEnabled = false;

            NeedStopCheck = false;
            ShowErMsg = false;

            while (NeedStopCheck == false)
            {
                System.Threading.Thread.Sleep(100);

                UpdateDeviceList();

                if (cbDeviceList.Text.Contains("Silicon Labs CP210x USB to UART Bridge") == false)
                {
                    continue;
                }

                btnClear_Click(sender, e);
                btnConnectDevice_Click(sender, e);

                if (tbxDeviceType.Text == string.Empty)
                {
                    continue;
                }

                if (GatewayCfg_isRight() >= 0)
                {
                    okPlayer.Play();
                }
                else
                {
                    ngPlayer.Play();
                }

                System.Threading.Thread.Sleep(1000);
            }

            ShowErMsg = true;
            btnStartCheck.IsEnabled = true;
        }

        private void btnStopCheck_Click(object sender, RoutedEventArgs e)
        {
            NeedStopCheck = true;
        }



        /*************************************/


    }
}
