namespace GitHub.Helpers
{
    using Akavache;
    using Caliburn.Micro;
    using ReactiveUI;
    using System;
    using System.ComponentModel.Composition;
    using System.Linq;

    [Export(typeof(IServiceProvider))]
    public class AppServiceProvider : IObjectCreator, IServiceProvider
    {
        private static readonly MemoizingMRUCache<Type, bool> isInjectableCache;

        static AppServiceProvider()
        {
            isInjectableCache = new MemoizingMRUCache<Type, bool>(delegate (Type t, object _) {
                if (!t.IsInterface)
                {
                }
                return (CS$<>9__CachedAnonymousMethodDelegate5 != null) || t.GetConstructors().SelectMany<ConstructorInfo, object>(CS$<>9__CachedAnonymousMethodDelegate5).Any<object>();
            }, 50, null);
        }

        public bool CanCreate(Type type)
        {
            lock (isInjectableCache)
            {
                return isInjectableCache.Get(type);
            }
        }

        public object GetService(Type serviceType)
        {
            bool flag;
            lock (isInjectableCache)
            {
                flag = isInjectableCache.Get(serviceType);
            }
            if (!flag)
            {
                return Activator.CreateInstance(serviceType);
            }
            return IoC.GetInstance(serviceType, null);
        }
    }
}

