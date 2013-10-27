namespace Mes.Framework
{
    using Caliburn.Micro;
    using System;
    using System.ComponentModel;

    public abstract class ReactiveConductorBaseWithActiveItem<T> : ReactiveConductorBase<T>, IConductActiveItem, IConductor, IParent, INotifyPropertyChangedEx, INotifyPropertyChanged, IHaveActiveItem
    {
        private T activeItem;

        protected ReactiveConductorBaseWithActiveItem()
        {
        }

        protected virtual void ChangeActiveItem(T newItem, bool closePrevious)
        {
            ScreenExtensions.TryDeactivate(this.activeItem, closePrevious);
            newItem = this.EnsureItem(newItem);
            if (base.IsActive)
            {
                ScreenExtensions.TryActivate(newItem);
            }
            this.activeItem = newItem;
            base.NotifyOfPropertyChange("ActiveItem");
            this.OnActivationProcessed(this.activeItem, true);
        }

        public T ActiveItem
        {
            get
            {
                return this.activeItem;
            }
            set
            {
                this.ActivateItem(value);
            }
        }

        object IHaveActiveItem.ActiveItem
        {
            get
            {
                return this.ActiveItem;
            }
            set
            {
                this.ActiveItem = (T) value;
            }
        }
    }
}

