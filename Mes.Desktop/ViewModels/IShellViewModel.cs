namespace GitHub.ViewModels
{
    using Caliburn.Micro;
    using GitHub.Models;
    using ReactiveUI;
    using ReactiveUI.Xaml;
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    public interface IShellViewModel : ILoginShell, IModalShell, IReactiveNotifyPropertyChanged, INotifyPropertyChanged, INotifyPropertyChanging, IEnableLogger
    {
        void ActivateItem(IScreen screen);
        IObservable<Unit> EnsureGitIsExtracted();
        void ShowAbout();
        void ShowCurrentUser();
        void ShowDashboard();
        void ShowLicenses();
        void ShowOptions();
        void ShowPrevious();
        void ShowRepository(IRepositoryModel repo);
        void ShowScanForRepos(IOptionsViewModel options);
        void UpdateSoftware(bool forceCheck = false);

        IScreen ActiveItem { get; set; }

        bool MainContentEnabled { get; }

        IProgressViewModel ProgressDialog { get; }

        IReactiveCommand ShowSoftwareUpdate { get; }
    }
}

