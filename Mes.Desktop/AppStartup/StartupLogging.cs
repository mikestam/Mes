namespace Mes.AppStartup
{
    using GitHub.Extensions;
    using GitHub.IO;
    using GitHub.Models;
    using GitHub.PortableGit.Helpers;
    using GitHub.ViewModels;
    using NLog;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;

    public static class StartupLogging
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static void EnsureExists(IOperatingSystem os, string path, string label)
        {
            if (os.DirectoryExists(path) || os.FileExists(path))
            {
                Log("{0} Exists: '{1}'", new object[] { label, path });
            }
            else
            {
                LogError("MISSING {0}: '{1}'", new object[] { label, path });
            }
        }

        public static void Log(IAppContext appContext, IEnvironment environment)
        {
            Log("#########################################", new object[0]);
            Log("GitHub for Windows started. VERSION: {0}", new object[] { Assembly.GetExecutingAssembly().GetName().Version });
            Log("Build version: {0}", new object[] { "e4588ba45ef7e4e642674dcfd5b05c92eae732d0" });
            Log("***************************************", new object[0]);
            Log("***                                 ***", new object[0]);
            Log("***                                 ***", new object[0]);
            Log("***        Have a problem?          ***", new object[0]);
            Log("***    Email support@github.com     ***", new object[0]);
            Log("***      and include this file      ***", new object[0]);
            Log("***                                 ***", new object[0]);
            Log("***                                 ***", new object[0]);
            Log("***************************************", new object[0]);
            Log("OS Version: {0}", new object[] { Environment.OSVersion.GetOperatingSystemVersionString() });
            Log("CLR Version: {0}", new object[] { Environment.Version });
            Log("Current culture: {0}", new object[] { CultureInfo.CurrentCulture.Name });
            Log("Terminal Services session: {0}", new object[] { SystemInformation.TerminalServerSession ? "yes" : "no" });
            Log("Location: {0}", new object[] { Assembly.GetExecutingAssembly().Location });
            Log("ActivationUri: {0}", new object[] { appContext.ActivationUri });
            LogDiagnostics(appContext);
            Log("PATH: {0}", new object[] { environment.GetEnvironmentVariable("PATH") });
        }

        private static void Log(string message, params object[] args)
        {
            log.Info(CultureInfo.InvariantCulture, " " + message, args);
        }

        private static void LogDiagnostics(IServiceProvider appContext)
        {
            try
            {
                IPortableGitManager manager = appContext.Get<IPortableGitManager>();
                IOperatingSystem os = appContext.Get<IOperatingSystem>();
                IGitEnvironment environment = appContext.Get<IGitEnvironment>();
                ProcessStartInfo psi = new ProcessStartInfo();
                environment.SetUpEnvironment(psi, null, false);
                string[] source = (from p in psi.EnvironmentVariables["PATH"].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    where !os.DirectoryExists(p)
                    select p).ToArray<string>();
                string portableGitDestinationDirectory = manager.GetPortableGitDestinationDirectory(false);
                Log("=====================================================", new object[0]);
                Log(" DIAGNOSTICS                                        |", new object[0]);
                Log("=====================================================", new object[0]);
                Log("Git Extracted: '{0}:", new object[] { manager.IsExtracted() });
                EnsureExists(os, portableGitDestinationDirectory, "PortableGit Dir");
                EnsureExists(os, manager.GitExecutablePath, "Git Executable");
                source.ForEach<string>((Action<string>) (path => LogError("MISSING PATH!!: '{0}'", new object[] { path })));
                Log("----------------------------------------------------", new object[0]);
            }
            catch (Exception exception)
            {
                log.ErrorException("Whoops, could not log diagnostics", exception);
            }
        }

        private static void LogError(string message, params object[] args)
        {
            log.Error(CultureInfo.InvariantCulture, " " + message, args);
        }
    }
}

