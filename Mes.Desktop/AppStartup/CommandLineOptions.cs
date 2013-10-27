namespace Mes.AppStartup
{
    using GitHub.Helpers;
    using System;

    public static class CommandLineOptions
    {
        public static OptionSet GetCommandLineOptions(ICommandHandler handler)
        {
            OptionSet options = new OptionSet().Add("open-shell:", "Open a Git Shell to the working directory. Specifying the working directory is optional.", new Action<string>(handler.OpenShell)).Add("set-up-ssh", "Setup SSH Keys", delegate (string x) {
                handler.SetUpSSHKeys(true);
            }).Add("credentials=", "Credential caching api for use with Git", new Action<string>(handler.HandleHttpsCreds)).Add("install", "Install the url protocol into the registry", delegate (string x) {
                handler.InstallUrlProtocol();
            }).Add("delete-cache", "Clean all locally cached data", delegate (string x) {
                handler.DeleteCache();
            }).Add("uninstall", "Uninstall the url protocol from the registry", delegate (string x) {
                handler.UninstallUrlProtocol();
            }).Add("u=|url=", "Clone the specified GitHub repository", new Action<string>(handler.ParseCloneUrl)).Add("config:", "Set or show configuration values", new Action<string>(handler.Config)).Add("reinstall-shortcuts", "Reinstall the GitHub for Windows and Git Shell shortcuts", delegate (string x) {
                handler.ReinstallShortcuts();
            });
            options.Add("h|?|help", "Display this message", delegate (string x) {
                handler.ShowHelp(options);
            });
            return options;
        }
    }
}

