using System.Threading.Tasks;

namespace CodeM.FastApi.Cache
{
    public interface ICache
    {
        void SetString(string key, string value);

        Task SetStringAsync(string key, string value);

        void SetString(string key, string value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        Task SetStringAsync(string key, string value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        string GetString(string key);

        Task<string> GetStringAsync(string key);

        void SetInt32(string key, int value);

        Task SetInt32Async(string key, int value);

        void SetInt32(string key, int value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        Task SetInt32Async(string key, int value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        int? GetInt32(string key);

        Task<int?> GetInt32Async(string key);

        void SetInt64(string key, long value);

        Task SetInt64Async(string key, long value);

        void SetInt64(string key, long value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        Task SetInt64Async(string key, long value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        long? GetInt64(string key);

        Task<long?> GetInt64Async(string key);

        void SetDouble(string key, double value);

        Task SetDoubleAsync(string key, double value);

        void SetDouble(string key, double value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        Task SetDoubleAsync(string key, double value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        double? GetDouble(string key);

        Task<double?> GetDoubleAsync(string key);

        void SetBoolean(string key, bool value);

        Task SetBooleanAsync(string key, bool value);

        void SetBoolean(string key, bool value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        Task SetBooleanAsync(string key, bool value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        bool GetBoolean(string key);

        Task<bool> GetBooleanAsync(string key);

        void Set(string key, byte[] value);

        Task SetAsync(string key, byte[] value);

        void Set(string key, byte[] value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        Task SetAsync(string key, byte[] value, long seconds, ExpirationType type = ExpirationType.RelativeToNow);

        byte[] Get(string key);

        Task<byte[]> GetAsync(string key);

        bool ContainsKey(string key);

        bool TryGetValue<T>(string key, out T result);

        void Remove(string key);

        Task RemoveAsync(string key);

        void MultiSet(string key, params object[] values);

        void MultiSet2(string key, long seconds, ExpirationType type, params object[] values);

        dynamic MultiGet(string key, params string[] names);

        Task MultiSetAsync(string key, params object[] values);

        Task MultiSet2Async(string key, long seconds, ExpirationType type = ExpirationType.RelativeToNow, params object[] values);

        Task<dynamic> MultiGetAsync(string key, params string[] names);
    }
}
