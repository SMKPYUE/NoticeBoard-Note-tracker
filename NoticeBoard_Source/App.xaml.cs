using System;
using System.Windows;

namespace StoryBoardAI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled UI Error:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", "NoticeBoard - Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Keep app running
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Unhandled System Error:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "NoticeBoard - Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
