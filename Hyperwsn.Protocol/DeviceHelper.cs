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
            if (requestBytes != null && requestBytes.Length == 28)
            {
                //发现SG5
                try
                {

                    if (requestBytes[5] == 0x60)
                    {
                        gate.DeviceID = "SG5";
                    }
                    gate.DeviceMac = CommArithmetic.DecodeMAC(requestBytes, 6);
                    gate.HardwareVersion = CommArithmetic.DecodeMAC(requestBytes, 10);
                    gate.SoftwareVersion = CommArithmetic.DecodeClientID(requestBytes, 14);



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
            gateway.WorkFunction = requestBytes[25];
            gateway.SymbolRate = requestBytes[26];
            gateway.Frequency = requestBytes[27];
            gateway.DisplayInterval = CommArithmetic.Bytes2Int(requestBytes, 28, 2);
            gateway.AlarmInterval = CommArithmetic.Bytes2Int(requestBytes, 30, 2);
            gateway.TransStrategy = requestBytes[32];
            gateway.DateTimeStrategy = requestBytes[33];
            gateway.AlarmStyle = requestBytes[34];
            gateway.AlarmSource = requestBytes[35];
            gateway.DisplayStyle = requestBytes[36];
            gateway.DisplayCount = requestBytes[37];
            gateway.LasRestart = requestBytes[42];
            gateway.RAMCountHigh = requestBytes[43];
            gateway.RAMCountLow = requestBytes[44];
            gateway.FlashCountHigh = CommArithmetic.Bytes2Int(requestBytes, 45, 3);
            gateway.FlashCountLow = CommArithmetic.Bytes2Int(requestBytes, 48, 3);

            byte domainLength = requestBytes[51];

            gateway.TargetDomain = CommArithmetic.DecodeByte2String(requestBytes, 52, domainLength);
            gateway.TargetPort = CommArithmetic.Bytes2Int(requestBytes, 56, 2);












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
            it1.Debug = CommArithmetic.DecodeClientID(requestBytes, 13);
            it1.Category = requestBytes[15];
            it1.WorkFunction = requestBytes[16];
            it1.MaxLength = requestBytes[17];
            it1.TXTimers = requestBytes[18];

            it1.Interval = CommArithmetic.Bytes2Int(requestBytes, 19, 2);
            it1.IntervalNormal = CommArithmetic.Bytes2Int(requestBytes, 21, 2);
            it1.IntervalWarning= CommArithmetic.Bytes2Int(requestBytes, 23, 2);
            it1.IntervalAlarm = CommArithmetic.Bytes2Int(requestBytes, 25, 2);
            //温湿度补偿
            it1.TemperatureCompensation = CommArithmetic.DecodeTemperature(requestBytes,27);
            it1.HumidityCompensation = CommArithmetic.DecodeHumidity(requestBytes, 29);
            //预警值设置
            it1.TemperatureInfoHigh = CommArithmetic.DecodeTemperature(requestBytes, 31);
            it1.TemperatureInfoLow = CommArithmetic.DecodeTemperature(requestBytes, 33);
            it1.HumidityInfoHigh = CommArithmetic.DecodeHumidity(requestBytes, 35);
            it1.HumidityInfoLow = CommArithmetic.DecodeHumidity(requestBytes, 37);
            //报警值设置
            it1.TemperatureWarnHigh = CommArithmetic.DecodeTemperature(requestBytes, 39);
            it1.TemperatureWarnLow = CommArithmetic.DecodeTemperature(requestBytes, 41);
            it1.HumidityWarnHigh = CommArithmetic.DecodeHumidity(requestBytes, 43);
            it1.HumidityWarnLow = CommArithmetic.DecodeHumidity(requestBytes, 45);


            it1.ICTemperature = requestBytes[51]; //read only
            it1.Volt = CommArithmetic.DecodeVoltage(requestBytes, 52); //read only
            it1.FlashFront = CommArithmetic.Bytes2Int(requestBytes, 54, 3); //read only
            it1.FlashRear = CommArithmetic.Bytes2Int(requestBytes, 57, 3); //read only
            it1.FlashQueueLength = CommArithmetic.Bytes2Int(requestBytes, 60, 3); //read only
            it1.Temperature = CommArithmetic.DecodeTemperature(requestBytes, 63); //read only
            it1.Humidity = CommArithmetic.DecodeHumidity(requestBytes, 65); //read only











            return it1;
        }

        public byte[] UpdateInternalSersor(InternalSensor sensor,byte SensorNo)
        {
            byte[] response = new byte[52];
            response[0] = 0xCB;
            response[1] = 0xCB;
            response[2] = 0x2D;
            response[3] = 0x67;
            response[4] = 0x01;
            response[5] = SensorNo;
            //devicemac 
            byte[] temp = CommArithmetic.HexStringToByteArray(sensor.DeviceMac);
            response[6] = temp[0];
            response[7] = temp[1];
            response[8] = temp[2];
            response[9] = temp[3];
            //debug
            temp = CommArithmetic.HexStringToByteArray(sensor.Debug);
            response[10] = temp[0];
            response[11] = temp[1];
            //category
            response[12] = sensor.Category;
            response[13] = sensor.WorkFunction;
            response[14] = sensor.MaxLength;
            response[15] = 0x00;//采集 发送 倍数 保留
            //采集时间
            temp = CommArithmetic.Int16_2Bytes(sensor.Interval);
            response[16] = temp[0];
            response[17] = temp[1];
            //正常间隔
            temp = CommArithmetic.Int16_2Bytes(sensor.IntervalNormal);
            response[18] = temp[0];
            response[19] = temp[1];
            //预警间隔
            temp = CommArithmetic.Int16_2Bytes(sensor.IntervalWarning);
            response[20] = temp[0];
            response[21] = temp[1];
            //报警间隔
            temp = CommArithmetic.Int16_2Bytes(sensor.IntervalAlarm);
            response[22] = temp[0];
            response[23] = temp[1];
            //温度补偿
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureCompensation);
            response[24] = temp[0];
            response[25] = temp[1];

            //湿度补偿
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityCompensation);
            response[26] = temp[0];
            response[27] = temp[1];

            //温度预警
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureInfoHigh);
            response[28] = temp[0];
            response[29] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureInfoLow);
            response[30] = temp[0];
            response[31] = temp[1];

            //湿度预警
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityInfoHigh);
            response[32] = temp[0];
            response[33] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityInfoLow);
            response[34] = temp[0];
            response[35] = temp[1];

            //温度报警
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureWarnHigh);
            response[36] = temp[0];
            response[37] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.TemperatureWarnLow);
            response[38] = temp[0];
            response[39] = temp[1];

            //湿度报警
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityWarnHigh);
            response[40] = temp[0];
            response[41] = temp[1];
            temp = CommArithmetic.Double_2Bytes(sensor.HumidityWarnLow);
            response[42] = temp[0];
            response[43] = temp[1];

            //保留
            response[44] = 0;
            response[45] = 0;
            response[46] = 0;
            response[47] = 0;

            //crc
            response[48] = 0;
            response[49] = 0;

            //结束位
            response[50] = 0xBC;
            response[51] = 0xBC;



            return response;

        }


    }
}
