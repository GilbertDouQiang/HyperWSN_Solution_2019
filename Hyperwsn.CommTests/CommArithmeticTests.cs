using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hyperwsn.Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hyperwsn.Comm.Tests
{
    [TestClass()]
    public class CommArithmeticTests
    {
        [TestMethod()]
        public void DecodeByte2StringTest()
        {
            string x = "0F 71 61 2E 68 79 70 65 72 77 73 6E 2E 63 6F 6D 2B";
            byte[] xSource = CommArithmetic.HexStringToByteArray(x);

            string result = CommArithmetic.DecodeByte2String(xSource, 1, 0x0f);

            Assert.IsTrue(result != null);

        }
    }
}