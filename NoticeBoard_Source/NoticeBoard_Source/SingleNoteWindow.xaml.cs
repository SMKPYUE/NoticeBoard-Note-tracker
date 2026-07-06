using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StoryBoardAI
{
    public partial class SingleNoteWindow : Window
    {
        private HwndSource? _hwndSource;
        private StoryBoardCard _card;

        public SingleNoteWindow(StoryBoardCard card)
        {
            InitializeComponent();
            _card = card;
            DataContext = _card;

            Loaded += SingleNoteWindow_Loaded;
            Closed += SingleNoteWindow_Closed;

            // Track active/focused popout window
            Activated += (s, e) =>
            {
                MainWindow.LastActivePopout = this;
            };
        }

        private void SingleNoteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Register WndProc hook for selective click-through hit-testing
            var helper = new WindowInteropHelper(this);
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            _hwndSource?.AddHook(HwndMessageHook);

            // Seed initial active tracking
            MainWindow.LastActivePopout = this;
        }

        private void SingleNoteWindow_Closed(object? sender, EventArgs e)
        {
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(HwndMessageHook);
                _hwndSource.Dispose();
            }

            if (MainWindow.LastActivePopout == this)
            {
                MainWindow.LastActivePopout = null;
            }
            
            // Save workspaces when window closes (writes changes to disk safely)
            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin?.SaveData();
        }

        private void ResetScaleOpacity_Click(object sender, RoutedEventArgs e)
        {
            _card.ScaleFactor = 1.0;
            _card.ContentOpacity = 1.0;
        }

        public void FocusContentBox()
        {
            NotesContentBox.Focus();
            NotesContentBox.Select(NotesContentBox.Text.Length, 0);
        }

        public void ToggleLockState()
        {
            _card.IsPinned = !_card.IsPinned;
        }

        #region Win32 Click-Through and Hit-Testing
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_NCHITTEST && _card.IsPinned)
                {
                    // Ensure visual is attached to prevent InvalidOperationException in PointFromScreen
                    if (PresentationSource.FromVisual(this) == null)
                        return IntPtr.Zero;

                    // Screen coordinates are encoded in lParam: X is low word, Y is high word
                    int x = (short)(lParam.ToInt32() & 0xFFFF);
                    int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

                    // Convert screen coordinates to client coordinates
                    Point clientPoint = PointFromScreen(new Point(x, y));

                    // Hit test our HeaderPanel to see if mouse is over it or its children
                    var result = VisualTreeHelper.HitTest(HeaderPanel, clientPoint);
                    if (result != null)
                    {
                        // Mouse is over the interactive header control bar!
                        // Let WPF handle this normally so minimize/pin/close/sliders can be clicked
                        handled = false;
                        return IntPtr.Zero;
                    }

                    // If not over the header panel, return HTTRANSPARENT so mouse clicks pass through
                    handled = true;
                    return new IntPtr(HTTRANSPARENT);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during hit-testing: {ex.Message}");
            }
            return IntPtr.Zero;
        }
        #endregion

        #region Window Interactions
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Thumbnail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is string imgPath)
            {
                var win = new FloatingImageWindow(imgPath);
                win.Owner = this;
                win.Show();
            }
        }
        #endregion
    }
}
