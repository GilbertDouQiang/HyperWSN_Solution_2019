using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Hyperwsn.Comm;

namespace Hyperwsn.Protocol
{
    public class BindingAnalyse
    {
        private Int16 RxPktIsRight(byte[] SrcBuf, UInt16 IndexOfStart)
        {
            if (SrcBuf[IndexOfStart] != 0xBC || SrcBuf[IndexOfStart + 1] != 0xBC)
            {
                return -1;
            }

            byte PktLen = SrcBuf[IndexOfStart + 2];
            if (SrcBuf[IndexOfStart + 3 + PktLen + 2] != 0xCB || SrcBuf[IndexOfStart + 3 + PktLen + 3] != 0xCB)
            {
                return -2;
            }

            UInt16 crc_chk = (UInt16)(((UInt16)SrcBuf[IndexOfStart + 3 + PktLen] << 8) | ((UInt16)SrcBuf[IndexOfStart + 4 + PktLen] << 0));
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, SrcBuf, (UInt16)(IndexOfStart + 3), PktLen);
            if (crc_chk != 0 && crc_chk != crc)
            {
                return -3;
            }

            // Cmd
            if (SrcBuf[IndexOfStart + 3] != 0x6A)
            {
                return -4;
            }

            // Protocol
            if (SrcBuf[IndexOfStart + 4] != 0x01)
            {
                return -5;
            }

            // Error
            if (SrcBuf[IndexOfStart + 10] != 0x00)
            {
                return -6;
            }

            return 0;
        }

        /// <summary>
        /// 读取可变长度的负载内容
        /// </summary>
        /// <param name="SrcBuf"></param>
        /// <param name="IndexOfStart"></param>
        /// <returns></returns>
        private Int16 ReadPayload(byte[] SrcBuf, UInt16 IndexOfStart, DataRow row)
        {
            string tempThrLowStr = "-∞";
            string tempThrHighStr = "+∞";
            string humThrLowStr = "0.00";
            string humThrHighStr = "100.00";
            string deviceName = "";

            row["温度范围(℃)"] = tempThrLowStr + " ~ " + tempThrHighStr;
            row["湿度范围(%)"] = humThrLowStr + " ~ " + humThrHighStr;
            row["设备名称"] = deviceName;

            byte PayLen = SrcBuf[IndexOfStart + 17];
            if (PayLen == 0)
            {
                return 1;
            }

            byte DataType = 0;
            byte ThrType = 0;

            byte thisIndex = 18;
            byte PayEndIndex = (byte)(thisIndex + PayLen);

            while (thisIndex < PayEndIndex)
            {
                DataType = SrcBuf[IndexOfStart + thisIndex];
                
                switch (DataType)
                {
                    case 0x65:              // 温度
                        {
                            if (PayEndIndex - thisIndex < 4)
                            {
                                return -2;  // 长度错误
                            }

                            ThrType = SrcBuf[IndexOfStart + thisIndex + 1];

                            Int16 tempI = (Int16)(((UInt16)SrcBuf[IndexOfStart + thisIndex + 2] << 8) | ((UInt16)SrcBuf[IndexOfStart + thisIndex + 3] << 0));
                            double tempF = (double)tempI / 100.0f;

                            switch (ThrType)
                            {
                                case 0x00:      // 阈值下限
                                    {
                                        tempThrLowStr = tempF.ToString("F2");
                                        break;
                                    }
                                case 0x01:      // 阈值上限
                                    {
                                        tempThrHighStr = tempF.ToString("F2");
                                        break;
                                    }
                                case 0x02:      // 预警下限
                                    {
                                        break;
                                    }
                                case 0x03:      // 预警上限
                                    {
                                        break;
                                    }
                                default:
                                    {
                                        return -3;
                                    }
                            }

                            thisIndex += 4;
                            break;
                        }
                    case 0x66:              // 湿度
                        {
                            if (PayEndIndex - thisIndex < 4)
                            {
                                return -2;  // 长度错误
                            }

                            ThrType = SrcBuf[IndexOfStart + thisIndex + 1];

                            UInt16 humI = (UInt16)(((UInt16)SrcBuf[IndexOfStart + thisIndex + 2] << 8) | ((UInt16)SrcBuf[IndexOfStart + thisIndex + 3] << 0));
                            double humF = (double)humI / 100.0f;

                            switch (ThrType)
                            {
                                case 0x00:      // 阈值下限
                                    {
                                        humThrLowStr = humF.ToString("F2");
                                        break;
                                    }
                                case 0x01:      // 阈值上限
                                    {
                                        humThrHighStr = humF.ToString("F2");
                                        break;
                                    }
                                case 0x02:      // 预警下限
                                    {
                                        break;
                                    }
                                case 0x03:      // 预警上限
                                    {
                                        break;
                                    }
                                default:
                                    {
                                        return -3;
                                    }
                            }

                            thisIndex += 4;
                            break;
                        }
                    case 0x73:              // 设备名称，GB2312
                        {
                            byte NameLen = SrcBuf[IndexOfStart + thisIndex + 1];

                            if (PayEndIndex - thisIndex < NameLen + 2)
                            {
                                return -2;  // 长度错误
                            }

                            deviceName = Encoding.Default.GetString(SrcBuf, IndexOfStart + thisIndex + 2, NameLen);

                            thisIndex += (byte)(NameLen + 2);
                            break;
                        }
                    default:
                        {
                            return -1;      // 未知的数据类型
                        }
                }
            }

            row["温度范围(℃)"] = tempThrLowStr + " ~ " + tempThrHighStr;
            row["湿度范围(%)"] = humThrLowStr + " ~ " + humThrHighStr;
            row["设备名称"] = deviceName;

            return 0;
        }


        public DataTable GatewayBinding(byte[] SrcBuf)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("通道号", typeof(int));
            dt.Columns.Add("设备编号", typeof(string));
            dt.Columns.Add("设备名称", typeof(string));
            dt.Columns.Add("温度范围(℃)", typeof(string));
            dt.Columns.Add("湿度范围(%)", typeof(string));

            if(SrcBuf == null || SrcBuf.Length == 0 || SrcBuf.Length > 0xFFFF)
            {
                return dt;
            }

            Int16 error = 0;

            byte Total = 0;
            byte Current = 0;            

            for (UInt16 iX = 0; iX < (UInt16)SrcBuf.Length; iX++)
            {
                error = RxPktIsRight(SrcBuf, iX);
                if (error < 0)
                {
                    continue;
                }

                Total = SrcBuf[iX + 11];
                Current = SrcBuf[iX + 12];

                if (Total == 0 || Current >= Total)
                {
                    continue;
                }

                DataRow row = dt.NewRow();
                row["通道号"] = Current + 1;
                row["设备编号"] = CommArithmetic.ByteArrayToHexString(SrcBuf, (UInt16)(iX + 13), 4);

                error = ReadPayload(SrcBuf, iX, row);
                if (error < 0)
                {
                    continue;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
