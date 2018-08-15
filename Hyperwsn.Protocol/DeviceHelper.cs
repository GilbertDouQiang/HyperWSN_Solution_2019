using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperwsn.Comm;

namespace Hyperwsn.Protocol
{
    public class DeviceHelper
    {


        /// <summary>
        /// 查询网关基本信息
        /// </summary>
        /// <param name="requestBytes"></param>
        /// <returns></returns>
        public Gateway GatewayInit(byte[] requestBytes)
        {

            //BC BC 15 01 01 60 40 00 02 80 FF FF FF FF B8 0F 00 00 00 00 00 00 00 00 18 4C CB CB 
            Gateway gate = new Gateway();
            if (requestBytes != null && requestBytes.Length == 32)
            {
                //发现SG5
                try
                {

                    if (requestBytes[5] == 0x60)
                    {
                        gate.DeviceID = "SG5";
                    }
                    gate.PrimaryMAC = CommArithmetic.DecodeMAC(requestBytes, 6);
                    gate.DeviceMac = CommArithmetic.DecodeMAC(requestBytes, 10);
                    gate.HardwareVersion = CommArithmetic.DecodeMAC(requestBytes, 14);
                    gate.SoftwareVersion = CommArithmetic.DecodeClientID(requestBytes, 18);



                }
                catch (Exception)
                {

                    return null;
                }
            }


            return gate;
        }

        /// <summary>
        /// 获取网关的基础信息的命令
        /// 
        /// 协议描述：
        /// 查询网关的基本信息
        /// </summary>
        /// <returns></returns>
        public byte[] ReadGatewayBasic()
        {
            string command = "CB CB 02 01 01 00 00 BC BC";
            return CommArithmetic.HexStringToByteArray(command);

        }

        /// <summary>
        /// 删除队列中的信息
        /// </summary>
        /// <returns></returns>
        public byte[] DeleteQueue(int QueueNumber)
        {
            string command = "CB CB 06 6A 01 00 00 00 00 00 00 BC BC";
            return CommArithmetic.HexStringToByteArray(command);

        }



        /// <summary>
        /// 查询网关的配置，详细配置
        /// </summary>
        /// <returns></returns>
        public byte[] CMDGatewayConfig()
        {
            string command = "CB CB 02 64 01 00 00 BC BC";
            return CommArithmetic.HexStringToByteArray(command);
        }


        public Gateway GatewayConfig(byte[] requestBytes)
        {
            //有效性验证
            if (requestBytes == null)
            {
                return null;
            }

            if (requestBytes.Length < 5)
            {
                return null;
            }

            int length = requestBytes[2] + 7;

            if (requestBytes.Length != length)
            {
                return null;
            }

            Gateway gateway = new Gateway();
            gateway.ProtocolVersion = requestBytes[4];


            if (requestBytes[5] == 0x60)
            {
                gateway.DeviceID = "SG5";
            }
            gateway.DeviceMac = CommArithmetic.DecodeMAC(requestBytes, 6);
            gateway.HardwareVersion = CommArithmetic.DecodeMAC(requestBytes, 10);
            gateway.SoftwareVersion = CommArithmetic.DecodeClientID(requestBytes, 14);
            gateway.SoftwareVersion2 = CommArithmetic.DecodeClientID(requestBytes, 16);
            gateway.ClientID = CommArithmetic.DecodeClientID(requestBytes, 18);
            gateway.Debug = CommArithmetic.DecodeClientID(requestBytes, 20);
            gateway.Category = requestBytes[22];
            gateway.Interval = CommArithmetic.Bytes2Int(requestBytes, 23, 2);
            //v3.0从这里开始改变
            gateway.RTC = CommArithmetic.DecodeDateTime(requestBytes, 25);

            gateway.WorkFunction = requestBytes[31];
            gateway.SymbolRate = requestBytes[32];
            gateway.Frequency = requestBytes[33];
            gateway.DisplayInterval = CommArithmetic.Bytes2Int(requestBytes, 34, 2);
            gateway.AlarmInterval = CommArithmetic.Bytes2Int(requestBytes, 36, 2);
            gateway.TransStrategy = requestBytes[38];
            gateway.DateTimeStrategy = requestBytes[39];
            gateway.AlarmStyle = requestBytes[40];
            gateway.AlarmSource = requestBytes[41];
            gateway.DisplayStyle = requestBytes[42];
            gateway.BackgroundLight = requestBytes[43];
            gateway.GPSStart = requestBytes[44];

            gateway.LasRestart = requestBytes[49];
            gateway.RAMCountHigh = requestBytes[50];
            gateway.RAMCountLow = requestBytes[51];
            gateway.FlashCountHigh = CommArithmetic.Bytes2Int(requestBytes, 52, 3);
            gateway.FlashCountLow = CommArithmetic.Bytes2Int(requestBytes, 55, 3);

            byte domainLength = requestBytes[58];

            gateway.TargetDomain = CommArithmetic.DecodeByte2String(requestBytes, 59, domainLength);
            gateway.TargetPort = CommArithmetic.Bytes2Int(requestBytes, 59+domainLength, 2);












            return gateway;

        }

