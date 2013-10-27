namespace GitHub.ViewModels
{
    using GitHub.Extensions;
    using GitHub.Extensions.Caliburn;
    using GitHub.Helpers;
    using GitHub.Models;
    using GitHub.UI;
    using NLog;
    using ReactiveUI;
    using ReactiveUI.Xaml;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Input;

    [Export(typeof(IRepositoryViewModelSelection)), Export(typeof(ILoginShell)), Export(typeof(IShellViewModel))]
    public class ShellViewModel : ReactiveConductor<IScreen>, IShellViewModel, ILoginShell, IModalShell, IRepositoryViewModelSelection, IReactiveNotifyPropertyChanged, INotifyPropertyChanged, INotifyPropertyChanging, IEnableLogger
    {
        private bool _isScreenshotsEnabled;
        private readonly Lazy<IDashboardAccountsListViewModel> accountsList;
        private readonly IAppContext appContext;
        private readonly IBrowser browser;
        private IRepositoryViewModel currentRepositoryViewModel;
        private readonly Lazy<IDashboardViewModel> dashboard;
        private readonly Lazy<GitExtractionVisualHelper> gitExtractionVisualHelper;
        private readonly Lazy<IGitShellLauncher> gitShellLauncher;
        private readonly Lazy<LicenseViewModel> licenseViewModel;
        private static readonly Logger log = NLog.LogManager.GetCurrentClassLogger();
        private readonly Lazy<LoginViewModel> loginViewModel;
        private readonly ObservableAsPropertyHelper<bool> mainContentEnabled;
        private readonly IMessageBus messageBus;
        private object modalActiveItem;
        private IModalScreen modalActiveItemViewModel;
        private readonly ObservableAsPropertyHelper<bool> modalItemShowing;
        private bool modalViewCanCancel;
        private readonly Lazy<IOptionsViewModel> optionsViewModel;
        private readonly IPresentationLocator presentationLocator;
        private IModalScreen previous;
        private readonly IProgressViewModel progressViewModel;
        private readonly Func<IScanViewModel> scanViewModel;
        private readonly IApplicationShellInitializer shellInitializer;
        private readonly Lazy<IWelcomeWizardState> welcomeWizardState;
        private readonly Lazy<WelcomeWizardViewModel> welcomeWizardViewModel;
        private readonly IWindows windows;

        [ImportingConstructor]
        public ShellViewModel(IAppContext appContext, Lazy<IDashboardViewModel> dashboard)
        {
            Action<bool> onNext = null;
            Func<UserError, IObservable<RecoveryOptionResult>> errorHandler = null;
            Func<KeyEventArgs, bool> predicate = null;
            Action<object> action2 = null;
            Ensure.ArgumentNotNull(appContext, "appContext");
            Ensure.ArgumentNotNull(dashboard, "dashboard");
            this.appContext = appContext;
            this.dashboard = dashboard;
            this.browser = appContext.Get<IBrowser>();
            this.windows = appContext.Get<IWindows>();
            this.Cache = appContext.Get<ISharedCache>();
            appContext.Events.Subscribe(this);
            this.messageBus = appContext.Get<IMessageBus>();
            this.presentationLocator = appContext.Get<IPresentationLocator>();
            this.Settings = appContext.Get<IUserSettingsModel>();
            this.loginViewModel = appContext.GetLazy<LoginViewModel>();
            this.optionsViewModel = appContext.GetLazy<IOptionsViewModel>();
            this.welcomeWizardViewModel = appContext.GetLazy<WelcomeWizardViewModel>();
            this.welcomeWizardState = appContext.GetLazy<IWelcomeWizardState>();
            this.licenseViewModel = appContext.GetLazy<LicenseViewModel>();
            this.scanViewModel = new Func<IScanViewModel>(AppContextExtensions.Get<IScanViewModel>);
            this.gitShellLauncher = appContext.GetLazy<IGitShellLauncher>();
            this.Dialog = new DialogViewModel();
            this.PasswordDialog = new PasswordDialogViewModel(appContext);
            this.TwoFactorDialog = appContext.Get<ITwoFactorDialogViewModel>();
            this.progressViewModel = appContext.Get<IProgressViewModel>();
            this.gitExtractionVisualHelper = appContext.GetLazy<GitExtractionVisualHelper>();
            this.shellInitializer = appContext.Get<IApplicationShellInitializer>();
            this.accountsList = appContext.GetLazy<IDashboardAccountsListViewModel>();
            base.DisplayName = "GitHub";
            this.modalItemShowing = this.WhenAny<ShellViewModel, bool, object>(x => x.ModalActiveItem, x => (x.Value != null)).ToProperty<ShellViewModel, bool>(this, x => x.ModalItemShowing, false, null, false);
            if (onNext == null)
            {
                onNext = isShowing => this.welcomeWizardState.Value.IsWelcomeWizardShowing = isShowing;
            }
            this.WhenAny<ShellViewModel, bool, IModalScreen>(x => x.ModalActiveItemViewModel, x => (x.Value is IWelcomeWizardViewModel)).Subscribe<bool>(onNext);
            this.mainContentEnabled = this.WhenAny<ShellViewModel, bool, bool, bool, bool, bool>(x => x.Dialog.IsShowing, x => x.ModalItemShowing, x => x.ProgressDialog.IsShowing, x => x.PasswordDialog.IsShowing, delegate (IObservedChange<ShellViewModel, bool> a, IObservedChange<ShellViewModel, bool> b, IObservedChange<ShellViewModel, bool> c, IObservedChange<ShellViewModel, bool> d) {
                if ((!a.Value && !b.Value) && !c.Value)
                {
                    return !d.Value;
                }
                return false;
            }).ToProperty<ShellViewModel, bool>(this, x => x.MainContentEnabled, false, null, false);
            if (errorHandler == null)
            {
                errorHandler = delegate (UserError ex) {
                    Func<IObservable<RecoveryOptionResult>> function = null;
                    Func<IObservable<RecoveryOptionResult>> func2 = null;
                    PasswordRequiredUserError passwordRequiredUserError = ex as PasswordRequiredUserError;
                    if (passwordRequiredUserError != null)
                    {
                        if (function == null)
                        {
                            function = () => this.PasswordDialog.Show(passwordRequiredUserError);
                        }
                        return Observable.Start<IObservable<RecoveryOptionResult>>(function, RxApp.DeferredScheduler).Merge<RecoveryOptionResult>();
                    }
                    TwoFactorRequiredUserError twoFactorRequired = ex as TwoFactorRequiredUserError;
                    if (twoFactorRequired == null)
                    {
                        return Observable.Start<IObservable<RecoveryOptionResult>>(() => this.Dialog.Show(ex).LogErrors<RecoveryOptionResult>(null), RxApp.DeferredScheduler).Merge<RecoveryOptionResult>();
                    }
                    if (func2 == null)
                    {
                        func2 = () => this.TwoFactorDialog.Show(twoFactorRequired);
                    }
                    return Observable.Start<IObservable<RecoveryOptionResult>>(func2, RxApp.DeferredScheduler).Merge<RecoveryOptionResult>();
                };
            }
            UserError.RegisterHandler(errorHandler);
            if (predicate == null)
            {
                predicate = x => base.IsActive;
            }
            this.messageBus.Listen<KeyEventArgs>(null).Where<KeyEventArgs>(predicate).Subscribe<KeyEventArgs>(new Action<KeyEventArgs>(this.ProcessKeyboardShortcuts));
            this.ShowSoftwareUpdate = new ReactiveCommand(null, null, true);
            if (action2 == null)
            {
                action2 = _ => this.UpdateSoftware(false);
            }
            this.ShowSoftwareUpdate.Subscribe<object>(action2);
        }

        protected virtual void ActivateRepository(IRepositoryModel repository)
        {
            IRepositoryViewModel item = this.CreateRepository(repository);
            this.CurrentRepositoryViewModel = item;
            this.ActivateItem(item);
        }

        public void CancelModalView()
        {
            if (this.ModalActiveItem != null)
            {
                ((IModalScreen) this.presentationLocator.LocateModelForView(this.ModalActiveItem)).Cancel();
            }
        }

        private static void CaptureScreenshot(bool addBackdrop)
        {
            string str = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "GitHub for Windows");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string str3 = Path.Combine(path, str);
            string str4 = str3 + ".png";
            string str5 = str3 + "@2x.png";
            Window mainWindow = Application.Current.MainWindow;
            ScreenshotHelper.CaptureAndSaveScreenshot(mainWindow, 1, addBackdrop, str4);
            ScreenshotHelper.CaptureAndSaveScreenshot(mainWindow, 2, addBackdrop, str5);
            Process.Start(str4);
        }

        protected virtual IRepositoryViewModel CreateRepository(IRepositoryModel repositoryModel)
        {
            return new RepositoryViewModel(repositoryModel, this.appContext);
        }

        public IObservable<Unit> EnsureGitIsExtracted()
        {
            return this.gitExtractionVisualHelper.Value.EnsureExtracted();
        }

        public void HideModalView()
        {
            this.ModalActiveItemViewModel = null;
            this.ModalActiveItem = null;
        }

        private bool IsScreenshotsEnabled()
        {
            if (!this._isScreenshotsEnabled)
            {
                IDashboardAccountsListViewModel model = this.accountsList.Value;
                if (((model == null) || (model.GitHubHost == null)) || (model.GitHubHost.Model == null))
                {
                    return false;
                }
                IRepositoryHost host = model.GitHubHost.Model;
                this._isScreenshotsEnabled = (host.IsLoggedIn && (host.User != null)) && host.User.IsGitHubStaff;
            }
            return this._isScreenshotsEnabled;
        }

        protected override void OnInitialize()
        {
            this.shellInitializer.SetupPostLoginHandler(new Func<IObservable<Unit>>(this.EnsureGitIsExtracted));
            this.ShowDashboard();
            this.EnsureGitIsExtracted();
        }

        private void ProcessKeyboardShortcuts(KeyEventArgs e)
        {
            if ((this.ModalItemShowing && !KeyboardSelectionHelper.IsTextInputFocused()) && e.IsNavigateBack())
            {
                this.CancelModalView();
                e.Handled = true;
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Oem3:
                    case Key.Oem5:
                    case Key.Oem8:
                        if (!KeyboardSelectionHelper.ShortcutsEnabled())
                        {
                            break;
                        }
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            this.appContext.FeaturePreview.ToggleStaffMode();
                            e.Handled = true;
                            return;
                        }
                        if (!KeyboardSelectionHelper.IsTextInputFocused())
                        {
                            string workingDirectory = null;
                            IRepositoryViewModel currentRepositoryViewModel = this.CurrentRepositoryViewModel;
                            if ((currentRepositoryViewModel != null) && currentRepositoryViewModel.IsActive)
                            {
                                workingDirectory = this.currentRepositoryViewModel.Model.LocalWorkingDirectory;
                            }
                            this.gitShellLauncher.Value.StartDefaultShell(workingDirectory);
                            e.Handled = true;
                        }
                        return;

                    case Key.F2:
                        if (!this.IsScreenshotsEnabled())
                        {
                            break;
                        }
                        ResetWindowSize();
                        return;

                    case Key.F3:
                        if (this.IsScreenshotsEnabled())
                        {
                            CaptureScreenshot(!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
                        }
                        break;

                    case Key.D:
                        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                        {
                            break;
                        }
                        this.gitShellLauncher.Value.StartDefaultShell(null);
                        return;

                    default:
                        return;
                }
            }
        }

        private static void ResetWindowSize()
        {
            Application.Current.MainWindow.Width = 1366.0;
            Application.Current.MainWindow.Height = 768.0;
        }

        public void ShowAbout()
        {
            this.ShowModalView(new AboutViewModel(this.appContext));
        }

        public void ShowCurrentUser()
        {
            this.browser.OpenUrl("https://github.com/dashboard");
        }

        public void ShowDashboard()
        {
            this.ActivateItem(this.dashboard.Value);
        }

        public void ShowLicenses()
        {
            this.ShowModalView(this.licenseViewModel.Value);
        }

        public void ShowLoginView()
        {
            IUserSettingsModel settings = this.Settings;
            if (settings == null)
            {
                log.Error("Impossible! User settings are null");
            }
            else
            {
                IObservable<IUserSettingsModel> loaded = settings.Loaded;
                if (loaded == null)
                {
                    log.Error("Settings returns a null Loaded observable");
                }
                else
                {
                    loaded.ObserveOn<IUserSettingsModel>(RxApp.DeferredScheduler).Subscribe<IUserSettingsModel>(delegate (IUserSettingsModel x) {
                        if (x == null)
                        {
                            log.Error("The User settings returned by the Loaded Observable's OnNext method is null. This should not be possible");
                        }
                        this.ShowModalView(((x == null) || !x.HasRunWelcomeWizard) ? ((IModalScreen) this.welcomeWizardViewModel.Value) : ((IModalScreen) this.loginViewModel.Value));
                    });
                }
            }
        }

        public void ShowLogoutView()
        {
            this.ShowModalView(this.AppContext.Get<LogoutViewModel>());
        }

        public void ShowModalView(IModalScreen viewModel)
        {
            UIElement view = this.presentationLocator.LocateViewForModel(viewModel);
            this.presentationLocator.Bind(viewModel, view);
            viewModel.Parent = this;
            this.ModalViewCanCancel = viewModel.ShowBackButton;
            viewModel.Activate();
            this.previous = this.ModalActiveItemViewModel;
            this.ModalActiveItemViewModel = viewModel;
            this.ModalActiveItem = view;
        }

        public void ShowOptions()
        {
            this.ShowModalView(this.optionsViewModel.Value);
        }

        public void ShowPrevious()
        {
            this.ShowModalView(this.previous);
        }

        public virtual void ShowRepository(IRepositoryModel repo)
        {
            Action<RecoveryOptionResult> onNext = null;
            if (!this.appContext.OperatingSystem.DirectoryExists(repo.LocalDotGitPath))
            {
                repo.IsLostOnDisk = true;
                if (onNext == null)
                {
                    onNext = delegate (RecoveryOptionResult _) {
                        string str = this.windows.BrowseForFolder(this.appContext.OperatingSystem.GetParentDirectory(repo.LocalWorkingDirectory), null);
                        if (!string.IsNullOrEmpty(str))
                        {
                            this.dashboard.Value.AddReposFromPaths(new string[] { str });
                        }
                    };
                }
                StandardUserErrors.ShowUserThatRepoWasNotFoundOnDisk(repo, this.dashboard.Value).Where<RecoveryOptionResult>(((Func<RecoveryOptionResult, bool>) (x => (x == RecoveryOptionResult.RetryOperation)))).Subscribe<RecoveryOptionResult>(onNext, (Action<Exception>) (e => log.ErrorException("Error while showing user repo not found message", e)));
            }
            else
            {
                repo.IsLostOnDisk = false;
                Exception exception = repo.CheckForCorruption();
                if (exception != null)
                {
                    exception.ShowUserErrorThatRequiresOpeningShellToDebug(ErrorType.RepoCorrupted, repo);
                }
                else
                {
                    try
                    {
                        repo.FixUpBadProxyConfig();
                    }
                    catch (Exception exception2)
                    {
                        log.ErrorException("Could not fixup bad proxy setting.", exception2);
                    }
                    this.ActivateRepository(repo);
                }
            }
        }

        public void ShowScanForRepos(IOptionsViewModel options)
        {
            IScanViewModel viewModel = this.scanViewModel();
            viewModel.Scan(options.StorageDirectory);
            this.ShowModalView(viewModel);
        }

        public void UpdateSoftware(bool forceCheck = false)
        {
            if (forceCheck)
            {
                this.SoftwareUpdate.ForceCheckForUpdates();
            }
            this.ShowModalView(this.SoftwareUpdate);
        }

        public IAppContext AppContext
        {
            get
            {
                return this.appContext;
            }
        }

        public ISharedCache Cache { get; private set; }

        public IRepositoryViewModel CurrentRepositoryViewModel
        {
            get
            {
                return this.currentRepositoryViewModel;
            }
            set
            {
                this.RaiseAndSetIfChanged<ShellViewModel, IRepositoryViewModel>(ref this.currentRepositoryViewModel, value, "CurrentRepositoryViewModel");
            }
        }

        public DialogViewModel Dialog { get; set; }

        public bool MainContentEnabled
        {
            get
            {
                return this.mainContentEnabled.Value;
            }
        }

        public object ModalActiveItem
        {
            get
            {
                return this.modalActiveItem;
            }
            set
            {
                this.RaiseAndSetIfChanged<ShellViewModel, object>(ref this.modalActiveItem, value, "ModalActiveItem");
            }
        }

        public IModalScreen ModalActiveItemViewModel
        {
            get
            {
                return this.modalActiveItemViewModel;
            }
            set
            {
                this.RaiseAndSetIfChanged<ShellViewModel, IModalScreen>(ref this.modalActiveItemViewModel, value, "ModalActiveItemViewModel");
            }
        }

        public bool ModalItemShowing
        {
            get
            {
                return this.modalItemShowing.Value;
            }
        }

        public bool ModalViewCanCancel
        {
            get
            {
                return this.modalViewCanCancel;
            }
            set
            {
                this.RaiseAndSetIfChanged<ShellViewModel, bool>(ref this.modalViewCanCancel, value, "ModalViewCanCancel");
            }
        }

        public PasswordDialogViewModel PasswordDialog { get; set; }

        public IProgressViewModel ProgressDialog
        {
            get
            {
                return this.progressViewModel;
            }
        }

        protected IUserSettingsModel Settings { get; private set; }

        public IReactiveCommand ShowSoftwareUpdate { get; set; }

        [Import]
        public ISoftwareUpdateViewModel SoftwareUpdate { get; set; }

        public ITwoFactorDialogViewModel TwoFactorDialog { get; private set; }
    }
}

