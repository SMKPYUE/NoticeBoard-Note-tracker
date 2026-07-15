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

        private static DateTime _lastErrorTime = DateTime.MinValue;

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var now = DateTime.Now;
            if ((now - _lastErrorTime).TotalMilliseconds < 500)
            {
                Environment.Exit(1);
            }
            _lastErrorTime = now;

            var ex = e.Exception;
            string msg = ex.Message;
            if (ex.InnerException != null)
            {
                msg += $"\n\nInner Exception:\n{ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
            }
            MessageBox.Show($"Unhandled UI Error:\n\n{msg}\n\nStack Trace:\n{ex.StackTrace}", "NoticeBoard - Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
