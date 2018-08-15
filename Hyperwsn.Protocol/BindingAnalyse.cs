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
        public DataTable GatewayBinding(byte[] source)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("设备标识", typeof(string));
            dt.Columns.Add("设备名称", typeof(string));
            dt.Columns.Add("温度范围(℃)",typeof(string));
            dt.Columns.Add("湿度范围(%)", typeof(string));


            int current = 0; //标记当前位置
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i]==0xBC && source[i+1]==0xBC)
                {
                    //找到第一条的开始位
                    int length = source[i + 2] + current + 7;
                    if (source.Length >= length)  // 确保有结束位
                    {
                        if(source[length-2]==0xCB && source[length-1]==0xCB)
                        {

                            DataRow row = dt.NewRow();
                            row["ID"] = source[current + 12];
                            row["设备标识"] = CommArithmetic.DecodeMAC(source,current+13);
                            row["设备名称"] = CommArithmetic.DecodeGB2312(source, current + 20 + source[current+17], source[current + 19 + source[current + 17]]);
                            //row["ID"] = source[current + 12];
                            //row["ID"] = source[current + 12];
                            double tempLow=-300;
                            double tempHigh=-300;
                            double humiLow = -300;
                            double humiHigh = -300;

                            //将传感信息单独形成一个数组
                            int SensorDataLength = source[current + 17];
                            byte[] sensorBytes = new byte[SensorDataLength];
                            for (int j = 0; j < SensorDataLength; j++)
                            {
                                sensorBytes[j] = source[SensorDataLength + j+2];
                            }

                            for (int j = 0; j < sensorBytes.Length; j=j+4)
                            {
                                if (sensorBytes[j] ==0x65 && sensorBytes[j+1]==0x00) //温度报警下限
                                {
                                     tempLow = CommArithmetic.DecodeTemperature(sensorBytes,j+2);

                                }
                                if (sensorBytes[j] == 0x65 && sensorBytes[j + 1] == 0x01) //温度报警上限
                                {
                                     tempHigh = CommArithmetic.DecodeTemperature(sensorBytes, j + 2);

                                }

                                if (sensorBytes[j] == 0x66 && sensorBytes[j + 1] == 0x00) //湿度报警下限
                                {
                                    humiLow = CommArithmetic.DecodeHumidity(sensorBytes, j + 2);

                                }
                                if (sensorBytes[j] == 0x66 && sensorBytes[j + 1] == 0x01) //湿度报警上限
                                {
                                    humiHigh = CommArithmetic.DecodeHumidity(sensorBytes, j + 2);

                                }

                            }
                            string tempLowString;
                            string tempHighString;
                            string humiLowString;
                            string humiHighString;
                            if (tempLow==-300)
                            {
                                tempLowString = "-∞";
                            }
                            else
                            {
                                tempLowString = tempLow.ToString("0.00");
                            }
                            if (tempHigh == -300)
                            {
                                tempHighString = "∞";
                            }
                            else
                            {
                                tempHighString = tempHigh.ToString("0.00");
                            }

                            if (humiLow == -300)
                            {
                                humiLowString = "-∞";
                            }
                            else
                            {
                                humiLowString = humiLow.ToString("0.00");
                            }
                            if (humiHigh == -300)
                            {
                                humiHighString = "∞";
                            }
                            else
                            {
                                humiHighString = humiHigh.ToString("0.00");
                            }


                            row["温度范围(℃)"] = tempLowString + "~"+ tempHighString;
                            row["湿度范围(%)"] = humiLowString + "~" + humiHighString;


                            dt.Rows.Add(row);

                            
                            current = length;
                            i = current - 1;
                        }
                    }
                }

            }








            return dt;
        }
    }
}
