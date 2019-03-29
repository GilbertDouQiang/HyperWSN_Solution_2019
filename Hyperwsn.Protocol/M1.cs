using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Hyperwsn.Comm;


namespace Hyperwsn.Protocol
{
    public class M1:Sensor//, INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;

        public M1()
        {

        }

        public M1(byte[] SourceData)
        {

            //2017版协议 v3.5
            //判断第三位 ，01 代表从传感器发出的正常数据，长度不是固定值



            //上电自检数据
            if (SourceData.Length==82)
            {
                if (SourceData[4]==0x51)
                {
                    Name = "M1";
                }
                if (SourceData[4] == 0x53)
                {
                    Name = "M1P";
                }
                if (SourceData[4] == 0x5C)
                {
                    Name = "M1NTC";
                }

                DeviceID = SourceData[4].ToString("X2");
                ProtocolVersion = SourceData[5];
                PrimaryMAC = CommArithmetic.DecodeMAC(SourceData, 6);
                DeviceMac = CommArithmetic.DecodeMAC(SourceData, 10);
                HardwareVersion = CommArithmetic.DecodeMAC(SourceData, 14);
                SoftwareVersion = CommArithmetic.DecodeClientID(SourceData,18);
                ClientID = CommArithmetic.DecodeClientID(SourceData, 20);
                byte[] debugBytes = new byte[2];
                debugBytes[0] = SourceData[22];
                debugBytes[1] = SourceData[23];

                DebugString = CommArithmetic.DecodeClientID(SourceData, 22);

                //Debug = debugBytes;
                Category = SourceData[24];
                Interval = SourceData[25] * 256 + SourceData[26];
                Calendar = CommArithmetic.DecodeDateTime(SourceData, 27);


                WorkFunction = SourceData[33];
                SymbolRate = SourceData[34];
                TXPower = SourceData[35];
                TXTimers = SourceData[36];
                Frequency = SourceData[37];

                TemperatureInfoHigh = CommArithmetic.DecodeTemperature(SourceData, 38);
                TemperatureInfoLow = CommArithmetic.DecodeTemperature(SourceData, 40);

                HumidityInfoHigh = CommArithmetic.DecodeHumidity(SourceData, 42);
                HumidityInfoLow = CommArithmetic.DecodeHumidity(SourceData, 44);

                TemperatureWarnHigh = CommArithmetic.DecodeTemperature(SourceData, 46);
                TemperatureWarnLow = CommArithmetic.DecodeTemperature(SourceData, 48);

                HumidityWarnHigh = CommArithmetic.DecodeHumidity(SourceData, 50);
                HumidityWarnLow = CommArithmetic.DecodeHumidity(SourceData, 52);

                TemperatureCompensation = CommArithmetic.DecodeTemperature(SourceData, 54);
                HumidityCompensation = CommArithmetic.DecodeTemperature(SourceData, 56);

                ICTemperature = SourceData[58];
                Volt = Math.Round(Convert.ToDouble((SourceData[59] * 256 + SourceData[60])) / 1000, 2);

                MaxLength = SourceData[63];

                Temperature = CommArithmetic.DecodeTemperature(SourceData, 73);
                Humidity = CommArithmetic.DecodeHumidity(SourceData, 75);
                RSSI = SourceData[78] - 256;

                //Falsh
                FlashID = CommArithmetic.DecodeClientID(SourceData, 61);
                FlashFront = SourceData[64] * 256 * 256 + SourceData[65] * 256 + SourceData[66];
                FlashRear = SourceData[67] * 256 * 256 + SourceData[68] * 256 + SourceData[69];
                FlashQueueLength = SourceData[70] * 256 * 256 + SourceData[71] * 256 + SourceData[72];


            }


            //兼容模式，兼容Z模式
            if (SourceData.Length == 28)
            {
                Name = "M1";
                DeviceID = "51";
                DeviceMac = CommArithmetic.DecodeMAC(SourceData, 5);
                ClientID = CommArithmetic.DecodeClientID(SourceData, 3);
                WorkFunction = SourceData[2];
                ProtocolVersion = 0x00;

                SensorSN = SourceData[12] * 256 + SourceData[13];
                //传感器信息
                ICTemperature = 0; //老协议中没有IC 温度


                Volt = CommArithmetic.SHT20Voltage(SourceData[24],SourceData[25]);


                Temperature = CommArithmetic.SHT20Temperature(SourceData[16], SourceData[17]);


                Humidity = CommArithmetic.SHT20Humidity(SourceData[20],SourceData[21]);
                //广播模式，补充采集和传输时间
                SensorCollectTime = System.DateTime.Now;
                SensorTransforTime = System.DateTime.Now;
                //RSSI = SourceData[30] / 2 - 138; //1101 方案
                //可能收到没有RSSI的数据
                if (SourceData.Length == 28)
                {
                    RSSI = SourceData[27] - 256;
                }
                this.SourceData = CommArithmetic.ByteArrayToHexString(SourceData);


            }


            //模式1 正常传输的数据，
            if (SourceData.Length==31)
            {
                
                //将收到的数据填充到属性
                Name = "M1";
                DeviceID = SourceData[3].ToString("X2");
                DeviceMac = CommArithmetic.DecodeMAC(SourceData, 7);
                ClientID = CommArithmetic.DecodeClientID(SourceData, 5);
                WorkFunction = SourceData[2];
                ProtocolVersion = SourceData[4];

                SensorSN = SourceData[13] * 256 + SourceData[14];
                //传感器信息
                ICTemperature = SourceData[16]; //todo : 有符号整形数
                if (ICTemperature >= 128)
                    ICTemperature -= 256;

                Volt = Math.Round(Convert.ToDouble((SourceData[18] * 256 + SourceData[19])) / 1000, 2);
                int tempCalc = SourceData[21] * 256 + SourceData[22];
                if (tempCalc >= 0x8000)
                    tempCalc -= 65536;
                Temperature = Math.Round((Convert.ToDouble(tempCalc) / 100), 2);


                Humidity = Math.Round(Convert.ToDouble((SourceData[24] * 256 + SourceData[25])) / 100, 2);
                //广播模式，补充采集和传输时间
                SensorCollectTime = System.DateTime.Now;
                SensorTransforTime = System.DateTime.Now;
                //RSSI = SourceData[30] / 2 - 138; //1101 方案
                //可能收到没有RSSI的数据
                if (SourceData.Length == 31)
                {
                    RSSI = SourceData[30] - 256;
                }
                this.SourceData = CommArithmetic.ByteArrayToHexString(SourceData);


            }




        }

