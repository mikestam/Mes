namespace GitHub.Helpers
{
    using System;

    public interface IStartupManager : IDisposable
    {
        void HandleCommandLine();
        void InstallShortcuts();
        void InstallUrlProtocolHandler();
        void LogStartup();
        void PrepareUserInterface();
        void RegisterForApplicationRecovery();
        void RunCacheMigrations();
        void StartCrashManager();
    }
}

