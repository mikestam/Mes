namespace GitHub.ViewModels
{
    using System;

    public interface IApplicationShellInitializer
    {
        void SetupPostLoginHandler(Func<IObservable<Unit>> gitExtractedEnsurer);
    }
}

