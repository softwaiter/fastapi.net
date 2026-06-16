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
        public static string FileName { get; set; } = string.Concat("logs", Path.DirectorySeparatorChar, "fastapi.log");

        private static string FilePath { get; set; }
        private static string FileShortName { get; set; }
        private static string FileExtension { get; set; }
        private static string FileFullName { get; set; }

        public static Encoding FileEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// 日志方式分割类型
        /// </summary>
        public static SplitType SplitType { get; set; } = SplitType.None;

        /// <summary>
        /// 日志最大备份文件数（），默认保留10个最近的日志文件；如果设置为0，则保留所有日志文件（注意空间占用问题）
        /// </summary>
        public static int MaxFileBackups { get; set; } = 10;

        /// <summary>
        /// 日志文件最大容量，单位byte，默认2M
        /// </summary>
        public static int MaxFileSize { get; set; } = 2 * 1024 * 1024;

        private static ConcurrentQueue<string> sLogs = new ConcurrentQueue<string>();

        private static bool sInited = false;

        // 标记线程是否正在运行（避免重复启动）
        private static volatile bool sIsWritingThreadRunning = false;
        // 退出信号（优雅关闭线程）
        private static volatile bool sStopWriting = false;

        public static void Init(IConfigurationSection options)
        {
            if (options != null)
            {
                if (!sInited)
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

                    sInited = true;
                }
            }
        }

        private static bool InitFileInfo()
        {
            if (sInited)
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

                return true;
            }
            return false;
        }

        // 安全删除文件（避免IO异常）
        private static void DeleteFileIfExists(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除文件失败 {filePath}：{ex.Message}");
            }
        }

        // 安全移动文件（避免IO异常）
        private static void MoveFileIfExists(string srcPath, string destPath)
        {
            try
            {
                if (System.IO.File.Exists(srcPath))
                {
                    // 覆盖前先删除目标文件（避免占用）
                    if (System.IO.File.Exists(destPath))
                    {
                        System.IO.File.Delete(destPath);
                    }
                    System.IO.File.Move(srcPath, destPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"移动文件失败 {srcPath} -> {destPath}：{ex.Message}");
            }
        }

        private static void SplitLogFile()
        {
            if (!System.IO.File.Exists(FileFullName))
            {
                return;
            }

            try
            {
                switch (SplitType)
                {
                    case SplitType.Date:
                        DateTime nowDay = DateTime.Now;
                        DateTime nextDay = new DateTime(nowDay.Year, nowDay.Month, nowDay.Day).AddDays(1);
                        TimeSpan timeToNextDay = nextDay - nowDay;

                        // 避免精确到秒的判断，改为判断是否跨天
                        if (timeToNextDay.TotalMinutes < 1)
                        {
                            DateTime deleteDay = nowDay.AddHours(-MaxFileBackups);
                            string deleteFile = Path.Combine(FilePath,
                                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", deleteDay.ToString("yyyy-MM-dd"), FileExtension));
                            DeleteFileIfExists(deleteFile);

                            string destFile = Path.Combine(FilePath,
                                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", nowDay.ToString("yyyy-MM-dd"), FileExtension));
                            MoveFileIfExists(FileFullName, destFile);
                        }
                        break;
                    case SplitType.Hour:
                        DateTime nowHour = DateTime.Now;
                        DateTime nextHour = new DateTime(nowHour.Year, nowHour.Month, nowHour.Day, nowHour.Hour, 0, 0).AddHours(1);
                        TimeSpan timeToNextHour = nextHour - nowHour;
                        if (timeToNextHour.TotalMinutes < 1)
                        {
                            DateTime deleteHour = nowHour.AddHours(-MaxFileBackups);
                            string deleteFile = Path.Combine(FilePath,
                                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", deleteHour.ToString("yyyy-MM-dd-HH"), FileExtension));
                            DeleteFileIfExists(deleteFile);

                            string destFile = Path.Combine(FilePath,
                                string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", nowHour.ToString("yyyy-MM-dd-HH"), FileExtension));
                            MoveFileIfExists(FileFullName, destFile);
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
                                MoveFileIfExists(FileFullName, destFile);
                            }
                            else
                            {
                                string destFile = Path.Combine(FilePath,
                                    string.Concat(FileShortName.Substring(0, FileShortName.Length - FileExtension.Length), "_", DateTime.Now.ToString("yyyyMMddHHmmss"), FileExtension));
                                MoveFileIfExists(FileFullName, destFile);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                // TODO 应该记录下来
                Console.WriteLine($"分割日志文件失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 新的分割文件产生时，将已有文件顺序向前覆盖，淘汰最早的一个文件，SplitType=Size时启用
        /// </summary>
        /// <param name="fileIndex"></param>
        private static void MoveSplitFileByOrderIfNeed(int fileIndex)
        {
            if (fileIndex <= 1) return;

            try
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
                    MoveFileIfExists(filename, destFile);
                }
            }
            catch (Exception ex)
            {
                // TODO 应该记录下来
                Console.WriteLine($"重命名分割文件失败：{ex.Message}");
            }
        }

        private static void WriteHandler()
        {
            sIsWritingThreadRunning = true;
            StringBuilder sbBuff = new StringBuilder();
            int emptyLoop = 0;

            try
            {
                while (!sStopWriting)
                {
                    if (sLogs.Count > 0)
                    {
                        emptyLoop = 0;
                        try
                        {
                            SplitLogFile();

                            // 批量出队（减少IO次数）
                            while (sLogs.TryDequeue(out var log) && !string.IsNullOrEmpty(log))
                            {
                                sbBuff.AppendLine(log);
                                // 缓冲区阈值：10KB（避免内存溢出）
                                if (sbBuff.Length > 10240)
                                {
                                    FlushBuffer(sbBuff);
                                }
                            }

                            // 刷剩余数据
                            FlushBuffer(sbBuff);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"写入日志缓冲区失败：{ex.Message}");
                            Thread.Sleep(100); // 失败后短暂休眠，避免死循环
                        }
                    }
                    else
                    {
                        // 无日志时刷缓冲区，避免数据残留
                        FlushBuffer(sbBuff);
                        emptyLoop++;
                        // 无日志时最多等待60秒后退出线程（原逻辑保留，但优化退出条件）
                        if (emptyLoop >= 60)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
            finally
            {
                // 最终刷缓冲区（进程退出时确保数据写入）
                FlushBuffer(sbBuff);
                sIsWritingThreadRunning = false;
                sWriteThread = null; // 释放线程引用
            }
        }

        // 刷缓冲区到文件（抽离逻辑，便于复用）
        private static void FlushBuffer(StringBuilder sbBuff)
        {
            if (sbBuff.Length == 0 || !InitFileInfo()) return;

            try
            {
                System.IO.File.AppendAllText(FileFullName, sbBuff.ToString(), FileEncoding);
                sbBuff.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"刷日志到文件失败：{ex.Message}");
            }
        }

        private static Thread sWriteThread;
        private static object sThreadStartLock = new object();

        public static void Write(string log)
        {
            if (string.IsNullOrEmpty(log)) return;

            sLogs.Enqueue(log);

            // 双重检查锁：确保线程唯一且不重复启动
            if (!sIsWritingThreadRunning)
            {
                lock (sThreadStartLock)
                {
                    if (!sIsWritingThreadRunning)
                    {
                        sStopWriting = false; // 重置退出信号

                        // 线程终止后必须重新创建实例（无法重启）
                        sWriteThread = new Thread(WriteHandler)
                        {
                            IsBackground = true, // 后台线程：不阻塞进程退出
                            Name = "dayuan-file-writer-log-thread" // 命名线程：便于调试
                        };
                        sWriteThread.Start();
                    }
                }
            }
        }

        // 优雅关闭（比如应用停止时调用）
        public static void Dispose()
        {
            sStopWriting = true;
            if (sWriteThread != null && sWriteThread.IsAlive)
            {
                // 等待线程退出（最多10秒）
                sWriteThread.Join(10000);
            }
            sInited = false;
            sIsWritingThreadRunning = false;
        }
    }
}
