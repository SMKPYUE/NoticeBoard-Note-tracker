using System.Windows;
using System.Windows.Input;

namespace StoryBoardAI
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string message, string title = "Confirm Action", string confirmText = "Confirm", string cancelText = "Cancel")
        {
            InitializeComponent();
            
            TitleTxt.Text = title;
            MessageTxt.Text = message;
            ConfirmBtn.Content = confirmText;
            CancelBtn.Content = cancelText;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