        /// <summary>
        /// 查询网关的配置，详细配置
        /// </summary>
        /// <returns></returns>
        public byte[] CMDGatewaySensorConfig(int Number)
        {
            string command;
            if (Number == 0)
            {
                command = "CB CB 06 66 01 00 00 00 00 00 00 BC BC";
            }
            else if (Number == 1)
            {
                command = "CB CB 06 66 01 01 00 00 00 00 00 BC BC";
            }
            else
            {
                command = "";
            }

            return CommArithmetic.HexStringToByteArray(command);
        }


        public InternalSensor GatewaySensorConfig(byte[] requestBytes)
        {

            InternalSensor it1 = new InternalSensor();
            it1.DeviceMac = CommArithmetic.DecodeMAC(requestBytes, 7); //read only
            it1.SensorType = requestBytes[11]; //read only
            it1.SensorOnline = requestBytes[12]; //read only
            //是否启用
            it1.Status = requestBytes[13];

            it1.Debug = CommArithmetic.DecodeClientID(requestBytes, 14);
            it1.Category = requestBytes[16];
            it1.WorkFunction = requestBytes[17];
            it1.MaxLength = requestBytes[18];
            it1.TXTimers = requestBytes[19];

            it1.Interval = CommArithmetic.Bytes2Int(requestBytes, 20, 2);
            it1.IntervalNormal = CommArithmetic.Bytes2Int(requestBytes, 22, 2);
            it1.IntervalWarning = CommArithmetic.Bytes2Int(requestBytes, 24, 2);
            it1.IntervalAlarm = CommArithmetic.Bytes2Int(requestBytes, 26, 2);
            //温湿度补偿
            it1.TemperatureCompensation = CommArithmetic.DecodeTemperature(requestBytes, 28);
            it1.HumidityCompensation = CommArithmetic.DecodeHumidity(requestBytes, 30);
            //预警值设置
            it1.TemperatureInfoHigh = CommArithmetic.DecodeTemperature(requestBytes, 32);
            it1.TemperatureInfoLow = CommArithmetic.DecodeTemperature(requestBytes, 34);
            it1.HumidityInfoHigh = CommArithmetic.DecodeHumidity(requestBytes, 36);
            it1.HumidityInfoLow = CommArithmetic.DecodeHumidity(requestBytes, 38);
            //报警值设置
            it1.TemperatureWarnHigh = CommArithmetic.DecodeTemperature(requestBytes, 40);
            it1.TemperatureWarnLow = CommArithmetic.DecodeTemperature(requestBytes, 42);
            it1.HumidityWarnHigh = CommArithmetic.DecodeHumidity(requestBytes, 44);
            it1.HumidityWarnLow = CommArithmetic.DecodeHumidity(requestBytes, 46);


            it1.ICTemperature = requestBytes[52]; //read only
            it1.Volt = CommArithmetic.DecodeVoltage(requestBytes, 53); //read only
            it1.FlashFront = CommArithmetic.Bytes2Int(requestBytes, 55, 3); //read only
            it1.FlashRear = CommArithmetic.Bytes2Int(requestBytes, 58, 3); //read only
            it1.FlashQueueLength = CommArithmetic.Bytes2Int(requestBytes, 61, 3); //read only
            it1.Temperature = CommArithmetic.DecodeTemperature(requestBytes, 64); //read only
            it1.Humidity = CommArithmetic.DecodeHumidity(requestBytes, 66); //read only











            return it1;
        }

        public byte[] UpdateInternalSersor(InternalSensor sensor, byte SensorNo)
        {
            byte[] response = new byte[53];
            response[0] = 0xCB;
            response[1] = 0xCB;
            response[2] = 0x2E;
            response[3] = 0x67;
            response[4] = 0x01;
            response[5] = SensorNo;
            //devicemac 
            byte[] temp = CommArithmetic.HexStringToByteArray(sensor.DeviceMac);
            response[6] = temp[0];
            response[7] = temp[1];
            response[8] = temp[2];
            response[9] = temp[3];
            //new status //是否启用
            response[10] = sensor.Status;

            //debug
            temp = CommArithmetic.HexStringToByteArray(sensor.Debug);
            response[11] = temp[0];
            response[12] = temp[1];
            //category
            response[13] = sensor.Category;
            response[14] = sensor.WorkFunction;
            response[15] = sensor.MaxLength;
            response[16] = 0x00;//采集 发送 倍数 保留
            //采集时间
            temp = CommArithmetic.Int16_2Bytes(sensor.Interval);
            response[17] = temp[0];
            response[18] = temp[1];
            //正常间隔
            temp = CommArithmetic.Int16_2Bytes(sensor.IntervalNormal);
            response[19] = temp[0];
            response[20] = temp[1];
            //预警间隔
            temp = CommArithmetic.Int16_2Bytes(sensor.IntervalWarning);
            response[21] = temp[0];
            response[22] = temp[1];
            //报警间隔
            temp = CommArithmetic.Int16_2Bytes(sensor.IntervalAlarm);
            response[23] = temp[0];
            response[24] = temp[1];
            //温度补偿
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureCompensation);
            response[25] = temp[0];
            response[26] = temp[1];

