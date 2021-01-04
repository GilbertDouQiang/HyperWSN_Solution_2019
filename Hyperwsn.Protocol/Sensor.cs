using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hyperwsn.Protocol
{
    public class Sensor:Device
    {
        public UInt16 SensorSN { get; set; }

        public DateTime SensorCollectTime { get; set; }
       
        public DateTime SensorTransforTime { get; set; }

        public Int16 RSSI { get; set; }

        public byte TxPower { get; set; }

        public byte LastHistory { get; set; }

        public byte AlertStatusBySs { get; set; }

        /// <summary>
        /// 报警项
        /// </summary>
        public byte AlertItemBySs { get; set; }

        /// <summary>
        /// 采集和发送的倍数，最小为1，最大为8
        /// </summary>
        public byte SampleSend { get; set; }

        public string FlashID { get; set; }

        public Int32 FlashFront { get; set; }

        public Int32 FlashRear { get; set; }
        public Int32 FlashQueueLength { get; set; }

        public byte MaxLength { get; set; }

        public Int16 ICTemperature { get; set; }

        /// <summary>
        /// 测量用电流
        /// </summary>
        public double MeasureDCI { get; set; }

        public int ErrorCode { get; set; }

        /// <summary>
        /// 正常状态下的传输时间
        /// </summary>
        public UInt16 IntervalOfNormal { get; set; }

        /// <summary>
        /// 预警状态下的传输间隔
        /// </summary>
        public UInt16 IntervalOfWarn { get; set; }

        /// <summary>
        /// 报警状态下的传输间隔
        /// </summary>
        public UInt16 IntervalOfAlert { get; set; }

        /// <summary>
        /// 系统时间
        /// </summary>
        public DateTime SystemTime { get; set; }
    }
}
