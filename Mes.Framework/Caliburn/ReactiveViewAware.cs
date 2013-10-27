namespace Mes.Framework
{
    using Caliburn.Micro;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;

    public class ReactiveViewAware : ReactiveValidatableObject, IViewAware
    {
        private bool cacheViews;
        internal static bool CacheViewsByDefault = true;
        private static readonly DependencyProperty PreviouslyAttachedProperty = DependencyProperty.RegisterAttached("PreviouslyAttached", typeof(bool), typeof(ReactiveViewAware), null);
        protected Dictionary<object, object> Views;

        public event EventHandler<ViewAttachedEventArgs> ViewAttached;

        public ReactiveViewAware() : this(null, CacheViewsByDefault)
        {
        }

        public ReactiveViewAware(bool cacheViews) : this(null, cacheViews)
        {
        }

        public ReactiveViewAware(IServiceProvider serviceProvider) : this(serviceProvider, CacheViewsByDefault)
        {
        }

        public ReactiveViewAware(IServiceProvider serviceProvider, bool cacheViews) : base(serviceProvider)
        {
            this.Views = new Dictionary<object, object>();
            this.ViewAttached = delegate (object param0, ViewAttachedEventArgs param1) {
            };
            this.CacheViews = cacheViews;
        }

        void IViewAware.AttachView(object view, object context)
        {
            RoutedEventHandler handler = null;
            if (this.CacheViews)
            {
                this.Views[context ?? View.DefaultContext] = view;
            }
            object obj2 = View.GetFirstNonGeneratedView(view);
            FrameworkElement element = obj2 as FrameworkElement;
            if ((element != null) && !((bool) element.GetValue(PreviouslyAttachedProperty)))
            {
                element.SetValue(PreviouslyAttachedProperty, true);
                if (handler == null)
                {
                    handler = (s, e) => this.OnViewLoaded(s);
                }
                View.ExecuteOnLoad(element, handler);
            }
            this.OnViewAttached(obj2, context);
            ViewAttachedEventArgs args = new ViewAttachedEventArgs {
                View = obj2,
                Context = context
            };
            this.ViewAttached(this, args);
        }

        public virtual object GetView(object context = null)
        {
            object obj2;
            this.Views.TryGetValue(context ?? View.DefaultContext, out obj2);
            return obj2;
        }

        protected internal virtual void OnViewAttached(object view, object context)
        {
        }

        protected internal virtual void OnViewLoaded(object view)
        {
        }

        protected bool CacheViews
        {
            get
            {
                return this.cacheViews;
            }
            set
            {
                this.cacheViews = value;
                if (!this.cacheViews)
                {
                    this.Views.Clear();
                }
            }
        }
    }
}

