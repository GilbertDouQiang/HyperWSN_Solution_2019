using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hyperwsn.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Hyperwsn.Comm;

namespace Hyperwsn.Protocol.Tests
{
    [TestClass()]
    public class BindingAnalyseTests
    {
        [TestMethod()]
        public void GatewayBindingTest()
        {
            byte[] ping = Encoding.GetEncoding("GB18030").GetBytes("内置温湿度采集器1");
            string str = Encoding.GetEncoding("GB18030").GetString(ping);


            BindingAnalyse analyse = new Protocol.BindingAnalyse();
            DataTable dt = analyse.GatewayBinding(CommArithmetic.HexStringToByteArray("BC BC 24 6A 01 60 40 00 04 00 00 10 02 52 01 47 06 10 65 01 27 10 65 00 D8 F0 66 01 27 10 66 00 00 00 73 03 4E 54 43 21 F8 CB CB"));
            //BC BC 32 6A 01 60 40 00 04 00 00 10 00 40 00 04 01 23 65 01 26 AC 65 00 F0 60 66 01 26 AC 66 00 00 64 73 11 C4 DA D6 C3 CE C2 CA AA B6 C8 B2 C9 BC AF C6 F7 31 3C 21 CB CB BC BC 2D 6A 01 60 40 00 04 00 00 10 0E 52 02 24 40 1E 65 01 26 AC 65 00 F0 60 66 01 26 AC 66 00 00 64 73 0C CE C2 CA AA B6 C8 B2 C9 BC AF C6 F7 0A 44 CB CB
            //BC BC 24 6A 01 60 40 00 04 00 00 10 02 52 01 47 06 10 65 01 27 10 65 00 D8 F0 66 01 27 10 66 00 00 00 73 03 4E 54 43 21 F8 CB CB 
            //DataTable dt = analyse.GatewayBinding(CommArithmetic.HexStringToByteArray("BC BC 32 6A 01 60 40 00 04 00 00 10 00 40 00 04 01 10 65 01 26 AC 65 00 F0 60 66 01 26 AC 66 00 00 64 73 11 C4 DA D6 C3 CE C2 CA AA B6 C8 B2 C9 BC AF C6 F7 31 8B 35 CB CB BC BC 32 6A 01 60 40 00 04 00 00 10 01 40 00 04 02 10 65 01 26 AC 65 00 F0 60 66 01 26 AC 66 00 00 64 73 11 C4 DA D6 C3 CE C2 CA AA B6 C8 B2 C9 BC AF C6 F7 32 0F 4A CB CB"));
        }
    }
}