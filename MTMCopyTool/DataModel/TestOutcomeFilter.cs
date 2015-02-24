using Microsoft.TeamFoundation.TestManagement.Client;

namespace MTMCopyTool.DataModel
{
    public class TestOutcomeFilter
    {
        public TestOutcome TestOutcome { get; set; }
        public bool IsSelected { get; set; }
    }
}
