namespace Mes.Framework
{
    using System;
    using System.Runtime.CompilerServices;

    public static class DisposableExtensions
    {
        public static void SafeDispose(this IDisposable disposable)
        {
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}

