namespace GitHub.ViewModels
{
    using GitHub.Extensions;
    using GitHub.Helpers;
    using GitHub.Models;
    using ReactiveUI;
    using ReactiveUI.Xaml;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    [Export(typeof(LogoutViewModel))]
    public class LogoutViewModel : ReactiveModalScreen
    {
        private readonly ObservableAsPropertyHelper<string> logoutMessage;
        private readonly IRepositoryHosts repositoryHosts;

        [ImportingConstructor]
        public LogoutViewModel(IRepositoryHosts repositoryHosts, IDashboardAccountsListViewModel accountsList)
        {
            Func<object, IObservable<Unit>> selector = null;
            Action<Unit> onNext = null;
            Ensure.ArgumentNotNull(repositoryHosts, "repositoryHosts");
            Ensure.ArgumentNotNull(accountsList, "accountsList");
            this.repositoryHosts = repositoryHosts;
            this.logoutMessage = accountsList.WhenAny<IDashboardAccountsListViewModel, string, bool, bool>(x => x.GitHubHost.IsLoggedIn, y => y.EnterpriseHost.IsLoggedIn, delegate (IObservedChange<IDashboardAccountsListViewModel, bool> x, IObservedChange<IDashboardAccountsListViewModel, bool> y) {
                string str;
                if (x.Value && y.Value)
                {
                    str = "GitHub and GitHub Enterprise accounts";
                }
                else if (y.Value)
                {
                    str = "GitHub Enterprise account";
                }
                else
                {
                    str = "GitHub account";
                }
                return string.Format(CultureInfo.InvariantCulture, "Are you sure you want to log out of your {0}? By default your user data is cached so that you don't have to log in again the next time you run the app.", new object[] { str });
            }).ToProperty<LogoutViewModel, string>(this, x => x.LogoutMessage, null, null, false);
            this.LogoutCommand = new ReactiveCommand(null, null, true);
            if (selector == null)
            {
                selector = _ => this.LogOutAllAccounts(accountsList);
            }
            if (onNext == null)
            {
                onNext = x => this.TryClose(true);
            }
            this.LogoutCommand.SelectMany<object, Unit>(selector).Subscribe<Unit>(onNext);
        }

        private static IEnumerable<IRepositoryHost> GetLoggedInHosts(IDashboardAccountsListViewModel accountsList)
        {
            if (accountsList.GitHubHost.IsLoggedIn)
            {
                yield return accountsList.GitHubHost.Model;
            }
            if ((accountsList.EnterpriseHost != null) && accountsList.EnterpriseHost.IsLoggedIn)
            {
                yield return accountsList.EnterpriseHost.Model;
            }
        }

        private IObservable<Unit> LogOutAllAccounts(IDashboardAccountsListViewModel accountsList)
        {
            return GetLoggedInHosts(accountsList).ToObservable<IRepositoryHost>().SelectMany<IRepositoryHost, Unit>(((Func<IRepositoryHost, IObservable<Unit>>) (host => host.LogOut().Finally<Unit>(delegate {
                if (host.IsEnterprise && this.repositoryHosts.EnterpriseHost.Equals(host))
                {
                    this.repositoryHosts.EnterpriseHost = null;
                }
            }))));
        }

        public ReactiveCommand LogoutCommand { get; private set; }

        public string LogoutMessage
        {
            get
            {
                return this.logoutMessage.Value;
            }
        }

    }
}

