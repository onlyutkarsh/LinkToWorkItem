using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LinkToWorkItem.Entities
{
    public class WorkItemDetail : INotifyPropertyChanged
    {
        private bool _isChecked;

        public int Id { get; set; }

        public string Title { get; set; }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public string Type { get; set; }

        public WorkItem WorkItemCache { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}