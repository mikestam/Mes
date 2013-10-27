namespace GitHub.Helpers
{
    using System;

    public interface IAppInstanceCommunicator
    {
        bool ShouldExitBecauseInstanceAlreadyRunning(string repoCloneUrl, Action<string> callback);

        bool IsMasterInstance { get; }
    }
}

