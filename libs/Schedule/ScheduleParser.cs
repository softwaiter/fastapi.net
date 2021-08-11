using CodeM.Common.Tools;
using CodeM.Common.Tools.Xml;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.Schedule
{
    public class ScheduleParser
    {
        public static List<ScheduleSetting> Parse(string file)
        {
            List<ScheduleSetting> result = new List<ScheduleSetting>();

            Regex reInt = new Regex("^[1-9][0-9]*$");

            XmlUtils.Iterate(file, (XmlNodeInfo nodeInfo) =>
            {
                if (!nodeInfo.IsEndNode)
                {
                    if (nodeInfo.Path == "/schedules/job")
                    {
                        string idStr = nodeInfo.GetAttribute("id");
                        if (idStr == null)
                        {
                            throw new Exception("缺少id属性。 " + file + " - Line " + nodeInfo.Line);
                        }
                        if (string.IsNullOrWhiteSpace(idStr))
                        {
                            throw new Exception("id属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                        }

                        string intervalStr = nodeInfo.GetAttribute("interval");
                        string cronStr = nodeInfo.GetAttribute("cron");

                        if (!string.IsNullOrWhiteSpace(intervalStr) &&
                            !string.IsNullOrWhiteSpace(cronStr))
                        {
                            throw new Exception("interval属性和cron属性不能同时存在。 " + file + " - Line " + nodeInfo.Line);
                        }

                        if (!string.IsNullOrWhiteSpace(intervalStr))
                        {
                            try
                            {
                                DateTimeUtils.CheckStringTimeSpan(intervalStr);
                            }
                            catch (Exception exp)
                            {
                                throw new Exception("格式非法。 " + file + " - Line " + nodeInfo.Line, exp);
                            }
                        }

                        int repeat = 0;
                        string repeatStr = nodeInfo.GetAttribute("repeat");
                        if (repeatStr != null)
                        {
                            if (string.IsNullOrWhiteSpace(repeatStr))
                            {
                                throw new Exception("repeat属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }

                            if (!reInt.IsMatch(repeatStr))
                            {
                                throw new Exception("repeat属性必须是有效正整数。 " + file + " - Line " + nodeInfo.Line);
                            }

                            repeat = int.Parse(repeatStr);
                        }

                        string handlerStr = nodeInfo.GetAttribute("handler");
                        if (string.IsNullOrWhiteSpace(handlerStr))
                        {
                            throw new Exception("handler属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                        }

                        ScheduleSetting setting = new ScheduleSetting();
                        setting.Id = idStr;
                        setting.Interval = intervalStr;
                        setting.Cron = cronStr;
                        setting.Repeat = repeat;
                        setting.Handler = handlerStr;
                        result.Add(setting);
                    }
                }
                return true;
            });

            return result;
        }
    }
}
