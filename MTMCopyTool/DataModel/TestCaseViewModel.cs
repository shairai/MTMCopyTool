using Microsoft.TeamFoundation.TestManagement.Client;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using MTMCopyTool.Helpers;

namespace MTMCopyTool.DataModel
{
    public class TestCaseViewModel : INotifyPropertyChanged
    {
        private string title;
        private int id;
        private string owner;
        private string state;
        private string area;
        private string testSuiteName;
        private string testPlanName;
        private int testSuiteId;
        [XmlIgnore]
        private ImageSource _imageSource;
        [XmlIgnore]
        private TestOutcome _outcome { get; set; }
        [XmlIgnore]
        private bool hasAutomation;

        [XmlIgnore]
        public ITestSuiteEntry TestCase { get; set; }
        [XmlIgnore]
        private ITestCase _testCaseObject;

        private TestOutcome _testRunOutome = TestOutcome.None;
        [XmlIgnore]
        public TestOutcome TestRunOutome
        {
            get { return _testRunOutome; }
            set
            {
                if (value != _testRunOutome)
                {
                    _testRunOutome = value;
                    this.OnPropertyChanged("TestRunOutome");
                }
            }
        }

        private Boolean _isStandAloneTest;
        [XmlAttribute]
        public Boolean IsStandAloneTest
        {
            get { return _isStandAloneTest; }
            set
            {
                if (value != _isStandAloneTest)
                {
                    _isStandAloneTest = value;
                    this.OnPropertyChanged("IsStandAloneTest");
                }
            }
        }

        private string _automatedTestType;
        [XmlAttribute]
        public string AutomatedTestType
        {
            get { return _automatedTestType; }
            set
            {
                if (value != _automatedTestType)
                {
                    _automatedTestType = value;
                    this.OnPropertyChanged("AutomatedTestType");
                }
            }
        }

        private string _automatedTestId;
        [XmlAttribute]
        public string AutomatedTestId
        {
            get { return _automatedTestId; }
            set
            {
                if (value != _automatedTestId)
                {
                    _automatedTestId = value;
                    this.OnPropertyChanged("AutomatedTestId");
                }
            }
        }

        private string _automatedTestStorage;
        [XmlAttribute]
        public string AutomatedTestStorage
        {
            get { return _automatedTestStorage; }
            set
            {
                if (value != _automatedTestStorage)
                {
                    _automatedTestStorage = value;
                    this.OnPropertyChanged("AutomatedTestStorage");
                }
            }
        }

        private string _automatedTestName;
        [XmlAttribute]
        public string AutomatedTestName
        {
            get { return _automatedTestName; }
            set
            {
                if (value != _automatedTestName)
                {
                    _automatedTestName = value;
                    this.OnPropertyChanged("AutomatedTestName");
                }
            }
        }

        public string ConfigurationName { get; set; }
        public int ConfigurationId { get; set; }

        public int RootSuiteId { get; set; }
        public int TestPlanId { get; set; }
      
        public TestCaseViewModel()
        {
            
        }

        public TestCaseViewModel(ITestSuiteEntry testcase, IdAndName configuration, bool Async = true)
        {
            this.ConfigurationId = configuration.Id;
            this.ConfigurationName = configuration.Name;

            this.TestCase = testcase;
            this._testCaseObject = testcase.TestCase ?? TfsShared.Instance.SourceTestProject.TestCases.Find(testcase.Id);

            this.Owner = _testCaseObject.OwnerName;
            this.State = _testCaseObject.State;

            this.Title = testcase.Title;
            this.Id = testcase.Id;

            if (Async)
            {
                Thread t = new Thread(new ThreadStart(CollectTestCaseDetails));
                t.Start();
            }
            else
            {
                CollectTestCaseDetails();
            }
        }

        public void RemoteReloadAutomationImplementation()
        {
            Thread t = new Thread(new ThreadStart(LoadTestAutomationImplementation));
            t.Start();
        }

