namespace Mes.Framework
{
    using Caliburn.Micro;
    using GitHub.ViewModels;
    using NLog;
    using ReactiveUI;
    using System;
    using System.ComponentModel;
    using System.Reactive.Concurrency;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class ReactiveScreen : ReactiveViewAware, IScreen, IHaveDisplayName, IActivate, IDeactivate, IGuardClose, IClose, INotifyPropertyChangedEx, INotifyPropertyChanged, IChild
    {
        private string displayName;
        private bool isActive;
        private bool isInitialized;
        private static readonly Logger log = NLog.LogManager.GetCurrentClassLogger();
        private object parent;

        public event EventHandler<ActivationEventArgs> Activated;

        public event EventHandler<DeactivationEventArgs> AttemptingDeactivation;

        public event EventHandler<DeactivationEventArgs> Deactivated;

        public ReactiveScreen() : this(null)
        {
        }

        public ReactiveScreen(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            this.Activated = delegate (object param0, ActivationEventArgs param1) {
            };
            this.AttemptingDeactivation = delegate (object param0, DeactivationEventArgs param1) {
            };
            this.Deactivated = delegate (object param0, DeactivationEventArgs param1) {
            };
            this.DisplayName = base.GetType().FullName;
        }

        void IActivate.Activate()
        {
            if (!this.IsActive)
            {
                bool flag = false;
                if (!this.IsInitialized)
                {
                    this.IsInitialized = flag = true;
                    this.OnInitialize();
                }
                this.IsActive = true;
                log.Info<ReactiveScreen>("Activating {0}.", this);
                this.OnActivate();
                ActivationEventArgs e = new ActivationEventArgs {
                    WasInitialized = flag
                };
                this.Activated(this, e);
            }
        }

        void IDeactivate.Deactivate(bool close)
        {
            if (this.IsActive || this.IsInitialized)
            {
                DeactivationEventArgs e = new DeactivationEventArgs {
                    WasClosed = close
                };
                this.AttemptingDeactivation(this, e);
                this.IsActive = false;
                log.Info<ReactiveScreen>("Deactivating {0}.", this);
                this.OnDeactivate(close);
                DeactivationEventArgs args2 = new DeactivationEventArgs {
                    WasClosed = close
                };
                this.Deactivated(this, args2);
                if (close)
                {
                    base.Views.Clear();
                    log.Info<ReactiveScreen>("Closed {0}.", this);
                }
            }
        }

        public virtual void CanClose(Action<bool> callback)
        {
            callback(true);
        }

        private System.Action GetViewCloseAction(bool? dialogResult)
        {
            IConductor conductor = this.Parent as IConductor;
            if (conductor != null)
            {
                return delegate {
                    conductor.CloseItem(this);
                };
            }
            foreach (object obj2 in base.Views.Values)
            {
                Type type = obj2.GetType();
                object localContextualView = obj2;
                MethodInfo closeMethod = type.GetMethod("Close");
                if (closeMethod != null)
                {
                    return delegate {
                        if (dialogResult.HasValue)
                        {
                            PropertyInfo property = localContextualView.GetType().GetProperty("DialogResult");
                            if (property != null)
                            {
                                property.SetValue(localContextualView, dialogResult, null);
                            }
                        }
                        closeMethod.Invoke(localContextualView, null);
                    };
                }
                PropertyInfo isOpenProperty = type.GetProperty("IsOpen");
                if (isOpenProperty != null)
                {
                    return delegate {
                        isOpenProperty.SetValue(localContextualView, false, null);
                    };
                }
            }
            return delegate {
                NotSupportedException exception = new NotSupportedException("TryClose requires a parent IConductor or a view with a Close method or IsOpen property.");
                log.ErrorException("Unhandeld exception.", exception);
                throw exception;
            };
        }

        public void NotifyOfPropertyChange(string propertyName)
        {
            base.raisePropertyChanged(propertyName);
        }

        protected virtual void OnActivate()
        {
        }

        protected virtual void OnDeactivate(bool close)
        {
        }

        protected virtual void OnInitialize()
        {
        }

        public void Refresh()
        {
            base.raisePropertyChanged(string.Empty);
        }

        public void TryClose()
        {
            RxApp.DeferredScheduler.Schedule((System.Action) (() => this.GetViewCloseAction(null)()));
        }

        public virtual void TryClose(bool? dialogResult)
        {
            RxApp.DeferredScheduler.Schedule((System.Action) (() => this.GetViewCloseAction(dialogResult)()));
        }

        public string DisplayName
        {
            get
            {
                return this.displayName;
            }
            set
            {
                this.RaiseAndSetIfChanged<ReactiveScreen, string>(ref this.displayName, value, "DisplayName");
            }
        }

        public bool IsActive
        {
            get
            {
                return this.isActive;
            }
            private set
            {
                this.RaiseAndSetIfChanged<ReactiveScreen, bool>(ref this.isActive, value, "IsActive");
            }
        }

        public bool IsInitialized
        {
            get
            {
                return this.isInitialized;
            }
            private set
            {
                this.RaiseAndSetIfChanged<ReactiveScreen, bool>(ref this.isInitialized, value, "IsInitialized");
            }
        }

        public bool IsNotifying { get; set; }

        public virtual object Parent
        {
            get
            {
                return this.parent;
            }
            set
            {
                this.RaiseAndSetIfChanged<ReactiveScreen, object>(ref this.parent, value, "Parent");
            }
        }

        public IShellViewModel ParentShell
        {
            get
            {
                return (this.Parent as IShellViewModel);
            }
        }
    }
}

