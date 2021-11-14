using CodeM.Common.Tools;
using System;

namespace CodeM.FastApi.DbUpgrade
{
    internal class UpgradeLoader
    {
        public static void Load(string filename)
        {
            int currentVersion = 0;
            Xmtool.Xml().Iterate(filename, (nodeInfo) =>
            {
                if (!nodeInfo.IsEndNode)
                {
                    if (nodeInfo.Path == "/versions/version")
                    {
                        string idStr = nodeInfo.GetAttribute("id");
                        if (idStr == null)
                        {
                            throw new Exception("缺少id属性。 " + filename + " - Line " + nodeInfo.Line);
                        }
                        else if (string.IsNullOrWhiteSpace(idStr))
                        {
                            throw new Exception("id属性不能为空。 " + filename + " - Line " + nodeInfo.Line);
                        }

                        if (!Xmtool.Regex().IsPositiveInteger(idStr))
                        {
                            throw new Exception("id属性必须是有效正整数。 " + filename + " - Line " + nodeInfo.Line);
                        }

                        currentVersion = int.Parse(idStr);
                        UpgradeData.AddVersion(currentVersion);
                    }
                    else if (nodeInfo.Path == "/versions/version/sql/@text")
                    {
                        UpgradeData.AddVersionCommand(currentVersion, nodeInfo.Text.Trim());
                    }
                }
                return true;
            });
        }
    }
}
