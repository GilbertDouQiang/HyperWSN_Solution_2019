using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Hyperwsn.Comm
{
    public class UserInfo
    {
        public string GetUserInfo()
        {
            return GetComputerName() +":"+ GetMacAddress();
        }

        string GetComputerName()
        {
            try
            {
                return System.Environment.MachineName;

            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        string GetMacAddress()
        {
            try
            {
                string mac = "";
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        mac = mo["MacAddress"].ToString();
                        break;
                    }
                }
                moc = null;
                mc = null;
                return mac;
            }
            catch
            {
                return "unknow";
            }
            finally
            {
            }

        }




    }
}
