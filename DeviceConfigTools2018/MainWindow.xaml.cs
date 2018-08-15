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

namespace DeviceConfigTools2018
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Gateway gateway;
        string gatewayFactoryID;
        //public InternalSensor interSensor1 { get; set; }

        InternalSensor it1;
        InternalSensor it2;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Config基础信息
            //1  读取版本号和标题，2 写入计算机名称，3 确定授权级别
            this.Title = ConfigurationManager.AppSettings["Title"] + " v" +
              System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //根据Key读取元素的Value
            UserInfo user = new UserInfo();
            string userAuth = user.GetUserInfo();
            //写入元素的Value
            config.AppSettings.Settings["LiceseName"].Value = userAuth;

            string liceseKey = Base64.base64encode(userAuth);


            //一定要记得保存，写不带参数的config.Save()也可以
            config.Save(ConfigurationSaveMode.Modified);

            UpdateDeviceList();
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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnectDevice_Click(object sender, RoutedEventArgs e)
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
                    commandBytes = deviceHelper.ReadGatewayBasic();
                    result = helper.Send(commandBytes, 500);

                    gateway = deviceHelper.GatewayInit(result);
                    //BindingDevice.DataContext = gateway;
                }
                catch (Exception)
                {

                    MessageBox.Show("读取网关基础配置失败！");
                    helper.Close();
                    return;
                }

                gatewayFactoryID = gateway.PrimaryMAC;


                System.Threading.Thread.Sleep(200);
                //读取网关基本信息结束


                try
                {
                    commandBytes = deviceHelper.CMDGatewayConfig();
                    result = helper.Send(commandBytes, 500);
                    gateway = deviceHelper.GatewayConfig(result);
                    gateway.PrimaryMAC = gatewayFactoryID;
                    //读取网关详细信息结束
                    GatewayDetail.DataContext = gateway;
                }
                catch (Exception)
                {

                    MessageBox.Show("读取网关详细配置失败！");
                    helper.Close();
                    return;
                }
                //读取网关详细信息


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
                    helper.Close();
                    return;
                }


                //MessageBox.Show(it1.DeviceMac);

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
                    helper.Close();
                    return;
                }





                helper.Close();
            }
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            //清除显示
            InternalSernsor1.DataContext = null;
            GatewayDetail.DataContext = null;
            gatewayFactoryID = null;

        }


        /// <summary>
        /// 更新网关基本设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateGateway_Click(object sender, RoutedEventArgs e)
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
                //配置1号传感器


                try
                {
                    byte[] commandBytes = deviceHelper.UpdateGateway(gateway);
                    byte[] result = helper.Send(commandBytes, 500);
                }
                catch (Exception)
                {

                    MessageBox.Show("更新网关信息失败！");
                }




                System.Threading.Thread.Sleep(200);

                helper.Close();

            }

            //刷新界面显示
            btnClearAll_Click(this, null);
            btnConnectDevice_Click(this, null);

            return;



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

                DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();
                //配置1号传感器


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

        /// <summary>
        /// 网关基本信息，保存到本地序列化的文件中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveGateway_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnLoadGateway_Click(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// 删除队列中的信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteFlash_Click(object sender, RoutedEventArgs e)
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
                    commandBytes = deviceHelper.DeleteQueue(0);
                    result = helper.Send(commandBytes, 500);

                    gateway = deviceHelper.GatewayInit(result);
                    //BindingDevice.DataContext = gateway;
                }
                catch (Exception)
                {

                    MessageBox.Show("读取网关基础配置失败！");
                    helper.Close();
                    return;
                }



                helper.Close();
                btnClearAll_Click(this, null);
                btnConnectDevice_Click(this, null);

            }
        }

        /// <summary>
        /// 读取并配置网关信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadGatewayBinding_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("c1");

            //GridViewBinding.ItemsSource = dt;

        }
    }
}
