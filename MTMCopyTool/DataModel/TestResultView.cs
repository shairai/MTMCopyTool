using System.Windows.Media.Imaging;
using Microsoft.TeamFoundation.TestManagement.Client;
using System;
using System.Windows.Media;
namespace MTMCopyTool.DataModel
{
    public class TestResultView
    {
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedByName { get; set; }
        public TestOutcome Outcome { get; set; }
        public TimeSpan Duration { get; set; }
        public string Comment { get; set; }
        public string TestConfigurationName { get; set; }
        public string ErrorMessage { get; set; }
        public string BuildNumber { get; set; }
        public string ComputerName { get; set; }

        public ImageSource ImageSrc
        {
            get
            {
                return new BitmapImage(new Uri("/MTMCopyTool;component/Images/" + Outcome + ".png", UriKind.RelativeOrAbsolute));
            }
        }
    }
}