        public byte[] UpdateFactory()
        {
            byte[] updateBytes = new byte[21];
            updateBytes[0] = 0xCE;
            updateBytes[1] = 0x10;
            updateBytes[2] = 0xA1;
            updateBytes[3] = 0x01;

            //兼容M1 和 M1P
            if (DeviceID == "51")
                updateBytes[4] = 0x51;
            else if (DeviceID == "53")
                updateBytes[4] = 0x53;
            else if (DeviceID == "57")
                updateBytes[4] = 0x57;
            else if (DeviceID == "5C")
                updateBytes[4] = 0x5C;

            updateBytes[5] = 0x02;

            byte[] deviceMacBytes = CommArithmetic.HexStringToByteArray(DeviceMac);
            updateBytes[6] = deviceMacBytes[0];
            updateBytes[7] = deviceMacBytes[1];
            updateBytes[8] = deviceMacBytes[2];
            updateBytes[9] = deviceMacBytes[3];

            deviceMacBytes = CommArithmetic.HexStringToByteArray(DeviceNewMAC);
            updateBytes[10] = deviceMacBytes[0];
            updateBytes[11] = deviceMacBytes[1];
            updateBytes[12] = deviceMacBytes[2];
            updateBytes[13] = deviceMacBytes[3];

            deviceMacBytes = CommArithmetic.HexStringToByteArray(HardwareVersion);
            updateBytes[14] = deviceMacBytes[0];
            updateBytes[15] = deviceMacBytes[1];
            updateBytes[16] = deviceMacBytes[2];
            updateBytes[17] = deviceMacBytes[3];


            updateBytes[18] = 0x00;
            updateBytes[19] = 0x00;
            updateBytes[20] = 0xEC;

            //updateBytes[0] = 0xCE;




            return updateBytes;
        }

