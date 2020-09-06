using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace CodeM.FastApi.Logger.File
{
    public enum RollingStyle
    {
        Date = 0,   //按照日期为界限，一天写一个日志文件，保留MaxSizeRollBackups设定个数的文件
        Size = 1,   //按照MaxFileSize大小为文件大小上限，轮询写入MaxSizeRollBackups设定个数的文件
        Normal = 2  //不滚动，始终写一个文件
    }

    public static class FileWriter
    {
        /// <summary>
        /// 日志文件名称
        /// </summary>
        public static string FileName { get; set; } = string.Concat("logs", Path.DirectorySeparatorChar, "fastapi.log");

        private static string FilePath { get; set; }
        private static string FileShortName { get; set; }
        private static string FileExtension { get; set; }
        private static string FileFullName { get; set; }

        /// <summary>
        /// 日志滚动备份方式
        /// </summary>
        public static RollingStyle RollingStyle { get; set; } = RollingStyle.Normal;

        /// <summary>
        /// 日志记录滚动备份最大个数
        /// </summary>
        public static int MaxSizeRollBackups { get; set; } = 10;

        /// <summary>
        /// 日志文件最大容量，单位byte
        /// </summary>
        public static int MaxFileSize { get; set; } = 1024;

        private static ConcurrentQueue<string> sLogs = new ConcurrentQueue<string>();

        private static void Init()
        {
            FileFullName = Path.Combine(Environment.CurrentDirectory, FileName);

            FileInfo fi = new FileInfo(FileFullName);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            FilePath = fi.Directory.FullName;
            FileShortName = fi.Name;
            FileExtension = fi.Extension;
        }

        private static void RollingBackup()
        {
            if (!System.IO.File.Exists(FileFullName))
            {
                return;
            }

            switch (RollingStyle)
            {
                case RollingStyle.Date:
                    DateTime now = DateTime.Now;
                    DateTime next = new DateTime(now.Year, now.Month, now.Day + 1, 0, 0, 0);
                    TimeSpan diff = next - now;
                    if (Math.Abs(diff.TotalSeconds) < 15)
                    {
                        FileInfo fi = new FileInfo(FileFullName);
                        if (fi.Length > 0)
                        {
                            string destFile = Path.Combine(FilePath,
                                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", now.ToString("yyyyMMdd"), FileExtension));
                            System.IO.File.Move(FileFullName, destFile);
                            Thread.Sleep(30000);
                        }
                    }
                    break;
                case RollingStyle.Size:
                    //TODO
                    break;
                default:
                    break;
            }
        }

        private static Thread sWriteThread = new Thread(() =>
        {
            Init();

            string log;
            while (true)
            {
                RollingBackup();

                if (sLogs.TryDequeue(out log))
                {
                    if (!string.IsNullOrEmpty(log))
                    {
                        System.IO.File.AppendAllText(FileFullName, log + Environment.NewLine);
                    }
                }
                else
                {
                    Thread.Sleep(3000);
                }
            }
        });

        public static void Write(string log)
        {
            if (!sWriteThread.IsAlive)
            {
                sWriteThread.IsBackground = true;
                sWriteThread.Start();
            }
            sLogs.Enqueue(log);
        }
    }
}
