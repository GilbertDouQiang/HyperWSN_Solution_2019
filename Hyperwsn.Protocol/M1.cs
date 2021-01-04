using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Hyperwsn.Comm;


namespace Hyperwsn.Protocol
{
    public class M1:Sensor
    {
        /*********************************************************************************************************************
         *  属性
         ********************************************************************************************************************/

        public double Temperature { get; set; }

        public double Humidity { get; set; }

        /// <summary>
        /// 温度预警上限
        /// </summary>
        public double TemperatureInfoHigh { get; set; }
        /// <summary>
        /// 温度预警下限
        /// </summary>
        public double TemperatureInfoLow { get; set; }

        /// <summary>
        /// 温度报警上限
        /// </summary>
        public double TemperatureWarnHigh { get; set; }

        /// <summary>
        /// 温度报警下限
        /// </summary>
        public double TemperatureWarnLow { get; set; }

        /// <summary>
        /// 湿度预警上限
        /// </summary>
        public double HumidityInfoHigh { get; set; }
        /// <summary>
        /// 湿度预警下限
        /// </summary>
        public double HumidityInfoLow { get; set; }

        /// <summary>
        /// 湿度报警上限
        /// </summary>有
        public double HumidityWarnHigh { get; set; }

        /// <summary>
        /// 湿度报警下限
        /// </summary>
        public double HumidityWarnLow { get; set; }

        /// <summary>
        /// 温度补偿
        /// </summary>
        public double TemperatureCompensation { get; set; }

        /// <summary>
        /// 湿度补偿
        /// </summary>
        public double HumidityCompensation { get; set; }

        /// <summary>
        /// 绑定设备列表设备的总数量
        /// </summary>
        public byte TotalOfBind { get; set; }

        /// <summary>
        /// 绑定设备列表设备的序号
        /// </summary>
        public byte SerialOfBind { get; set; }

        /// <summary>
        /// RAM中的待发数据的数量
        /// </summary>
        public byte ToSendRam { get; set; }

        /// <summary>
        /// Flash中的待发数据的数量
        /// </summary>
        public UInt16 ToSendFlash { get; set; }

        // 导出数据所用

        /// <summary>
        /// 导出结果
        /// </summary>
        public byte ExportRrror { get; set; }

        /// <summary>
        /// 导出组的序号
        /// </summary>
        public UInt32 Group { get; set; }

        /// <summary>
        /// 导出组的容量
        /// </summary>
        public UInt32 GroupCapacity { get; set; }

        /// <summary>
        /// 组内序号
        /// </summary>
        public UInt32 SerialInGroup { get; set; }

        public byte SendOk { get; set; }

        /// <summary>
        /// 网关判定的报警状态
        /// </summary>
        public byte AlertStatusByGw { get; set; }

        public string tempS { get; set; }

        public string humS { get; set; }

        /*********************************************************************************************************************
         *  方法
         ********************************************************************************************************************/


        public M1(byte[] SourceData)
        {
        }

        public M1(byte[] SrcData, UInt16 IndexOfStart, Device.DataPktType dataPktType, Device.DeviceType deviceType)
        {
            if (dataPktType != Device.DataPktType.UsbReadBindOfGw)
            {
                return;
            }

            if (deviceType != Device.DeviceType.M1)
            {
                return;
            }

            // 数据包长度
            UInt16 PktLen = SrcData[IndexOfStart + 2];

            // 配置长度
            UInt16 CfgLen = SrcData[IndexOfStart + 17];

            if (PktLen - 15 < CfgLen)
            {
                return;
            }

            // Error
            if(SrcData[IndexOfStart + 10] != 0)
            {
                return;
            }

            UInt16 iCnt = (UInt16)(IndexOfStart + 11);

            // 数据包类型
            dataPktType = DataPktType.UsbReadBindOfGw;

            // Total
            TotalOfBind = SrcData[iCnt];
            iCnt += 1;

            // Serial Number
            SerialOfBind = SrcData[iCnt];
            iCnt += 1;

            // Sensor ID
            SetDeviceMac(SrcData, iCnt);
            iCnt += 4;

            // 系统时间            
            SystemTime = System.DateTime.Now;

            // 源数据
            this.SourceData = CommArithmetic.ByteArrayToHexString(SrcData, IndexOfStart, (UInt16)(PktLen + 7));
        }