        public byte[] UpdateUserConfig()
        {
            byte[] updateBytes = new byte[27];
            updateBytes[0] = 0xCE;
            if (DeviceID == "57")
            {
                updateBytes[1] = 0x14;
            }
                
            else
            {
                updateBytes[1] = 0x16;
            }
            
            updateBytes[2] = 0xA2;
            updateBytes[3] = 0x01;
            //兼容M1 和 M1P
            if (DeviceID == "51")
                updateBytes[4] = 0x51;
            else if (DeviceID == "53")
                updateBytes[4] = 0x53;
            else if (DeviceID == "57")
                updateBytes[4] = 0x57;
            else if (DeviceID == "5C")
                updateBytes[4] = 0x5C;


            updateBytes[5] = 0x02;
            //Mac
            byte[] deviceMacBytes = CommArithmetic.HexStringToByteArray(DeviceMac);
            updateBytes[6] = deviceMacBytes[0];
            updateBytes[7] = deviceMacBytes[1];
            updateBytes[8] = deviceMacBytes[2];
            updateBytes[9] = deviceMacBytes[3];
            //clientID
            deviceMacBytes = CommArithmetic.HexStringToByteArray(ClientID);
            updateBytes[10] = deviceMacBytes[0];
            updateBytes[11] = deviceMacBytes[1];

            //Debug
            deviceMacBytes = CommArithmetic.HexStringToByteArray(DebugString);          
            updateBytes[12] = deviceMacBytes[0];
            updateBytes[13] = deviceMacBytes[1];

            //category
            updateBytes[14] = Category;
            //WorkFunction
            updateBytes[15] = WorkFunction;
            //SymbolRate
            updateBytes[16] = SymbolRate;
            //TXPower
            updateBytes[17] = TXPower;
            //Frequency
            updateBytes[18] = Frequency;

            //温度补偿：TODO 暂未实现
            updateBytes[19] = 0x00;
            updateBytes[20] = 0x00;

            if (DeviceID == "57")
            {
                //湿度补偿：TODO 暂未实现,M2 没有湿度补偿
               

                updateBytes[21] = MaxLength;

                //CRC：TODO 暂未实现
                updateBytes[22] = 0x00;
                updateBytes[23] = 0x00;


                updateBytes[24] = 0xEC;
            }
            else
            {
                updateBytes[21] = 0x00;
                updateBytes[22] = 0x00;

                updateBytes[23] = MaxLength;

                //CRC：TODO 暂未实现
                updateBytes[24] = 0x00;
                updateBytes[25] = 0x00;


                updateBytes[26] = 0xEC;

            }
                

            //updateBytes[0] = 0xCE;




            return updateBytes;
        }


