using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperwsn.Comm;

namespace Hyperwsn.Protocol
{
    /// <summary>
    /// 所有设备的父类，抽象类，不能直接实例化
    /// </summary>
    public abstract class Device
    {
        /// <summary>
        /// 设备类型列表
        /// </summary>
        public enum DeviceType
        {
            M1 = 0x51,                  // M1：CC1310+W25Q16CL+SHT30
            SG2CC1310 = 0x52,           // SG2:CC1310+W25Q16CL+ADP5062+MSP430F5529+W25Q16CL+Air200
            S1P = 0x53,                 // S1+：CC1310+PM25LQ020+SHT20
            USB0 = 0x54,                // USB0: MSP430F5529+CC1101
            GM = 0x55,                  // GM:CC1310+W25Q16CL
            USB1 = 0x56,                // USB1: MSP430F5529+CC1310
            M2 = 0x57,                  // M2:CC1310+W25Q16CL+Buzzer+段码屏+MAX31855
            SK = 0x58,                  // SK:CC1310+W25Q16CL+HLW8012
            AlertMSP432 = 0x59,         // Alert: MSP432P401R+MSP430F5510+CC1310+W25Q256FV+LCD+Buzzer
            S1 = 0x5A,                  // S1: MSP430F5510+CC1101+SHT20+PM25LD020
            SGA3 = 0x5B,                // SGA3: MSP432P401R
            M1_NTC = 0x5C,              // M1_NTC: CC1310+W25Q16CL+NTC3950
            M1_Beetech = 0x5D,          // M1_Beetech:CC1310+W25Q16CL+SHT30+CP2102
            AlertCC1310 = 0x5E,         // Alert(CC1310)
            SG5CC1310 = 0x5F,           // SG5(CC1310):MSP432+CC1310+W25Q256FV+LCD+M26+泰斗+CP2102
            SG5 = 0x60,                 // SG5(MSP432):MSP432+CC1310+W25Q256FV+LCD+M26+泰斗+CP2102
            SC = 0x61,                  // SC: MSP430F5529+ADG712
            TB2 = 0x62,                 // TB2: CC1310+CMT2300A
            USB2 = 0x63,                // USB2: CC1310+CP2102
            BB = 0x64,                  // BB: MSP430F5529+CC1101+SIM800A(+SIM28)
            SG5CC1310SHT30 = 0x65,      // SG5_CC1310_SHT30
            SG5CC1310NTC = 0x66,        // SG5_CC1310_NTC
            SG5CC1310PT100 = 0x67,      // SG5_CC1310_PT100
            SG6 = 0x68,                 // SG6(MSP432):MSP432+CC1310+W25Q256FV+LCD+SIM7600CE+CP2102
            SG6CC1310 = 0x69,           // SG6(CC1310):MSP432+CC1310+W25Q256FV+LCD+SIM7600CE+CP2102
            SG6P = 0x6A,                // SG6P(MSP432):MSP432+CC1310(PA)+W25Q256FV+LCD+SIM7600CE+CP2102+CC2640+ADXL362
            SG6PCC1310 = 0x6B,          // SG6P(CC1310):MSP432+CC1310(PA)+W25Q256FV+LCD+SIM7600CE+CP2102+CC2640+ADXL362
            SG6PCC2640 = 0x6C,          // SG6P(CC2640):MSP432+CC1310(PA)+W25Q256FV+LCD+SIM7600CE+CP2102+CC2640+ADXL362
            M6 = 0x6D,                  // M6:CC1310(PA)+W25Q16CL+SHT30+MCP144+段码屏
            M2_PT100 = 0x6E,            // M2:CC1310+W25Q16CL+Buzzer+段码屏+ADS1220+PT100
            M2_SHT30 = 0x6F,            // M2:CC1310+W25Q16CL+Buzzer+段码屏+SHT30
            PM = 0x70,                  // PM: CC1310+SKY66115
            LBGZ_TC04 = 0x71,           // LBGZ_TC04(MSP432):MSP432+CC1310(PA)+W25Q256FV+LCD+SIM7600CE+CP2102
            LBGZ_TC04CC1310 = 0x72,     // LBGZ_TC04(CC1310):MSP432+CC1310(PA)+W25Q256FV+LCD+SIM7600CE+CP2102
            SG6XM = 0x73,               // SG6X(MSP432):MSP432+CC1310(PA)+CC1101+W25Q256FV+LCD+SIM7600CE+CP2102+CC2640+ADXL362
            SG6XCC1310 = 0x74,          // SG6X(CC1310):MSP432+CC1310(PA)+CC1101+W25Q256FV+LCD+SIM7600CE+CP2102+CC2640+ADXL362
            SG6XCC2640 = 0x75,          // SG6X(CC2640):MSP432+CC1310(PA)+CC1101+W25Q256FV+LCD+SIM7600CE+CP2102+CC2640+ADXL362
            S2 = 0x76,                  // S2: MSP430F5529+CC1101+PM25LD020+MAX31855
            M9 = 0x77,                  // M9: CC1310+W25Q16CL+ADXL362
            ACO2 = 0x78,                // ACO2:CC1310+SHT30+MinIR-C02+W25Q16CL
            M30 = 0x79,                 // M30:MSP432+CC1310+W25Q256FV+LCD+CP2102
            AO2 = 0x7A,                 // AO2:CC1310+W25Q16CL+SHT30+LMP91000+02
            RT = 0x7B,                  // RT:CC1352P+W25Q16CL+TPL5010
            GMP = 0x7C,                 // GMP:CC1352P+GD25VQ32C+TPL5010，支持蓝牙
            ZQM1 = 0x7D,                // ZQM1:CC1310+GD25VQ32C+SHT30
            M5 = 0x7E,                  // M5:CC1310+GD25VQ32C+HP303S(BMP280) 
            M40 = 0x7F,                 // （门磁）M40: CC1310+GD25VQ32C+干簧管
            M20 = 0x80,                 // M20:M20+3SHT30
            M1X = 0x81,                 // M1X: CC1310+W25Q16CL+SHT30+TPL5010
            ZQSG1CC1352P = 0x82,        // ZQSG1(CC1352P)
            ZQSG1 = 0x83,               // ZQSG1(MSP432)
            M10 = 0x84,                 // M10
            L1 = 0x85,                  // L1(光照传感器)
            SG6M = 0x86,                // SG6M(MSP432)
            ZQSG2 = 0x88,               // ZQSG2(MSP432)
            ESK = 0x8A,                 // ESK
            IR20 = 0x8B,                // IR20
            M44 = 0x8C,                 // M44(MSP432)
            WP = 0x8E,                  // WP
            AC2 = 0x8F,                 // AC2
            C1 = 0x92,                  // C1: 爱立信
            SG10 = 0x93,                // SG10
            ZQSG6M = 0x94,              // ZQSG6M
            SG6E = 0x9A,                // SG6E
            M60 = 0x9F,                 // M60
            M70 = 0xA0,                 // M70
            M60_SHT30 = 0xA1,           // M60_SHT30
            M60_MAX31855 = 0xA2,        // M60_MAX31855
            M70_SHT30 = 0xA3,           // M60_SHT30
            M70_MAX31855 = 0xA4,        // M60_MAX31855
            S1Find = 0xA5,              // S1+ 找货神器
            SG9 = 0xA6,                 // SG9 野保网关
            SG6Lite = 0xA7,             // SG6Lite
            Beetech_M20 = 0xA8,         // Beetech M20
            M1000 = 0xA9,               // M1000
            MaxValue,                   // 此枚举类型的最大值
        }

