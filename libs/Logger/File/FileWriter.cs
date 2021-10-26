using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace CodeM.FastApi.Log.File
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

    public class FileWriter
    {
        /// <summary>
        /// 日志文件名称
        /// </summary>
        public string FileName { get; set; } = string.Concat("logs", Path.DirectorySeparatorChar, "fastapi.log");

        private string FilePath { get; set; }
        private string FileShortName { get; set; }
        private string FileExtension { get; set; }
        private string FileFullName { get; set; }

        public Encoding FileEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 日志方式分割类型
        /// </summary>
        public SplitType SplitType { get; set; } = SplitType.None;

        /// <summary>
        /// 日志最大备份文件数（），默认保留10个最近的日志文件；如果设置为0，则保留所有日志文件（注意空间占用问题）
        /// </summary>
        public int MaxFileBackups { get; set; } = 10;

        /// <summary>
        /// 日志文件最大容量，单位byte，默认2M
        /// </summary>
        public int MaxFileSize { get; set; } = 2 * 1024 * 1024;

        private ConcurrentQueue<string> sLogs = new ConcurrentQueue<string>();

        public FileWriter(IConfigurationSection options)
        {
            if (options != null)
            {
                string fileName = options.GetValue<string>("FileName", null);
                if (!string.IsNullOrEmpty(fileName))
                {
                    FileName = fileName;
                }

                string splitType = options.GetValue<string>("SplitType", null);
                if (!string.IsNullOrEmpty(splitType))
                {
                    SplitType stResult;
                    if (Enum.TryParse<SplitType>(splitType, out stResult))
                    {
                        SplitType = stResult;
                    }
                }

                int? maxFileSize = options.GetValue<int?>("MaxFileSize", null);
                if (maxFileSize != null)
                {
                    MaxFileSize = (int)maxFileSize;
                }

                int? maxFileBackups = options.GetValue<int?>("MaxFileBackups", null);
                if (maxFileBackups != null)
                {
                    MaxFileBackups = (int)maxFileBackups;
                }

                string encoding = options.GetValue<string>("Encoding", null);
                if (!string.IsNullOrEmpty(encoding))
                {
                    FileEncoding = Encoding.GetEncoding(encoding);
                }
            }
        }

        private void Init()
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

        private void SplitLogFile()
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
                        DateTime deleteDay = nowDay.AddHours(-MaxFileBackups);
                        string deleteFile = Path.Combine(FilePath,
                            string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", deleteDay.ToString("yyyy-MM-dd"), FileExtension));
                        System.IO.File.Delete(deleteFile);

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
                        DateTime deleteHour = nowHour.AddHours(-MaxFileBackups);
                        string deleteFile = Path.Combine(FilePath,
                            string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", deleteHour.ToString("yyyy-MM-dd-HH"), FileExtension));
                        System.IO.File.Delete(deleteFile);

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
        private void MoveSplitFileByOrderIfNeed(int fileIndex)
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

        private bool isAbort = false;
        private Thread sWriteThread = new Thread((object start) =>
        {
            FileWriter w = (FileWriter)start;
            w.Init();

            string log;
            StringBuilder sbBuff = new StringBuilder();
            while (true)
            {
                w.SplitLogFile();

                while (w.sLogs.TryDequeue(out log))
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

                if (sbBuff.Length > 0 || w.isAbort)
                {
                    System.IO.File.AppendAllText(w.FileFullName, sbBuff.ToString(), w.FileEncoding);
                    sbBuff.Length = 0;
                }

                if (w.isAbort)
                {
                    break;
                }

                Thread.Sleep(1000);
            }
        });

        public void Write(string log)
        {
            if (!sWriteThread.IsAlive)
            {
                sWriteThread.IsBackground = true;
                sWriteThread.Start(this);
            }
            sLogs.Enqueue(log);
        }

        public void Dispose()
        {
            isAbort = true;
        }
    }
}
