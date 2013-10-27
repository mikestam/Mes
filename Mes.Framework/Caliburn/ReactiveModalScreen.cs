namespace Mes.Framework
{
    using Caliburn.Micro;
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ReactiveModalScreen : ReactiveScreen, IModalScreen, IScreen, IHaveDisplayName, IActivate, IDeactivate, IGuardClose, IClose, INotifyPropertyChangedEx, INotifyPropertyChanged
    {
        public ReactiveModalScreen() : this(null)
        {
        }

        public ReactiveModalScreen(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            this.ShowBackButton = true;
        }

        public virtual void Cancel()
        {
            this.TryClose(false);
        }

        public override void TryClose(bool? dialogResult)
        {
            if (base.ParentShell != null)
            {
                ((IDeactivate) this).Deactivate(true);
                base.ParentShell.HideModalView();
            }
        }

        public bool ShowBackButton { get; set; }
    }
}

