using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace BootLoaderLibrary
{
    public class BootLoader
    {
        public enum BootLoaderResult
        {
            None,                   // 
            CreateSriptError,       // 创建脚本文件失败
            SriptPathError,         // 脚本文件路径错误
            ImagePathError,         // 镜像文件路径错误
            SriptNotAvailable,      // 脚本文件路径错误
            UartError,              // 
            UartBusy,               // 串口被占用
            PasswordError,          // 
            ReadCfgError,           // 
            WriteAppError,          // 
            WriteCfgError,          // 
            AckError,               //
            OtherError,             //
            CatchError,             // Catch到错误
            Success,                // 
        }

        /// <summary>
        /// 处理执行过程中的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void BootLoaderEventHander(object sender, BootLoaderEventArgs e);
        public event BootLoaderEventHander OutputStatusEvent;                                 // 输出状态

        /// <summary>
        /// 输出BSL-Sript反馈的内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public event BootLoaderEventHander OutputRxLogEvent;                                 // 输出状态

        Process ThisProcess;                            // 外部线程指针     

        public BootLoaderResult Result = BootLoaderResult.None;

        /// <summary>
        /// 串行设备的名称
        /// </summary>
        public string SerialDevice { get; set; }

        /// <summary>
        /// 脚本文件的相对路径
        /// </summary>
        public string FilePathOfScript { get; } = "\\BSL-Scripter";

        /// <summary>
        /// 脚本文件的文件名
        /// </summary>
        public string SafeFileNameOfScript { get; } = "Script_P4xx_Uart.txt";

        /// <summary>
        /// 镜像文件的绝对路径和文件名
        /// </summary>
        public string FileNameOfImage { get; set; }


        //===============================================================================

        /// <summary>
        /// 处理烧录过程中的异常信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //如果e为空，则不代表有问题，不要向外抛出
            if (e.Data == null)
            {
                return;
            }

            OnOutputRxLogEvent("Catch Error!" + e.Data.ToString() + "\r\n");
        }


        /// <summary>
        /// 处理烧录过程中的正常信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }
     
            OnOutputRxLogEvent(e.Data.ToString() + "\r\n");

            if (e.Data.Contains("File Scripter is not available"))
            {
                Result = BootLoaderResult.SriptNotAvailable;
                return;
            }

            if (e.Data.Contains("Initialization of P4xx BSL failed"))
            {
                Result = BootLoaderResult.UartError;
                return;
            }

            if (e.Data.Contains("open: 拒绝访问"))
            {
                Result = BootLoaderResult.UartBusy;
                return;
            }

            if (e.Data.Contains("BSL is locked"))
            {
                Result = BootLoaderResult.PasswordError;
                return;
            }

            if (e.Data.Contains("ACK_ERROR_MESSAGE"))
            {
                Result = BootLoaderResult.AckError;
                return;
            }

            if (e.Data.Contains("ERROR_MESSAGE"))
            {
                Result = BootLoaderResult.OtherError;
                return;
            }

            if (e.Data.Contains("Exit the scripter"))
            {
                Result = BootLoaderResult.OtherError;
                return;
            }

            if (e.Data.Contains("Command is invalid"))
            {
                Result = BootLoaderResult.OtherError;
                return;
            }

            if (e.Data.Contains("open: 系统找不到指定的文件"))
            {
                Result = BootLoaderResult.OtherError;
                return;
            }

            if (e.Data.Contains("REBOOT_RESET"))
            {
                Result = BootLoaderResult.Success;
                return;
            }
        }

        /// <summary>
        /// 从完整名称中，截取COM名称
        /// </summary>
        /// <param name="DeviceName"></param>
        /// <returns></returns>
        public static string GetSerialPortName(string DeviceName)
        {
            int first = DeviceName.IndexOf("(COM");
            if (first == -1)
            {
                return null;
            }

            int last = DeviceName.IndexOf(')');
            if (last == -1)
            {
                return null;
            }

            if (first >= last)
            {
                return null;
            }

            return DeviceName.Substring(first + 1, last - first - 1);
        }

        /// <summary>
        /// BootLoader前的准备，生成脚本文件
        /// </summary>
        /// <returns></returns>
        private Int16 CreateScriptFile()
        {
            string PortName = "";

            if (SerialDevice.Contains("(COM") == false)
            {
                OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   串行设备名称错误" + "\r\n");
                return -1;
            }

            PortName = GetSerialPortName(SerialDevice);
            if (PortName == "")
            {
                OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   获取串行设备端口号失败" + "\r\n");
                return -2;
            }

            OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   开始创建脚本文件" + "\r\n");

            string fileName = SafeFileNameOfScript;                                     // 脚本文件的文件名
            string filePath = Directory.GetCurrentDirectory() + FilePathOfScript;       // 脚本文件的绝对路径

            if (Directory.Exists(filePath) == false)                                    // 工程目录下BSL-Scripter\HyperWSN目录
            {
                OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   目录下缺少BSL-Scripter脚本程序" + "\r\n");
                return -3;
            }

            filePath += "\\" + fileName;

            if (File.Exists(filePath) == true)
            {   // 存在脚本文件

                // 读取脚本文件，对比内容，判断是否需要重写脚本文件
                StreamReader FileReader = new StreamReader(filePath, Encoding.ASCII);
                string FileTxt = FileReader.ReadToEnd();
                bool NotNeedWriteScript = (FileTxt.Contains(PortName) == true && FileTxt.Contains(FileNameOfImage) == true);
                FileReader.Close();
                FileReader = null;

                if (NotNeedWriteScript == true)
                {   // 不需要重写脚本文件
                    OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   脚本文件已存在" + "\r\n");
                    return 1;
                }
            }            

            // 需要创建或覆盖脚本文件

            StreamWriter FileWriter = new StreamWriter(filePath, false, Encoding.ASCII); // 若存在，则覆盖；若不存在，则创建；

            FileWriter.WriteLine("\nMODE P4xx UART 115200 " + PortName + " PARITY");    
            FileWriter.WriteLine("\nRX_PASSWORD_32 .\\Pass256_HyperWSN.txt");
            FileWriter.WriteLine("\nTX_DATA_BLOCK_32 0x3F000 0x1000 ReadCfg.txt");
            FileWriter.WriteLine("\nMASS_ERASE");
            FileWriter.WriteLine("\nRX_DATA_BLOCK_32 " + FileNameOfImage);
            FileWriter.WriteLine("\nRX_DATA_BLOCK_32 ReadCfg.txt");
            FileWriter.WriteLine("\nREBOOT_RESET");

            FileWriter.Close();                 // 关闭StreamWriter对象
            FileWriter = null;

            OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   脚本文件创建完成" + "\r\n");

            return 0;
        }

        /// <summary>
        /// 调用CMD，执行BSL-Scripter.exe
        /// </summary>
        /// <returns></returns>
        private Int16 ExecuteScripter(UInt16 TimeoutS, DateTime Start)
        {
            string filePath = Directory.GetCurrentDirectory() + FilePathOfScript; ;            // 脚本文件的绝对路径

            OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   开始烧录" + "\r\n");

            // 启动CMD
            CMD_Setup();

            //进入指定目录
            ThisProcess.StandardInput.WriteLine("cd " + filePath);

            //开始烧录程序
            ThisProcess.StandardInput.WriteLine(@"BSL-Scripter.exe " + SafeFileNameOfScript);

            // 等待烧录完成：要么烧录成功，退出；要么烧录失败，退出；要么烧录超时，退出；
            TimeoutS = (UInt16)(TimeoutS > 600 ? 600 : TimeoutS);

            for (UInt16 iCnt = 0; iCnt < TimeoutS; iCnt++)
            {
                System.Threading.Thread.Sleep(1000);

                if (Result == BootLoaderResult.None)
                {
                    continue;
                }

                break;
            }

            // 关闭CMD
            CMD_Cleanup();

            // 计算烧录所用的时间
            DateTime End = System.DateTime.Now;
            TimeSpan Diff = End.Subtract(Start);

            if (Result == BootLoader.BootLoaderResult.Success)
            {   // 烧录成功
                OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   烧录结束，成功！ 耗时：" + Diff.Minutes.ToString("D2") + "分" + Diff.Seconds.ToString("D2") + "秒； " + "\r\n");
            }
            else
            {   // 烧录失败
                OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   烧录结束，失败！ 耗时：" + Diff.Minutes.ToString("D2") + "分" + Diff.Seconds.ToString("D2") + "秒； " + Result.ToString() + "\r\n");
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// 烧录文件
        /// </summary>
        /// <param name="TimeoutS"> 烧录的超时时间，单位：秒；最大值是600秒； </param>
        /// <param name="Start"> 起始时间，用于计算整个烧录过程所用的时间； </param>
        /// <returns></returns>
        public BootLoaderResult BSL_Scripter(UInt16 TimeoutS, DateTime Start)
        {
            // 将烧录结果重置
            Result = BootLoaderResult.None;

            try
            {
                // 创建脚本文件
                Int16 Error = CreateScriptFile();
                if (Error < 0)
                {
                    Result = BootLoaderResult.CreateSriptError;
                    return Result;
                }

                // 烧录
                ExecuteScripter(TimeoutS, Start);      // 执行过程中，会对Result进行赋值
            }
            catch
            {
                Result = BootLoaderResult.CatchError;
                OnOutputStatusEvent(System.DateTime.Now.ToString("HH:mm:ss.fff") + "   捕捉到未知错误！" + Result.ToString() + "\r\n");
            }

            return Result;  
        }

        /// <summary>
        /// 设置初始的CMD环境
        /// </summary>
        private void CMD_Setup()
        {
            ThisProcess = new Process();

            //做好事件输出的准备
            ThisProcess.OutputDataReceived += Proc_OutputDataReceived;
            ThisProcess.ErrorDataReceived += Proc_ErrorDataReceived;

            //todo 成功烧录的事件

            ThisProcess.StartInfo.CreateNoWindow = true;
            ThisProcess.StartInfo.FileName = "cmd.exe";
            ThisProcess.StartInfo.UseShellExecute = false;
            ThisProcess.StartInfo.RedirectStandardError = true;
            ThisProcess.StartInfo.RedirectStandardInput = true;
            ThisProcess.StartInfo.RedirectStandardOutput = true;
            ThisProcess.Start();
            ThisProcess.BeginOutputReadLine();
            ThisProcess.BeginErrorReadLine();
        }

        /// <summary>
        /// 关闭退出CMD环境
        /// </summary>
        private void CMD_Cleanup()
        {
            ThisProcess.StandardInput.WriteLine("exit");

            //后续处理

            ThisProcess.WaitForExit();
            ThisProcess.CancelOutputRead();
            ThisProcess.CancelErrorRead();
            ThisProcess.Close();
            ThisProcess.Dispose();
        }              

        public void OnOutputStatusEvent(string msg)
        {
            if (OutputStatusEvent == null)
            {
                return;
            }

            BootLoaderEventArgs evtArgs = new BootLoaderEventArgs();
            evtArgs.Message = msg;
            this.OutputStatusEvent(this, evtArgs);
        }

        public void OnOutputRxLogEvent(string msg)
        {
            if (OutputRxLogEvent == null)
            {
                return;
            }

            BootLoaderEventArgs evtArgs = new BootLoaderEventArgs();
            evtArgs.Message = msg;
            this.OutputRxLogEvent(this, evtArgs);
        }
    }
}
