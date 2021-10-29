namespace CodeM.FastApi.Cache
{
    public enum ExpirationType
    {
        Absolute = 0,
        RelativeToNow = 1,
        Sliding = 2
    }
}
