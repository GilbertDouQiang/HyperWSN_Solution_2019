using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperwsn.Comm;

namespace Hyperwsn.Protocol
{
    public class Gateway:Device
    {
        /// <summary>
        /// 轮播间隔
        /// </summary>
        public UInt16 Carousel { get; set; }

        /// <summary>
        /// 报警间隔
        /// </summary>
        public UInt16 IntervalOfAlert { get; set; }

        /// <summary>
        /// CC1310/CC1352P软件版本
        /// </summary>
        public string cSwRevisionS { get; set; }

        /// <summary>
        /// 转发策略
        /// </summary>
        public byte TransPolicy { get; set; }

        /// <summary>
        /// 显示的时间的来源
        /// </summary>
        public byte TimeSrc { get; set; }

        /// <summary>
        /// 报警方式，声光报警
        /// </summary>
        public byte AlertWay { get; set; }

        /// <summary>
        /// 报警依据
        /// </summary>
        public byte criSrc { get; set; }

        /// <summary>
        /// 显示方式：单一显示，智能显示
        /// </summary>
        public byte DisplayStyle { get; set; }

        /// <summary>
        /// RAM High Count
        /// </summary>
        public byte RAMCountHigh { get; set; }

        /// <summary>
        /// RAM High Count
        /// </summary>
        public byte RAMCountLow { get; set; }

        /// <summary>
        /// 主服务器，Flash待发Sensor数据的数量，优先级：高
        /// </summary>
        public UInt32 FlashToSend_SSH_M { get; set; }

        /// <summary>
        /// 主服务器，Flash待发Sensor数据的数量，优先级：低
        /// </summary>
        public UInt32 FlashToSend_SSL_M { get; set; }

        /// <summary>
        /// 主服务器，Flash待发网关状态包的数量
        /// </summary>
        public UInt32 FlashToSend_Status_M { get; set; }

        /// <summary>
        /// 主服务器，Flash待发定位数据包的数量
        /// </summary>
        public UInt32 FlashToSend_Locate_M { get; set; }

        /// <summary>
        /// 副服务器，Flash待发Sensor数据的数量，优先级：高
        /// </summary>
        public UInt32 FlashToSend_SSH_S { get; set; }

        /// <summary>
        /// 主副服务器，Flash待发Sensor数据的数量，优先级：低
        /// </summary>
        public UInt32 FlashToSend_SSL_S { get; set; }

        /// <summary>
        /// 副服务器，Flash待发网关状态包的数量
        /// </summary>
        public UInt32 FlashToSend_Status_S { get; set; }

        /// <summary>
        /// 副服务器，Flash待发定位数据包的数量
        /// </summary>
        public UInt32 FlashToSend_Locate_S { get; set; }

        /// <summary>
        /// 重启的原因
        /// </summary>
        public byte RstSrc { get; set; }

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
        /// 网关的当前日期和时间
        /// </summary>
        public DateTime Current { get; set; }

        /// <summary>
        /// 亮屏策略
        /// </summary>
        public byte brightMode { get; set; }

        /// <summary>
        /// 启用
        /// </summary>
        public byte use { get; set; }

        /// <summary>
        /// 定位的时间间隔，单位：秒
        /// </summary>
        public UInt16 IntervalOfLocate { get; set; }

        /// <summary>
        /// 发射功率，单位：dBm
        /// </summary>
        public Int16 TxPower { get; set; }

        /// <summary>
        /// 配置保留位
        /// </summary>
        public byte[] ReservedV { get; set; }

        /// <summary>
        /// 配置保留位
        /// </summary>
        public string ReservedS { get; set; }

        /// <summary>
        /// 设置保留位
        /// </summary>
        /// <param name="SrcData"></param>
        /// <param name="StartIndex"></param>
        public void SetReserved(byte[] SrcData, UInt16 StartIndex, UInt16 Len)
        {
            ReservedS = CommArithmetic.ByteBuf_to_HexString(SrcData, StartIndex, Len);

            byte[] Reserved = new byte[Len];

            for(UInt16 iX = 0; iX < Len; iX++)
            {
                Reserved[iX] = SrcData[StartIndex + iX];
            }

            ReservedV = Reserved;
        }
    }
}
