using System;
using System.Collections.Generic;

namespace CodeM.FastApi.DbUpgrade
{
    internal class UpgradeData
    {
        internal static SortedList<int, UpgradeVersion> sVersions = new SortedList<int, UpgradeVersion>();

        public static void AddVersion(int version)
        {
            sVersions.Add(version, new UpgradeVersion(version));
        }

        public static bool AddVersionCommand(int version, string command)
        {
            UpgradeVersion uv;
            if (sVersions.TryGetValue(version, out uv))
            {
                uv.AddCommand(command);
                return true;
            }
            return false;
        }

        public static void Enum(Action<UpgradeVersion> callback)
        {
            foreach (int key in sVersions.Keys)
            {
                callback(sVersions[key]);
            }
        }
    }
}