        public void LoadTestAutomationImplementation()
        {
            ITestCase test;
            try
            {
                test = TfsShared.Instance.SourceTestProject.TestCases.Find(Id);
            }
            catch (Exception not){
                  return;
            }

            ITmiTestImplementation implementation = (ITmiTestImplementation)test.Implementation;
            if (implementation != null)
            {
                this.AutomatedTestType = implementation.TestType;
                this.AutomatedTestId = implementation.TestId.ToString();
                this.AutomatedTestStorage = implementation.Storage;
                this.AutomatedTestName = implementation.TestName;
                this.HasAutomation = true;
            }
            else
            {
                this.HasAutomation = false;
            }
        }

        private void CollectTestCaseDetails()
        {
            this.TestSuiteName = this.TestCase.ParentTestSuite.Title;
            this.TestSuiteId = this.TestCase.ParentTestSuite.Id;
            this.Area = _testCaseObject.Area;
            this.Outcome = TestHelper.Instance.GetLastTestOutcome(this.TestCase, this.ConfigurationId);

            this.RootSuiteId = this.TestCase.ParentTestSuite.Plan.RootSuite.Id;
            this.TestPlanId = this.TestCase.ParentTestSuite.Plan.Id;
            this.TestPlanName = this.TestCase.ParentTestSuite.Plan.Name;

            LoadTestAutomationImplementation();
        }

        [XmlIgnore]
        public ImageSource ImageSrc
        {
            get
            {
                return new BitmapImage(new Uri("/BeeTester;component/Images/" + this.Outcome + ".png", UriKind.RelativeOrAbsolute));
            }
        }

        [XmlAttribute]
        public int TestSuiteId
        {
            get { return testSuiteId; }
            set
            {
                if (value != testSuiteId)
                {
                    testSuiteId = value;
                    this.OnPropertyChanged("TestSuiteId");
                }
            }
        }

        [XmlAttribute]
        public string TestPlanName
        {
            get { return testPlanName; }
            set
            {
                if (value != testPlanName)
                {
                    testPlanName = value;
                    this.OnPropertyChanged("TestPlanName");
                }
            }
        }

        [XmlAttribute]
        public string TestSuiteName
        {
            get { return testSuiteName; }
            set
            {
                if (value != testSuiteName)
                {
                    testSuiteName = value;
                    this.OnPropertyChanged("TestSuiteName");
                }
            }
        }

        [XmlAttribute]
        public string Title
        {
            get { return title; }
            set
            {
                if (value != title)
                {
                    title = value;
                    this.OnPropertyChanged("Title");
                }
            }
        }

        [XmlIgnore]
        public bool HasAutomation
        {
            get { return hasAutomation; }
            set
            {
                if (value != hasAutomation)
                {
                    hasAutomation = value;
                    this.OnPropertyChanged("HasAutomation");
                }
            }
        }

      
        [XmlAttribute]
        public int Id
        {
            get { return id; }
            set
            {
                if (value != id)
                {
                    id = value;
                    this.OnPropertyChanged("Id");
                }
            }

        }
        [XmlAttribute]
        public string Owner
        {
            get { return owner; }
            set
            {
                if (value != owner)
                {
                    owner = value;
                    this.OnPropertyChanged("Owner");
                }
            }

        }
        [XmlAttribute]
        public string State
        {
            get { return state; }
            set
            {
                if (value != state)
                {
                    state = value;
                    this.OnPropertyChanged("State");
                }
            }

        }
        [XmlAttribute]
        public string Area
        {
            get { return area; }
            set
            {
                if (value != area)
                {
                    area = value;
                    this.OnPropertyChanged("Area");
                }
            }

        }
        [XmlAttribute]
        public TestOutcome Outcome
        {
            get { return _outcome; }
            set
            {
                _outcome = value;
                this.OnPropertyChanged("Outcome");
                this.OnPropertyChanged("ImageSrc");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
