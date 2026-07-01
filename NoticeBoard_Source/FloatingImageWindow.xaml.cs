using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StoryBoardAI
{
    public partial class FloatingImageWindow : Window
    {
        public FloatingImageWindow(string imagePath)
        {
            InitializeComponent();

            try
            {
                if (File.Exists(imagePath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();

                    DisplayImg.Source = bitmap;
                    
                    // Set title to filename for ease of reference
                    Title = $"Image - {Path.GetFileName(imagePath)}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load floating image: {ex.Message}");
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
