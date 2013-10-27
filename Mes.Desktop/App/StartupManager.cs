namespace GitHub.Helpers
{
    using GitHub.Models;
    using GitHub.PortableGit.Helpers;
    using GitHub.UI.Helpers;
    using Microsoft.WindowsAPICodePack.ApplicationServices;
    using NLog;
    using ReactiveUI.Xaml;
    using System;
    using System.ComponentModel.Composition;
    using System.Reactive.Concurrency;

    [Export(typeof(IStartupManager))]
    public sealed class StartupManager : IStartupManager, IDisposable
    {
        private CrashManager crashManager;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public StartupManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            CrashManager crashManager = this.crashManager;
            if (crashManager != null)
            {
                crashManager.Dispose();
            }
        }

        public void HandleCommandLine()
        {
            this.serviceProvider.Get<ICommandLineHandler>().Handle();
        }

        public void InstallShortcuts()
        {
            this.serviceProvider.Get<IShortcutManager>().Install();
        }

        public void InstallUrlProtocolHandler()
        {
            this.serviceProvider.Get<IUrlProtocolInstaller>().Install();
        }

        public void LogStartup()
        {
            this.serviceProvider.Get<StartupLogger>().Start();
        }

        public void PrepareUserInterface()
        {
            this.serviceProvider.Get<IMessageBus>().RegisterScheduler<KeyEventArgs>(Scheduler.Immediate, null);
            HardwareRenderingHelper.DisableHwRenderingForCrapVideoCards();
            UserError.RegisterHandler((Func<UserError, IObservable<RecoveryOptionResult>>) (x => GitProcessErrorFilter.HandleUserError(x)));
        }

        public void RegisterForApplicationRecovery()
        {
            try
            {
                ApplicationRestartRecoveryManager.RegisterForApplicationRestart(new RestartSettings("--justdied", RestartRestrictions.None));
            }
            catch (Exception exception)
            {
                if (!(exception is PlatformNotSupportedException))
                {
                    log.WarnException("Failed to set up application restart", exception);
                }
            }
        }

        public void RunCacheMigrations()
        {
            this.serviceProvider.Get<ICacheMigrationRunner>().RunMigrations();
        }

        public void StartCrashManager()
        {
            this.crashManager = this.serviceProvider.Get<CrashManager>();
        }
    }
}

