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
using MTMCopyTool.Helpers;
using MTMCopyTool.Infrastructure;
using MTMCopyTool.Properties;
using Newtonsoft.Json;

namespace MTMCopyTool.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _logger = "Log";
        private readonly MappingViewModel _mappingViewModel;
        private bool _working;
        private OptionsViewModel _options;

        public SettingsViewModel(MappingViewModel mappingViewModel, OptionsViewModel options)
        {
            _options = options;

            DuplicatedTestCase =
                JsonConvert.DeserializeObject<ObservableCollection<TestCaseOldNewMapping>>(Settings.Default.Mappings);
            if (DuplicatedTestCase == null)
                DuplicatedTestCase = new ObservableCollection<TestCaseOldNewMapping>();
            _mappingViewModel = mappingViewModel;

            StartMigrationCommand = new DelegateCommand(StartMigration, CanWork);

            DeleteMappingCommand = new DelegateCommand<IList>(DeleteMapping);
        }

        public ICommand StartMigrationCommand { get; private set; }
        public ICommand DeleteMappingCommand { get; private set; }

        public ObservableCollection<TestCaseOldNewMapping> DuplicatedTestCase { get; set; }
        public TestCaseOldNewMapping SelectedMapping { get; set; }

        public string Logger
        {
            get { return _logger; }
            set
            {
                if (value == _logger) return;
                _logger = value;
                OnPropertyChanged("Logger");
            }
        }

        public bool Working
        {
            get { return _working; }
            set
            {
                if (value == _working) return;
                _working = value;
                OnPropertyChanged("Working");
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

        private void DeleteMapping(IList selectedList)
        {
            if (selectedList == null || selectedList.Count == 0) return;
            var itemsToDelete = selectedList.Cast<TestCaseOldNewMapping>().ToList();

            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (TestCaseOldNewMapping map in itemsToDelete)
                {
                    DuplicatedTestCase.Remove(map);
                }
            });

            Settings.Default.Mappings = JsonConvert.SerializeObject(DuplicatedTestCase);
            Settings.Default.Save();
        }

        private TestObjectViewModel HasSelectedItem(IEnumerable<TestObjectViewModel> suites,
            TestObjectViewModel selectedSuite)
        {
            foreach (TestObjectViewModel suite in suites)
            {
                if (suite == null) continue;

                if (suite.IsSelected)
                    selectedSuite = suite;
                else if (suite.Children.Count > 0)
                    selectedSuite = HasSelectedItem(suite.Children, selectedSuite);
            }
            return selectedSuite;
        }

        private async void StartMigration()
        {
            List<TestOutcomeFilter> selectedOutcomes = _options.TestOutcomeFilters.Where(t => t.IsSelected).ToList();

            TestObjectViewModel selectedSuite = null;
            selectedSuite = HasSelectedItem(_mappingViewModel.TestPlans, selectedSuite);
            if (_mappingViewModel.tree.Items.Count == 0 || selectedSuite == null)
            {
                MessageBox.Show("Please select suites & test cases to copy and target test plan.", "Missing Data",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Logger = "Collecting Test Cases Candidates...";
            _mappingViewModel.Working = true;
            Working = true;

            var firsetLevel = new List<TestObjectViewModel>();
            foreach (TestObjectViewModel item in _mappingViewModel.tree.Items)
            {
                firsetLevel.Add(item);
            }

            List<TestObjectViewModel> selectedItems = null;
            await
                Task.Run(
                    () => { selectedItems = GetSelectedSuitesAndTests(firsetLevel, new List<TestObjectViewModel>()); });

            List<TestObjectViewModel> migrationCandidates = new List<TestObjectViewModel>();
            await
                Task.Run(
                    () =>
                    {
                        foreach (TestObjectViewModel test in selectedItems)
                        {
                            foreach (IdAndName config in test._testObject.Configurations)
                            {
                                TestOutcome outcome = TestHelper.Instance.GetLastTestOutcome(
                                    test._testObject.TestPlanID, test.TestSuiteId, test.ID, config.Id);
                                if (selectedOutcomes.Any(t => t.TestOutcome.Equals(outcome)))
                                {
                                    test.Configuration = config;
                                    migrationCandidates.Add(test);
                                }
                            }
                        }
                    });

            if (migrationCandidates.Count == 0)
            {
                Logger = "No Test Cases Found.\nCopy Stopped. ";
                _mappingViewModel.Working = false;
                Working = false;
                return;
            }

            Logger = "Starting Copy...\n";

            await DuplicateTestCases(migrationCandidates, _options.DuplicateSelected);

            _mappingViewModel.Working = false;
            Working = false;

            Settings.Default.Mappings = JsonConvert.SerializeObject(DuplicatedTestCase);
            Settings.Default.Save();

            Logger = "Copy Completed!\n" + Logger;
        }

        private async Task DuplicateTestCases(List<TestObjectViewModel> selectedItems, bool duplicate)
        {
            await Task.Factory.StartNew(() =>
            {
                TestObjectViewModel selectedSuite = null;
                selectedSuite = HasSelectedItem(_mappingViewModel.TestPlans, selectedSuite);

                ITestPlan plan =
                    TfsShared.Instance.TargetTestProject.TestPlans.Find(selectedSuite._testObject.TestPlanID);

                try
                {
                    foreach (TestObjectViewModel test in selectedItems)
                    {
                        var parentTestSuites = new Stack();
                        ITestSuiteBase sourceSuite =
                            TfsShared.Instance.SourceTestProject.TestSuites.Find(test.TestSuiteId);

                        ITestCase testCase = TfsShared.Instance.SourceTestProject.TestCases.Find(test.ID);
                        WorkItem targetWorkItem = null;
                        if (duplicate)
                        {
                            WorkItem duplicateWorkItem = null;
                            if (!DuplicatedTestCase.Any(t => t.OldID.Equals(test.ID)))
                            {
                                duplicateWorkItem = testCase.WorkItem.Copy(TfsShared.Instance.TargetProjectWorkItemType,
                                    WorkItemCopyFlags.CopyFiles);
                                duplicateWorkItem.WorkItemLinks.Clear();

                                if (!duplicateWorkItem.IsValid())
                                {
                                    Logger = "Cannot Save Work Item - Stoping Migration\n" + Logger;
                                    ArrayList badFields = duplicateWorkItem.Validate();
                                    foreach (Field field in badFields)
                                    {
                                        Logger =
                                            string.Format("Name: {0}, Reference Name: {1},  Invalid Value: {2}\n",
                                                field.Name, field.ReferenceName, field.Value) + Logger;
                                    }

                                    break;
                                }
                                else
                                {
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
                                        string.Format("Duplicate Test Case: {0} completed, new Test Case ID: {1}\n",
                                            test.ID,
                                            duplicateWorkItem.Id) + Logger;
                                }
                            }
                            else
                            {
                                TestCaseOldNewMapping mapping =
                                    DuplicatedTestCase.FirstOrDefault(t => t.OldID.Equals(test.ID));
                                if (mapping == null) throw new NullReferenceException("Cannot locate new id");
                                duplicateWorkItem =
                                    TfsShared.Instance.TargetTestProject.TestCases.Find(mapping.NewID).WorkItem;

                                Logger =
                                    string.Format("Test Case: {0} already exists, Test Case ID: {1}\n", test.ID,
                                        duplicateWorkItem.Id) + Logger;
                            }

                            targetWorkItem = duplicateWorkItem;
                        }
                        else
                            targetWorkItem = testCase.WorkItem;

                        ITestSuiteBase suite = sourceSuite;
                        while (suite != null)
                        {
                            parentTestSuites.Push(suite);
                            suite = suite.TestSuiteEntry.ParentTestSuite;
                        }

                        parentTestSuites.Pop();

                        var parentSuite = (IStaticTestSuite)selectedSuite._testObject.TestSuiteBase;
                        foreach (ITestSuiteBase testSuite in parentTestSuites)
                        {
                            ITestSuiteEntry existingSuite =
                                parentSuite.Entries.FirstOrDefault(s => s.Title.Equals(testSuite.Title));
                            if (existingSuite == null)
                            {
                                Logger = "Creating new suite called - " + testSuite.Title + "\n" + Logger;
                                IStaticTestSuite newSuite =
                                    TfsShared.Instance.TargetTestProject.TestSuites.CreateStatic();
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

                        ITestCase targetTestCase =
                            TfsShared.Instance.TargetTestProject.TestCases.Find(targetWorkItem.Id);
                        if (!parentSuite.Entries.Contains(targetTestCase))
                        {
                            
                            ITestSuiteEntry entry =  parentSuite.Entries.Add(targetTestCase);
                            entry.Configurations.Add(test.Configuration);
                            Logger = "Adding duplicated test case completed.\n" + Logger;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger = string.Format("** ERROR ** : {0}\n", ex.Message) + Logger;
                }
            });
        }

        private List<TestObjectViewModel> GetSelectedSuitesAndTests(IEnumerable<TestObjectViewModel> list,
            List<TestObjectViewModel> selectedItems)
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
                    List<TestObjectViewModel> testCases = null;
                    if (suite.TestSuiteType == TestSuiteType.RequirementTestSuite)
                    {
                        testCases =
                            suite.TestCases.Select(test => new TestObjectViewModel(test) { TestSuiteId = suite.Id })
                                .ToList();
                    }
                    else
                    {
                        testCases = RecursiveSuiteCollector(suite as IStaticTestSuite, new List<TestObjectViewModel>());
                    }

                    selectedItems.AddRange(testCases);
                }

                if (item.Children.Count > 0 && item.Children.All(i => i.ID != 0))
                {
                    selectedItems = GetSelectedSuitesAndTests(item.Children, selectedItems);
                }
            }
            return selectedItems;
        }

        private List<TestObjectViewModel> RecursiveSuiteCollector(IStaticTestSuite suite,
            List<TestObjectViewModel> selectedItems)
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
    }

    public class TestCaseOldNewMapping
    {
        public int OldID { get; set; }
        public int NewID { get; set; }
    }
}