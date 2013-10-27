namespace Mes.AppStartup
{
    using Akavache;
    using Caliburn.Micro;
    using GitHub.Helpers;
    using ReactiveUI;
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;

    public static class Bindings
    {
        public static void RegisterBindings(CompositionBatch batch)
        {
            Ensure.ArgumentNotNull(batch, "batch");
            batch.AddExportedValue<Func<ApiUnauthorizedWebException, IObservable<TwoFactorChallengeResult>>>(new Func<ApiUnauthorizedWebException, IObservable<TwoFactorChallengeResult>>(StandardUserInteraction.RequestTwoFactorAuthenticationCode));
            batch.AddExportedValue<IKeyedOperationQueue>(new KeyedOperationQueue(null));
            batch.AddExportedValue<IEventAggregator>(new EventAggregator());
            batch.AddExportedValue<IWindowManager>(new WindowManager());
            batch.AddExportedValue<Func<bool>>("IsCurrentContextOnUI", new Func<bool>(App.IsCurrentContextOnUI));
            batch.AddExportedValue<IMessageBus>(MessageBus.Current);
        }
    }
}

