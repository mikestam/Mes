namespace GitHub.ViewModels
{
    using Caliburn.Micro;
    using GitHub.Extensions;
    using GitHub.Helpers;
    using GitHub.Models;
    using ReactiveUI;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;

    [Export(typeof(LoginViewModel)), PartCreationPolicy(CreationPolicy.NonShared)]
    public class LoginViewModel : ReactiveModalScreen, ILoginViewModel, IReactiveNotifyPropertyChanged, INotifyPropertyChanging, IEnableLogger, IDataErrorInfo, IModalScreen, IScreen, IHaveDisplayName, IActivate, IDeactivate, IGuardClose, IClose, INotifyPropertyChangedEx, INotifyPropertyChanged
    {
        [ImportingConstructor]
        public LoginViewModel(IAppContext appContext) : base(appContext)
        {
            Action<object> onNext = null;
            Action<AuthenticationResult> action2 = null;
            this.AccountsList = appContext.Get<IDashboardAccountsListViewModel>();
            this.AppContext = appContext;
            this.FontSize = 24.0;
            this.GitConfig = appContext.Get<IGitConfig>();
            this.LoginControlViewModel = appContext.Get<ILoginControlViewModel>();
            this.Settings = appContext.Get<IUserSettingsModel>();
            if (onNext == null)
            {
                onNext = _ => this.Cancel();
            }
            this.LoginControlViewModel.CancelCommand.Subscribe<object>(onNext);
            if (action2 == null)
            {
                action2 = delegate (AuthenticationResult result) {
                    if (result.IsSuccess())
                    {
                        this.OnLogInSucceeded();
                    }
                };
            }
            this.LoginControlViewModel.AuthenticationResults.Subscribe<AuthenticationResult>(action2);
        }

        protected override void OnActivate()
        {
            this.LoginControlViewModel.Reset();
            Observable.Start<IObservable<IRestResponse<User>>>(() => this.AccountsList.GitHubHost.Model.ApiClient.GetUser("Test-Octowin"), RxApp.TaskpoolScheduler);
        }

        protected virtual void OnLogInSucceeded()
        {
            this.TryClose(true);
        }

        protected IDashboardAccountsListViewModel AccountsList { get; private set; }

        protected IAppContext AppContext { get; private set; }

        public double FontSize { get; set; }

        protected IGitConfig GitConfig { get; private set; }

        public ILoginControlViewModel LoginControlViewModel { get; private set; }

        public IUserSettingsModel Settings { get; private set; }
    }
}

