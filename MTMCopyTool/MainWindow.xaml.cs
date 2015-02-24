using MahApps.Metro.Controls;
using MTMCopyTool.Controls;
using MTMCopyTool.ViewModels;

namespace MTMCopyTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            App.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;

            OptionsViewModel optionsViewModel = new OptionsViewModel();
            MappingViewModel mappingViewModel = new MappingViewModel(plansTree);
            ConnectionsViewModel connectionsViewModel = new ConnectionsViewModel();

            gp_options.DataContext = optionsViewModel;
            gp_copy.DataContext = mappingViewModel;
            gp_connections.DataContext = connectionsViewModel;
            gp_settings.DataContext = new SettingsViewModel(mappingViewModel, optionsViewModel);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ExceptionDetails exception = new ExceptionDetails(e.Exception);
            exception.ShowDialog();
            exception = null;
            e.Handled = true;
        }

        private void ButtonBase_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            About a = new About();
            a.ShowDialog();
        }
    }
}
