using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.TeamFoundation.TestManagement.Client;

namespace MTMCopyTool.DataModel
{
    public class TestObject
    {
        public TestObject(string name, int id)
        {
            this.Name = name;
            this.ID = id;
        }

        public TestObject(string name, int id, TestObjectType type)
        {
            this.Name = name;
            this.ID = id;
            this.Type = type;
        }

        public TestObjectType Type { get; set; }
        public string Name { get; set; }
        public int ID { get; set; }
        public int TestPlanID { get; set; }
        public IList<IdAndName> Configurations { get; set; }
        public string TestCaseCount { get; set; }
        public TestOutcome LastTestOutcome { get; set; }
        public ITestSuiteBase TestSuiteBase { get; set; }

        public ITestSuiteEntry TestSuite { get; set; }
    }
}
