using CodeM.Common.Tools.Json;
using System;
using System.Threading.Tasks;

namespace CodeM.FastApi.Cache
{
    public class CacheBase : ICache
    {
        public virtual bool ContainsKey(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual byte[] Get(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task<byte[]> GetAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool GetBoolean(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task<bool> GetBooleanAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual int? GetInt32(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task<int?> GetInt32Async(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual string GetString(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task<string> GetStringAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public dynamic MultiGet(string key, params string[] names)
        {
            dynamic obj = new DynamicObjectExt();
            if (ContainsKey(key))
            {
                int? propCount = GetInt32(key);
                if (propCount != null && propCount.HasValue)
                {
                    for (int i = 0; i < Math.Min(propCount.Value, names.Length); i++)
                    {
                        string name = names[i];

                        string typeKey = string.Concat(key, "$", i, "$type");
                        string valueKey = string.Concat(key, "$", i, "$value");

                        int? type = GetInt32(typeKey);
                        if (type != null && type.HasValue)
                        {
                            if (type.Value == 8)
                            {
                                double? value = GetDouble(valueKey);
                                if (value != null && value.HasValue)
                                {
                                    obj.TrySetValue(name, value.Value);
                                }
                            }
                            else if (type.Value == 7)
                            {
                                double? value = GetDouble(valueKey);
                                if (value != null && value.HasValue)
                                {
                                    obj.TrySetValue(name, (decimal)value.Value);
                                }
                            }
                            else if (type.Value == 6)
                            {
                                double? value = GetDouble(valueKey);
                                if (value != null && value.HasValue)
                                {
                                    obj.TrySetValue(name, (float)value.Value);
                                }
                            }
                            else if (type.Value == 5)
                            {
                                string value = GetString(valueKey);
                                obj.TrySetValue(name, DateTime.Parse(value));
                            }
                            else if (type.Value == 4)
                            {
                                bool vaule = GetBoolean(valueKey);
                                obj.TrySetValue(name, vaule);
                            }
                            else if (type.Value == 3)
                            {
                                long? value = GetInt64(valueKey);
                                if (value != null && value.HasValue)
                                {
                                    obj.TrySetValue(name, value.Value);
                                }
                            }
                            else if (type.Value == 2)
                            {
                                int? value = GetInt32(valueKey);
                                if (value != null && value.HasValue)
                                {
                                    obj.TrySetValue(name, value.Value);
                                }
                            }
                            else if (type.Value == 1)
                            {
                                int? value = GetInt32(valueKey);
                                if (value != null && value.HasValue)
                                {
                                    obj.TrySetValue(name, (Int16)value.Value);
                                }
                            }
                            else
                            {
                                string value = GetString(valueKey);
                                obj.TrySetValue(name, value);
                            }
                        }
                    }
                }
            }
            return obj;
        }

        public async Task<dynamic> MultiGetAsync(string key, params string[] names)
        {
            return await Task.Run(() =>
            {
                return MultiGetAsync(key, names);
            });
        }

        public void MultiSet(string key, params object[] values)
        {
            SetInt32(key, values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                string typeKey = string.Concat(key, "$", i, "$type");
                string valueKey = string.Concat(key, "$", i, "$value");

                object value = values[i];
                if (value == null)
                {
                    value = "";
                }
                Type _typ = value.GetType();
                if (_typ == typeof(double))
                {
                    SetInt32(typeKey, 8);
                    SetDouble(valueKey, (double)value);
                }
                else if (_typ == typeof(decimal))
                {
                    SetInt32(typeKey, 7);
                    SetDouble(valueKey, (double)value);
                }
                else if (_typ == typeof(float))
                {
                    SetInt32(typeKey, 6);
                    SetDouble(valueKey, (double)value);
                }
                else if (_typ == typeof(DateTime))
                {
                    SetInt32(typeKey, 5);
                    SetString(valueKey, ((DateTime)value).ToString("yyyy-MM-dd hh:mm:ss"));
                }
                else if (_typ == typeof(Boolean))
                {
                    SetInt32(typeKey, 4);
                    SetBoolean(valueKey, (bool)value);
                }
                else if (_typ == typeof(Int64))
                {
                    SetInt32(typeKey, 3);
                    SetInt64(valueKey, (long)value);
                }
                else if (_typ == typeof(Int32))
                {
                    SetInt32(typeKey, 2);
                    SetInt32(valueKey, (int)value);
                }
                else if (_typ == typeof(Int16))
                {
                    SetInt32(typeKey, 1);
                    SetInt32(valueKey, (Int16)value);
                }
                else
                {
                    SetInt32(typeKey, 0);
                    SetString(valueKey, value.ToString());
                }
            }
        }

        public void MultiSet2(string key, long seconds, ExpirationType type, params object[] values)
        {
            SetInt32(key, values.Length, seconds, type);
            for (int i = 0; i < values.Length; i++)
            {
                string typeKey = string.Concat(key, "$", i, "$type");
                string valueKey = string.Concat(key, "$", i, "$value");

                object value = values[i];
                if (value == null)
                {
                    value = "";
                }
                Type _typ = value.GetType();
                if (_typ == typeof(double))
                {
                    SetInt32(typeKey, 8);
                    SetDouble(valueKey, (double)value, seconds, type);
                }
                else if (_typ == typeof(decimal))
                {
                    SetInt32(typeKey, 7);
                    SetDouble(valueKey, (double)value, seconds, type);
                }
                else if (_typ == typeof(float))
                {
                    SetInt32(typeKey, 6);
                    SetDouble(valueKey, (double)value, seconds, type);
                }
                if (_typ == typeof(DateTime))
                {
                    SetInt32(typeKey, 5);
                    SetString(valueKey, ((DateTime)value).ToString("yyyy-MM-dd hh:mm:ss"), seconds, type);
                }
                else if (_typ == typeof(Boolean))
                {
                    SetInt32(typeKey, 4, seconds, type);
                    SetBoolean(valueKey, (bool)value, seconds, type);
                }
                else if (_typ == typeof(Int64))
                {
                    SetInt32(typeKey, 3, seconds, type);
                    SetInt64(valueKey, (long)value, seconds, type);
                }
                else if (_typ == typeof(Int32))
                {
                    SetInt32(typeKey, 2, seconds, type);
                    SetInt32(valueKey, (int)value, seconds, type);
                }
                else if (_typ == typeof(Int16))
                {
                    SetInt32(typeKey, 1, seconds, type);
                    SetInt32(valueKey, (Int16)value, seconds, type);
                }
                else
                {
                    SetInt32(typeKey, 0, seconds, type);
                    SetString(valueKey, value.ToString(), seconds, type);
                }
            }
        }

        public async Task MultiSetAsync(string key, params object[] values)
        {
            await Task.Run(() =>
            {
                MultiSet(key, values);
            });
        }

        public async Task MultiSet2Async(string key, long seconds, ExpirationType type, params object[] values)
        {
            await Task.Run(() =>
            {
                MultiSet(key, seconds, type, values);
            });
        }

        public virtual void Remove(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task RemoveAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Set(string key, byte[] value)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Set(string key, byte[] value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetAsync(string key, byte[] value)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetAsync(string key, byte[] value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetBoolean(string key, bool value)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetBoolean(string key, bool value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetBooleanAsync(string key, bool value)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetBooleanAsync(string key, bool value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetInt32(string key, int value)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetInt32(string key, int value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetInt32Async(string key, int value)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetInt32Async(string key, int value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetString(string key, string value)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetString(string key, string value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetStringAsync(string key, string value)
        {
            throw new System.NotImplementedException();
        }

        public virtual Task SetStringAsync(string key, string value, long seconds, ExpirationType type)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool TryGetValue<T>(string key, out T result)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetInt64(string key, long value)
        {
            throw new NotImplementedException();
        }

        public virtual Task SetInt64Async(string key, long value)
        {
            throw new NotImplementedException();
        }

        public virtual void SetInt64(string key, long value, long seconds, ExpirationType type)
        {
            throw new NotImplementedException();
        }

        public virtual Task SetInt64Async(string key, long value, long seconds, ExpirationType type)
        {
            throw new NotImplementedException();
        }

        public virtual long? GetInt64(string key)
        {
            throw new NotImplementedException();
        }

        public virtual Task<long?> GetInt64Async(string key)
        {
            throw new NotImplementedException();
        }

        public virtual void SetDouble(string key, double value)
        {
            throw new NotImplementedException();
        }

        public virtual Task SetDoubleAsync(string key, double value)
        {
            throw new NotImplementedException();
        }

        public virtual void SetDouble(string key, double value, long seconds, ExpirationType type)
        {
            throw new NotImplementedException();
        }

        public virtual Task SetDoubleAsync(string key, double value, long seconds, ExpirationType type)
        {
            throw new NotImplementedException();
        }

        public virtual double? GetDouble(string key)
        {
            throw new NotImplementedException();
        }

        public virtual Task<double?> GetDoubleAsync(string key)
        {
            throw new NotImplementedException();
        }
    }
}