            //湿度补偿
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityCompensation);
            response[27] = temp[0];
            response[28] = temp[1];

            //温度预警
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureInfoHigh);
            response[29] = temp[0];
            response[30] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureInfoLow);
            response[31] = temp[0];
            response[32] = temp[1];

            //湿度预警
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityInfoHigh);
            response[33] = temp[0];
            response[34] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityInfoLow);
            response[35] = temp[0];
            response[36] = temp[1];

            //温度报警
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureWarnHigh);
            response[37] = temp[0];
            response[38] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureWarnLow);
            response[39] = temp[0];
            response[40] = temp[1];

            //湿度报警
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityWarnHigh);
            response[41] = temp[0];
            response[42] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityWarnLow);
            response[43] = temp[0];
            response[44] = temp[1];

            //保留
            response[45] = 0;
            response[46] = 0;
            response[47] = 0;
            response[48] = 0;

            //crc
            response[49] = 0;
            response[50] = 0;

            //结束位
            response[51] = 0xBC;
            response[52] = 0xBC;



            return response;

        }

        public byte[] UpdateGateway(Gateway gateway)
        {
            byte[] domainBytes = CommArithmetic.EncodeByte2String(gateway.TargetDomain);

            byte[] response = new byte[43+domainBytes.Length];
            response[0] = 0xCB;
            response[1] = 0xCB;
            response[2] = (byte)(response.Length-7); //长度不确定，需要根据域名进行计算,总长度-7
            response[3] = 0x65;
            response[4] = 0x01;
            

            //Clientid
            byte[]  temp = CommArithmetic.HexStringToByteArray(gateway.ClientID);
            response[5] = temp[0];
            response[6] = temp[1];

            //debug
            temp = CommArithmetic.HexStringToByteArray(gateway.Debug);
            response[7] = temp[0];
            response[8] = temp[1];

            //cagtegory
            response[9] = gateway.Category;

            //采集时间
            temp = CommArithmetic.Int16_2Bytes(gateway.Interval);
            response[10] = temp[0];
            response[11] = temp[1];

            //加入当前时间的改变，注意UI要输入当前时间
            temp = CommArithmetic.EncodeDateTime(System.DateTime.Now);
            response[12] = temp[0];
            response[13] = temp[1];
            response[14] = temp[2];
            response[15] = temp[3];
            response[16] = temp[4];
            response[17] = temp[5];



            response[18] = gateway.WorkFunction;
            response[19] = gateway.SymbolRate;
            response[20] = gateway.Frequency;

            //轮播间隔
            temp = CommArithmetic.Int16_2Bytes(gateway.DisplayInterval);
            response[21] = temp[0];
            response[22] = temp[1];

            //报警间隔
            temp = CommArithmetic.Int16_2Bytes(gateway.AlarmInterval);
            response[23] = temp[0];
            response[24] = temp[1];

            //TransStrategy
            response[25] = gateway.TransStrategy;
            //
            response[26] = gateway.DateTimeStrategy;
            response[27] = gateway.AlarmStyle;
            response[28] = gateway.AlarmSource;
            response[29] = gateway.DisplayStyle;
            response[30] = gateway.BackgroundLight;
            response[31] = gateway.GPSStart;

            //保留位
            response[32] = 0;
            response[33] = 0;
            response[34] = 0;
            response[35] = 0;

            //域名
            response[36] =(byte) gateway.TargetDomain.Length;
           
            for (int i = 0; i < domainBytes.Length; i++)
            {
                response[37 + i] = domainBytes[i];

            }

            //端口
            temp = CommArithmetic.Int16_2Bytes(gateway.TargetPort);
            response[37 + domainBytes.Length] = temp[0];
            response[38 + domainBytes.Length] = temp[1];


            //crc
            response[39 + domainBytes.Length] = 0;
            response[40 + domainBytes.Length] = 0;
            //结束位
            response[41 + domainBytes.Length] = 0xBC;
            response[42 + domainBytes.Length] = 0xBC;






            return response;
        }


        public byte[] UpdateGatewayFactory(Gateway gateway)
        {
           

            byte[] response = new byte[17];
            response[0] = 0xCB;
            response[1] = 0xCB;
            response[2] = 0x0A;
            response[3] = 0x68;
            response[4] = 0x01;
            //devicemac 
            byte[] temp = CommArithmetic.HexStringToByteArray(gateway.DeviceMac);
            response[5] = temp[0];
            response[6] = temp[1];
            response[7] = temp[2];
            response[8] = temp[3];

            //Hareware Version
            temp = CommArithmetic.HexStringToByteArray(gateway.HardwareVersion);
            response[9] = temp[0];
            response[10] = temp[1];
            response[11] = temp[2];
            response[12] = temp[3];

            //Clientid
          


            //crc
            response[13] = 0;
            response[14] = 0;
            //结束位
            response[15] = 0xBC;
            response[16] = 0xBC;






            return response;
        }


    }
}
