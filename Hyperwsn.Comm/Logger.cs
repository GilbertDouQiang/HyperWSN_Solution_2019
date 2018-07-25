using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;

namespace Hyperwsn.Comm
{
    public class Logger
    {
        private static StringBuilder LogStringBuilder = new StringBuilder();
        private static Timer writeTimer;


        private static object ob = "lock";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strFunctionName"></param>
        /// <param name="strErrorNum"></param>
        /// <param name="strErrorDescription"></param>
        public static void AddLog(string LogText)
        {
            if (writeTimer == null)
            {
                writeTimer = new Timer();
                writeTimer.Interval = 100;
                writeTimer.Enabled = true;
                writeTimer.Elapsed += WriteTimer_Elapsed;
            }

            lock (ob)
            {
                LogStringBuilder.Append(LogText + "\r\n");
            }





        }

        private static void WriteTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //定期写入,去掉没有必要的写入操作
            if (LogStringBuilder.Length == 0)
            {
                return;
            }

            string strMatter; //错误内容
            string strPath; //错误文件的路径
            DateTime dt = DateTime.Now;
            string fileName;
            try
            {

                fileName = "HyperWSN_" + dt.ToString("yyyyMMdd") + ".log";
                strPath = Directory.GetCurrentDirectory() + "\\Log";


                if (Directory.Exists(strPath) == false)  //工程目录下 Log目录 '目录是否存在,为true则没有此目录
                {
                    Directory.CreateDirectory(strPath); //建立目录　Directory为目录对象
                }
                strPath = strPath + "\\" + fileName;

               


                lock (ob)
                {
                    StreamWriter FileWriter = new StreamWriter(strPath, true, Encoding.Unicode); //创建日志文件
                    strMatter = LogStringBuilder.ToString();

                   
                    LogStringBuilder.Clear();
                    FileWriter.Write(strMatter);

                    FileWriter.Close(); //关闭StreamWriter对象
                    FileWriter = null;

                }


            }
            catch (Exception ex)
            {

                string str = ex.Message.ToString();
            }
        }

        public static void AddLogAutoTime(string LogText)
        {




            AddLog(GetTimeString() + "\t" + LogText);


        }

        public static String GetTimeString()
        {


            string timeString = "";
            DateTime dt = DateTime.Now;
            if (dt.Hour < 10)
            {
                timeString += "0" + dt.Hour;
            }
            else

            {
                timeString += dt.Hour;
            }

            if (dt.Minute < 10)
            {
                timeString += ":0" + dt.Minute;
            }
            else
            {
                timeString += ":" + dt.Minute;
            }

            if (dt.Second < 10)
            {
                timeString += ":0" + dt.Second;

            }
            else
            {
                timeString += ":" + dt.Second;
            }

            if (dt.Millisecond < 10)
            {
                timeString += ":00" + dt.Millisecond;
            }
            else if (dt.Millisecond < 100)
            {
                timeString += ":0" + dt.Millisecond;
            }
            else
            {
                timeString += ":" + dt.Millisecond;
            }

            return timeString;

        }

    }
}
