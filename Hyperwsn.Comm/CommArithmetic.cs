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

        public static byte[] HexStringToByteArray(string s)
        {
            try
            {
                s = s.Replace(" ", "");
                byte[] buffer = new byte[s.Length / 2];
                for (int i = 0; i < s.Length; i += 2)
                    buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
                return buffer;
            }
            catch (Exception)
            {

                return null;
            }

        }

        public static string ByteArrayToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2") + " ");
                }
                hexString = strB.ToString();
            }
            return hexString;

        }

        public static string DecodeMAC(byte[] source, int start)
        {
            byte[] mac = new byte[4];
            if (source != null && source.Length > start + 3)
            {
                mac[0] = source[start];
                mac[1] = source[start + 1];
                mac[2] = source[start + 2];
                mac[3] = source[start + 3];
                return ByteArrayToHexString(mac);

            }

            return null;


        }

        public static string DecodeClientID(byte[] source, int start)
        {
            byte[] clientID = new byte[2];
            if (source != null && source.Length > start + 1)
            {
                clientID[0] = source[start];
                clientID[1] = source[start + 1];

                return ByteArrayToHexString(clientID);

            }

            return null;


        }

        public static DateTime DecodeDateTime(byte[] source, int start)
        {
            DateTime dt;
            byte[] tempDate = new byte[6];

            if (source != null && source.Length > start + 5)
            {
                tempDate[0] = source[start];
                tempDate[1] = source[start + 1];
                tempDate[2] = source[start + 2];
                tempDate[3] = source[start + 3];
                tempDate[4] = source[start + 4];
                tempDate[5] = source[start + 5];
            }


            string strDate = ByteArrayToHexString(tempDate);




            try
            {
                dt = DateTime.ParseExact(strDate, "yy MM dd HH mm ss ", System.Globalization.CultureInfo.CurrentCulture);

            }
            catch (Exception)
            {
                dt = DateTime.ParseExact("20010101", "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
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
                tempCalc -= 65536;
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




    }
}
