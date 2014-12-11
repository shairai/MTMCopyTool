using System;
using System.Windows;
using MahApps.Metro.Controls;

namespace MTMCopyTool.Controls
{
    /// <summary>
    /// Interaction logic for ExceptionDetails.xaml
    /// </summary>
    public partial class ExceptionDetails : MetroWindow
    {
        public ExceptionDetails(Exception ex)
        {
            InitializeComponent();

            this.txtMessage.Text = ex.Message;
            this.txtDetails.Text = string.Format("Stack Trace:\n{0}", ex.StackTrace);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
