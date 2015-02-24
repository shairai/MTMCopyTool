using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.TeamFoundation.TestManagement.Client;
using MTMCopyTool.DataModel;

namespace MTMCopyTool.ViewModels
{
    public class OptionsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TestOutcomeFilter> TestOutcomeFilters { get; set; }
        public TestCaseOldNewMapping SelectedMapping { get; set; }

        public OptionsViewModel()
        {
            TestOutcomeFilters = new ObservableCollection<TestOutcomeFilter>
            {
                new TestOutcomeFilter() {TestOutcome = TestOutcome.Passed, IsSelected = true},
                new TestOutcomeFilter() {TestOutcome = TestOutcome.Failed, IsSelected = true},
                new TestOutcomeFilter() {TestOutcome = TestOutcome.Blocked, IsSelected = true},
                new TestOutcomeFilter() {TestOutcome = TestOutcome.Unspecified, IsSelected = true},
            };
        }

        private bool _duplicateSelected = true;
        public bool DuplicateSelected
        {
            get { return _duplicateSelected; }
            set
            {
                if (value == _duplicateSelected) return;
                _duplicateSelected = value;
                OnPropertyChanged("DuplicateSelected");
            }
        }

        private bool _linkSelected;
        public bool LinkSelected
        {
            get { return _linkSelected; }
            set
            {
                if (value == _linkSelected) return;
                _linkSelected = value;
                OnPropertyChanged("LinkSelected");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}