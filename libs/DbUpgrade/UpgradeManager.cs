using CodeM.Common.Orm;
using CodeM.Common.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.DbUpgrade
{
    internal class UpgradeInfo
    {
        public UpgradeInfo(string modelPath, string file, int version)
        {
            this.ModelPath = modelPath;
            this.File = file;
            this.Version = version;
        }

        public string ModelPath { get; set; }

        public string File { get; set; }

        public int Version { get; set; }
    }

    public class UpgradeManager
    {
        public static void EnableVersionControl()
        {
            if (Derd.GetVersion() < 0)
            {
                Derd.EnableVersionControl();
            }
        }

        private static string SpecialHandle(string modelPath, string command)
        {
            string datetimeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            ConnectionSetting cs = Derd.GetConnectionSetting(modelPath);
            if ("oracle".Equals(cs.Dialect, StringComparison.OrdinalIgnoreCase))
            {
                Regex re = new Regex("^\\s*INSERT\\s*INTO\\s*(\\w*)\\s*\\((.*)\\)\\s*VALUES\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                Match match = re.Match(command);
                if (match.Success)
                {
                    string prefix = command.Substring(0, match.Groups[1].Index);
                    string table = command.Substring(match.Groups[1].Index, match.Groups[1].Length);
                    string fields = command.Substring(match.Groups[2].Index, match.Groups[2].Length);
                    string suffix = command.Substring(match.Groups[2].Index + match.Groups[2].Length);

                    string newTable = string.Concat("\"", table, "\"");

                    string[] fieldItems = fields.Replace(" ", "").Split(",");
                    string newFields = string.Concat("\"", string.Join("\", \"", fieldItems), "\"");

                    command = string.Concat(prefix, newTable, " (", newFields, suffix).Trim();
                }

                string orclDatetimeNow = string.Concat("TO_TIMESTAMP('", datetimeNow, "', 'yyyy-mm-dd HH24:mi:ss')");
                command = command.Replace("{{$nowtime$}}", orclDatetimeNow);

                command = command.Replace("'9999-12-31'", "TO_DATE('9999-12-31', 'yyyy-MM-dd')");

                if (command.EndsWith(";"))
                {
                    command = command.Substring(0, command.Length - 1);
                }
            }
            else if ("dm".Equals(cs.Dialect, StringComparison.OrdinalIgnoreCase))
            {
                Regex re = new Regex("^\\s*INSERT\\s*INTO\\s*(\\w*)\\s*\\((.*)\\)\\s*VALUES\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                Match match = re.Match(command);
                if (match.Success)
                {
                    string prefix = command.Substring(0, match.Groups[1].Index);
                    string table = command.Substring(match.Groups[1].Index, match.Groups[1].Length);
                    string fields = command.Substring(match.Groups[2].Index, match.Groups[2].Length);
                    string suffix = command.Substring(match.Groups[2].Index + match.Groups[2].Length);

                    string newTable = string.Concat("\"", table, "\"");

                    string[] fieldItems = fields.Replace(" ", "").Split(",");
                    string newFields = string.Concat("\"", string.Join("\", \"", fieldItems), "\"");

                    command = string.Concat(prefix, newTable, " (", newFields, suffix).Trim();
                }

                command = command.Replace("{{$nowtime$}}", string.Concat("'", datetimeNow, "'"));
            }
            else
            {
                command = command.Replace("{{$nowtime$}}", string.Concat("'", datetimeNow, "'"));
            }
            return command;
        }

        private static void ExecuteUpgradeFile(UpgradeInfo info)
        {
            int lastVersion = Derd.GetVersion(info.ModelPath);
            if (info.Version > lastVersion)
            {
                List<string> commands = UpgradeLoader.Load(info.File);

                int trans = Derd.GetTransaction(info.ModelPath);
                try
                {
                    foreach (string cmd in commands)
                    {
                        string sql = SpecialHandle(info.ModelPath, cmd);
                        Derd.ExecSql(sql, trans);
                    }

                    Derd.SetVersion(info.ModelPath, info.Version, trans);
                    Derd.CommitTransaction(trans);
                }
                catch
                {
                    Derd.RollbackTransaction(trans);
                    throw;
                }
            }
        }

        public static void Upgrade(string modelPath)
        {
            int currentVersion = Derd.GetVersion();
            if (currentVersion < 0)
            {
                throw new Exception("版本控制未开启，请先使用EnableVersionControl方法开启版本控制。");
            }
            if (!Derd.IsVersionControlEnabled())
            {
                throw new Exception("版本控制未开启，请先使用EnableVersionControl方法开启版本控制。");
            }

            IEnumerable<string> upgradeFiles = Directory.EnumerateFiles(modelPath, ".upgrade.v*.xml", SearchOption.AllDirectories);
            IEnumerator<string> e = upgradeFiles.GetEnumerator();

            List<UpgradeInfo> sortedUpgradeData = new List<UpgradeInfo>();
            while (e.MoveNext())
            {
                string upgradeFile = e.Current;
                FileInfo fi = new FileInfo(upgradeFile);
                string filename = fi.Name;
                string version = filename.Substring(10, filename.Length - 14);
                if (Xmtool.Regex().IsPositiveInteger(version))
                {
                    string currPath = fi.DirectoryName.ToLower().Replace(modelPath.ToLower(), "");
                    if (string.IsNullOrWhiteSpace(currPath))
                    {
                        currPath = "/";
                    }
                    else
                    {
                        currPath = currPath.Replace(Path.DirectorySeparatorChar, '/');
                    }
                    sortedUpgradeData.Add(new UpgradeInfo(currPath, upgradeFile, int.Parse(version)));
                }
            }

            sortedUpgradeData.Sort((left, right) =>
            {
                if (left.File.Length < right.File.Length)
                {
                    return -1;
                }
                else if (left.File.Length > right.File.Length)
                {
                    return 1;
                }
                else
                {
                    if (left.Version < right.Version)
                    {
                        return -1;
                    }
                    else if (left.Version > right.Version)
                    {
                        return 1;
                    }
                }

                return 0;
            });

            foreach (var upgradeData in sortedUpgradeData)
            {
                ExecuteUpgradeFile(upgradeData);
            }
        }

    }
}