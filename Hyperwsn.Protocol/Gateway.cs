using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperwsn.Protocol
{
    public class Gateway:Device
    {
        /// <summary>
        /// 显示间隔
        /// </summary>
        public int DisplayInterval { get; set; }

        /// <summary>
        /// 报警显示间隔
        /// </summary>
        public int AlarmInterval { get; set; }
        /// <summary>
        /// SG5 中1310 版本号
        /// </summary>
        public string SoftwareVersion2 { get; set; }

        /// <summary>
        /// 转发策略
        /// </summary>
        public byte TransStrategy { get; set; }

        /// <summary>
        /// 显示的时间策略
        /// </summary>
        public byte DateTimeStrategy { get; set; }

        /// <summary>
        /// 报警方式，声光报警
        /// </summary>
        public byte AlarmStyle { get; set; }

        /// <summary>
        /// 报警来源，Sensor, 绑定，不报警
        /// </summary>
        public byte AlarmSource { get; set; }

        /// <summary>
        /// 显示方式：单一显示，智能显示
        /// </summary>
        public byte DisplayStyle { get; set; }

        /// <summary>
        /// 显示最大数量
        /// </summary>
        public byte DisplayCount { get; set; }


        /// <summary>
        /// RAM High Count
        /// </summary>
        public byte RAMCountHigh { get; set; }

        /// <summary>
        /// RAM High Count
        /// </summary>
        public byte RAMCountLow { get; set; }

        /// <summary>
        /// Flash High Count
        /// </summary>
        public int FlashCountHigh { get; set; }


        /// <summary>
        /// Flash High Low
        /// </summary>
        public int FlashCountLow { get; set; }


        /// <summary>
        /// 最后一次重启的原因
        /// </summary>
        public byte LasRestart { get; set; }

        /// <summary>
        /// 目标域名或IP地址
        /// </summary>
        public string TargetDomain { get; set; }

        /// <summary>
        /// 目标端口
        /// </summary>
        public int TargetPort { get; set; }


        /// <summary>
        /// 目标域名或IP地址
        /// </summary>
        public string TargetDomain2 { get; set; }

        /// <summary>
        /// 目标端口
        /// </summary>
        public int TargetPort2 { get; set; }

        /// <summary>
        /// 系统RTC时间
        /// </summary>
        public DateTime RTC { get; set; }

        /// <summary>
        /// 亮屏策略
        /// </summary>
        public byte BackgroundLight { get; set; }

        /// <summary>
        /// 是否启用GPS
        /// </summary>
        public byte GPSStart { get; set; }


        //public byte ProtocolVersion { get; set; }



        /// <summary>
        /// GPS 采集间隔
        /// </summary>
        public int GPSInterval { get; set; }

        public byte TXPower { get; set; }














    }
}
