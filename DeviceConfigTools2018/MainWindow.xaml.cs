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

namespace DeviceConfigTools2018
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate Int16 TxAndRxHandler(object sender, RoutedEventArgs e, SerialPortHelper helper);

        Gateway gateway;
        string gatewayFactoryID;

        InternalSensor it1;
        InternalSensor it2;

        System.Timers.Timer TimerOfBsl;
        UInt16 TimeCntOfBsl = 0;

        public MainWindow()
        {
            InitializeComponent();

            TimerOfBsl = new System.Timers.Timer(1000);
            TimerOfBsl.Elapsed += TimerEventOfBsl;
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

            //一定要记得保存，写不带参数的config.Save()也可以
            config.Save(ConfigurationSaveMode.Modified);

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

            // 打开串口
            SerialPortHelper helper = new SerialPortHelper();
            helper.IsLogger = true;
            helper.InitCOM(cbDeviceList.Text);
            try
            {
                helper.OpenPort();
            }
            catch (Exception)
            {
                MessageBox.Show("串口被占用或串口异常！");
                return;
            }

            // 发送命令，接收反馈
            if (aTxAndRxHandler != null)
            {
                try
                {
                    aTxAndRxHandler(sender, e, helper);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("TX And RX 过程异常：" + ex.Message);
                }
            }

            // 延时，然后关闭串口
            System.Threading.Thread.Sleep(100);
            helper.Close();
        }

        private void UpdateDeviceList()
        {
            string[] strDeviceList = SerialPortHelper.GetSerialPorts();
            cbDeviceList.Items.Clear();
            foreach (string item in strDeviceList)
            {
                cbDeviceList.Items.Add(item);
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

        private Int16 TxAndRx_Connect(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();

            byte[] commandBytes = null;
            byte[] result = null;

            // 读取网关基本信息
            try
            {
                commandBytes = deviceHelper.ReadGatewayBasic();
                result = helper.Send(commandBytes, 500);

                gateway = deviceHelper.GatewayInit(result);
                if(gateway == null)
                {
                    MessageBox.Show("读取网关基础配置失败！");
                    return -1;
                }

                gatewayFactoryID = gateway.PrimaryMacS;
            }
            catch (Exception)
            {
                MessageBox.Show("读取网关基础配置失败！");
                return -1;
            }

            System.Threading.Thread.Sleep(200);

            // 读取网关的配置
            try
            {
                if (gateway.ProtocolVersion == 1)
                {
                    commandBytes = deviceHelper.CMDGatewayConfig();
                }
                else
                {
                    commandBytes = deviceHelper.CMDGatewayConfigV2();
                }

                result = helper.Send(commandBytes, 500);

                if (gateway.ProtocolVersion == 1)
                {
                    gateway = deviceHelper.GatewayConfig(result);
                }
                else
                {
                    gateway = deviceHelper.GatewayConfigV2(result);
                }

                if(gateway == null)
                {
                    MessageBox.Show("读取网关详细配置失败！");
                    return -2;
                }

                gateway.PrimaryMacS = gatewayFactoryID;
                GatewayDetail.DataContext = gateway;

                if(gateway.WorkFunction == 7)
                {   // 龙邦甘肃，显示DeptCode
                    tbkLabelOfDeptCode.Visibility = Visibility.Visible;
                    tbkDeptCode.Visibility = Visibility.Visible;
                    tbxNewDeptCode.Visibility = Visibility.Visible;
                    btnReadDeptCode.Visibility = Visibility.Visible;
                    btnUpdateDeptCode.Visibility = Visibility.Visible;

                    ReadDeptCode(sender, e, helper);        // 读取DeptCode
                }
                else
                {
                    tbkLabelOfDeptCode.Visibility = Visibility.Hidden;
                    tbkDeptCode.Visibility = Visibility.Hidden;
                    tbxNewDeptCode.Visibility = Visibility.Hidden;
                    btnReadDeptCode.Visibility = Visibility.Hidden;
                    btnUpdateDeptCode.Visibility = Visibility.Hidden; 
                }
            }
            catch (Exception)
            {
                MessageBox.Show("读取网关详细配置失败！");
                return -2;
            }

            System.Threading.Thread.Sleep(100);

            // 读取通道1传感器的配置
            try
            {
                //读取1号传感器信息
                commandBytes = deviceHelper.CMDGatewaySensorConfig(0);
                result = helper.Send(commandBytes, 500);

                it1 = deviceHelper.GatewaySensorConfig(result);

                InternalSernsor1.DataContext = it1;
            }
            catch (Exception)
            {

                MessageBox.Show("读取内置传感器1 失败！");
                return -3;
            }

            System.Threading.Thread.Sleep(100);

            // 读取通道2传感器的配置
            try
            {
                //读取2号传感器信息
                commandBytes = deviceHelper.CMDGatewaySensorConfig(1);
                result = helper.Send(commandBytes, 500);

                it2 = deviceHelper.GatewaySensorConfig(result);
                InternalSernsor2.DataContext = it2;
            }
            catch (Exception)
            {
                MessageBox.Show("读取内置传感器2 失败！");
                return -4;
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
            OpenTxRxClose(sender, e, TxAndRx_Connect);
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            InternalSernsor1.DataContext = null;
            InternalSernsor2.DataContext = null;
            GatewayDetail.DataContext = null;
            gatewayFactoryID = null;
        }

        private Int16 TxAndRx_SetCfg(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();

            byte[] commandBytes = null;
            byte[] result = null;

            if (gateway == null)
            {
                helper.Close();
                return -1;
            }

            if (gateway.ProtocolVersion == 1)
            {
                commandBytes = deviceHelper.UpdateGateway(gateway);
                result = helper.Send(commandBytes, 500);
            }
            else
            {
                commandBytes = deviceHelper.UpdateGatewayV2(gateway);
                result = helper.Send(commandBytes, 500);
            }

            return 0;
        }

        /// <summary>
        /// 更新网关基本设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateGateway_Click(object sender, RoutedEventArgs e)
        {
            OpenTxRxClose(sender, e, TxAndRx_SetCfg);

            //刷新界面显示
            btnClearAll_Click(this, null);
            btnConnectDevice_Click(this, null);
        }

        private void btnUpdateSensor1_Click(object sender, RoutedEventArgs e)
        {
            updateInternalSensor(it1, 0);
            //刷新界面
            btnClearAll_Click(this, null);
            btnConnectDevice_Click(this, null);
        }

        private void btnUpdateSensor2_Click(object sender, RoutedEventArgs e)
        {
            updateInternalSensor(it2, 1);
            btnClearAll_Click(this, null);
            btnConnectDevice_Click(this, null);
        }

        /// <summary>
        /// 修改内置传感器参数
        /// </summary>
        /// <param name="sensor"></param>
        /// <param name="sensorNo"></param>
        /// <returns></returns>
        private int updateInternalSensor(InternalSensor sensor, byte sensorNo)
        {
            if (sensor == null)
            {

                MessageBox.Show("更新内置传感器" + sensorNo + "失败！");
                return -2;

            }
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

                    return -1;
                }

                DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();                //配置1号传感器

                try
                {
                    byte[] commandBytes = deviceHelper.UpdateInternalSersor(sensor, sensorNo);
                    byte[] result = helper.Send(commandBytes, 500);
                }
                catch (Exception)
                {
                    MessageBox.Show("更新内置传感器" + sensorNo + "失败！");
                    helper.Close();
                }

                System.Threading.Thread.Sleep(200);

                helper.Close();
            }
            return 0;
        }

        /// <summary>
        /// 控制网关的工厂信息，包括MAC和硬件版本号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateFactory_Click(object sender, RoutedEventArgs e)
        {
            if (gateway == null)
            {
                MessageBox.Show("更新内置传感器" + gateway + "失败！");
                return;
            }
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
                //配置1号传感器

                try
                {
                    byte[] commandBytes = deviceHelper.UpdateGatewayFactory(gateway);
                    byte[] result = helper.Send(commandBytes, 500);
                }
                catch (Exception)
                {
                    MessageBox.Show("更新内置传感器" + gateway + "失败！");
                    helper.Close();
                }

                System.Threading.Thread.Sleep(200);

                helper.Close();
            }

            //刷新界面
            btnClearAll_Click(this, null);
            btnConnectDevice_Click(this, null);

            return;
        }

        private Int16 TxAndRx_DeleteHistory(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();

            byte[] TxBuf = deviceHelper.DeleteQueue(0);
            byte[] RxBuf = helper.Send(TxBuf, 4000);

            gateway = deviceHelper.GatewayInit(RxBuf);

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

            btnClearAll_Click(this, null);
            btnConnectDevice_Click(this, null);

        }

        /// <summary>
        /// 读取绑定
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        private byte[] ReadBind(SerialPortHelper helper)
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

            return helper.Send(TxBuf, 0, TxLen, 500, true);
        }

        /// <summary>
        /// 读取绑定设备列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadGatewayBinding_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = new DataTable();

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
                    byte[] RxBuf = ReadBind(helper);

                    BindingAnalyse analyse = new BindingAnalyse();
                    dt = analyse.GatewayBinding(RxBuf);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("读取网关基础配置失败！" + ex.Message);
                    helper.Close();
                    return;
                }

                helper.Close();
            }

            RadGridView1.ItemsSource = dt;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteGatewayBinding_Click(object sender, RoutedEventArgs e)
        {
            if (RadGridView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择需要解除绑定的数据");
                return;
            }
            DeviceHelper helper = new DeviceHelper();


            //MessageBox.Show("Delete Items" + RadGridView1.SelectedItems.Count);
            if (RadGridView1.SelectedItems.Count == RadGridView1.Items.Count)
            {
                if (MessageBox.Show("解除所有绑定信息  ?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    //删除所有绑定信息
                    byte[] command = helper.RemoveBinding("00 00 00 00");
                    SendCommand(command);
                }

                return;
            }

            if (MessageBox.Show("解除 " + RadGridView1.SelectedItems.Count + " 个绑定信息  ?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var item in RadGridView1.SelectedItems)
                {
                    GridViewRow row = RadGridView1.ItemContainerGenerator.ContainerFromItem(item) as GridViewRow;
                    //MessageBox.Show(row.Cells[1].ToString());
                    var cell = row.Cells[1] as GridViewCell;
                    //删除所有绑定信息
                    byte[] command = helper.RemoveBinding(cell.Value.ToString());
                    SendCommand(command);
                }
            }
        }


        private void SendCommand(byte[] command)
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
                byte[] commandBytes;
                byte[] result;
                //读取网关基本信息
                try
                {
                    commandBytes = command;
                    result = helper.Send(commandBytes, 400);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("执行串口命令失败！" + ex.Message);
                    helper.Close();
                    return;
                }

                helper.Close();
            }
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
            if(txPower >= 128)
            {
                txPower -= 256;
            }
            tbxTxPowerOfCC1101.Text = txPower.ToString("D");
            iCnt += 1;

            tbxCustomer.Text = SrcData[iCnt].ToString("X2") + " " + SrcData[iCnt + 1].ToString("X2");
            iCnt += 2;

            if (Protocol == 2)
            {
                tbxFreqOfCC1101.Text = (SrcData[iCnt] * 256 * 256 * 256 + SrcData[iCnt + 1] * 256 * 256 + SrcData[iCnt + 2] * 256 + SrcData[iCnt + 3]).ToString("D");
                iCnt += 4;

                tbxXtOfCC1101.Text = SrcData[iCnt].ToString("D");
                iCnt += 1;
            }

            tbxReserved.Text = SrcData[iCnt].ToString("X2") + " " + SrcData[iCnt + 1].ToString("X2");
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
            if(Error != 0)
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
            if(LenOfApn > 63)
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
            tbkApn.Text = StrOfApn;
            tbxNewApn.Text = StrOfApn;

            tbkUserName.Text = StrOfUsername;
            tbxNewUserName.Text = StrOfUsername;

            tbkPassword.Text = StrOfPassword;
            tbxNewPassword.Text = StrOfPassword;

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
            if(Error > 0x80)
            {
                Error -= 0x100;
            }

            if(Error < 0)
            {
                return -3;
            }

            // DeptCode
            string DeptCode = System.Text.Encoding.UTF8.GetString(SrcData, IndexOfStart + 11, 10);
            tbkDeptCode.Text = DeptCode;
            tbxNewDeptCode.Text = DeptCode;

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
            Int16 State = SrcData[IndexOfStart + 10];
            if (State == 0)
            {
                return -3;      // 不支持BootLoader
            }

            if (State == 1)
            {
                return 0;       // 支持BootLoader
            }

            return -4;          // 其他错误
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
                        case 0x02:
                            {
                                ExeError = RxPkt_BootLoader(SrcData, iCnt);
                                break;
                            }
                        case 0x6A:
                            {
                                ExeError = RxPkt_ReadBind(SrcData, iCnt);
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

            return helper.Send(TxBuf, 0, TxLen, 500, true);
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
                    byte[] RxBuf = ReadCfgOfCC1101(helper);

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
                    TxBuf[TxLen++] = Convert.ToByte(tbxOnOffNew.Text);

                    // Bps
                    byte[] ByteBufTmp = MyCustomFxn.HexStringToByteArray(tbxBpsOfCC1101New.Text);
                    if (ByteBufTmp == null || ByteBufTmp.Length < 1)
                    {
                        return;
                    }
                    TxBuf[TxLen++] = ByteBufTmp[0];

                    // Tx Power
                    TxBuf[TxLen++] = (byte)Convert.ToInt16(tbxTxPowerOfCC1101New.Text);

                    // Customer
                    ByteBufTmp = MyCustomFxn.HexStringToByteArray(tbxCustomerNew.Text);
                    if (ByteBufTmp == null || ByteBufTmp.Length < 2)
                    {
                        return;
                    }
                    TxBuf[TxLen++] = ByteBufTmp[0];
                    TxBuf[TxLen++] = ByteBufTmp[1];

                    // Freq
                    UInt32 freq = Convert.ToUInt32(tbxFreqOfCC1101New.Text);
                    TxBuf[TxLen++] = (byte)((freq & 0xFF000000) >> 24);
                    TxBuf[TxLen++] = (byte)((freq & 0x00FF0000) >> 16);
                    TxBuf[TxLen++] = (byte)((freq & 0x0000FF00) >> 8);
                    TxBuf[TxLen++] = (byte)((freq & 0x000000FF) >> 0);

                    // XT
                    TxBuf[TxLen++] = Convert.ToByte(tbxXtOfCC1101New.Text);

                    // Reserved
                    ByteBufTmp = MyCustomFxn.HexStringToByteArray(tbxReservedNew.Text);
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

                    byte[] RxBuf = helper.Send(TxBuf, 0, TxLen, 500, true);

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
            tbxOnOffNew.Text = tbxOnOff.Text;
            tbxBpsOfCC1101New.Text = tbxBpsOfCC1101.Text;
            tbxTxPowerOfCC1101New.Text = tbxTxPowerOfCC1101.Text;
            tbxCustomerNew.Text = tbxCustomer.Text;
            tbxFreqOfCC1101New.Text = tbxFreqOfCC1101.Text;
            tbxXtOfCC1101New.Text = tbxXtOfCC1101.Text;
            tbxReservedNew.Text = tbxReserved.Text;
        }

        private void btnClearCfg_Click(object sender, RoutedEventArgs e)
        {
            tbxOnOff.Text = "";
            tbxBpsOfCC1101.Text = "";
            tbxTxPowerOfCC1101.Text = "";
            tbxCustomer.Text = "";
            tbxFreqOfCC1101.Text = "";
            tbxXtOfCC1101.Text = "";
            tbxReserved.Text = "";
        }

        private void btnAddBind_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeleteBind_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeleteAllBind_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeleteAndAddBind_Click(object sender, RoutedEventArgs e)
        {

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

            byte[] RxBuf = helper.Send(TxBuf, 0, TxLen, 500, true);

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
            if (deptCode == null || deptCode.Length < 10)
            {
                MessageBox.Show("DeptCode格式错误！");
                return -1;
            }

            for (UInt16 ix = 0; ix < 10; ix++)
            {
                TxBuf[TxLen++] = deptCode[ix];
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

            byte[] RxBuf = helper.Send(TxBuf, 0, TxLen, 500, true);

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

            byte[] RxBuf = helper.Send(TxBuf, 500);

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

            if(ByteBufTmp == null || ByteBufTmp.Length == 0)
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

            byte[] RxBuf = helper.Send(TxBuf, 0, TxLen, 500, true);

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
        /// 烧录过程中出错
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BootLoader_ErrorEvent(object sender, BootLoaderEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                tbxLoadStatus.Text += e.Message + "\r\n";
            }));
        }

        private void Proc_OutputStatusReceived(object sender, BootLoaderEventArgs e)
        {
            if (e.Message == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(delegate
            {
                tbxLoadStatus.Text += e.Message;

                // 使文本框一直显示在最后一行
                tbxLoadStatus.SelectionStart = tbxLoadStatus.Text.Length;
                tbxLoadStatus.SelectionLength = 0;
                tbxLoadStatus.ScrollToEnd();
            }));            
        }

        private void Proc_OutputRxLogReceived(object sender, BootLoaderEventArgs e)
        {
            if (e.Message == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(delegate
            {
                tbxLoadStatus2.Text += e.Message;

                // 使文本框一直显示在最后一行
                tbxLoadStatus2.SelectionStart = tbxLoadStatus2.Text.Length;
                tbxLoadStatus2.SelectionLength = 0;
                tbxLoadStatus2.ScrollToEnd();
            }));
        }

        string SelectedDeviceText = "";

        bool SupportBsl = false;

        /// <summary>
        /// 查询是否支持BootLoader，若支持，则开始BootLoader；
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        private Int16 TxAndRx_BootLoader(object sender, RoutedEventArgs e, SerialPortHelper helper)
        {
            SupportBsl = false;

            byte[] TxBuf = new byte[14];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x02;

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

            byte[] RxBuf = helper.Send(TxBuf, 0, TxLen, 500, true);

            Int16 error = RxPkt_Handle(RxBuf);
            if (error < 0)
            {
                MessageBox.Show("设备进入BootLoader失败:" + error.ToString("G"));
                return -1;
            }

            SupportBsl = true;
            tbxLoadStatus.Text += System.DateTime.Now.ToString("HH:mm:ss.fff") + "   允许升级！" + "\r\n";

            return 0;
        }

        /// <summary>
        /// 处理线程结束的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ThreadEndEventHander();
        public event ThreadEndEventHander ThreadEndEvent;               // 线程结束

        Thread ThisThread;

        void ThisThreadEnd()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                UnProtectBootLoader();
            }));
        }

        bool BootLoading = false;               // 表示烧录是否正在进行

        /// <summary>
        /// 为了保证BootLoader的正常执行，将一些有影响的控件禁用，等执行完后，再解禁；
        /// </summary>
        void ProtectBootLoader()
        {
            cbDeviceList.IsEnabled = false;
            btnBrowse.IsEnabled = false;
            btnLoadImage.IsEnabled = false;
            cbxOnlyLoadImage.IsEnabled = false;
            btnConnectDevice.IsEnabled = false;

            RadTabItemOfCfg.IsEnabled = false;
            RadTabItemOfInternalSensor.IsEnabled = false;
            RadTabItemOfSG6XCC1101.IsEnabled = false;
            RadTabItemOfBind.IsEnabled = false;

            BootLoading = true;

            TimeCntOfBsl = 0;
            tbkCount.Text = TimeCntOfBsl.ToString();

            TimerOfBsl.Enabled = true;          // 启动计时器
        }

        /// <summary>
        /// 解禁
        /// </summary>
        void UnProtectBootLoader()
        {
            cbDeviceList.IsEnabled = true;
            btnBrowse.IsEnabled = true;
            btnLoadImage.IsEnabled = true;
            cbxOnlyLoadImage.IsEnabled = true;
            btnConnectDevice.IsEnabled = true;

            RadTabItemOfCfg.IsEnabled = true;
            RadTabItemOfInternalSensor.IsEnabled = true;
            RadTabItemOfSG6XCC1101.IsEnabled = true;
            RadTabItemOfBind.IsEnabled = true;

            BootLoading = false;

            TimerOfBsl.Enabled = false;             // 停止计时器
        }

        void ThisThreadStart()
        {
            ThreadEndEvent += ThisThreadEnd;

            DateTime Start = System.DateTime.Now;           // 记录烧录的开始时间

            BootLoader Bsl = new BootLoader();

            Bsl.OutputRxLogEvent += Proc_OutputRxLogReceived;
            Bsl.OutputStatusEvent += Proc_OutputStatusReceived;

            Bsl.SerialDevice = SelectedDeviceText;
            Bsl.FileNameOfImage = ofd.FileName;

            Bsl.BSL_Scripter(45, Start);

            // 通知主线程，子线程执行完毕
            if(ThreadEndEvent != null)
            {
                ThreadEndEvent();
            }
        }

        void TimerEventOfBsl(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (BootLoading == false)
                {   // 烧录结束
                    TimerOfBsl.Enabled = false;
                }
                else
                {
                    TimeCntOfBsl++;
                    tbkCount.Text = TimeCntOfBsl.ToString();
                }
            }));
        }

        /// <summary>
        /// 烧录文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            if (cbDeviceList.SelectedIndex < 0)
            {
                MessageBox.Show("请选择串行设备！");
                return;
            }

            if (ofd.FileName == "")
            {
                MessageBox.Show("请选择烧录文件！");
                return;
            }
            else if (ofd.SafeFileName.Contains(".txt") == false)
            {
                MessageBox.Show("请选择.txt格式的烧录文件！");
                return;
            }

            ProtectBootLoader();

            tbxLoadStatus.Text = "";
            tbxLoadStatus2.Text = "";

            try
            {
                 SelectedDeviceText = cbDeviceList.Text;

                // 创建线程
                ThisThread = new Thread(ThisThreadStart);
                ThisThread.Name = "烧录程序";
                do
                {
                    if (cbxOnlyLoadImage.IsChecked == false)
                    {   // 需要查询
                        OpenTxRxClose(sender, e, TxAndRx_BootLoader);
                        if (SupportBsl == false)
                        {
                            UnProtectBootLoader();
                            break;
                        }
                    }

                    ThisThread.Start();

                } while (false);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {           
            if (ofd.ShowDialog() == true)
            {
                tbxLoadImage.Text = ofd.SafeFileName;
            }
        }

        private void btnClearLoadStatus_Click(object sender, RoutedEventArgs e)
        {
            tbxLoadStatus.Text = "";
            tbxLoadStatus2.Text = "";
        }
    }
}
