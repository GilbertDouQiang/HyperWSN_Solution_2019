using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hyperwsn.SerialPortLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hyperwsn.Comm;

namespace Hyperwsn.SerialPortLibrary.Tests
{
    [TestClass()]
    public class SerialPortHelperTests
    {
        [TestMethod()]
        public void GetSerialPortsTest()
        {
            //从效率的角度考虑，计算耗时
            DateTime start = System.DateTime.Now;
            string[] comportStrings = SerialPortHelper.GetSerialPorts();
            DateTime end = System.DateTime.Now;
            TimeSpan span = end - start;
            double totalSpan = span.TotalMilliseconds;

            Assert.IsTrue(totalSpan < 150);


            Assert.AreEqual(1, comportStrings.Length);

        }

        [TestMethod()]
        public void SendTest()
        {
            //目标：测试串口的打开，发送数据，在超时时间内接收数据
            SerialPortHelper helper = new SerialPortHelper();
            //helper.Name = "COM11"; //TDD, 这里输入的可能是完整的名称
            //helper.InitCOM("Silicon Labs CP210x USB to UART Bridge (COM11)");
            helper.InitCOM("COM11");
            helper.OpenPort();
            string command = "CB CB 03 01 01 00 00 BC BC";
            byte[] commandBytes = CommArithmetic.HexStringToByteArray(command);
            byte[] result= helper.Send(commandBytes,300);
            Assert.IsTrue(result !=null);


            helper.Close();



        }

    }
}