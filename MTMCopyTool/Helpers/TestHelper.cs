using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.TestManagement.Client;
using MTMCopyTool.DataModel;

namespace MTMCopyTool.Helpers
{
    public class TestHelper
    {
        private TestHelper() { }

        private static TestHelper _instance;
        public static TestHelper Instance
        {
            get { return _instance ?? (_instance = new TestHelper()); }
        }

        public TestOutcome GetLastTestOutcome(ITestSuiteEntry testCase, int configurationId)
        {
            ITestPointCollection tpc = testCase.ParentTestSuite.Plan.QueryTestPoints("SELECT * FROM TestPoint WHERE SuiteId = " + testCase.ParentTestSuite.Id);
            var testPoints = tpc.FirstOrDefault(t => t.TestCaseId.Equals(testCase.Id) && t.ConfigurationId.Equals(configurationId));

            if (testPoints == null) return TestOutcome.None;

            return testPoints.MostRecentResultOutcome;
        }

        public IEnumerable<TestResultView> GetTestCaseResults(ITestSuiteEntry testCase)
        {
            if (testCase == null) return null;
            var testResults = TfsShared.Instance.SourceTestProject.TestResults.ByTestId(testCase.Id);

            List<TestResultView> testcaseResults = new List<TestResultView>();

            foreach (var result in testResults)
            {
                ITestCaseResult tr = (ITestCaseResult)result;

                testcaseResults.Add(new TestResultView()
                {
                    Outcome = tr.Outcome,
                    Duration = tr.Duration,
                    Comment = tr.Comment,
                    LastUpdated = tr.LastUpdated,
                    LastUpdatedByName = tr.LastUpdatedByName,
                    TestConfigurationName = tr.TestConfigurationName,
                    ComputerName = tr.ComputerName,
                    ErrorMessage = tr.ErrorMessage,
                    BuildNumber = tr.BuildNumber
                });
            }

            return testcaseResults.OrderByDescending(a => a.LastUpdated);
        }
    }
}
