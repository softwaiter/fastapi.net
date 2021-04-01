using CodeM.Common.Orm;
using System;

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
            if (OrmUtils.GetVersion() < 0)
            {
                OrmUtils.EnableVersionControl();
            }
        }

        public static void Upgrade()
        {
            int currentVersion = OrmUtils.GetVersion();
            if (currentVersion < 0)
            {
                throw new Exception("版本控制未开启，请先使用EnableVersionControl方法开启版本控制。");
            }

            UpgradeData.Enum(upgradeVersion =>
            {
                if (upgradeVersion.Version > currentVersion)
                {
                    int trans = OrmUtils.GetTransaction();
                    try
                    {
                        upgradeVersion.Enum(sqlCmd =>
                        {
                            OrmUtils.ExecSql(sqlCmd, trans);
                        });
                        OrmUtils.SetVersion(upgradeVersion.Version, trans);
                        OrmUtils.CommitTransaction(trans);

                        currentVersion = upgradeVersion.Version;
                    }
                    catch (Exception exp)
                    {
                        OrmUtils.RollbackTransaction(trans);
                        throw exp;
                    }
                }
            });
        }

    }
}
