using System;
using System.Windows;

namespace LinkToWorkItem
{
    /// <summary>
    /// Interaction logic for SearchWorkItemWindow.xaml
    /// </summary>
    public partial class SearchWorkItemWindow : Window
    {
        public SearchWorkItemWindow(IServiceProvider serviceProvider, LinkToWorkItemPackage package)
        {
            InitializeComponent();
            var searchWorkItemViewModel = new SearchWorkItemViewModel(serviceProvider, package);
            DataContext = searchWorkItemViewModel;
            if (searchWorkItemViewModel.CloseAction == null)
                searchWorkItemViewModel.CloseAction = new Action(Close);
        }
    }
}