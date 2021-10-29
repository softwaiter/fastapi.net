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

        bool TryGetValue(object key, out object result);

        void Remove(string key);

        Task RemoveAsync(string key);
    }
}
