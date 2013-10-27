namespace Mes.Framework
{
    using Caliburn.Micro;
    using GitHub.Extensions.Caliburn;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection;

    public class ReactiveConductor<T> : ReactiveConductorBaseWithActiveItem<T> where T: class
    {
        public override void ActivateItem(T item)
        {
            if ((item != null) && item.Equals(base.ActiveItem))
            {
                if (base.IsActive)
                {
                    ScreenExtensions.TryActivate(item);
                    this.OnActivationProcessed(item, true);
                }
            }
            else
            {
                base.CloseStrategy.Execute(new T[] { base.ActiveItem }, delegate (bool canClose, IEnumerable<T> items) {
                    if (canClose)
                    {
                        ((ReactiveConductor<T>) this).ChangeActiveItem(item, true);
                    }
                    else
                    {
                        ((ReactiveConductor<T>) this).OnActivationProcessed(item, false);
                    }
                });
            }
        }

        public override void CanClose(Action<bool> callback)
        {
            base.CloseStrategy.Execute(new T[] { base.ActiveItem }, (canClose, items) => callback(canClose));
        }

        public override void DeactivateItem(T item, bool close)
        {
            if ((item != null) && item.Equals(base.ActiveItem))
            {
                base.CloseStrategy.Execute(new T[] { base.ActiveItem }, delegate (bool canClose, IEnumerable<T> items) {
                    if (canClose)
                    {
                        ((ReactiveConductor<T>) this).ChangeActiveItem(default(T), close);
                    }
                });
            }
        }

        public override IEnumerable<T> GetChildren()
        {
            return new T[] { base.ActiveItem };
        }

        protected override void OnActivate()
        {
            ScreenExtensions.TryActivate(base.ActiveItem);
        }

        protected override void OnDeactivate(bool close)
        {
            ScreenExtensions.TryDeactivate(base.ActiveItem, close);
        }

        public class Collection
        {
            public class AllActive : ConductorBase<T>
            {
                private readonly BindableCollection<T> items;
                private readonly bool openPublicItems;

                public AllActive()
                {
                    NotifyCollectionChangedEventHandler handler = null;
                    this.items = new BindableCollection<T>();
                    if (handler == null)
                    {
                        handler = delegate (object s, NotifyCollectionChangedEventArgs e) {
                            Action<IChild> action = null;
                            Action<IChild> action2 = null;
                            switch (e.Action)
                            {
                                case NotifyCollectionChangedAction.Add:
                                case NotifyCollectionChangedAction.Replace:
                                    if (action == null)
                                    {
                                        action = x => x.Parent = this;
                                    }
                                    e.NewItems.OfType<IChild>().Apply<IChild>(action);
                                    return;

                                case NotifyCollectionChangedAction.Remove:
                                case NotifyCollectionChangedAction.Move:
                                    break;

                                case NotifyCollectionChangedAction.Reset:
                                    if (action2 == null)
                                    {
                                        action2 = x => x.Parent = this;
                                    }
                                    base.items.OfType<IChild>().Apply<IChild>(action2);
                                    break;

                                default:
                                    return;
                            }
                        };
                    }
                    this.items.CollectionChanged += handler;
                }

                public AllActive(bool openPublicItems) : this()
                {
                    this.openPublicItems = openPublicItems;
                }

                public override void ActivateItem(T item)
                {
                    if (item != null)
                    {
                        item = this.EnsureItem(item);
                        if (base.IsActive)
                        {
                            ScreenExtensions.TryActivate(item);
                        }
                        this.OnActivationProcessed(item, true);
                    }
                }

                public override void CanClose(Action<bool> callback)
                {
                    base.CloseStrategy.Execute(this.items, delegate (bool canClose, IEnumerable<T> closable) {
                        if (!canClose && closable.Any<T>())
                        {
                            closable.OfType<IDeactivate>().Apply<IDeactivate>(x => x.Deactivate(true));
                            ((ReactiveConductor<T>.Collection.AllActive) this).items.RemoveRange(closable);
                        }
                        callback(canClose);
                    });
                }

                private void CloseItemCore(T item)
                {
                    ScreenExtensions.TryDeactivate(item, true);
                    this.items.Remove(item);
                }

                public override void DeactivateItem(T item, bool close)
                {
                    Action<bool, IEnumerable<T>> callback = null;
                    if (item != null)
                    {
                        if (close)
                        {
                            if (callback == null)
                            {
                                callback = delegate (bool canClose, IEnumerable<T> closable) {
                                    if (canClose)
                                    {
                                        ((ReactiveConductor<T>.Collection.AllActive) this).CloseItemCore(item);
                                    }
                                };
                            }
                            base.CloseStrategy.Execute(new T[] { item }, callback);
                        }
                        else
                        {
                            ScreenExtensions.TryDeactivate(item, false);
                        }
                    }
                }

                protected override T EnsureItem(T newItem)
                {
                    int index = this.items.IndexOf(newItem);
                    if (index == -1)
                    {
                        this.items.Add(newItem);
                    }
                    else
                    {
                        newItem = this.items[index];
                    }
                    return base.EnsureItem(newItem);
                }

                public override IEnumerable<T> GetChildren()
                {
                    return this.items;
                }

                protected override void OnActivate()
                {
                    this.items.OfType<IActivate>().Apply<IActivate>(x => x.Activate());
                }

                protected override void OnDeactivate(bool close)
                {
                    this.items.OfType<IDeactivate>().Apply<IDeactivate>(x => x.Deactivate(close));
                    if (close)
                    {
                        this.items.Clear();
                    }
                }

                protected override void OnInitialize()
                {
                    Func<PropertyInfo, object> selector = null;
                    if (this.openPublicItems)
                    {
                        if (selector == null)
                        {
                            selector = x => x.GetValue(this, null);
                        }
                        (from x in base.GetType().GetProperties()
                            where (x.Name != "Parent") && typeof(T).IsAssignableFrom(x.PropertyType)
                            select x).Select<PropertyInfo, object>(selector).Cast<T>().Apply<T>(new Action<T>(this.ActivateItem));
                    }
                }

                public IObservableCollection<T> Items
                {
                    get
                    {
                        return this.items;
                    }
                }
            }

            public class OneActive : ConductorBaseWithActiveItem<T>
            {
                private readonly BindableCollection<T> items;

                public OneActive()
                {
                    NotifyCollectionChangedEventHandler handler = null;
                    this.items = new BindableCollection<T>();
                    if (handler == null)
                    {
                        handler = delegate (object s, NotifyCollectionChangedEventArgs e) {
                            Action<IChild> action = null;
                            Action<IChild> action2 = null;
                            switch (e.Action)
                            {
                                case NotifyCollectionChangedAction.Add:
                                case NotifyCollectionChangedAction.Replace:
                                    if (action == null)
                                    {
                                        action = x => x.Parent = this;
                                    }
                                    e.NewItems.OfType<IChild>().Apply<IChild>(action);
                                    return;

                                case NotifyCollectionChangedAction.Remove:
                                case NotifyCollectionChangedAction.Move:
                                    break;

                                case NotifyCollectionChangedAction.Reset:
                                    if (action2 == null)
                                    {
                                        action2 = x => x.Parent = this;
                                    }
                                    base.items.OfType<IChild>().Apply<IChild>(action2);
                                    break;

                                default:
                                    return;
                            }
                        };
                    }
                    this.items.CollectionChanged += handler;
                }

                public override void ActivateItem(T item)
                {
                    if ((item != null) && item.Equals(base.ActiveItem))
                    {
                        if (base.IsActive)
                        {
                            ScreenExtensions.TryActivate(item);
                            this.OnActivationProcessed(item, true);
                        }
                    }
                    else
                    {
                        this.ChangeActiveItem(item, false);
                    }
                }

                public override void CanClose(Action<bool> callback)
                {
                    base.CloseStrategy.Execute(this.items, delegate (bool canClose, IEnumerable<T> closable) {
                        if (!canClose && closable.Any<T>())
                        {
                            if (closable.Contains<T>(((ReactiveConductor<T>.Collection.OneActive) this).ActiveItem))
                            {
                                List<T> list = ((ReactiveConductor<T>.Collection.OneActive) this).items.ToList<T>();
                                T activeItem = ((ReactiveConductor<T>.Collection.OneActive) this).ActiveItem;
                                do
                                {
                                    T local2 = activeItem;
                                    activeItem = ((ReactiveConductor<T>.Collection.OneActive) this).DetermineNextItemToActivate(list, list.IndexOf(local2));
                                    list.Remove(local2);
                                }
                                while (closable.Contains<T>(activeItem));
                                T item = ((ReactiveConductor<T>.Collection.OneActive) this).ActiveItem;
                                ((ReactiveConductor<T>.Collection.OneActive) this).ChangeActiveItem(activeItem, true);
                                ((ReactiveConductor<T>.Collection.OneActive) this).items.Remove(item);
                                List<T> list2 = closable.ToList<T>();
                                list2.Remove(item);
                                closable = list2;
                            }
                            closable.OfType<IDeactivate>().Apply<IDeactivate>(x => x.Deactivate(true));
                            ((ReactiveConductor<T>.Collection.OneActive) this).items.RemoveRange(closable);
                        }
                        callback(canClose);
                    });
                }

                private void CloseItemCore(T item)
                {
                    if (item.Equals(base.ActiveItem))
                    {
                        int index = this.items.IndexOf(item);
                        T newItem = this.DetermineNextItemToActivate(this.items, index);
                        this.ChangeActiveItem(newItem, true);
                    }
                    else
                    {
                        ScreenExtensions.TryDeactivate(item, true);
                    }
                    this.items.Remove(item);
                }

                public override void DeactivateItem(T item, bool close)
                {
                    Action<bool, IEnumerable<T>> callback = null;
                    if (item != null)
                    {
                        if (!close)
                        {
                            ScreenExtensions.TryDeactivate(item, false);
                        }
                        else
                        {
                            if (callback == null)
                            {
                                callback = delegate (bool canClose, IEnumerable<T> closable) {
                                    if (canClose)
                                    {
                                        ((ReactiveConductor<T>.Collection.OneActive) this).CloseItemCore(item);
                                    }
                                };
                            }
                            base.CloseStrategy.Execute(new T[] { item }, callback);
                        }
                    }
                }

                protected virtual T DetermineNextItemToActivate(IList<T> list, int lastIndex)
                {
                    if ((lastIndex == 0) && (list.Count > 1))
                    {
                        return list[1];
                    }
                    if ((lastIndex > 0) && (lastIndex < list.Count))
                    {
                        return list[lastIndex - 1];
                    }
                    return default(T);
                }

                protected override T EnsureItem(T newItem)
                {
                    if (newItem == null)
                    {
                        newItem = this.DetermineNextItemToActivate(this.items, (base.ActiveItem != null) ? this.items.IndexOf(base.ActiveItem) : 0);
                    }
                    else
                    {
                        int index = this.items.IndexOf(newItem);
                        if (index == -1)
                        {
                            this.items.Add(newItem);
                        }
                        else
                        {
                            newItem = this.items[index];
                        }
                    }
                    return base.EnsureItem(newItem);
                }

                public override IEnumerable<T> GetChildren()
                {
                    return this.items;
                }

                protected override void OnActivate()
                {
                    ScreenExtensions.TryActivate(base.ActiveItem);
                }

                protected override void OnDeactivate(bool close)
                {
                    if (close)
                    {
                        this.items.OfType<IDeactivate>().Apply<IDeactivate>(x => x.Deactivate(true));
                        this.items.Clear();
                    }
                    else
                    {
                        ScreenExtensions.TryDeactivate(base.ActiveItem, false);
                    }
                }

                public IObservableCollection<T> Items
                {
                    get
                    {
                        return this.items;
                    }
                }
            }
        }
    }
}

