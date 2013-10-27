namespace Mes.AppStartup
{
    using Akavache;
using Caliburn.Micro;
using GitHub;
using GitHub.AppStartup;
using GitHub.IO;
using GitHub.PortableGit.Helpers;
using GitHub.UI;
using GitHub.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

    public sealed class AppBootstrapper : Bootstrapper<IShellViewModel>, IDisposable
    {
        internal static Lazy<IAppContext> AppContext = new Lazy<IAppContext>(new Func<IAppContext>(AppBootstrapper.CreateAppContext));
        private IAppLogManager appLogManager;
        private CompositionContainer container;
        private static readonly IEnvironment environment = new GitHubEnvironment(operatingSystemInfo, () => "GitHub");
        private static readonly bool isDebug;
        internal static readonly IOperatingSystem OperatingSystemBridge = new GitHub.IO.OperatingSystemBridge(environment);
        private static readonly IOperatingSystemInfo operatingSystemInfo = new OperatingSystemInfo();
        private static readonly IProgram program = new Program();
        private IStartupManager startupManager;

        public AppBootstrapper() : base(true)
        {
        }

        [CompilerGenerated]
        private static string <.cctor>b__2()
        {
            return "GitHub";
        }

        protected override void BuildUp(object instance)
        {
            this.container.SatisfyImportsOnce(instance);
        }

        protected override void Configure()
        {
            AggregateCatalog catalog = new AggregateCatalog(new ComposablePartCatalog[] { new AssemblyCatalog(typeof(App).Assembly), new AssemblyCatalog(typeof(CoreUtility).Assembly), new AssemblyCatalog(typeof(IWindows).Assembly), new AssemblyCatalog(typeof(PortableGitManager).Assembly) });
            this.container = new CompositionContainer(catalog, new ExportProvider[0]);
            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue<IEnvironment>(environment);
            batch.AddExportedValue<IOperatingSystemInfo>(operatingSystemInfo);
            batch.AddExportedValue<IOperatingSystem>(OperatingSystemBridge);
            batch.AddExportedValue<IProgram>(program);
            batch.AddExportedValue<IAppLogManager>(this.appLogManager);
            batch.AddExportedValue<CompositionContainer>(this.container);
            Bindings.RegisterBindings(batch);
            this.container.Compose(batch);
            base.Application.Activated += (s, e) => this.container.GetExportedValue<IEventAggregator>().Publish(new ApplicationActivatedEvent());
            base.Application.Deactivated += (s1, e1) => this.container.GetExportedValue<IEventAggregator>().Publish(new ApplicationDeActivatedEvent());
        }

        private static IAppContext CreateAppContext()
        {
            try
            {
                return IoC.Get<IAppContext>();
            }
            catch (Exception)
            {
                return new GitHub.ViewModels.AppContext(new AppServiceProvider());
            }
        }

        public void Dispose()
        {
            CompositionContainer container = this.container;
            if (container != null)
            {
                container.Dispose();
            }
            IStartupManager startupManager = this.startupManager;
            if (startupManager != null)
            {
                startupManager.Dispose();
            }
            IAppLogManager appLogManager = this.appLogManager;
            if (appLogManager != null)
            {
                appLogManager.FlushLogBeforeExit();
                appLogManager.Dispose();
            }
        }

        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return this.container.GetExportedValues<object>(AttributedModelServices.GetContractName(serviceType));
        }

        protected override object GetInstance(Type serviceType, string key)
        {
            string contractName = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
            object obj2 = this.container.GetExportedValues<object>(contractName).FirstOrDefault<object>();
            if (obj2 == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Could not locate any instances of contract {0}.", new object[] { contractName }));
            }
            return obj2;
        }

        public void InitializeLogger()
        {
            this.appLogManager = new AppLogManager(environment, MessageBus.Current, isDebug);
            this.appLogManager.HaystackTarget = new HaystackTarget(this.appLogManager, program, environment);
            this.appLogManager.Initialize();
        }

        public void StartApp()
        {
            base.StartRuntime();
            BlobCache.ServiceProvider = this.appLogManager.ServiceProvider = IoC.Get<IServiceProvider>();
            this.startupManager = IoC.Get<IStartupManager>();
            Ensure.ArgumentNotNull(IoC.Get<IAppContext>(), "ac");
            StartupSequence.Start(this.startupManager);
        }

        protected override void StartRuntime()
        {
        }
    }
}