        public byte[] UpdateApplicationConfig()
        {
            byte[] updateBytes = new byte[38];
            updateBytes[0] = 0xCE;
            if (DeviceID != "57")
            {
                updateBytes[1] = 0x21;
            }
            else
            {
                updateBytes[1] = 0x19;

            }
                
            updateBytes[2] = 0xA3;
            updateBytes[3] = 0x01;
            //兼容M1 和 M1P
            if (DeviceID == "51")
                updateBytes[4] = 0x51;
            else if (DeviceID == "53")
                updateBytes[4] = 0x53;
            else if (DeviceID == "57")
                updateBytes[4] = 0x57;
            else if (DeviceID == "5C")
                updateBytes[4] = 0x5C;

            updateBytes[5] = 0x02;
            //Mac
            byte[] deviceMacBytes = CommArithmetic.HexStringToByteArray(DeviceMac);
            updateBytes[6] = deviceMacBytes[0];
            updateBytes[7] = deviceMacBytes[1];
            updateBytes[8] = deviceMacBytes[2];
            updateBytes[9] = deviceMacBytes[3];
            //Interval
            deviceMacBytes = CommArithmetic.Int16_2Bytes(Interval);
            updateBytes[10] = deviceMacBytes[0];
            updateBytes[11] = deviceMacBytes[1];

            //Calendar
            deviceMacBytes = CommArithmetic.EncodeDateTime(Calendar);
            updateBytes[12] = deviceMacBytes[0];
            updateBytes[13] = deviceMacBytes[1];
            updateBytes[14] = deviceMacBytes[2];
            updateBytes[15] = deviceMacBytes[3];
            updateBytes[16] = deviceMacBytes[4];
            updateBytes[17] = deviceMacBytes[5];

            updateBytes[18] = TXTimers;

            if (DeviceID == "57")
            {
                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureInfoHigh);
                updateBytes[19] = deviceMacBytes[0];
                updateBytes[20] = deviceMacBytes[1];

                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureInfoLow);
                updateBytes[21] = deviceMacBytes[0];
                updateBytes[22] = deviceMacBytes[1];

               

                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureWarnHigh);
                updateBytes[23] = deviceMacBytes[0];
                updateBytes[24] = deviceMacBytes[1];


                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureWarnLow);
                updateBytes[25] = deviceMacBytes[0];
                updateBytes[26] = deviceMacBytes[1];

               



                //CRC：TODO 暂未实现
                updateBytes[27] = 0x00;
                updateBytes[28] = 0x00;


                updateBytes[29] = 0xEC;

            }
            else
            {
                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureInfoHigh);
                updateBytes[19] = deviceMacBytes[0];
                updateBytes[20] = deviceMacBytes[1];

                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureInfoLow);
                updateBytes[21] = deviceMacBytes[0];
                updateBytes[22] = deviceMacBytes[1];

                deviceMacBytes = CommArithmetic.Double_2Bytes(HumidityInfoHigh);
                updateBytes[23] = deviceMacBytes[0];
                updateBytes[24] = deviceMacBytes[1];

                deviceMacBytes = CommArithmetic.Double_2Bytes(HumidityInfoLow);
                updateBytes[25] = deviceMacBytes[0];
                updateBytes[26] = deviceMacBytes[1];

                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureWarnHigh);
                updateBytes[27] = deviceMacBytes[0];
                updateBytes[28] = deviceMacBytes[1];


                deviceMacBytes = CommArithmetic.Double_2Bytes(TemperatureWarnLow);
                updateBytes[29] = deviceMacBytes[0];
                updateBytes[30] = deviceMacBytes[1];

                deviceMacBytes = CommArithmetic.Double_2Bytes(HumidityWarnHigh);
                updateBytes[31] = deviceMacBytes[0];
                updateBytes[32] = deviceMacBytes[1];

                deviceMacBytes = CommArithmetic.Double_2Bytes(HumidityWarnLow);
                updateBytes[33] = deviceMacBytes[0];
                updateBytes[34] = deviceMacBytes[1];



                //CRC：TODO 暂未实现
                updateBytes[35] = 0x00;
                updateBytes[36] = 0x00;


                updateBytes[37] = 0xEC;

            }
            

            //updateBytes[0] = 0xCE;




            return updateBytes;
        }

        public byte[] DeleteData()
        {
            byte[] updateBytes = new byte[19];
            updateBytes[0] = 0xCE;
            updateBytes[1] = 0x0E;
            updateBytes[2] = 0xA4;
            updateBytes[3] = 0x01;
            //兼容M1 和 M1P
            if (DeviceID == "51")
                updateBytes[4] = 0x51;
            else if (DeviceID == "53")
                updateBytes[4] = 0x53;
            else if (DeviceID == "57")
                updateBytes[4] = 0x57;
            else if (DeviceID == "5C")
                updateBytes[4] = 0x5C;

            updateBytes[5] = 0x02;

            byte[] deviceMacBytes = CommArithmetic.HexStringToByteArray(DeviceMac);
            updateBytes[6] = deviceMacBytes[0];
            updateBytes[7] = deviceMacBytes[1];
            updateBytes[8] = deviceMacBytes[2];
            updateBytes[9] = deviceMacBytes[3];

           
            updateBytes[18] = 0xEC;

           




            return updateBytes;
        }


        

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
        /// 生产过程中，利用串口数据读取参数配置
        /// </summary>
        /// <param name="response"></param>
       

        /// <summary>
        /// 生产过程中，利用串口数据读取参数配置
        /// </summary>
        /// <param name="response"></param>
        public void RFInit(byte[] response)
        {

            if (response[3] == 0x51)
            {
                Name = "M1";

            }
            else if (response[3] == 0x5C)
            {
                Name = "M1 NTC";
            }
            else if (response[3] == 0x5D)
            {
                Name = "M1 Beetech";
            }

            PrimaryMAC = CommArithmetic.DecodeMAC(response, 5);
            DeviceID = response[3].ToString("X2"); ;
            DeviceMac = CommArithmetic.DecodeMAC(response, 9);
            HardwareVersion = CommArithmetic.DecodeMAC(response, 13);
            SoftwareVersion = CommArithmetic.DecodeClientID(response, 17);
            ClientID = CommArithmetic.DecodeClientID(response, 19);
            byte[] debug = new byte[2];
            debug[0] = response[21];
            debug[1] = response[22];
            DebugString = CommArithmetic.DecodeClientID(response, 21);
            //Debug = debug;
            Category = response[23];
            Interval = response[24] * 256 + response[25];
            Calendar = CommArithmetic.DecodeDateTime(response, 26);
            WorkFunction = response[32];
            SymbolRate = response[33];
            TXPower = response[34];
            TXTimers = response[35];
            Frequency = response[36];

            TemperatureInfoHigh = CommArithmetic.DecodeTemperature(response, 37);
            TemperatureInfoLow = CommArithmetic.DecodeTemperature(response, 39);

            HumidityInfoHigh = CommArithmetic.DecodeHumidity(response, 41);
            HumidityInfoLow = CommArithmetic.DecodeHumidity(response, 43);

            TemperatureWarnHigh = CommArithmetic.DecodeTemperature(response, 45);
            TemperatureWarnLow = CommArithmetic.DecodeTemperature(response, 47);

            HumidityWarnHigh = CommArithmetic.DecodeHumidity(response, 49);
            HumidityWarnLow = CommArithmetic.DecodeHumidity(response, 51);

            TemperatureCompensation = CommArithmetic.DecodeTemperature(response, 53);
            HumidityCompensation = CommArithmetic.DecodeTemperature(response, 55);

            MaxLength = response[62];

            ICTemperature = response[57];
            Volt = Math.Round(Convert.ToDouble((response[58] * 256 + response[59])) / 1000, 2);



            Temperature = CommArithmetic.DecodeTemperature(response, 72);
            Humidity = CommArithmetic.DecodeHumidity(response, 74);




            //Falsh
            FlashID = CommArithmetic.DecodeClientID(response, 60);
            FlashFront = response[63] * 256 * 256 + response[64] * 256 + response[65];
            FlashRear = response[66] * 256 * 256 + response[67] * 256 + response[68];
            FlashQueueLength = response[69] * 256 * 256 + response[70] * 256 + response[71];



            RSSI = response[80] - 256;







        }


        public void RFInitV3(byte[] response)
        {

            if (response[3] == 0x51)
            {
                Name = "M1";

            }
            else if (response[3] == 0x5C)
            {
                Name = "M1 NTC";
            }
            else if (response[3] == 0x5D)
            {
                Name = "M1 Beetech";
            }

            PrimaryMAC = CommArithmetic.DecodeMAC(response, 5);
            DeviceID = response[3].ToString("X2"); ;
            DeviceMac = CommArithmetic.DecodeMAC(response, 9);
            HardwareVersion = CommArithmetic.DecodeMAC(response, 13);
            SoftwareVersion = CommArithmetic.DecodeClientID(response, 17);
            ClientID = CommArithmetic.DecodeClientID(response, 19);
            byte[] debug = new byte[2];
            debug[0] = response[21];
            debug[1] = response[22];
            DebugString = CommArithmetic.DecodeClientID(response, 21);
            //Debug = debug;
            Category = response[23];
            Interval = response[24] * 256 + response[25];
            Calendar = CommArithmetic.DecodeDateTime(response, 26);
            WorkFunction = response[32];
            SymbolRate = response[33];
            TXPower = response[34];
            TXTimers = response[35];
            Frequency = response[36];

            TemperatureInfoHigh = CommArithmetic.DecodeTemperature(response, 37);
            TemperatureInfoLow = CommArithmetic.DecodeTemperature(response, 39);

            HumidityInfoHigh = CommArithmetic.DecodeHumidity(response, 41);
            HumidityInfoLow = CommArithmetic.DecodeHumidity(response, 43);

            TemperatureWarnHigh = CommArithmetic.DecodeTemperature(response, 45);
            TemperatureWarnLow = CommArithmetic.DecodeTemperature(response, 47);

            HumidityWarnHigh = CommArithmetic.DecodeHumidity(response, 49);
            HumidityWarnLow = CommArithmetic.DecodeHumidity(response, 51);

            TemperatureCompensation = CommArithmetic.DecodeTemperature(response, 53);
            HumidityCompensation = CommArithmetic.DecodeTemperature(response, 55);

            MaxLength = response[62];

            ICTemperature = response[57];
            Volt = Math.Round(Convert.ToDouble((response[58] * 256 + response[59])) / 1000, 2);



            Temperature = CommArithmetic.DecodeTemperature(response, 72);
            Humidity = CommArithmetic.DecodeHumidity(response, 74);




            //Falsh
            FlashID = CommArithmetic.DecodeClientID(response, 60);
            FlashFront = response[63] * 256 * 256 + response[64] * 256 + response[65];
            FlashRear = response[66] * 256 * 256 + response[67] * 256 + response[68];
            FlashQueueLength = response[69] * 256 * 256 + response[70] * 256 + response[71];



            RSSI = response[88] - 256;

            //v3版本价格各种间隔

            //IntervalNormal
            IntervalNormal = response[76] * 256 + response[77];
            IntervalWarning = response[78] * 256 + response[79];
            IntervalAlarm = response[80] * 256 + response[81];





        }








        /*
        public void OnPropertyChanged(String strProperty)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(strProperty));
            }
        }
        */

    }
}
