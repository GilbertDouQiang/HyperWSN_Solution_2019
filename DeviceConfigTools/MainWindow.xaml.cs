using System;
using System.Collections.Generic;
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
using Hyperwsn.SerialPortLibrary;
using Hyperwsn.Comm;
using Hyperwsn.Protocol;

namespace DeviceConfigTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Gateway gateway;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            if (strDeviceList!=null && strDeviceList.Length>0)
            {
                cbDeviceList.SelectedIndex = 0;
            }
        }

      

        private void btnRefersh_Click(object sender, RoutedEventArgs e)
        {
            UpdateDeviceList();
        }

        private void btnConnectDevice_Click(object sender, RoutedEventArgs e)
        {
            if (cbDeviceList.SelectedIndex >=0)
            {
                //目标：测试串口的打开，发送数据，在超时时间内接收数据
                SerialPortHelper helper = new SerialPortHelper();
                //helper.Name = "COM11"; //TDD, 这里输入的可能是完整的名称
                //helper.InitCOM("Silicon Labs CP210x USB to UART Bridge (COM11)");
                helper.InitCOM(cbDeviceList.Text);
                helper.OpenPort();
                DeviceHelper deviceHelper = new Hyperwsn.Protocol.DeviceHelper();
                //读取网关基本信息
                byte[] commandBytes = deviceHelper.ReadGatewayBasic();
                byte[] result = helper.Send(commandBytes, 500);

                gateway = deviceHelper.GatewayInit(result);
                BindingDevice.DataContext = gateway;

                System.Threading.Thread.Sleep(200);
                //读取网关基本信息结束


                //读取网关详细信息
                commandBytes = deviceHelper.CMDGatewayConfig();
                result = helper.Send(commandBytes, 500);
                gateway = deviceHelper.GatewayConfig(result);

                //读取网关详细信息结束
                GatewayDetail.DataContext = gateway;


                //读取传感器信息






                helper.Close();
            }
        }
    }
}
