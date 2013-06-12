namespace ProtoBuf.Linq.LinqImpl
{
    internal static class Singleton<T>
        where T : class, new()
    {
        private static readonly T Instance = new T();

        public static T GetInstance()
        {
            return Instance;
        }
    }
}