using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hyperwsn.Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hyperwsn.Protocol;

namespace Hyperwsn.Comm.Tests
{
    [TestClass()]
    public class SaveObject2FileTests
    {
        /// <summary>
        /// 测试对象的序列化函数
        /// </summary>
        [TestMethod()]
        public void btnSaveXml_ClickTest()
        {
            SaveObject2File save = new SaveObject2File();
            Gateway gateway = new Gateway();
            gateway.DeviceMacS = "10 10 10 10";
            save.Save(gateway, "C:\\1.xml");

            Assert.Fail();
        }
    }
}