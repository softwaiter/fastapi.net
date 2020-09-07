using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace CodeM.FastApi.Logger.File
{
    /// <summary>
    /// 日志文件分割类型
    /// </summary>
    public enum SplitType
    {
        Date = 0,   //按照日期为界限进行分割，同一天的日志内容写入同一个日志文件
        Hour = 1,   //按照小时为界限进行分割，同一小时的日志内容写入同一个日志文件
        Size = 2,   //按照MaxFileSize设置的文件大小为界限，日志内容每到达MaxFileSize大小就写入一个日志文件
        None = 3    //不分割，所有日志内容写入同一个日志文件
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

        public static Encoding FileEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 日志方式分割类型
        /// </summary>
        public static SplitType SplitType { get; set; } = SplitType.Size;

        /// <summary>
        /// 日志最大备份文件数（），默认保留10个最近的日志文件；如果设置为0，则保留所有日志文件（注意空间占用问题）
        /// </summary>
        public static int MaxFileBackups { get; set; } = 10;

        /// <summary>
        /// 日志文件最大容量，单位byte，默认2M
        /// </summary>
        public static int MaxFileSize { get; set; } = 2 * 1024 * 1024;

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

        private static void SplitLogFile()
        {
            if (!System.IO.File.Exists(FileFullName))
            {
                return;
            }

            switch (SplitType)
            {
                case SplitType.Date:
                    DateTime nowDay = DateTime.Now;
                    DateTime nextDay = nowDay.AddDays(1);
                    DateTime nextDay2 = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 0, 0, 0);
                    TimeSpan diffDay = nextDay2 - nowDay;
                    if (Math.Abs(diffDay.TotalSeconds) < 2)
                    {
                        string destFile = Path.Combine(FilePath,
                            string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", nowDay.ToString("yyyy-MM-dd"), FileExtension));
                        System.IO.File.Move(FileFullName, destFile, true);
                        Thread.Sleep(5000);
                    }
                    break;
                case SplitType.Hour:
                    DateTime nowHour = DateTime.Now;
                    DateTime nextHour = nowHour.AddHours(1);
                    DateTime nextHour2 = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 0);
                    TimeSpan diffHour = nextHour2 - nowHour;
                    if (Math.Abs(diffHour.TotalSeconds) < 2)
                    {
                        string destFile = Path.Combine(FilePath,
                            string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", nowHour.ToString("yyyy-MM-dd-HH"), FileExtension));
                        System.IO.File.Move(FileFullName, destFile, true);
                        Thread.Sleep(5000);
                    }
                    break;
                case SplitType.Size:
                    FileInfo fi = new FileInfo(FileFullName);
                    if (fi.Length >= MaxFileSize)
                    {
                        if (MaxFileBackups > 0)
                        {
                            MoveSplitFileByOrderIfNeed(MaxFileBackups);

                            string fileSuffix = ("" + MaxFileBackups).PadLeft(("" + MaxFileBackups).Length, '0');
                            string destFile = Path.Combine(FilePath, 
                                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", fileSuffix, FileExtension));
                            System.IO.File.Move(FileFullName, destFile, true);
                        }
                        else
                        {
                            string destFile = Path.Combine(FilePath,
                                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", DateTime.Now.ToString("yyyyMMddHHmmss"), FileExtension));
                            System.IO.File.Move(FileFullName, destFile, true);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 新的分割文件产生时，将已有文件顺序向前覆盖，淘汰最早的一个文件，SplitType=Size时启用
        /// </summary>
        /// <param name="fileIndex"></param>
        private static void MoveSplitFileByOrderIfNeed(int fileIndex)
        {
            if (fileIndex > 1)
            {
                string fileSuffix = ("" + fileIndex).PadLeft(("" + MaxFileBackups).Length, '0');
                string filename = Path.Combine(FilePath,
                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", fileSuffix, FileExtension));
                if (System.IO.File.Exists(filename))
                {
                    MoveSplitFileByOrderIfNeed(fileIndex - 1);

                    string fileSuffix2 = ("" + (fileIndex - 1)).PadLeft(("" + MaxFileBackups).Length, '0');
                    string destFile = Path.Combine(FilePath,
                        string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", fileSuffix2, FileExtension));
                    System.IO.File.Move(filename, destFile, true);
                }
            }
        }

        private static Thread sWriteThread = new Thread(() =>
        {
            Init();

            string log;
            StringBuilder sbBuff = new StringBuilder();
            while (true)
            {
                SplitLogFile();

                while (sLogs.TryDequeue(out log))
                {
                    if (!string.IsNullOrEmpty(log))
                    {
                        sbBuff.AppendLine(log);
                        if (sbBuff.Length > 10240)
                        {
                            break;
                        }
                    }
                }
                
                if (sbBuff.Length > 0)
                {
                    System.IO.File.AppendAllText(FileFullName, sbBuff.ToString(), FileEncoding);
                    sbBuff.Length = 0;
                }

                Thread.Sleep(1000);
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
