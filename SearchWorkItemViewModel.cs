using EnvDTE80;
using LinkToWorkItem.Command;
using LinkToWorkItem.Entities;
using LinkToWorkItem.Extensions;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.MVVM;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using Microsoft.VisualStudio.TeamFoundation.WorkItemTracking;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using OperationCanceledException = System.OperationCanceledException;

namespace LinkToWorkItem
{
    public class SearchWorkItemViewModel : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Fields

        private ITeamFoundationContextManager _teamFoundationContext;
        private IServiceProvider _serviceProvider;
        private string _searchText;
        private Visibility _isLoading;
        private string _errors;
        private Visibility _hasErrors;
        private ObservableCollection<WorkItemDetail> _workItemDetails;
        private AsyncDelegateCommand _saveCommand;
        private RelayCommand _cancelCommand;
        private RelayCommand _cancelSearchCommand;
        private CancellationTokenSource _cancellationToken;
        private AsyncDelegateCommand _searchCommand;
        private string _errorHeader;
        private RelayCommand _openWorkItem;
        private LinkToWorkItemPackage _package;

        #endregion Fields

        #region Properties

        public RelayCommand CancelCommand
        {
            get
            {
                return _cancelCommand = new RelayCommand(OnCancelClicked);
            }
        }

        public RelayCommand CancelSearchCommand
        {
            get
            {
                return _cancelSearchCommand = new RelayCommand(OnCancelSearchClicked);
            }
        }

        public Action CloseAction { get; set; }

        private DocumentService DocumentService
        {
            get
            {
                return _serviceProvider.GetService(typeof(DocumentService)) as DocumentService;
            }
        }

        public string ErrorHeader
        {
            get { return _errorHeader; }
            set
            {
                _errorHeader = value;
                OnPropertyChanged();
            }
        }

        public string Errors
        {
            get { return _errors; }
            set
            {
                _errors = value;
                OnPropertyChanged();
            }
        }

        public Visibility HasErrors
        {
            get { return _hasErrors; }
            set
            {
                _hasErrors = value;
                OnPropertyChanged();
            }
        }

        public Visibility IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand OpenWorkItemCommand
        {
            get
            {
                return _openWorkItem = new RelayCommand(OnOpenWorkItemClicked, CanExecuteSave);
            }
        }

        public AsyncDelegateCommand SaveCommand
        {
            get
            {
                return _saveCommand = new AsyncDelegateCommand(OnSaveClicked, CanExecuteSave);
            }
        }

