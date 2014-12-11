using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using MTMCopyTool.DataModel;
using MTMCopyTool.Infrastructure;
using MTMCopyTool.Helpers;
using Newtonsoft.Json;

namespace MTMCopyTool.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public ICommand StartMigrationCommand { get; private set; }
        public ICommand DeleteMappingCommand { get; private set; }

        private MappingViewModel _mappingViewModel;

        public ObservableCollection<TestCaseOldNewMapping> DuplicatedTestCase { get; set; }
        public TestCaseOldNewMapping SelectedMapping { get; set; }

        public SettingsViewModel(MappingViewModel mappingViewModel)
        {
            DuplicatedTestCase = JsonConvert.DeserializeObject<ObservableCollection<TestCaseOldNewMapping>>(Properties.Settings.Default.Mappings);
            if (DuplicatedTestCase == null)
                DuplicatedTestCase = new ObservableCollection<TestCaseOldNewMapping>();
            this._mappingViewModel = mappingViewModel;
            StartMigrationCommand = new DelegateCommand(StartMigration, CanWork);

            DeleteMappingCommand = new DelegateCommand(DeleteMapping);
        }

        private void DeleteMapping()
        {
            if (SelectedMapping == null) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                DuplicatedTestCase.Remove(SelectedMapping);
            });

            Properties.Settings.Default.Mappings = JsonConvert.SerializeObject(DuplicatedTestCase);
            Properties.Settings.Default.Save();
        }

        private async void StartMigration()
        {
            if (_mappingViewModel.tree.Items.Count == 0 || _mappingViewModel.TargetTestPlan == null)
            {
                MessageBox.Show("Please select suites & test cases to copy and target test plan.", "Missing Data",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Logger = "Collecting Test Cases Candidates...";
            _mappingViewModel.Working = true;
            Working = true;

            List<TestObjectViewModel> firsetLevel = new List<TestObjectViewModel>();
            foreach (TestObjectViewModel item in _mappingViewModel.tree.Items)
            {
                firsetLevel.Add(item);
            }

            List<TestObjectViewModel> selectedItems = null;
            await Task.Run(() =>
            {
                selectedItems = GetSelectedSuitesAndTests(firsetLevel, new List<TestObjectViewModel>());
            });


            if (selectedItems.Count == 0)
            {
                Logger = "No Test Cases Found.\nCopy Stopped. ";
                _mappingViewModel.Working = false;
                Working = false;
                return;
            }

            Logger = "Starting Copy...\n";

            await DuplicateTestCases(selectedItems);

            _mappingViewModel.Working = false;
            Working = false;

            Properties.Settings.Default.Mappings = JsonConvert.SerializeObject(DuplicatedTestCase);
            Properties.Settings.Default.Save();

            Logger = "Copy Completed!\n" + Logger;
        }

        private async Task DuplicateTestCases(List<TestObjectViewModel> selectedItems)
        {
            await Task.Factory.StartNew(() =>
            {
                ITestPlan plan = TfsShared.Instance.TargetTestProject.TestPlans.Find(_mappingViewModel.TargetTestPlan._testObject.TestPlanID);

                try
                {
                    foreach (TestObjectViewModel test in selectedItems)
                    {
                        Stack parentTestSuites = new Stack();
                        ITestSuiteBase sourceSuite =
                            TfsShared.Instance.SourceTestProject.TestSuites.Find(test.TestSuiteId);

                        ITestCase testCase = TfsShared.Instance.SourceTestProject.TestCases.Find(test.ID);
                        WorkItem duplicateWorkItem = null;
                        if (!DuplicatedTestCase.Any(t => t.OldID.Equals(test.ID)))
                        {
                            duplicateWorkItem = testCase.WorkItem.Copy(TfsShared.Instance.TargetProjectWorkItemType,
                                WorkItemCopyFlags.CopyFiles);
                            duplicateWorkItem.Save();

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                DuplicatedTestCase.Add(new TestCaseOldNewMapping()
                                {
                                    OldID = testCase.Id,
                                    NewID = duplicateWorkItem.Id
                                });
                            });

                            Logger =
                                string.Format("Duplicate Test Case: {0} completed, new Test Case ID: {1}\n", test.ID,
                                    duplicateWorkItem.Id) + Logger;
                        }
                        else
                        {
                            var mapping = DuplicatedTestCase.FirstOrDefault(t => t.OldID.Equals(test.ID));
                            if (mapping == null) throw new NullReferenceException("Cannot locate new id");
                            duplicateWorkItem =
                                TfsShared.Instance.TargetTestProject.TestCases.Find(mapping.NewID).WorkItem;

                            Logger =
                                string.Format("Test Case: {0} already exists, Test Case ID: {1}\n", test.ID,
                                    duplicateWorkItem.Id) + Logger;
                        }

                        ITestSuiteBase suite = sourceSuite;
                        while (suite != null)
                        {
                            parentTestSuites.Push(suite);
                            suite = suite.TestSuiteEntry.ParentTestSuite;
                        }

                        parentTestSuites.Pop();

                        IStaticTestSuite parentSuite = plan.RootSuite;
                        foreach (ITestSuiteBase testSuite in parentTestSuites)
                        {
                            ITestSuiteEntry existingSuite =
                                parentSuite.Entries.FirstOrDefault(s => s.Title.Equals(testSuite.Title));
                            if (existingSuite == null)
                            {
                                Logger = "Creating new suite called - " + testSuite.Title + "\n" + Logger;
                                var newSuite = TfsShared.Instance.TargetTestProject.TestSuites.CreateStatic();
                                newSuite.Title = testSuite.Title;
                                newSuite.State = testSuite.State;
                                newSuite.Description = testSuite.Description;

                                parentSuite.Entries.Add(newSuite);
                                parentSuite = newSuite;
                            }
                            else
                            {
                                Logger = string.Format("Suite '{0}' already exists.\n{1}", existingSuite.Title, Logger);
                                parentSuite = existingSuite.TestSuite as IStaticTestSuite;
                            }

                            plan.Save();
                        }

                        var targetTestCase = TfsShared.Instance.TargetTestProject.TestCases.Find(duplicateWorkItem.Id);
                        if (!parentSuite.Entries.Contains(targetTestCase))
                        {
                            parentSuite.Entries.Add(targetTestCase);
                            Logger = "Adding duplicated test case copmleted.\n" + Logger;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger = string.Format("** ERROR ** : {0}\n", ex.Message) + Logger;
                }
            });
        }
        private List<TestObjectViewModel> GetSelectedSuitesAndTests(IEnumerable<TestObjectViewModel> list, List<TestObjectViewModel> selectedItems)
        {
            foreach (TestObjectViewModel item in list)
            {
                if (item == null) continue;

                if (item.IsChecked && item.Type == TestObjectType.Test)
                {
                    selectedItems.Add(item);
                    continue;
                }
                else if (item.IsChecked && item.Type == TestObjectType.Suite)
                {
                    ITestSuiteBase suite = TfsShared.Instance.SourceTestProject.TestSuites.Find(item.ID);
                    var testCases = RecursiveSuiteCollector(suite as IStaticTestSuite, new List<TestObjectViewModel>());
                    selectedItems.AddRange(testCases);
                }

                if (item.Children.Count > 0 && item.Children.All(i => i.ID != 0))
                {
                    selectedItems = GetSelectedSuitesAndTests(item.Children, selectedItems);
                }
            }
            return selectedItems;
        }

        private List<TestObjectViewModel> RecursiveSuiteCollector(IStaticTestSuite suite, List<TestObjectViewModel> selectedItems)
        {
            selectedItems.AddRange(suite.TestCases.Select(test => new TestObjectViewModel(test) { TestSuiteId = suite.Id }));

            if (suite.SubSuites.Count > 0)
            {
                foreach (ITestSuiteBase subSuite in suite.SubSuites)
                {
                    if (subSuite.TestSuiteType != TestSuiteType.StaticTestSuite) continue;
                    RecursiveSuiteCollector(subSuite as IStaticTestSuite, selectedItems);
                }
            }

            return selectedItems;
        }

        private bool CanWork()
        {
            return !Working;
        }

        private string _logger = "Log";
        public string Logger
        {
            get { return _logger; }
            set
            {
                if (value == _logger) return;
                _logger = value;
                this.OnPropertyChanged("Logger");
            }
        }

        private bool _working;
        public bool Working
        {
            get { return _working; }
            set
            {
                if (value == _working) return;
                _working = value;
                this.OnPropertyChanged("Working");
            }
        }
    }

    public class TestCaseOldNewMapping
    {
        public int OldID { get; set; }
        public int NewID { get; set; }
    }
}