using System;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client.Catalog.Objects;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.Server;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using MTMCopyTool.Properties;

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
                    Settings.Default.SourceTFS = tfsUrl;

                    string projectName = tpp.SelectedProjects[0].Name;
                    Settings.Default.SourceProject = projectName;
                    Settings.Default.Save();

                    await Connect(tfsUrl, projectName, true, false);

                    Connected(this, new ConnectionArgs() { IsSource = true, Success = true });
                }
                else
                {
                    string tfsUrl = tpp.SelectedTeamProjectCollection.Uri.AbsoluteUri;
                    Settings.Default.TargetTFS = tfsUrl;

                    string projectName = tpp.SelectedProjects[0].Name;
                    Settings.Default.TargetProject = projectName;
                    Settings.Default.Save();

                    await Connect(tfsUrl, projectName, false, Settings.Default.BypassRules);

                    Connected(this, new ConnectionArgs() { IsSource = false, Success = true });
                }
            }
            else
            {
                Connected(this, new ConnectionArgs() { IsSource = source, Success = false });
            }
        }

        private async Task Connect(string tfsUrl, string projectName, bool source, bool bypassRules)
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

                    WorkItemStore store = new WorkItemStore(TargetTFS, bypassRules ? WorkItemStoreFlags.BypassRules : WorkItemStoreFlags.None);

                    TargetProjectWorkItemType = store.Projects[projectName].WorkItemTypes["Test Case"];
                }
            });
        }

        public void Disconnect(bool source)
        {
            if (source)
            {
                SourceProject = null;
                SourceTFS = null;
                SourceTestProject = null;
                Connected(this, new ConnectionArgs() { IsSource = true, Success = false });
            }
            else
            {
                TargetProject = null;
                TargetTFS = null;
                TargetTestProject = null;
                TargetProjectWorkItemType = null;
                Connected(this, new ConnectionArgs() { IsSource = false, Success = false });
            }
        }

        public async void Connect(bool source, bool bypassRules = false)
        {
            if (source)
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.SourceTFS))
                    OpenTeamProjectPicker(true);
                else
                {
                    await
                        Connect(Properties.Settings.Default.SourceTFS, Properties.Settings.Default.SourceProject, true, false);
                    Connected(this, new ConnectionArgs() { IsSource = true, Success = true });
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.TargetTFS))
                    OpenTeamProjectPicker(false);
                else
                {
                    await Connect(Properties.Settings.Default.TargetTFS, Properties.Settings.Default.TargetProject, false, bypassRules);
                    Connected(this, new ConnectionArgs() { IsSource = false, Success = true });
                }
            }
        }
    }
}
