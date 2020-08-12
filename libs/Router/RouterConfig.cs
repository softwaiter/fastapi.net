using CodeM.Common.Tools.Xml;
using CodeM.FastApi.Config;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.Router
{
    internal class RouterConfig
    {
        public class RouterItem
        {
            public RouterItem()
            {
            }

            public string Path
            {
                get;
                set;
            }

            public string Method
            {
                get;
                set;
            }

            public string Handler
            {
                get;
                set;
            }

            public string Resource
            {
                get;
                set;
            }

            public string Model
            {
                get;
                set;
            }

            //最小空闲处理器数量
            public int MaxIdle
            {
                get;
                set;
            } = 10;

            //最大并发请求数
            public int MaxConcurrent
            {
                get;
                set;
            } = 100;

            //每个处理器最大使用次数
            public int MaxInvokePerInstance
            {
                get;
                set;
            } = 10000;

            //路由中间件（通常用于特殊的业务检查等）
            public List<string> Middlewares
            {
                get;
            } = new List<string>();

        }

        public List<RouterItem> Items { get; } = new List<RouterItem>();

        public void Load(AppConfig config, string file)
        {
            Regex reInt = new Regex("^[1-9][0-9]*$");
            Regex reInt2 = new Regex("^[0-9]$");

            XmlUtils.Iterate(file, (XmlNodeInfo nodeInfo) =>
            {
                if (!nodeInfo.IsEndNode)
                {
                    if (nodeInfo.Path == "/routers/router")
                    {
                        RouterItem item = new RouterItem();
                        item.MaxIdle = config.Router.MaxIdlePerRouter;
                        item.MaxConcurrent = config.Router.MaxConcurrentPerRouter;
                        item.MaxInvokePerInstance = config.Router.MaxInvokePerInstance;

                        string path = nodeInfo.GetAttribute("path");
                        if (path == null)
                        {
                            throw new Exception("缺少path属性。 " + file + " - Line " + nodeInfo.Line);
                        }
                        else if (string.IsNullOrWhiteSpace(path))
                        {
                            throw new Exception("path属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                        }
                        item.Path = path.Trim().ToLower();

                        string method = nodeInfo.GetAttribute("method");
                        if (method != null)
                        {
                            if (string.IsNullOrWhiteSpace(method))
                            {
                                throw new Exception("method属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }
                            else if (!"GET,POST,PUT,DELETE".Contains(method.ToUpper()))
                            {
                                throw new Exception("method属性只能是[GET,POST,PUT,DELETE]其中的一项。 " + file + " - Line " + nodeInfo.Line);
                            }
                            item.Method = method.Trim().ToLower();
                        }

                        string handler = nodeInfo.GetAttribute("handler");
                        if (handler != null)
                        {
                            if (string.IsNullOrWhiteSpace(handler))
                            {
                                throw new Exception("handler属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }
                            item.Handler = handler.Trim().ToLower();
                        }

                        string resource = nodeInfo.GetAttribute("resource");
                        if (resource != null)
                        {
                            if (string.IsNullOrWhiteSpace(resource))
                            {
                                throw new Exception("resource属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }
                            item.Resource = resource.Trim().ToLower();
                        }
                        if (!string.IsNullOrWhiteSpace(item.Method) && 
                            !string.IsNullOrWhiteSpace(item.Resource))
                        {
                            throw new Exception("method属性和resource属性不能同时存在。 " + file + " - Line " + nodeInfo.Line);
                        }

                        string model = nodeInfo.GetAttribute("model");
                        if (model != null)
                        {
                            if (string.IsNullOrWhiteSpace(model))
                            {
                                throw new Exception("model属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }
                            item.Model = model.Trim().ToLower();
                        }
                        if (!string.IsNullOrWhiteSpace(item.Method) &&
                            !string.IsNullOrWhiteSpace(item.Model))
                        {
                            throw new Exception("method属性和resource属性不能同时存在。 " + file + " - Line " + nodeInfo.Line);
                        }

                        if (string.IsNullOrWhiteSpace(handler) &&
                            string.IsNullOrWhiteSpace(resource) &&
                            string.IsNullOrWhiteSpace(model))
                        {
                            throw new Exception("handler属性、resource属性、model属性必须设置一个。 " + file + " - Line " + nodeInfo.Line);
                        }

                        string middlewares = nodeInfo.GetAttribute("middlewares");
                        if (middlewares != null)
                        {
                            if (string.IsNullOrWhiteSpace(middlewares))
                            {
                                throw new Exception("middlewares属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }

                            string[] items = middlewares.Split(",");
                            item.Middlewares.AddRange(items);
                        }

                        string maxConcurrent = nodeInfo.GetAttribute("maxConcurrent");
                        if (maxConcurrent != null)
                        {
                            if (string.IsNullOrWhiteSpace(maxConcurrent))
                            {
                                throw new Exception("maxConcurrent属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }

                            if (!reInt.IsMatch(maxConcurrent))
                            {
                                throw new Exception("maxConcurrent属性必须是有效正整数。 " + file + " - Line " + nodeInfo.Line);
                            }

                            item.MaxConcurrent = int.Parse(maxConcurrent);
                        }

                        string maxIdle = nodeInfo.GetAttribute("maxIdle");
                        if (maxIdle != null)
                        {
                            if (string.IsNullOrWhiteSpace(maxConcurrent))
                            {
                                throw new Exception("maxIdle属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }

                            if (!reInt.IsMatch(maxIdle) && !reInt2.IsMatch(maxIdle))
                            {
                                throw new Exception("maxIdle属性必须是有效自然数。 " + file + " - Line " + nodeInfo.Line);
                            }

                            item.MaxIdle = int.Parse(maxIdle);
                        }

                        string maxInvokePerInstance = nodeInfo.GetAttribute("maxInvokePerInstance");
                        if (maxInvokePerInstance != null)
                        {
                            if (string.IsNullOrWhiteSpace(maxInvokePerInstance))
                            {
                                throw new Exception("maxInvokePerInstance属性不能为空。 " + file + " - Line " + nodeInfo.Line);
                            }

                            if (!reInt.IsMatch(maxInvokePerInstance))
                            {
                                throw new Exception("maxInvokePerInstance属性必须是有效正整数。 " + file + " - Line " + nodeInfo.Line);
                            }

                            item.MaxInvokePerInstance = int.Parse(maxInvokePerInstance);
                        }

                        Items.Add(item);
                    }
                }
                return true;
            });
        }
    }
}
