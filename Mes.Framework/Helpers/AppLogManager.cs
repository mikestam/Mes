namespace GitHub.Helpers
{
    using Akavache;
    using GitHub;
    using GitHub.Api;
    using GitHub.IO;
    using GitHub.Models;
    using NLog;
    using NLog.Config;
    using NLog.Targets;
    using ReactiveUI;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;

    public sealed class AppLogManager : IAppLogManager, IHaystackContext, IDisposable
    {
        private readonly bool isDebug;
        private const string layout = "${longdate}|${level:uppercase=true}|thread:${threadid}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}";

        public AppLogManager(IEnvironment environment, IMessageBus messageBus, bool isDebug)
        {
            Action<string> onNext = null;
            Ensure.ArgumentNotNull(environment, "environment");
            Ensure.ArgumentNotNull(messageBus, "messageBus");
            this.Environment = environment;
            this.isDebug = isDebug;
            if (!App.IsInDesignMode())
            {
                this.LogFilePath = environment.LocalGitHubApplicationDataPath.Combine("TheLog.txt");
                this.Target = isDebug ? ((TargetWithLayout) new DebuggerTarget()) : ((TargetWithLayout) new RingTarget());
                this.Target.Layout = "${longdate}|${level:uppercase=true}|thread:${threadid}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}";
                if (onNext == null)
                {
                    onNext = x => this.SelectedRepositoryNameWithOwner = x;
                }
                messageBus.Listen<string>("SelectedRepoNameWithOwner").Subscribe<string>(onNext);
            }
        }

        public void Dispose()
        {
            this.Target.SafeDispose();
            GitHub.Helpers.HaystackTarget haystackTarget = this.HaystackTarget;
            if (haystackTarget != null)
            {
                haystackTarget.Dispose();
            }
        }

        public void FlushLogBeforeExit()
        {
            try
            {
                this.WriteToLogFile();
            }
            catch (Exception)
            {
            }
        }

        public void Initialize()
        {
            GitHub.Helpers.HaystackTarget haystackTarget = this.HaystackTarget;
            if (haystackTarget == null)
            {
                throw new InvalidOperationException("No HaystackTarget set");
            }
            LoggingConfiguration configuration = new LoggingConfiguration();
            NLog.LogLevel minLevel = this.isDebug ? NLog.LogLevel.Debug : NLog.LogLevel.Info;
            configuration.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Warn, haystackTarget));
            LoggingRule item = new LoggingRule("GitHub.*", minLevel, this.Target) {
                Final = true
            };
            configuration.LoggingRules.Add(item);
            configuration.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Warn, this.Target));
            NLog.LogManager.Configuration = configuration;
        }

        public void SpillTheLogToDesktop()
        {
            if (!(this.Target is RingTarget))
            {
                Console.WriteLine("Can't spill the log in Debug mode");
            }
            else
            {
                PathString str = this.Environment.DesktopDirectoryPath.Combine("GitHubLog.txt");
                using (StreamWriter writer = new StreamWriter((string) str))
                {
                    RingTarget.DumpToWriterOrConsole(null, writer);
                }
                Process.Start((string) str);
            }
        }

        private void WriteToLogFile()
        {
            if (this.Target is RingTarget)
            {
                using (StreamWriter writer = new StreamWriter((string) this.LogFilePath, true, Encoding.UTF8))
                {
                    RingTarget.DumpToWriterOrConsole(null, writer);
                }
            }
        }

        public IObservable<string> CurrentGitHubUserName
        {
            get
            {
                IServiceProvider serviceProvider = this.ServiceProvider;
                if (serviceProvider == null)
                {
                    return Observable.Return<string>(null);
                }
                return serviceProvider.Get<ILoginCache>().GetLoginAsync(ApiClient.GitHubDotComApiBaseUri.Host).Select<LoginInfo, string>(((Func<LoginInfo, string>) (x => x.UserName)));
            }
        }

        public string EnterpriseHost
        {
            get
            {
                IServiceProvider serviceProvider = this.ServiceProvider;
                if (serviceProvider == null)
                {
                    return null;
                }
                IRepositoryHosts hosts = serviceProvider.Get<IRepositoryHosts>();
                if (hosts.EnterpriseHost == null)
                {
                    return null;
                }
                return hosts.EnterpriseHost.ApiBaseUri.Host;
            }
        }

        public IEnvironment Environment { get; private set; }

        public GitHub.Helpers.HaystackTarget HaystackTarget { get; set; }

        public PathString LogFilePath { get; private set; }

        public string SelectedRepositoryNameWithOwner { get; private set; }

        public IServiceProvider ServiceProvider { get; set; }

        public TargetWithLayout Target { get; private set; }
    }
}

