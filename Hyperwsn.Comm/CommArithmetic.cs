using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperwsn.Comm
{
    public class CommArithmetic
    {
        /// <summary>
        /// 计算PPM差异
        /// </summary>
        /// <param name="actrueFreq">实际测试值</param>
        /// <param name="exceptRreq">参考值</param>
        /// <returns></returns>
        public static double PPM(double actrueFreq,double exceptRreq)
        {
            return (actrueFreq - exceptRreq) / exceptRreq * 1000000;
        }

        public static Int32 Bytes2Int(byte[] source)
        {
            if (source == null)
            {
                return 0;
            }
            if (source.Length==1)
            {
                return source[0];
            }
            if (source.Length == 2)
            {
                return source[0] * 256 + source[1];
            }
            if (source.Length == 3)
            {
                return source[0] * 65536 + source[1] * 256+ source[2];
            }

            return 0;
        }

        public static Int32 Bytes2Int(byte[] source,int Start,int Length)
        {
            if (Length==2)
            {
                return source[Start] * 256 + source[Start + 1];
            }
            if (Length==3)
            {
                return source[Start] * 65536 + source[Start + 1] * 256 + source[Start + 2]; 
            }

            return 0;
        }

        public static string IntToHexString(int source)
        {
            string s = source.ToString("X2");
            if (s.Length%2==1)
            {
                s = "0" + s;
            }

            string result="";
            for (int i = 0; i < s.Length/2; i++)
            {
                result = result+ s.Substring(i*2,2) +" ";
            }

            result = result.Trim();                

            return result;
        }

        public static UInt32 ByteBuf_to_UInt32(byte[] source, int start)
        {
            return ((UInt32)source[start] << 24) | ((UInt32)source[start + 1] << 16) | ((UInt32)source[start + 2] << 8) | ((UInt32)source[start + 3] << 0);
        }

        public static UInt16 ByteBuf_to_UInt16(byte[] source, int start)
        {
            return (UInt16)(((UInt16)source[start] << 8) | ((UInt16)source[start + 1] << 0));
        }

        /// <summary>
        /// 将一个字节按照有符号数来解析
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static Int16 ByteBuf_to_Int8(byte[] source, int start)
        {
            Int16 val = (Int16)source[start];

            if (val >= 0x80)
            {
                val -= 0x100;
            }

            return val;
        }

        public static string ByteBuf_to_HexString(byte[] source, int start, int len)
        {
            string hexStr = string.Empty;

            for (int iX = 0; iX < len; iX++)
            {
                hexStr += source[start + iX].ToString("X2") + " ";
            }

            return hexStr.Trim();
        }

        public static byte[] HexStringToByteArray(string s)
        {
            try
            {
                s = s.Replace(" ", "");

                byte[] buffer = new byte[s.Length / 2];

                for (int i = 0; (i < s.Length && (i + 2) <= s.Length); i += 2)
                {
                    buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
                }

                return buffer;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string ByteArrayToHexString(byte[] Buf)
        {
            return ByteBuf_to_HexString(Buf, 0, Buf.Length);
        }

        public static string ByteArrayToHexString(byte[,] Buf, UInt16 IndexOfStart, UInt16 Len)
        {
            string hexString = string.Empty;

            if (Buf != null)
            {
                StringBuilder strB = new StringBuilder();

                for (UInt16 i = 0; i < Len; i++)
                {
                    strB.Append(Buf[IndexOfStart + i, 0].ToString("X2") + " ");
                    strB.Append(Buf[IndexOfStart + i, 1].ToString("X2") + " ");
                }

                hexString = strB.ToString();
            }

            return hexString.Trim();
        }

        /// <summary>
        /// 将字节数组转换为GB2312 编码
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static string DecodeGB2312(byte[] source, int start,int length)
        {
            byte[] gbString = new byte[length];
            for (int i = 0; i < length; i++)
            {
                gbString[i] = source[start + i];
            }            

            return Encoding.GetEncoding("GB18030").GetString(gbString);
        }

        public static DateTime DecodeDateTime(byte[] source, int start)
        {
            DateTime dt;

            byte year = source[start + 0];
            byte month = source[start + 1];
            byte mday = source[start + 2];
            byte hour = source[start + 3];
            byte minute = source[start + 4];
            byte second = source[start + 5];

            string DateStr = "20" + year.ToString("X2") + "-" + month.ToString("X2") + "-" + mday.ToString("X2") + " " + hour.ToString("X2") + ":" + minute.ToString("X2") + ":" + second.ToString("X2");

            try
            {
                dt = DateTime.ParseExact(DateStr, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {   
                dt = DateTime.ParseExact("20000401", "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
            }

            return dt;
        }

        /// <summary>
        /// 将温度16进制字节数组，转换为浮点数
        /// </summary>
        /// <param name="SourceData"></param>
        /// <param name="Start"></param>
        /// <returns></returns>
        public static double DecodeTemperature(byte[] SourceData, int Start)
        {
            int tempCalc = SourceData[Start] * 256 + SourceData[Start + 1];
            if (tempCalc >= 0x8000)
            {
                tempCalc -= 65536;
            }

            return Math.Round((Convert.ToDouble(tempCalc) / 100), 2);
        }

        public static double DecodeHumidity(byte[] SourceData, int Start)
        {
            return Math.Round(Convert.ToDouble((SourceData[Start] * 256 + SourceData[Start + 1])) / 100, 2);
        }

        public static double DecodeVoltage(byte[] SourceData, int Start)
        {
            UInt16 voltint = (UInt16)((UInt16)SourceData[Start] * (UInt16)256 + (UInt16)SourceData[Start + 1]);
            double volt;
            if (voltint >= 32768)
            {
                //连接到充电器
                volt = ((double)voltint - 32768) / (double)1000;
            }
            else
            {
                //未连接充电器
                volt = (double)voltint / (double)1000;
            }
            return Math.Round(volt, 2);
        }

        public static double DecodeSensorVoltage(byte[] SourceData, int Start)
        {
            double volt = (double)(SourceData[Start] * 256 + SourceData[Start + 1]) / (double)1000;

            return Math.Round(volt, 2);

        }

        public static double SHT20Voltage(byte a, byte b)
        {
            //x = MSB*256 + LSB， U = x*4/1023

            double c = Math.Round(((a * 256 + b) * 4 / (double)1023), 2);
            return c;

        }

        public static byte DecodeACPower(byte a)
        {
            if (a >= 0x80)
            {
                return 1;

            }
            else
            {
                return 0;
            }
        }

        public static double SHT20Temperature(byte a, byte b)
        {
            //double a = ((Convert.ToInt32(buf[bufRef + 5].ToString("X2"), 16) * 256 + Convert.ToInt32(buf[bufRef + 6].ToString("X2"), 16)) / 65536) * 175.72 - 46.85;
            double c = Math.Round((-46.85 + 175.72 * (a * 256 + b) / 65536), 2);

            return c;

        }


        public static double SHT20Humidity(byte a, byte b)
        {
            //double a = ((Convert.ToInt32(buf[bufRef + 5].ToString("X2"), 16) * 256 + Convert.ToInt32(buf[bufRef + 6].ToString("X2"), 16)) / 65536) * 175.72 - 46.85;
            double c = Math.Round((0 - 6.0 + 125.0 * (a * 256.0 + b) / 65536), 2);

            return c;

        }

        /// <summary>
        /// 将3个字节的数字转换成字节数组</br>
        /// 适用于Interval的结算
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] Int32_3Bytes(Int32 source)
        {
            byte[] result = new byte[3];
            if (source < 256)
            {
                result[0] = 0;
                result[1] = 0;
                result[2] = (byte)source;
            }
            else if  (source<65536)
            {
                result[0] = 0;
                result[1] = (byte)(source / 256);
                result[2] = (byte)(source - result[0] * 256);

            }
            else
            {
                result[0] = (byte)(source / 256/256);
                result[1] = (byte)((source - 65536)/256);
                result[2] = (byte)(source - 65536 -result[1]*256);
            }


            return result;
        }

        /// <summary>
        /// 将2个字节的数字转换成字节数组</br>
        /// 适用于Interval的结算
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] Int16_2Bytes(int source)
        {
            byte[] result = new byte[2];
            if (source < 256)
            {
                result[0] = 0;
                result[1] = (byte)source;
            }
            else
            {
                result[0] = (byte)(source / 256);
                result[1] = (byte)(source - result[0] * 256);

            }


            return result;
        }

        /// <summary>
        /// 将2个字节的浮点数转换成字节数组</br>
        /// 适用于温湿度各种阈值的反相解算
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] Double_2Bytes(double source)
        {
            source = source * 100;
            if (source < 0)
            {
                source += 65536;
            }

            byte[] result = new byte[2];
            if (source < 256)
            {
                result[0] = 0;
                result[1] = (byte)source;
            }
            else
            {
                result[0] = (byte)(source / 256);
                result[1] = (byte)(source - result[0] * 256);

            }


            return result;
        }


        public static byte[] EncodeDateTime(DateTime dateTime)
        {

            string dateString = (dateTime.Year - 2000).ToString();
            if (dateTime.Month < 10)
            {
                dateString += " 0" + dateTime.Month;

            }
            else
            {
                dateString += " " + dateTime.Month;
            }

            if (dateTime.Day < 10)
            {
                dateString += " 0" + dateTime.Day;

            }
            else
            {
                dateString += " " + dateTime.Day;
            }


            if (dateTime.Hour < 10)
            {
                dateString += " 0" + dateTime.Hour;

            }
            else
            {
                dateString += " " + dateTime.Hour;
            }

            if (dateTime.Minute < 10)
            {
                dateString += " 0" + dateTime.Minute;

            }
            else
            {
                dateString += " " + dateTime.Minute;
            }

            if (dateTime.Second < 10)
            {
                dateString += " 0" + dateTime.Second;

            }
            else
            {
                dateString += " " + dateTime.Second;
            }

            byte[] datetimeByte = CommArithmetic.HexStringToByteArray(dateString);

            return datetimeByte;
        }

        ///<summary>
        ///由秒数得到日期几天几小时。。。
        ///</summary
        ///<param name="t">秒数</param>
        ///<param name="type">0：转换后带秒，1:转换后不带秒</param>
        ///<returns>几天几小时几分几秒</returns>
        public static string Second2String(int t)
        {
            string r = "";
            int day, hour, minute, second;
            if (t >= 86400) //天,
            {
                day = Convert.ToInt16(t / 86400);
                hour = Convert.ToInt16((t % 86400) / 3600);
                minute = Convert.ToInt16((t % 86400 % 3600) / 60);
                second = Convert.ToInt16(t % 86400 % 3600 % 60);
               
                    r = day + "天" + hour + (":") + minute + (":") + second ;
              

            }
            else if (t >= 3600)//时,
            {
                hour = Convert.ToInt16(t / 3600);
                minute = Convert.ToInt16((t % 3600) / 60);
                second = Convert.ToInt16(t % 3600 % 60);
                
                    r = hour + (":") + minute + (":") + second ;
               
            }
            else if (t >= 60)//分
            {
                minute = Convert.ToInt16(t / 60);
                second = Convert.ToInt16(t % 60);
                r ="00:" + minute + (":") + second ;
            }
            else
            {
                second = Convert.ToInt16(t);
                r = "00:00"+second + ("second");
            }
            return r;
        }


        /// <summary>
        /// 将2个字节的数字转换成字节数组</br>
        /// 适用于Interval的结算
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string DecodeByte2String(byte[] source,int start,int length)
        {
            byte[] byteArray = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byteArray[i] = source[start + i];
            }

            string str = System.Text.Encoding.UTF8.GetString(byteArray);

            return str;
        }

        /// <summary>
        /// 将2个字节的数字转换成字节数组</br>
        /// 适用于Interval的结算
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] EncodeByte2String(string source)
        {
            byte[] strToBytes1 = System.Text.Encoding.UTF8.GetBytes(source);



            return strToBytes1;
        }

        /// <summary>
        /// 将温度转换为2个字节的数组
        /// </summary>
        /// <param name="temperature"></param>
        /// <returns></returns>
        public static byte[] EncodeTemperature(double temperature)
        {

           


            return null;
        }

        /// <summary>
        /// 将湿度转换为2个字节的数组
        /// </summary>
        /// <param name="temperature"></param>
        /// <returns></returns>
        public static byte[] EncodeHumidity(double temperature)
        {




            return null;
        }




    }
}
