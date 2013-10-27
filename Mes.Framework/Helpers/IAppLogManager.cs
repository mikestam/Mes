namespace GitHub.Helpers
{
    using NLog.Targets;
    using System;

    public interface IAppLogManager : IHaystackContext, IDisposable
    {
        void FlushLogBeforeExit();
        void Initialize();
        void SpillTheLogToDesktop();

        GitHub.Helpers.HaystackTarget HaystackTarget { get; set; }

        IServiceProvider ServiceProvider { get; set; }

        TargetWithLayout Target { get; }
    }
}

