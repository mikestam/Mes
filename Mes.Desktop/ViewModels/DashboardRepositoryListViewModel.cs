namespace GitHub.ViewModels
{
    using GitHub.Extensions;
    using GitHub.Helpers;
    using GitHub.Models;
    using ReactiveUI;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    [PartCreationPolicy(CreationPolicy.Shared), Export(typeof(IDashboardRepositoryListViewModel))]
    public class DashboardRepositoryListViewModel : ReactiveObject, IDashboardRepositoryListViewModel, IReactiveNotifyPropertyChanged, INotifyPropertyChanged, INotifyPropertyChanging, IEnableLogger
    {
        private readonly IDashboardAccountsListViewModel accountsList;
        private IDashboardAccountTileViewModel currentAccountTile;
        private ReactiveDerivedCollection<IRepositoryModel> filteredRepositories;
        private string filterText;
        private readonly IRepositoryHosts hosts;
        private readonly ObservableAsPropertyHelper<bool> isListFiltered;
        private readonly ObservableAsPropertyHelper<bool> isRepositorySelected;
        private readonly ITrackedRepositories repositories;
        private IRepositoryModel selectedRepository;
        private readonly IObservable<string> throttledFilterText;

        [ImportingConstructor]
        public DashboardRepositoryListViewModel(ITrackedRepositories repositories, IDashboardAccountsListViewModel accountsList, IRepositoryHosts hosts)
        {
            Action<IDashboardAccountTileViewModel> onNext = null;
            Ensure.ArgumentNotNull(repositories, "repositories");
            Ensure.ArgumentNotNull(accountsList, "accountsList");
            Ensure.ArgumentNotNull(hosts, "hosts");
            this.repositories = repositories;
            this.accountsList = accountsList;
            this.hosts = hosts;
            this.throttledFilterText = this.WhenAny<DashboardRepositoryListViewModel, string, string>(x => x.FilterText, x => x.Value).Throttle<string>(TimeSpan.FromMilliseconds(100.0), RxApp.DeferredScheduler);
            if (onNext == null)
            {
                onNext = delegate (IDashboardAccountTileViewModel _) {
                    this.FilterText = null;
                    this.ReFilterRepositories();
                };
            }
            this.WhenAny<DashboardRepositoryListViewModel, IDashboardAccountTileViewModel, IDashboardAccountTileViewModel>(x => x.accountsList.SelectedAccount, x => x.Value).Subscribe<IDashboardAccountTileViewModel>(onNext);
            this.isRepositorySelected = this.WhenAny<DashboardRepositoryListViewModel, bool, IRepositoryModel>(x => x.SelectedRepository, x => (x.Value != null)).ToProperty<DashboardRepositoryListViewModel, bool>(this, x => x.IsRepositorySelected, false, Scheduler.Immediate, false);
            this.isListFiltered = this.ObservableForProperty<DashboardRepositoryListViewModel, string>(x => x.FilterText, false, true).Select<IObservedChange<DashboardRepositoryListViewModel, string>, bool>(((Func<IObservedChange<DashboardRepositoryListViewModel, string>, bool>) (x => !string.IsNullOrEmpty(x.Value)))).ToProperty<DashboardRepositoryListViewModel, bool>(this, x => x.IsListFiltered, false, null, false);
        }

        private void FilterRepositoriesForAccount(IDashboardAccountTileViewModel accountTile)
        {
            Func<IRepositoryModel, bool> filter = null;
            if ((accountTile != null) && (accountTile != this.currentAccountTile))
            {
                if (this.FilteredRepositories != null)
                {
                    this.FilteredRepositories.Dispose();
                }
                if (accountTile.Model.IsLocal)
                {
                    this.FilteredRepositories = this.repositories.LocalRepositories.CreateDerivedCollection<IRepositoryModel, IRepositoryModel, string>(x => x, new Func<IRepositoryModel, bool>(this.FilterRepositoriesUsingFilterText), null, this.throttledFilterText);
                }
                else
                {
                    IRepositoryHost repositoryHost = ((accountTile.Model != null) && (accountTile.Model.Host != null)) ? accountTile.Model.Host : this.hosts.GitHubHost;
                    if (filter == null)
                    {
                        filter = x => this.FilterRepositoriesForAccount(x, accountTile.Model);
                    }
                    this.FilteredRepositories = this.repositories.GetHostedRepositories(repositoryHost).CreateDerivedCollection<IRepositoryModel, IRepositoryModel, string>(x => x, filter, null, this.throttledFilterText);
                }
                this.currentAccountTile = accountTile;
            }
        }

        private bool FilterRepositoriesForAccount(IRepositoryModel repo, IAccount account)
        {
            if (account.IsUser && ((repo.Owner == account.Login) || repo.IsCollaborator))
            {
                if (!string.IsNullOrEmpty(this.FilterText))
                {
                    return this.RepoNameContainsFilter(repo);
                }
                return true;
            }
            return (((!account.IsUser && (repo.Owner == account.Login)) && RepositoryIsAssociatedWithAccount(repo, account)) && this.FilterRepositoriesUsingFilterText(repo));
        }

        private bool FilterRepositoriesUsingFilterText(IRepositoryModel repo)
        {
            if (!string.IsNullOrEmpty(this.FilterText))
            {
                return this.RepoNameContainsFilter(repo);
            }
            return true;
        }

        private IRepositoryModel GetPreviousOrNextRepository()
        {
            if (this.FilteredRepositories.Count <= 1)
            {
                return null;
            }
            int index = this.FilteredRepositories.IndexOf(this.selectedRepository);
            if (index > 0)
            {
                return this.FilteredRepositories[index - 1];
            }
            return this.FilteredRepositories[1];
        }

        public void ReFilterRepositories()
        {
            this.FilterRepositoriesForAccount(this.accountsList.SelectedAccount);
        }

        private bool RepoNameContainsFilter(IRepositoryModel repo)
        {
            return repo.Name.Contains(this.FilterText, StringComparison.OrdinalIgnoreCase);
        }

        private static bool RepositoryIsAssociatedWithAccount(IRepositoryModel repository, IAccount account)
        {
            return (repository.Owner.Equals(account.Login, StringComparison.OrdinalIgnoreCase) || (account.IsUser && repository.IsCollaborator));
        }

        public void StopTracking(IRepositoryModel repository)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            IRepositoryModel previousOrNextRepository = this.GetPreviousOrNextRepository();
            this.SelectedRepository = null;
            if (!repository.IsHosted)
            {
                this.repositories.Remove(repository);
            }
            else
            {
                repository.StopTracking();
            }
            this.SelectedRepository = previousOrNextRepository;
        }

        public ReactiveDerivedCollection<IRepositoryModel> FilteredRepositories
        {
            get
            {
                return this.filteredRepositories;
            }
            private set
            {
                this.RaiseAndSetIfChanged<DashboardRepositoryListViewModel, ReactiveDerivedCollection<IRepositoryModel>>(ref this.filteredRepositories, value, "FilteredRepositories");
            }
        }

        public string FilterText
        {
            get
            {
                return this.filterText;
            }
            set
            {
                this.RaiseAndSetIfChanged<DashboardRepositoryListViewModel, string>(ref this.filterText, value, "FilterText");
            }
        }

        public bool IsListFiltered
        {
            get
            {
                return this.isListFiltered.Value;
            }
        }

        public bool IsRepositorySelected
        {
            get
            {
                return this.isRepositorySelected.Value;
            }
        }

        public IRepositoryModel SelectedRepository
        {
            get
            {
                return this.selectedRepository;
            }
            set
            {
                this.RaiseAndSetIfChanged<DashboardRepositoryListViewModel, IRepositoryModel>(ref this.selectedRepository, value, "SelectedRepository");
            }
        }
    }
}

