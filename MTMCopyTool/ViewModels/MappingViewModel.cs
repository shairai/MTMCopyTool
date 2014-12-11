using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Microsoft.TeamFoundation.TestManagement.Client;
using MTMCopyTool.DataModel;
using MTMCopyTool.Infrastructure;
using MTMCopyTool.Helpers;

namespace MTMCopyTool.ViewModels
{
    public class MappingViewModel : INotifyPropertyChanged
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
        public ICommand CreateTestPlanCommand { get; private set; }
        public ObservableCollection<TestObjectViewModel> TestPlans { get; set; }
        public TestObjectViewModel TargetTestPlan { get; set; }

        public TreeView tree { get; set; }
        public MappingViewModel(TreeView _tree)
        {
            TestPlans = new ObservableCollection<TestObjectViewModel>();
            tree = _tree;
            TfsShared.Instance.Connected += Instance_Connected;

            CreateTestPlanCommand = new DelegateCommand(CreatePlan, CanWork);
        }

        void Instance_Connected(object sender, ConnectionArgs e)
        {
            if (e.Success && e.IsSource)
            {
                Working = true;
                StartMapping();
            }
            if (e.Success && !e.IsSource)
            {
                GetTargetProjectPlans();
            }
        }

        public async void CreatePlan()
        {
            await Task.Factory.StartNew(() =>
            {
                if (string.IsNullOrEmpty(TestPlanName))
                {
                    MessageBox.Show("Please enter valid test plan name", "Missing Value", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Working = true;

                ITestPlan plan = TfsShared.Instance.TargetTestProject.TestPlans.Create();
                plan.Name = TestPlanName;
                plan.Save();

                App.Current.Dispatcher.Invoke(() =>
                {
                    TestPlans.Add(new TestObjectViewModel(plan.RootSuite));
                });

                Working = false;
            });
        }

        private bool CanWork()
        {
            return !Working;
        }

        private async void GetTargetProjectPlans()
        {
            if (TfsShared.Instance.TargetTestProject == null)
            {
                throw new NullReferenceException("Tfs Object is Null");
            }

            List<TestObjectViewModel> plansCollection = await Task.Run(() =>
               {
                   
                   var plans = TfsShared.Instance.TargetTestProject.TestPlans.Query("Select * From TestPlan");
                   List<TestObjectViewModel> rootItems = plans.Select(plan => new TestObjectViewModel(plan.RootSuite)).OrderBy(n=>n.Name).ToList();

                   App.Current.Dispatcher.Invoke(() =>
                   {
                       TestPlans.Clear();
                       foreach (var p in rootItems)
                           TestPlans.Add(p);
                   });

                   return rootItems;
               });

            Working = false;
        }

        private async void StartMapping()
        {
            if (TfsShared.Instance.SourceTestProject == null)
            {
                throw new NullReferenceException("Tfs Object is Null");
            }

            List<TestObjectViewModel> plansCollection = await Task.Run(() =>
               {
                   var plans = TfsShared.Instance.SourceTestProject.TestPlans.Query("Select * From TestPlan");
                   List<TestObjectViewModel> rootItems = plans.Select(plan => new TestObjectViewModel(plan.RootSuite)).ToList();
                   return rootItems;
               });

            App.Current.Dispatcher.Invoke(() =>
            {
                tree.ItemsSource = plansCollection.OrderBy(n => n.Name);
            });

            Working = false;
        }        

        private string _testPlanName;
        public string TestPlanName
        {
            get { return _testPlanName; }
            set
            {
                if (value == _testPlanName) return;
                _testPlanName = value;
                this.OnPropertyChanged("TestPlanName");
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
}