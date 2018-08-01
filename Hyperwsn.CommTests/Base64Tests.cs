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
    public class Base64Tests
    {
        [TestMethod()]
        public void base64encodeTest()
        {

            UserInfo user = new UserInfo();
            string name=  user.GetUserInfo();

            string userBase = Base64.base64encode(name);

            name = "中文 中文 LKJ 123 456 .，";

            userBase = Base64.base64encode(name);

            Assert.IsTrue(userBase.Length > 10);

            //Assert.Fail();
        }
    }
}