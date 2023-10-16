using CodeM.Common.Tools;
using System.Collections.Generic;

namespace CodeM.FastApi.DbUpgrade
{
    internal class UpgradeLoader
    {
        public static List<string> Load(string filename)
        {
            List<string> data = new List<string>();

            Xmtool.Xml().Iterate(filename, (nodeInfo) =>
            {
                if (!nodeInfo.IsEndNode)
                {
                    if (nodeInfo.Path == "/sqls/sql/@text")
                    {
                        data.Add(nodeInfo.Text.Trim());
                    }
                    else if (nodeInfo.Path == "/sqls/sql/@cdata")
                    {
                        data.Add(nodeInfo.CData.Trim());
                    }
                }
                return true;
            });

            return data;
        }
    }
}
