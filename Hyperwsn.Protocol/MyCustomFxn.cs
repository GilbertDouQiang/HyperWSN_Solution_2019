using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperwsn.Protocol
{
    /// <summary>
    /// 抽象类，不能直接实例化，我自定义的函数(方法)
    /// </summary>
    public abstract class MyCustomFxn
    {
        /// <summary>
        /// CHISCDC CRC表
        /// </summary>
        private static UInt16[] CRC_TAB = new UInt16[] { 0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5,
                             0x60c6, 0x70e7, 0x8108, 0x9129, 0xa14a, 0xb16b,
                             0xc18c, 0xd1ad, 0xe1ce, 0xf1ef, 0x1231, 0x0210,
                             0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
                             0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c,
                             0xf3ff, 0xe3de, 0x2462, 0x3443, 0x0420, 0x1401,
                             0x64e6, 0x74c7, 0x44a4, 0x5485, 0xa56a, 0xb54b,
                             0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
                             0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6,
                             0x5695, 0x46b4, 0xb75b, 0xa77a, 0x9719, 0x8738,
                             0xf7df, 0xe7fe, 0xd79d, 0xc7bc, 0x48c4, 0x58e5,
                             0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
                             0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969,
                             0xa90a, 0xb92b, 0x5af5, 0x4ad4, 0x7ab7, 0x6a96,
                             0x1a71, 0x0a50, 0x3a33, 0x2a12, 0xdbfd, 0xcbdc,
                             0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
                             0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03,
                             0x0c60, 0x1c41, 0xedae, 0xfd8f, 0xcdec, 0xddcd,
                             0xad2a, 0xbd0b, 0x8d68, 0x9d49, 0x7e97, 0x6eb6,
                             0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
                             0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a,
                             0x9f59, 0x8f78, 0x9188, 0x81a9, 0xb1ca, 0xa1eb,
                             0xd10c, 0xc12d, 0xf14e, 0xe16f, 0x1080, 0x00a1,
                             0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
                             0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c,
                             0xe37f, 0xf35e, 0x02b1, 0x1290, 0x22f3, 0x32d2,
                             0x4235, 0x5214, 0x6277, 0x7256, 0xb5ea, 0xa5cb,
                             0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
                             0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447,
                             0x5424, 0x4405, 0xa7db, 0xb7fa, 0x8799, 0x97b8,
                             0xe75f, 0xf77e, 0xc71d, 0xd73c, 0x26d3, 0x36f2,
                             0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
                             0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9,
                             0xb98a, 0xa9ab, 0x5844, 0x4865, 0x7806, 0x6827,
                             0x18c0, 0x08e1, 0x3882, 0x28a3, 0xcb7d, 0xdb5c,
                             0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
                             0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0,
                             0x2ab3, 0x3a92, 0xfd2e, 0xed0f, 0xdd6c, 0xcd4d,
                             0xbdaa, 0xad8b, 0x9de8, 0x8dc9, 0x7c26, 0x6c07,
                             0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
                             0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba,
                             0x8fd9, 0x9ff8, 0x6e17, 0x7e36, 0x4e55, 0x5e74,
                             0x2e93, 0x3eb2, 0x0ed1, 0x1ef0 };

        /// <summary>
        /// 获取生成多项式CRC16_ITU_POLYNOMIAL
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        static public UInt16 GetItuPolynomialOfCrc16()
        {
            return 0x1021;
        }

        /// <summary>
        /// 计算CRC
        /// </summary>
        /// <param name="polynomial"></param>
        /// <param name="seed"></param>
        /// <param name="input"></param>
        /// <param name="nbrOfBytes"></param>
        /// <returns></returns>
        static public UInt16 CRC16(UInt16 polynomial, UInt16 seed, byte[] input, UInt16 IndexOfStart, UInt16 nbrOfBytes)
        {
            UInt16 crc = seed, bit = 0, byteCtr = 0;

            for (byteCtr = 0; byteCtr < nbrOfBytes; byteCtr++)
            {
                crc ^= (UInt16)(input[IndexOfStart + byteCtr] << 8);

                for (bit = 8; bit > 0; bit--)
                {
                    if ((crc & 0x8000) == 0)
                    {
                        crc <<= 1;
                    }
                    else
                    {
                        crc = (UInt16)((crc << 1) ^ polynomial);
                    }
                }
            }

            return crc;
        }

        /// <summary>
        /// 判断字符串中是否有非16进制数
        /// </summary>
        /// <param name="hexStr">此处输入的字符串已经去除了内部的空格</param>
        /// <returns> 假设返回值是X，那么从hexStr的第一个字节开始到hexStr的第X个字节的这部分字符串里是一个不存在非16进制数的字符串
        /// </returns>
        static public UInt16 HexString_isRight(string hexStr)
        {
            byte thisValue = 0;
            UInt16 StrLen = 0;

            // 字符串长度保护
            if (hexStr.Length > 0xFFFE)
            {
                StrLen = 0xFFFE;
            }
            else
            {
                StrLen = (UInt16)hexStr.Length;
            }

            // 判断是否含有非16进制字符
            for (UInt16 iCnt = 0; iCnt < StrLen; iCnt++)
            {
                thisValue = (byte)hexStr[iCnt];

                if ((thisValue >= '0' && thisValue <= '9') || (thisValue >= 'a' && thisValue <= 'f') || (thisValue >= 'A' && thisValue <= 'F'))
                {   // 是16进制数
                    continue;
                }
                else
                {   // 字符串里含有非16进制数
                    return iCnt;
                }
            }

            return StrLen;
        }

        /// <summary>
        /// 将16进制的字符串转换为字节数组
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static public byte[] HexStringToByteArray(string hexStr)
        {
            try
            {
                // 去除字符串中的空格、回车、换行
                hexStr = hexStr.Replace(" ", "");
                hexStr = hexStr.Replace("\r", "");
                hexStr = hexStr.Replace("\n", "");

                // 判断字符串中是否有非16进制数
                UInt16 StrLen = HexString_isRight(hexStr);
                if (StrLen == 0)
                {
                    return null;
                }

                // 计算字节数组的长度
                UInt16 BufLen = 0;
                if (0 == (StrLen % 2))
                {
                    BufLen = (UInt16)(StrLen / 2);
                }
                else
                {
                    BufLen = (UInt16)(StrLen / 2 + 1);
                }

                // 创建字节数组
                byte[] Buf = new byte[BufLen];

                // 填充字节数组的内容
                for (UInt16 iCnt = 0; iCnt < BufLen - 1; iCnt++)
                {
                    Buf[iCnt] = (byte)Convert.ToByte(hexStr.Substring(iCnt * 2, 2), 16);
                }
                if (0 == (StrLen % 2))
                {
                    Buf[BufLen - 1] = (byte)Convert.ToByte(hexStr.Substring((BufLen - 1) * 2, 2), 16);
                }
                else
                {
                    Buf[BufLen - 1] = (byte)Convert.ToByte(hexStr.Substring((BufLen - 1) * 2, 1), 16);
                }

                return Buf;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// CHISCDC计算CRC
        /// </summary>
        /// <param name="input"></param>
        /// <param name="nbrOfBytes"></param>
        /// <returns></returns>
        static public UInt16 CRC16_By_CHISCDC(byte[] input, UInt16 IndexOfStart, UInt16 nbrOfBytes)
        {
            UInt16 crc = 0x0000;
            UInt16 iCnt = 0;

            UInt16 index = 0;

            for (; iCnt < nbrOfBytes; iCnt++)
            {
                index = (UInt16)((crc ^ input[IndexOfStart + iCnt]) & 0xFF);

                crc = (UInt16)(CRC_TAB[index] ^ (crc >> 8));
            }

            return crc;
        }

        /// <summary>
        /// 将日期和时间转换为字节数据，输出格式：YY MM DD HH mm SS，Decimal编码
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public byte[] DataTimeToByteArray(DateTime dateTime)
        {
            byte[] Buf = new byte[6];

            Buf[0] = (byte)(dateTime.Year - 2000);
            Buf[1] = (byte)dateTime.Month;
            Buf[2] = (byte)dateTime.Day;
            Buf[3] = (byte)dateTime.Hour;
            Buf[4] = (byte)dateTime.Minute;
            Buf[5] = (byte)dateTime.Second;

            return Buf;
        }

        /// <summary>
        /// 将输入的Decimal数值转换为BCD码数值
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public byte DecimalToBcd(byte iValue)
        {
            if (iValue > 99)
            {
                return 0;       // 输入的数值无法转换为BCD码
            }

            return (byte)((iValue / 10 * 0x10) + (iValue % 10));
        }

        static public byte DecimalToBcd(int iValue)
        {
            if (iValue > 99)
            {
                return 0;       // 输入的数值无法转换为BCD码
            }

            return (byte)((iValue / 10 * 0x10) + (iValue % 10));
        }

        /// <summary>
        /// 将输入的BCD码数值转换为Decimal数值
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public byte BcdToDecimal(byte iValue)
        {
            // 十位
            byte tens = (byte)((iValue & 0xF0) >> 4);
            if (tens > 9)
            {
                return 0;
            }

            // 个位
            byte units = (byte)((iValue & 0x0F) >> 0);
            if (units > 9)
            {
                return 0;
            }

            return (byte)(tens * 10 + units);
        }

        static public string ToHexString(byte[] HexBuf, UInt16 IndexOfStart, UInt16 HexLen)
        {
            string hexString = string.Empty;
            if (HexBuf == null || HexLen == 0 || IndexOfStart + HexLen > HexBuf.Length)
            {
                return hexString;
            }

            StringBuilder strB = new StringBuilder();

            for (UInt16 iCnt = 0; iCnt < HexLen; iCnt++)
            {
                if (iCnt == 0)
                {
                    strB.Append(HexBuf[IndexOfStart + iCnt].ToString("X2"));
                }
                else
                {
                    strB.Append(" " + HexBuf[IndexOfStart + iCnt].ToString("X2"));
                }
            }

            hexString = strB.ToString();

            return hexString;
        }

        static public byte[] IpStr2ByteBuf(string ipStr)
        {   
            string[] ipStrCell = ipStr.Split('.');

            byte[] ipByteBuf = new byte[4];
            ipByteBuf[0] = byte.Parse(ipStrCell[0]);
            ipByteBuf[1] = byte.Parse(ipStrCell[1]);
            ipByteBuf[2] = byte.Parse(ipStrCell[2]);
            ipByteBuf[3] = byte.Parse(ipStrCell[3]);

            return ipByteBuf;
        }

        static public UInt32 DateTime_to_UTC(System.DateTime time)
        {
            double intResult = 0;

            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

            intResult = (time - startTime).TotalSeconds;

            return (UInt32)intResult;
        }

        static public DateTime UTC_to_DateTime(UInt32 utc)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

            startTime = startTime.AddSeconds((double)utc);          

            return startTime;
        }
    }
}
