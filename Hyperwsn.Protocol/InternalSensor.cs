using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperwsn.Protocol
{
    public class InternalSensor:Sensor
    {
        /// <summary>
        /// 传感器的编号
        /// </summary>
        public byte iX { get; set; }

        /// <summary>
        /// 内置传感器类型，温度，温湿度，低温
        /// </summary>
        public byte SensorType { get; set; }

        /// <summary>
        /// 内置传感器是否在线：0 = 未接入传感器；未检测到传感器；不支持外接传感器；传感器类型不匹配；1 = 已检测到传感器；传感器在线；
        /// </summary>
        public byte Online { get; set; }

        /// <summary>
        /// 0 = Off； 1 = On；
        /// </summary>
        public byte OnOff { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public double Temp { get; set; }

        /// <summary>
        /// 湿度
        /// </summary>
        public double Hum { get; set; }

        /// <summary>
        /// 温度报警下限
        /// </summary>
        public double TempThrLow { get; set; }

        /// <summary>
        /// 温度报警上限
        /// </summary>
        public double TempThrHigh { get; set; }

        /// <summary>
        /// 湿度报警下限
        /// </summary>
        public double HumThrLow { get; set; }

        /// <summary>
        /// 湿度报警上限
        /// </summary>
        public double HumThrHigh { get; set; }        

        /// <summary>
        /// 温度补偿
        /// </summary>
        public double TempCompensation { get; set; }

        /// <summary>
        /// 湿度补偿
        /// </summary>
        public double HumCompensation { get; set; }
    }
}
