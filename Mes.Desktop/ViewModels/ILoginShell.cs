namespace GitHub.ViewModels
{
    using ReactiveUI;
    using System;
    using System.ComponentModel;

    public interface ILoginShell : IModalShell, IReactiveNotifyPropertyChanged, INotifyPropertyChanged, INotifyPropertyChanging, IEnableLogger
    {
        void ShowLoginView();
        void ShowLogoutView();
    }
}

