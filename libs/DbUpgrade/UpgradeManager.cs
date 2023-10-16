using CodeM.Common.Orm;
using CodeM.Common.Tools;
using System;
using System.Collections.Generic;
using System.IO;

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
                        Derd.ExecSql(cmd, trans);
                    }

                    Derd.SetVersion(info.ModelPath, info.Version, trans);
                    Derd.CommitTransaction(trans);
                }
                catch (Exception exp)
                {
                    Derd.RollbackTransaction(trans);
                    throw exp;
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

            EnumerationOptions option = new EnumerationOptions();
            option.RecurseSubdirectories = true;
            IEnumerable<string> upgradeFiles = Directory.EnumerateFiles(modelPath, ".upgrade.v*.xml", option);
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