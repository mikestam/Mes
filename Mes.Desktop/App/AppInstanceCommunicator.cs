namespace GitHub.Helpers
{
    using GitHub;
    using GitHub.Threading;
    using NLog;
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    [Export(typeof(IAppInstanceCommunicator))]
    public class AppInstanceCommunicator : IAppInstanceCommunicator
    {
        private readonly ICrossProcessMessageBus crossProcessMessageBus;
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IAppSemaphoreFactory semaphoreFactory;

        [ImportingConstructor]
        public AppInstanceCommunicator(IAppSemaphoreFactory semaphoreFactory, ICrossProcessMessageBus crossProcessMessageBus)
        {
            Ensure.ArgumentNotNull(semaphoreFactory, "semaphoreFactory");
            Ensure.ArgumentNotNull(crossProcessMessageBus, "crossProcessMessageBus");
            this.semaphoreFactory = semaphoreFactory;
            this.crossProcessMessageBus = crossProcessMessageBus;
        }

        private static void GiveMasterAppWindowFocus()
        {
            string fileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            int ourPid = Process.GetCurrentProcess().Id;
            Process process = Process.GetProcessesByName(fileName).FirstOrDefault<Process>(x => x.Id != ourPid);
            if (process != null)
            {
                GitHub.NativeMethods.SetForegroundWindow(process.MainWindowHandle);
            }
        }

        public void Listen(Action<string> messageReceivedCallback)
        {
            Task.Factory.StartNew(x => this.crossProcessMessageBus.Listen((IAppSemaphore) x, messageReceivedCallback), this.semaphoreFactory.Create(), TaskCreationOptions.LongRunning);
        }

        public bool ShouldExitBecauseInstanceAlreadyRunning(string repoCloneUrl, Action<string> repoToCloneCallback)
        {
            using (IAppSemaphore semaphore = this.semaphoreFactory.Create())
            {
                if (semaphore.AlreadyExists)
                {
                    this.log.Info("An instance of GitHub for Windows is already running. Checking to see if we should pass control...");
                    if (string.IsNullOrEmpty(repoCloneUrl))
                    {
                        this.log.Info("Not a call to clone a repo, so just run the app as usual.");
                        return false;
                    }
                    this.log.Info(CultureInfo.InvariantCulture, "Passing on the call to clone the repo '{0}' to the existing instance.", repoCloneUrl);
                    this.crossProcessMessageBus.Send(repoCloneUrl);
                    try
                    {
                        semaphore.Release();
                    }
                    catch (SemaphoreFullException exception)
                    {
                        this.log.ErrorException("ASSERT! Semaphore full for some reason.", exception);
                        return false;
                    }
                    GiveMasterAppWindowFocus();
                    return true;
                }
                this.log.Info("Starting up as master instance of GitHub for Windows");
                this.IsMasterInstance = true;
                this.Listen(repoToCloneCallback);
                return false;
            }
        }

        public bool IsMasterInstance { get; private set; }
    }
}

