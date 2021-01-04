using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hyperwsn.Comm;

namespace Hyperwsn.Protocol
{
    public class DeviceHelper
    {
        /// <summary>
        /// 读取APN
        /// </summary>
        /// <returns></returns>
        public byte[] ReadApn()
        {
            byte[] TxBuf = new byte[13];
            UInt16 TxLen = 0;

            // 起始位
            TxBuf[TxLen++] = 0xCB;
            TxBuf[TxLen++] = 0xCB;

            // 长度位
            TxBuf[TxLen++] = 0x00;

            // 功能位
            TxBuf[TxLen++] = 0x72;

            // 协议版本
            TxBuf[TxLen++] = 0x01;

            // 保留位
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;
            TxBuf[TxLen++] = 0x00;

            // CRC16
            UInt16 crc = MyCustomFxn.CRC16(MyCustomFxn.GetItuPolynomialOfCrc16(), 0, TxBuf, 3, (UInt16)(TxLen - 3));
            TxBuf[TxLen++] = (byte)((crc & 0xFF00) >> 8);
            TxBuf[TxLen++] = (byte)((crc & 0x00FF) >> 0);

            // 结束位
            TxBuf[TxLen++] = 0xBC;
            TxBuf[TxLen++] = 0xBC;

            // 重写长度位
            TxBuf[2] = (byte)(TxLen - 7);

            // 判断长度是否正确
            if(TxBuf.Length != TxLen)
            {
                while (true) ;
            }

            return TxBuf;
        }


       

    }
}
