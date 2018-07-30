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


        public Gateway GatewaySensorConfig(byte[] requestBytes)
        {


            return null;
        }


    }
}
