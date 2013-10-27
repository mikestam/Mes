namespace GitHub.Helpers
{
    using GitHub.Api;
    using GitHub.AppStartup;
    using GitHub.IO;
    using GitHub.ViewModels;
    using NLog;
    using System;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Net;

    [Export(typeof(StartupLogger))]
    public class StartupLogger
    {
        private readonly IAppContext appContext;
        private readonly IEnvironment environment;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        [ImportingConstructor]
        public StartupLogger(IAppContext appContext, IEnvironment environment)
        {
            Ensure.ArgumentNotNull(appContext, "appContext");
            Ensure.ArgumentNotNull(environment, "environment");
            this.appContext = appContext;
            this.environment = environment;
        }

        private static void LogProxyServerConfiguration()
        {
            WebProxy defaultProxy = WebProxy.GetDefaultProxy();
            string argument = (defaultProxy.Address != null) ? defaultProxy.Address.ToString() : "(None)";
            log.Info(CultureInfo.InvariantCulture, "Proxy information: {0}", argument);
            try
            {
                if (defaultProxy.Credentials == null)
                {
                    log.Info(CultureInfo.InvariantCulture, "Couldn't fetch creds for proxy", new object[0]);
                }
                else
                {
                    NetworkCredential credential = defaultProxy.Credentials.GetCredential(ApiClient.GitHubDotComUri, "Basic");
                    log.Info(CultureInfo.InvariantCulture, "Proxy is authenticated: {0}", (credential != null) && !string.IsNullOrWhiteSpace(credential.UserName));
                }
            }
            catch (Exception exception)
            {
                log.InfoException("Couldn't fetch creds for proxy", exception);
            }
        }

        public virtual void Start()
        {
            StartupLogging.Log(this.appContext, this.environment);
            LogProxyServerConfiguration();
        }
    }
}

