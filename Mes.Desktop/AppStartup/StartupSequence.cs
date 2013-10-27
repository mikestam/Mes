namespace Mes.AppStartup
{
    using Akavache;
    using Akavache.Sqlite3;
    using Caliburn.Micro;
    using GitHub;
    using GitHub.Extensions;
    using GitHub.Helpers;
    using NLog;
    using ReactiveUI;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Reactive.Linq;
    using System.Security.Cryptography.X509Certificates;

    public static class StartupSequence
    {
        private static readonly Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static void InitializeApplication()
        {
            Func<int[]> block = null;
            ChromiumPackageManager chromeExtractor = new ChromiumPackageManager(AppBootstrapper.OperatingSystemBridge);
            try
            {
                if (block == null)
                {
                    block = () => chromeExtractor.ExtractChrome().WaitUntilFinished<int>();
                }
                block.Retry<int[]>(5);
            }
            catch (Exception exception)
            {
                log.ErrorException("Couldn't extract chrome even after three tries", exception);
            }
            BlobCache.ApplicationName = "GitHub";
            string databaseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitHub", "cache.db");
            string str2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitHub", "cache.db");
            string str3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitHub", "secure-cache.db");
            (from x in new string[] { databaseFile, str2, str3 }
                select new DirectoryInfo(Path.GetDirectoryName(x)) into x
                where !x.Exists
                select x).ForEach<DirectoryInfo>((Action<DirectoryInfo>) (x => x.Create()));
            BlobCache.LocalMachine = new SqlitePersistentBlobCache(databaseFile, null, null);
            BlobCache.UserAccount = new SqlitePersistentBlobCache(str2, null, null);
            BlobCache.Secure = new Akavache.Sqlite3.EncryptedBlobCache(str3, null);
            RxApp.GetFieldNameForPropertyNameFunc = x => char.ToLowerInvariant(x[0]) + x.Substring(1);
            if (!App.IsInDesignMode())
            {
                if (!RxApp.InUnitTestRunner())
                {
                    Execute.ResetWithoutDispatcher();
                }
                Environment.SetEnvironmentVariable("windir", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                ServicePointManager.ServerCertificateValidationCallback = (RemoteCertificateValidationCallback) Delegate.Combine(ServicePointManager.ServerCertificateValidationCallback, delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                    {
                        return true;
                    }
                    LogCertificateChainStatus(chain, certificate, sslPolicyErrors);
                    return false;
                });
            }
        }

        private static void LogCertificateChainStatus(X509Chain chain, X509Certificate certificate, SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                string subject = "(null)";
                string issuer = "(null)";
                if (certificate != null)
                {
                    subject = certificate.Subject;
                    issuer = certificate.Issuer;
                }
                log.Info<string, string, SslPolicyErrors>(CultureInfo.InvariantCulture, "SSL Cert Error. Certificate Subject: '{0}', Issuer: '{1}', SSLPolicyErrors: '{2}'", subject, issuer, sslPolicyErrors);
                if (((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None) && (chain != null))
                {
                    foreach (X509ChainStatus status in chain.ChainStatus)
                    {
                        log.Warn<X509ChainStatusFlags, string>(CultureInfo.InvariantCulture, "Chain Status Flag: '{0}', StatusInformation: '{1}'", status.Status, status.StatusInformation);
                    }
                }
            }
            catch (Exception exception)
            {
                log.ErrorException("Error occurred while logging certificate chain", exception);
            }
        }

        public static void Start(IStartupManager startupManager)
        {
            Ensure.ArgumentNotNull(startupManager, "startupManager");
            startupManager.StartCrashManager();
            startupManager.HandleCommandLine();
            startupManager.RunCacheMigrations();
            Observable.Start(delegate {
                startupManager.LogStartup();
                startupManager.InstallUrlProtocolHandler();
                startupManager.InstallShortcuts();
            }, RxApp.TaskpoolScheduler);
            startupManager.RegisterForApplicationRecovery();
            startupManager.PrepareUserInterface();
        }
    }
}

