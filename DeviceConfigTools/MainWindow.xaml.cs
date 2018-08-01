﻿using System;
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
        //public InternalSensor interSensor1 { get; set; }
        
        InternalSensor it1;

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


                //读取1号传感器信息
                commandBytes = deviceHelper.CMDGatewaySensorConfig(0);
                result = helper.Send(commandBytes, 500);

                it1 = deviceHelper.GatewaySensorConfig(result);

                InternalSernsor1.DataContext = it1;

                //MessageBox.Show(it1.DeviceMac);
                
                

                //读取2号传感器信息
                commandBytes = deviceHelper.CMDGatewaySensorConfig(1);
                result = helper.Send(commandBytes, 500);

                InternalSensor it2 = deviceHelper.GatewaySensorConfig(result);

                InternalSernsor2.DataContext = it2;
               


                helper.Close();
            }
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            //清除显示
            InternalSernsor1.DataContext = null;
            GatewayDetail.DataContext = null;

        }

        private void btnUpdateGateway_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(it1.ClientID);


          

        }

        private void btnUpdateSensor1_Click(object sender, RoutedEventArgs e)
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
                byte[] commandBytes = deviceHelper.UpdateInternalSersor(it1, 0x00);
                byte[] result = helper.Send(commandBytes, 500);



                System.Threading.Thread.Sleep(200);



                helper.Close();
            }
        }
    }
}
