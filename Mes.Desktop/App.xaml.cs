
using Mes.AppStartup;
using Mes.Framework;
using Mes.Native;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Mes
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private readonly AppBootstrapper bootstrapper;
        private static readonly int initialThreadId = Thread.CurrentThread.ManagedThreadId;
        private static readonly Logger log = NLog.LogManager.GetCurrentClassLogger();
        public const string Name = "Mes";

        static App()
        {
            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = typeof(App).Assembly;
            }
        }

        public App()
        {
            StartupSequence.InitializeApplication();
            this.bootstrapper = new AppBootstrapper();
            this.bootstrapper.InitializeLogger();
            this.bootstrapper.StartApp();
        }

        //[GeneratedCode("PresentationBuildTasks", "4.0.0.0"), STAThread, DebuggerNonUserCode]
        //public static void Main()
        //{
        //    App app = new App();
        //    app.InitializeComponent();
        //    app.Run();
        //}


        internal static BitmapImage CreateBitmapImage(string packUrl)
        {
            BitmapImage image = new BitmapImage(new Uri(packUrl));
            image.Freeze();
            return image;
        }

   
        public static bool IsCurrentContextOnUI()
        {
            return (Thread.CurrentThread.ManagedThreadId == initialThreadId);
        }

        public static bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //ITrackedRepositories repositories = IoC.Get<ITrackedRepositories>();
            //if (repositories != null)
            //{
            //    try
            //    {
            //        repositories.SaveToCache().Subscribe<Unit>(delegate(Unit _)
            //        {
            //        }, (Action<Exception>)(ex => log.WarnException("Failed to save tracked repositories.", ex)));
            //    }
            //    catch (Exception exception)
            //    {
            //        NLog.LogManager.GetCurrentClassLogger().ErrorException("Failed to save repos to cache on exit.", exception);
            //    }
            //}
            this.bootstrapper.SafeDispose();
            UnsafeNativeMethods.TerminateProcess(SafeNativeMethods.GetCurrentProcess(), 0);
        }
    }
}
