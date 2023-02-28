using CodeM.Common.Orm;
using System;
using System.Collections.Generic;

namespace CodeM.FastApi.DbUpgrade
{
    public class UpgradeManager
    {
        public static void Load(string filename)
        {
            UpgradeLoader.Load(filename);
        }

        public static void EnableVersionControl()
        {
            if (Derd.GetVersion() < 0)
            {
                Derd.EnableVersionControl();
            }
        }

        public static void Upgrade()
        {
            int currentVersion = Derd.GetVersion();
            if (currentVersion < 0)
            {
                throw new Exception("版本控制未开启，请先使用EnableVersionControl方法开启版本控制。");
            }

            UpgradeData.Enum(upgradeVersion =>
            {
                if (upgradeVersion.Version > currentVersion)
                {
                    int trans = Derd.GetTransaction();
                    try
                    {
                        upgradeVersion.Enum(sqlCmd =>
                        {
                            Derd.ExecSql(sqlCmd, trans);
                        });
                        Derd.SetVersion(upgradeVersion.Version, trans);
                        Derd.CommitTransaction(trans);

                        currentVersion = upgradeVersion.Version;
                    }
                    catch (Exception exp)
                    {
                        Derd.RollbackTransaction(trans);
                        throw exp;
                    }
                }
            });
        }

    }
}