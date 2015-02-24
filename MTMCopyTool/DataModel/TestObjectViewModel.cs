using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.TeamFoundation.TestManagement.Client;
using System.Threading;
using System.Windows.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace MTMCopyTool.DataModel
{
    public class TestObjectViewModel : TreeViewItemViewModel
    {
        public TestObject _testObject { get; set; }
        private bool _includeTests;
        public IdAndName Configuration { get; set; }
        public TestObjectViewModel(ITestSuiteBase testSuite, bool includeTests = true)
            : base(null, true)
        {
            _includeTests = includeTests;
            _testObject = new TestObject(testSuite.Title, testSuite.Id, TestObjectType.Suite);
            _testObject.TestSuiteBase = testSuite;
            _testObject.TestPlanID = testSuite.Plan.Id;

            StructureFlow += ";" + testSuite.ToString();

            CanChecked = testSuite.TestSuiteType == TestSuiteType.StaticTestSuite || testSuite.TestSuiteType == TestSuiteType.RequirementTestSuite;

            Thread t = new Thread(new ThreadStart(GetInternalTestCaseDetails));
            t.Start();
        }

        public TestObjectViewModel()
            : base(null, true)
        {
            _testObject = new TestObject("N/A", 0);
        }

        public TestObjectViewModel(ITestSuiteEntry test)
            : base(null, true)
        {
            _testObject = new TestObject(test.Title, test.Id, TestObjectType.Test);
            CanChecked = true;
            _testObject.TestPlanID = test.ParentTestSuite.Plan.Id;
            _testObject.Configurations = test.Configurations;
            _testObject.TestSuite = test;
        }

        public void GetInternalTestCaseDetails()
        {
            if (_testObject != null && this._testObject.TestSuiteBase != null)
            {
                int testCaseCount = _testObject.TestSuiteBase.TestCases.Count;
                int withConfigurations = _testObject.TestSuiteBase.TestCases.SelectMany(c => c.Configurations).Count();

                if (testCaseCount == 0 && withConfigurations == 0)
                {
                    TestCaseCount = "";
                }
                else if (withConfigurations == 0)
                {
                    TestCaseCount = string.Format(" ({0})", testCaseCount);
                }
                else
                {
                    TestCaseCount = string.Format(" ({0},{1})", testCaseCount, withConfigurations);
                }
            }
        }

        public string StructureFlow { get; set; }

        private string _testCaseCount;
        public string TestCaseCount
        {
            get { return _testCaseCount; }
            set
            {
                if (value != _testCaseCount)
                {
                    _testCaseCount = value;
                    this.OnPropertyChanged("TestCaseCount");
                }
            }
        }

        public BitmapImage SuiteImage
        {
            get
            {
                string imageName = TestSuiteType.ToString() == "None" ? "TestCase" : TestSuiteType.ToString();
                if (imageName == "TestCase")
                    IsExpanded = true;
                return new BitmapImage(new Uri("Images/" + imageName + ".png", UriKind.RelativeOrAbsolute));
            }
        }

        public string Name
        {
            get { return _testObject.Name; }
        }

        public TestSuiteType TestSuiteType
        {
            get { return _testObject.TestSuiteBase == null ? TestSuiteType.None : _testObject.TestSuiteBase.TestSuiteType; }
        }

        public int ID
        {
            get { return _testObject.ID; }
        }

        public int TestSuiteId { get; set; }

        public TestObjectType Type
        {
            get { return _testObject.Type; }
        }       

        protected override void LoadChildren()
        {
            if (_testObject.TestSuiteBase != null)
            {
                if (_testObject.TestSuiteBase.TestSuiteType == TestSuiteType.StaticTestSuite || _testObject.TestSuiteBase.TestSuiteType == TestSuiteType.RequirementTestSuite)
                {
                    if (_testObject.TestSuiteBase.TestSuiteType == TestSuiteType.RequirementTestSuite)
                    {
                        var reqSuite = (IRequirementTestSuite)_testObject.TestSuiteBase;
                        if (!_includeTests) return;
                        foreach (ITestSuiteEntry test in reqSuite.TestCases)
                            base.Children.Add(new TestObjectViewModel(test) { Parent = this, TestSuiteId = reqSuite.Id, StructureFlow = ID.ToString(CultureInfo.InvariantCulture), IsChecked = this.IsChecked });
                    }
                    else
                    {
                        var staticSuite = (IStaticTestSuite)_testObject.TestSuiteBase;
                        foreach (ITestSuiteBase suite in staticSuite.SubSuites)
                            base.Children.Add(new TestObjectViewModel(suite, _includeTests) { Parent = this, StructureFlow = ID.ToString(), IsChecked = this.IsChecked });

                        if (!_includeTests) return;
                        foreach (ITestSuiteEntry test in staticSuite.TestCases)
                            base.Children.Add(new TestObjectViewModel(test) { Parent = this, TestSuiteId = staticSuite.Id, StructureFlow = ID.ToString(CultureInfo.InvariantCulture), IsChecked = this.IsChecked });
                    }
                }
            }
        }

        protected override void ToggleChildrenSuites()
        {
            foreach (TestObjectViewModel c in base.Children)
            {
                if (c == null) continue;
                c.IsChecked = this.IsChecked;
            }
        }

    }
}