        /// <summary>
        /// 数据包类型列表
        /// </summary>
        public enum DataPktType
        {
            Null,                   // 
            SelfTest,               // SS发出的上电自检数据包
            SensorFromSsToGw,       // SS发给GW的传感数据包
            AckFromGwToSs,          // GW发给SS的确认包
            SensorDataFromGmToPc,   // GM反馈给上位机的Sensor数据包
            UsbReadBindOfGw,        // USB读取网关的绑定设备列表
        }

        /*********************************************************************************************************************
        *  属性
        ********************************************************************************************************************/

        public int DisplayID { get; set; }

        /// <summary>
        /// 如果设备包含电池，代表电池的电压
        /// </summary>
        public double Volt { get; set; }

        /// <summary>
        /// 是否连接了充电器
        /// </summary>
        public bool LinkCharge { get; set; }

        /// <summary>
        /// 设备代号，如M1 代号为51
        /// </summary>
        public String DeviceID { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public byte DeviceTypeV { get; set; }

        /// <summary>
        /// 设备类型，如M1 代号为51
        /// </summary>
        public String DeviceTypeS { get; set; }

        /// <summary>
        /// DQ: 设备的原始MAC
        /// </summary>
        public UInt32 PrimaryMacV { get; set; }

        /// <summary>
        /// 设备的原始MAC
        /// </summary>
        public String PrimaryMacS { get; set; }

        /// <summary>
        /// 设备的MAC地址
        /// </summary>
        public UInt32 DeviceMacV { get; set; }

        /// <summary>
        /// 设备的8位MAC地址
        /// </summary>
        public String DeviceMacS { get; set; }


        public string DeviceNewMacS { get; set; }

        public UInt32 DeviceNewMacV { get; set; }

        /// <summary>
        /// 硬件版本
        /// </summary>
        public UInt32 HwRevisionV { get; set; }

        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HwRevisionS { get; set; }

        /// <summary>
        /// 软件版本
        /// </summary>
        public UInt16 SwRevisionV { get; set; }

        /// <summary>
        /// 软件版本
        /// </summary>
        public string SwRevisionS { get; set; }

        /// <summary>
        /// DQ: 客户码
        /// </summary>
        public UInt16 CustomerV { get; set; }

        /// <summary>
        /// 客户识别码
        /// </summary>
        public String CustomerS { get; set; }

        /// <summary>
        /// Debug
        /// </summary>
        public UInt16 DebugV { get; set; }

        /// <summary>
        /// Debug
        /// </summary>
        public String DebugS { get; set; }

        /// <summary>
        /// 设备原始数据
        /// </summary>
        public String SourceData { get; set; }

        /// <summary>
        /// 协议版本
        /// </summary>
        public byte Protocol { get; set; }

        public byte Category { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public UInt16 Interval { get; set; }

        public DateTime Calendar { get; set; }

        /// <summary>
        /// Sensor的工作模式
        /// </summary>
        public byte Pattern { get; set; } 

        /// <summary>
        /// 传输速率
        /// </summary>
        public byte bps { get; set; }

        /// <summary>
        /// 设备的最后传输日期和时间
        /// </summary>
        public DateTime LastTransforDate { get; set; }

        /// <summary>
        /// 传输频率
        /// </summary>
        public byte channel { get; set; }


        /*********************************************************************************************************************
         *  方法
         ********************************************************************************************************************/


        public override string ToString()
        {
            return SourceData;
        }

        /// <summary>
        /// 设置设备类型，并且更新Name属性
        /// </summary>
        /// <param name="deviceType"></param>
        public string SetDeviceName(byte deviceType)
        {
            DeviceTypeV = deviceType;
            DeviceTypeS = deviceType.ToString("X2");

            switch (deviceType)
            {
                case 0x51:
                    {
                        Name = "M1";
                        break;
                    }
                case 0x52:
                    {
                        Name = "SG2";
                        break;
                    }
                case 0x53:
                    {
                        Name = "M1P";
                        break;
                    }
                case 0x54:
                    {
                        Name = "USB_MSP430+CC1101";
                        break;
                    }
                case 0x55:
                    {
                        Name = "GM";
                        break;
                    }
                case 0x56:
                    {
                        Name = "USB_MSP430+CC1310";
                        break;
                    }
                case 0x57:
                    {
                        Name = "M2";
                        break;
                    }
                case 0x58:
                    {
                        Name = "M4(SK)";
                        break;
                    }
                case 0x59:
                    {
                        Name = "Alert";
                        break;
                    }
                case 0x5A:
                    {
                        Name = "S1";
                        break;
                    }
                case 0x5B:
                    {
                        Name = "SGA3";
                        break;
                    }
                case 0x5C:
                    {
                        Name = "M1_NTC";
                        break;
                    }
                case 0x5D:
                    {
                        Name = "M1_Beetech";
                        break;
                    }
                case 0x60:
                    {
                        Name = "SG5";
                        break;
                    }
                case 0x61:
                    {
                        Name = "SC";
                        break;
                    }
                case 0x62:
                    {
                        Name = "TB2";
                        break;
                    }
                case 0x63:
                    {
                        Name = "USB_CC1310+CP2102";
                        break;
                    }
                case 0x64:
                    {
                        Name = "BB";
                        break;
                    }
                case 0x65:
                    {
                        Name = "SGX_SHT30";
                        break;
                    }
                case 0x66:
                    {
                        Name = "SGX_NTC";
                        break;
                    }
                case 0x67:
                    {
                        Name = "SGX_PT100";
                        break;
                    }
                case 0x68:
                    {
                        Name = "SG6";
                        break;
                    }
                case 0x6A:
                    {
                        Name = "SG6P";
                        break;
                    }
                case 0x6D:
                    {
                        Name = "M6";
                        break;
                    }
                case 0x6E:
                    {
                        Name = "M2_PT100";
                        break;
                    }
                case 0x6F:
                    {
                        Name = "M2_SHT30";
                        break;
                    }
                case 0x70:
                    {
                        Name = "PM";
                        break;
                    }
                case 0x71:
                    {
                        Name = "LBGZ_TC04";
                        break;
                    }
                case 0x73:
                    {
                        Name = "SG6X";
                        break;
                    }
                case 0x77:
                    {
                        Name = "M9(振动)";
                        break;
                    }
                case 0x78:
                    {
                        Name = "ACO2";
                        break;
                    }
                case 0x79:
                    {
                        Name = "M30";
                        break;
                    }
                case 0x7A:
                    {
                        Name = "AO2";
                        break;
                    }
                case 0x7B:
                    {
                        Name = "RT";
                        break;
                    }
                case 0x7C:
                    {
                        Name = "GMP";
                        break;
                    }
                case 0x7D:
                    {
                        Name = "M1_Zigin";
                        break;
                    }
                case 0x7E:
                    {
                        Name = "M5(气压)";
                        break;
                    }
                case 0x7F:
                    {
                        Name = "M40（门磁）";
                        break;
                    }
                case 0x80:
                    {
                        Name = "M20";
                        break;
                    }
                case 0x81:
                    {
                        Name = "M1X";
                        break;
                    }
                case 0x82:
                    {
                        Name = "ZQSG1CC1352P";
                        break;
                    }
                case 0x83:
                    {
                        Name = "ZQSG1MSP432";
                        break;
                    }
                case 0x84:
                    {
                        Name = "M10";
                        break;
                    }
                case 0x85:
                    {
                        Name = "L1";
                        break;
                    }
                case 0x86:
                    {
                        Name = "SG6M(MSP432)";
                        break;
                    }
                case 0x87:
                    {
                        Name = "SG6M(CC1352P)";
                        break;
                    }
                case 0x88:
                    {
                        Name = "ZQSG2(MSP432)";
                        break;
                    }
                case 0x89:
                    {
                        Name = "ZQSG2(CC1352P)";
                        break;
                    }
                case 0x8A:
                    {
                        Name = "ESK";
                        break;
                    }
                case 0x8B:
                    {
                        Name = "IR20";
                        break;
                    }
                case 0x8C:
                    {
                        Name = "M44(MSP432)";
                        break;
                    }
                case 0x8D:
                    {
                        Name = "M44(CC1352P)";
                        break;
                    }
                case 0x8E:
                    {
                        Name = "WP";
                        break;
                    }
                case 0x8F:
                    {
                        Name = "M44_485_AC2";
                        break;
                    }
                case 0x90:
                    {
                        Name = "M44_485_CO2: CO2";
                        break;
                    }
                case 0x91:
                    {
                        Name = "M44_485_CO2: O2";
                        break;
                    }
                case 0x92:
                    {
                        Name = "C1";
                        break;
                    }
                case 0x93:
                    {
                        Name = "SG10";
                        break;
                    }
                case 0x94:
                    {
                        Name = "ZQSG6M";
                        break;
                    }
                case 0x95:
                    {
                        Name = "M24";
                        break;
                    }
                case 0x96:
                    {
                        Name = "M24_485_CO2: CO2";
                        break;
                    }
                case 0x97:
                    {
                        Name = "M24_485_CO2: O2";
                        break;
                    }
                case 0x98:
                    {
                        Name = "M24_485_AC2";
                        break;
                    }
                case 0x99:
                    {
                        Name = "M24_485_TEC3000";
                        break;
                    }
                case 0x9A:
                    {
                        Name = "SG6E";
                        break;
                    }
                case 0x9B:
                    {
                        Name = "RT";
                        break;
                    }
                case 0x9C:
                    {
                        Name = "M24_CSF11";
                        break;
                    }
                case 0x9D:
                    {
                        Name = "EK";
                        break;
                    }
                case 0x9E:
                    {
                        Name = "EK_SHT30";
                        break;
                    }
                case 0x9F:
                    {
                        Name = "M60";
                        break;
                    }
                case 0xA0:
                    {
                        Name = "M70";
                        break;
                    }
                case 0xA1:
                    {
                        Name = "M60_SHT30";
                        break;
                    }
                case 0xA2:
                    {
                        Name = "M60_MAX31855";
                        break;
                    }
                case 0xA3:
                    {
                        Name = "M70_SHT30";
                        break;
                    }
                case 0xA4:
                    {
                        Name = "M70_MAX31855";
                        break;
                    }
                case 0xA5:
                    {
                        Name = "S1+";
                        break;
                    }
                case 0xA6:
                    {
                        Name = "SG9";
                        break;
                    }
                case 0xA7:
                    {
                        Name = "SG6L";
                        break;
                    }
                case 0xA8:
                    {
                        Name = "Beetech M20";
                        break;
                    }
                case 0xA9:
                    {
                        Name = "M1000";
                        break;
                    }
                default:
                    {
                        Name = deviceType.ToString("X2");
                        break;
                    }
            }

            return Name;
        }

        /// <summary>
        /// 设置Device的Primary Mac
        /// </summary>
        /// <param name="deviceType"></param>
        public void SetDevicePrimaryMac(byte[] SrcData, UInt16 StartIndex)
        {
            PrimaryMacS = CommArithmetic.ByteBuf_to_HexString(SrcData, StartIndex, 4);

            PrimaryMacV = (UInt32)(SrcData[StartIndex] * 256 * 256 * 256 + SrcData[StartIndex + 1] * 256 * 256 + SrcData[StartIndex + 2] * 256 + SrcData[StartIndex + 3]);
        }

        /// <summary>
        /// 设置Device Mac
        /// </summary>
        /// <param name="deviceType"></param>
        public void SetDeviceMac(byte[] SrcData, UInt16 StartIndex)
        {
            DeviceMacS = CommArithmetic.ByteBuf_to_HexString(SrcData, StartIndex, 4);

            DeviceMacV = (UInt32)(SrcData[StartIndex] * 256 * 256 * 256 + SrcData[StartIndex + 1] * 256 * 256 + SrcData[StartIndex + 2] * 256 + SrcData[StartIndex + 3]);
        }

        /// <summary>
        /// 设置硬件版本
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="StartIndex"></param>
        public void SetHardwareRevision(byte[] SrcData, UInt16 StartIndex)
        {
            HwRevisionS = CommArithmetic.ByteBuf_to_HexString(SrcData, StartIndex, 4);

            HwRevisionV = (UInt32)(SrcData[StartIndex] * 256 * 256 * 256 + SrcData[StartIndex + 1] * 256 * 256 + SrcData[StartIndex + 2] * 256 + SrcData[StartIndex + 3]);
        }

        /// <summary>
        /// 设置软件版本
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="StartIndex"></param>
        public void SetSoftwareRevision(byte[] SrcData, UInt16 StartIndex)
        {
            SwRevisionS = CommArithmetic.ByteBuf_to_HexString(SrcData, StartIndex, 2);

            SwRevisionV = (UInt16)(SrcData[StartIndex] * 256 + SrcData[StartIndex + 1]);
        }

        /// <summary>
        /// 设置Device的客户码
        /// </summary>
        /// <param name="deviceType"></param>
        public void SetDeviceCustomer(byte[] SrcData, UInt16 StartIndex)
        {
            CustomerS = CommArithmetic.ByteBuf_to_HexString(SrcData, StartIndex, 2);

            CustomerV = (UInt16)(SrcData[StartIndex] * 256 + SrcData[StartIndex + 1]);
        }

        /// <summary>
        /// 设置Debug
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="StartIndex"></param>
        public void SetDebug(byte[] SrcData, UInt16 StartIndex)
        {
            DebugS = CommArithmetic.ByteBuf_to_HexString(SrcData, StartIndex, 2);

            DebugV = (UInt16)(SrcData[StartIndex] * 256 + SrcData[StartIndex + 1]);
        }
    }
}
