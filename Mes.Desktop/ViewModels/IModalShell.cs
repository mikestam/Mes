namespace GitHub.ViewModels
{
    using GitHub.Extensions;
    using ReactiveUI;
    using System;
    using System.ComponentModel;

    public interface IModalShell : IReactiveNotifyPropertyChanged, INotifyPropertyChanged, INotifyPropertyChanging, IEnableLogger
    {
        void CancelModalView();
        void HideModalView();
        void ShowModalView(IModalScreen viewModel);

        object ModalActiveItem { get; set; }

        IModalScreen ModalActiveItemViewModel { get; }

        bool ModalItemShowing { get; }
    }
}

