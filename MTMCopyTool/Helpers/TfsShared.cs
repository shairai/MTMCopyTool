using System;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client.Catalog.Objects;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.Server;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace MTMCopyTool.Helpers
{
    public class ConnectionArgs
    {
        public bool IsSource { get; set; }
        public bool Success { get; set; }
    }

    public class TfsShared
    {
        public TfsTeamProjectCollection SourceTFS { get; set; }
        public TfsTeamProjectCollection TargetTFS { get; set; }
        public ProjectInfo SourceProject { get; set; }
        public ProjectInfo TargetProject { get; set; }
        public WorkItemType TargetProjectWorkItemType { get; set; }
        public ITestManagementTeamProject SourceTestProject { get; set; }
        public ITestManagementTeamProject TargetTestProject { get; set; }

        private static TfsShared _instance;

        public delegate void ConnectedEventHandler(object sender, ConnectionArgs e);
        public event ConnectedEventHandler Connected;

        private TfsShared()
        {

        }

        public static TfsShared Instance
        {
            get { return _instance ?? (_instance = new TfsShared()); }
        }

        public async void OpenTeamProjectPicker(bool source)
        {
            var tpp = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false);
            if (tpp.ShowDialog() == System.Windows.Forms.DialogResult.OK && tpp.SelectedTeamProjectCollection != null && tpp.SelectedProjects.Any())
            {
                if (source)
                {

                    string tfsUrl = tpp.SelectedTeamProjectCollection.Uri.AbsoluteUri;
                    Properties.Settings.Default.SourceTFS = tfsUrl;

                    string projectName = tpp.SelectedProjects[0].Name;
                    Properties.Settings.Default.SourceProject = projectName;
                    Properties.Settings.Default.Save();

                    await Connect(tfsUrl, projectName, true);

                    Connected(this, new ConnectionArgs() { IsSource = true, Success = true });
                }
                else
                {
                    string tfsUrl = tpp.SelectedTeamProjectCollection.Uri.AbsoluteUri;
                    Properties.Settings.Default.TargetTFS = tfsUrl;

                    string projectName = tpp.SelectedProjects[0].Name;
                    Properties.Settings.Default.TargetProject = projectName;
                    Properties.Settings.Default.Save();

                    await Connect(tfsUrl, projectName, false);

                    Connected(this, new ConnectionArgs() { IsSource = false, Success = true });
                }
            }
            else
            {
                Connected(this, new ConnectionArgs() { IsSource = source, Success = false });
            }
        }

        private async Task Connect(string tfsUrl, string projectName, bool source)
        {
            await Task.Factory.StartNew(() =>
            {
                if (source)
                {
                    SourceTFS = new TfsTeamProjectCollection(new Uri(tfsUrl));
                    var service = (ITestManagementService)SourceTFS.GetService(typeof(ITestManagementService));
                    SourceTestProject = (ITestManagementTeamProject)service.GetTeamProject(projectName);
                    SourceProject = new ProjectInfo()
                    {
                        Name = SourceTestProject.WitProject.Name,
                        Uri = SourceTestProject.WitProject.Uri.AbsoluteUri
                    };
                }
                else
                {
                    TargetTFS = new TfsTeamProjectCollection(new Uri(tfsUrl));
                    var service = (ITestManagementService)TargetTFS.GetService(typeof(ITestManagementService));
                    TargetTestProject = (ITestManagementTeamProject)service.GetTeamProject(projectName);
                    TargetProject = new ProjectInfo()
                    {
                        Name = TargetTestProject.WitProject.Name,
                        Uri = TargetTestProject.WitProject.Uri.AbsoluteUri
                    };

                    WorkItemStore store = new WorkItemStore(TargetTFS);

                    TargetProjectWorkItemType = store.Projects[projectName].WorkItemTypes["Test Case"];
                }
            });
        }

        public async void Connect(bool source)
        {
            if (source)
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.SourceTFS))
                    OpenTeamProjectPicker(true);
                else
                {
                    await
                        Connect(Properties.Settings.Default.SourceTFS, Properties.Settings.Default.SourceProject, true);
                    Connected(this, new ConnectionArgs() { IsSource = true, Success = true });
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.TargetTFS))
                    OpenTeamProjectPicker(false);
                else
                {
                    await Connect(Properties.Settings.Default.TargetTFS, Properties.Settings.Default.TargetProject, false);
                    Connected(this, new ConnectionArgs() { IsSource = false, Success = true });
                }
            }
        }
    }
}
