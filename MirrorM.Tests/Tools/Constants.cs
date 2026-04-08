namespace MirrorM.Tests.Tools
{
    internal class Constants
    {
        public const string DATABASE_NAME = "mirrorm_tests";
        public const string DATABASE_INDEPENDENT_CONNECTION_STRING = "Host=localhost;Username=postgres;Password=secret;Port=5432;Pooling=false";
        public const string CONNECTION_STRING = $"Host=localhost;Database={DATABASE_NAME};Username=postgres;Password=secret;Port=5432;Pooling=false";
    }
}