        /// <summary>
        /// 将输入的数据包按照网关USB导出数据的协议进行解析
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="IndexOfStart"></param>
        public M1(byte[] SrcData, UInt16 IndexOfStart)
        {
            // 数据包的长度，不包含头尾
            byte LenOfPkt = SrcData[IndexOfStart + 2];

            ExportRrror = SrcData[IndexOfStart + 11];
            SerialInGroup = (UInt32)(SrcData[IndexOfStart + 13] * 256 * 256 + SrcData[IndexOfStart + 13] * 256 + SrcData[IndexOfStart + 14]);
            SendOk = SrcData[IndexOfStart + 15];
            SetDeviceMac(SrcData, (UInt16)(IndexOfStart + 17));
            SensorTransforTime = CommArithmetic.DecodeDateTime(SrcData, IndexOfStart + 21);
            RSSI = SrcData[IndexOfStart + 27];
            if (RSSI >= 0x80)
            {
                RSSI = (Int16)(RSSI - 0x100);
            }
            AlertStatusByGw = SrcData[IndexOfStart + 28];
            Pattern = SrcData[IndexOfStart + 29];
            SetDeviceName(SrcData[IndexOfStart + 30]);
            Protocol = SrcData[IndexOfStart + 31];
            LastHistory = SrcData[IndexOfStart + 32];
            AlertStatusBySs = SrcData[IndexOfStart + 33];
            AlertItemBySs = SrcData[IndexOfStart + 34];
            SensorSN = (UInt16)(SrcData[IndexOfStart + 35] * 256 + SrcData[IndexOfStart + 36]);
            SensorCollectTime = CommArithmetic.DecodeDateTime(SrcData, IndexOfStart + 37);
            ICTemperature = SrcData[IndexOfStart + 43];
            if (ICTemperature >= 0x80)
            {
                ICTemperature = (Int16)(ICTemperature - 0x100);
            }

            UInt16 VoltValue = (UInt16)(SrcData[IndexOfStart + 44] * 256 + SrcData[IndexOfStart + 45]);
            if(0 == (VoltValue & 0x8000))
            {
                LinkCharge = false;
            }else
            {
                LinkCharge = true;
                VoltValue = (UInt16)(VoltValue & 0x7FFF);
            }

            Volt = (double)(VoltValue) / 1000.0f;

            ToSendRam = SrcData[IndexOfStart + 46];
            ToSendFlash = (UInt16)(SrcData[IndexOfStart + 47] * 256 + SrcData[IndexOfStart + 48]);

            tempS = "";
            humS = "";

            Int16 tempI = 0;
            UInt16 humI = 0;

            if(SrcData[IndexOfStart + 50] == 6)
            {
                if(SrcData[IndexOfStart + 51] == 0x65)
                {
                    tempI = (Int16)(SrcData[IndexOfStart + 52] * 256 + SrcData[IndexOfStart + 53]);
                    Temperature = (double) tempI / 100.0f;
                    tempS = Temperature.ToString("F2");
                }

                if (SrcData[IndexOfStart + 54] == 0x66)
                {
                    humI = (UInt16)(SrcData[IndexOfStart + 55] * 256 + SrcData[IndexOfStart + 56]);
                    Humidity = (double)humI / 100.0f;
                    humS = Humidity.ToString("F2");
                }
            }
            else if(SrcData[IndexOfStart + 50] == 3)
            {
                if (SrcData[IndexOfStart + 51] == 0x65)
                {
                    tempI = (Int16)(SrcData[IndexOfStart + 52] * 256 + SrcData[IndexOfStart + 53]);
                    Temperature = (double)tempI / 100.0f;
                    tempS = Temperature.ToString("F2");
                }
            }

            SystemTime = DateTime.Now;

            this.SourceData = CommArithmetic.ByteArrayToHexString(SrcData, IndexOfStart, (UInt16)(LenOfPkt + 7));
        }
    }
}
