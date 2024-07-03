namespace RainLanguageServer.RainLanguage2
{
    internal interface IDisposable
    {
        void Dispose(Manager manager);
    }
    internal class Manager
    {
        public const int LIBRARY_SELF = -1;
        public const int LIBRARY_KERNEL = -2;
        public readonly AbstractLibrary kernel;
        public readonly AbstractLibrary library;

    }
}