        public ICommand SearchCommand
        {
            get
            {
                return _searchCommand = new AsyncDelegateCommand(OnSearchAsync);
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public ITeamFoundationContext TeamFoundationContext
        {
            get
            {
                _teamFoundationContext = _serviceProvider.GetService<ITeamFoundationContextManager>();
                return _teamFoundationContext != null ? _teamFoundationContext.CurrentContext : null;
            }
        }

        public VersionControlExt VersionControlExt
        {
            get
            {
                if (_serviceProvider != null)
                {
                    DTE2 dte = _serviceProvider.GetService(typeof(SDTE)) as DTE2;
                    if (dte != null)
                    {
                        return
                            dte.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as
                                VersionControlExt;
                    }
                }

                return null;
            }
        }

        public VersionControlServer VersionControlServer
        {
            get
            {
                var versionControlServer = TeamFoundationContext.TeamProjectCollection.GetService<VersionControlServer>();
                return versionControlServer;
            }
        }

        public ObservableCollection<WorkItemDetail> WorkItemDetails
        {
            get { return _workItemDetails; }
            set
            {
                _workItemDetails = value;
                OnPropertyChanged();
            }
        }

        private WorkItemStore WorkItemStore
        {
            get { return TeamFoundationContext.TeamProjectCollection.GetService(typeof(WorkItemStore)) as WorkItemStore; }
        }

        #endregion Properties

        #region Constructors

        public SearchWorkItemViewModel(IServiceProvider serviceProvider, LinkToWorkItemPackage package)
        {
            _serviceProvider = serviceProvider;
            _package = package;
            HasErrors = Visibility.Collapsed;
            IsLoading = Visibility.Collapsed;
        }

        #endregion Constructors

        #region Methods

        private bool CanExecuteSave(object o)
        {
            var canExecuteSave = WorkItemDetails != null && WorkItemDetails.Any(x => x.IsChecked);
            return canExecuteSave;
        }

        private void ClearListView()
        {
            if (WorkItemDetails != null)
            {
                WorkItemDetails.Clear();
            }
        }

        private void CloseWindow()
        {
            CloseAction();
        }

        private void OnCancelClicked(object arg)
        {
            CloseWindow();
        }

        private void OnCancelSearchClicked()
        {
            //if (_cancellationToken != null)
            //{
            //    _cancellationToken.Cancel();
            //}
        }

        private void OnOpenWorkItemClicked(object arg)
        {
            var workItemDetail = WorkItemDetails.FirstOrDefault(x => x.IsChecked);
            var workItemDocumentService = DocumentService;
            if (workItemDetail != null && workItemDocumentService != null)
            {
                IsLoading = Visibility.Visible;

                var workItemDocument = workItemDocumentService.GetWorkItem(TeamFoundationContext.TeamProjectCollection, workItemDetail.Id, this);

                try
                {
                    if (!workItemDocument.IsLoaded)
                        workItemDocument.Load();
                    workItemDocumentService.ShowWorkItem(workItemDocument);
                }
                catch (Exception exception)
                {
                    //TODO: Logging
                }
                finally
                {
                    workItemDocument.Release(this);
                }

                IsLoading = Visibility.Collapsed;
            }
            else
            {
                HasErrors = Visibility.Visible;
                IsLoading = Visibility.Collapsed;
                ErrorHeader = "Error(s)";
                Errors = "Error occurred while opening the work item.";
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OnSaveClicked(object arg)
        {
            ErrorHeader = "Error(s) occurred during save";
            Errors = string.Empty;//clear previous error messages
            var workItemDetail = WorkItemDetails.FirstOrDefault(x => x.IsChecked);
            if (workItemDetail != null)
            {
                IsLoading = Visibility.Visible;
                var workItem = workItemDetail.WorkItemCache;
                await Task.Run(() =>
                {
                    int[] changesetIds =
                        VersionControlExt.History.ActiveWindow.SelectedChangesets.Select(x => x.ChangesetId).ToArray();
                    foreach (int changesetId in changesetIds)
                    {
                        Changeset changeset = VersionControlServer.GetChangeset(changesetId);

                        try
                        {
                            workItem.Links.Add(new ExternalLink(
                                WorkItemStore.RegisteredLinkTypes[ArtifactLinkIds.Changeset],
                                changeset.ArtifactUri.AbsoluteUri));
                            var validationErrors = workItem.Validate();
                            if (validationErrors.Count == 0)
                            {
                                workItem.Save();
                            }
                            else
                            {
                                foreach (var validationError in validationErrors)
                                {
                                    Errors += string.Format("{0}{1}", validationError, Environment.NewLine);
                                }
                                HasErrors = Visibility.Visible;
                                IsLoading = Visibility.Collapsed;
                            }
                        }
                        catch (Exception exception)
                        {
                            //TODO:Logging
                            //To catch duplicate links
                            HasErrors = Visibility.Visible;
                            IsLoading = Visibility.Collapsed;
                            Errors += string.Format("{0}{1}", exception.Message, Environment.NewLine);
                        }
                    }
                });
                if (HasErrors == Visibility.Collapsed)
                {
                    //no errors
                    CloseWindow();
                }
                IsLoading = Visibility.Collapsed;
            }
        }

        private async Task OnSearchAsync(object o)
        {
            //PRE
            ErrorHeader = "Error(s) occurred during search";
            HasErrors = Visibility.Collapsed;
            IsLoading = Visibility.Visible;
            ClearListView();
            _cancellationToken = new CancellationTokenSource();

            //PROCESS
            try
            {
                WorkItem workItem = await SearchAsync(_cancellationToken.Token);

                //POST
                IsLoading = Visibility.Collapsed;
                if (workItem != null)
                {
                    _workItemDetails = new ObservableCollection<WorkItemDetail>();
                    _workItemDetails.Add(new WorkItemDetail
                    {
                        Id = workItem.Id,
                        Title = workItem.Title,
                        Type = workItem.Type.Name,
                        IsChecked = true,
                        WorkItemCache = workItem
                    });
                    WorkItemDetails = _workItemDetails.ToObservableCollection();
                }
            }
            catch (OperationCanceledException cancelledException)
            {
                IsLoading = Visibility.Collapsed;
                ClearListView();
            }
        }

        private async Task<WorkItem> SearchAsync(CancellationToken token)
        {
            WorkItem workitem = null;
            await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                if (TeamFoundationContext != null && TeamFoundationContext.HasCollection &&
                    TeamFoundationContext.HasTeamProject)
                {
                    //var activeTFS = new TfsTeamProjectCollection(TeamFoundationContext.TeamProjectCollection.Uri.AbsoluteUri);
                    var store = WorkItemStore;

                    int workItemId;
                    if (store != null && int.TryParse(SearchText, out workItemId))
                    {
                        try
                        {
                            workitem = store.GetWorkItem(workItemId);
                        }
                        catch (Exception ex)
                        {
                            HasErrors = Visibility.Visible;
                            Errors = ex.Message;
                        }
                    }
                }
                else
                {
                    HasErrors = Visibility.Visible;
                    Errors = "Unable to search. Please validate you are connected to TFS.";
                }
            }, token);
            //workItem is still null for some reason
            if (workitem == null)
            {
                HasErrors = Visibility.Visible;
                Errors = string.Format("The work item '{0}' does not exist, or you do not have permission to access it.", SearchText);
            }
            return workitem;
        }

        #endregion Methods
    }
}