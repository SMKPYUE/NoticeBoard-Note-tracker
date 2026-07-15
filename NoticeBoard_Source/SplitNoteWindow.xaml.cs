using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StoryBoardAI
{
    public partial class SplitNoteWindow : Window
    {
        private HwndSource? _hwndSource;
        private StoryBoardCard _card;
        private string? _currentImagePath;

        public SplitNoteWindow(StoryBoardCard card)
        {
            InitializeComponent();
            _card = card;
            DataContext = _card;

            Loaded += SplitNoteWindow_Loaded;
            Closed += SplitNoteWindow_Closed;

            Activated += (s, e) =>
            {
                MainWindow.LastActivePopout = this;
            };

            // Setup default drawing attributes
            DrawingCanvas.DefaultDrawingAttributes.Color = (Color)ColorConverter.ConvertFromString("#FF3333");
            DrawingCanvas.DefaultDrawingAttributes.Width = 3;
            DrawingCanvas.DefaultDrawingAttributes.Height = 3;
            DrawingCanvas.DefaultDrawingAttributes.FitToCurve = true;
        }

        private void SplitNoteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // WndProc hook for selective click-through
            var helper = new WindowInteropHelper(this);
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            _hwndSource?.AddHook(HwndMessageHook);

            MainWindow.LastActivePopout = this;

            // Load first image if available
            if (_card.Images.Count > 0)
            {
                LoadImageAndAnnotations(_card.Images[0]);
            }
            else
            {
                // No images, disable drawing canvas and display a fallback placeholder
                DrawingCanvas.IsEnabled = false;
                PenRadio.IsEnabled = false;
                EraserRadio.IsEnabled = false;
                ColorPanel.IsEnabled = false;
            }
        }

        private void LoadImageAndAnnotations(string imagePath)
        {
            _currentImagePath = imagePath;
            try
            {
                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    UnderlayImage.Source = bitmap;
                }

                // Load Ink Annotations
                DrawingCanvas.Strokes.Clear();
                if (_card.ImageAnnotations.TryGetValue(imagePath, out string? base64Strokes) && !string.IsNullOrEmpty(base64Strokes))
                {
                    byte[] bytes = Convert.FromBase64String(base64Strokes);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var strokes = new StrokeCollection(ms);
                        DrawingCanvas.Strokes = strokes;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image/annotations: {ex.Message}");
            }
        }

        private void SaveAnnotations()
        {
            if (string.IsNullOrEmpty(_currentImagePath)) return;

            try
            {
                using (var ms = new MemoryStream())
                {
                    DrawingCanvas.Strokes.Save(ms);
                    byte[] bytes = ms.ToArray();
                    string base64Strokes = Convert.ToBase64String(bytes);

                    _card.ImageAnnotations[_currentImagePath] = base64Strokes;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving ink annotations: {ex.Message}");
            }
        }

        private void SplitNoteWindow_Closed(object? sender, EventArgs e)
        {
            SaveAnnotations();

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(HwndMessageHook);
                _hwndSource.Dispose();
            }

            if (MainWindow.LastActivePopout == this)
            {
                MainWindow.LastActivePopout = null;
            }

            // Save data to write all updates to disk
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

        #region Win32 Click-Through WndProc Hook
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_NCHITTEST && _card.IsPinned)
                {
                    if (PresentationSource.FromVisual(this) == null)
                        return IntPtr.Zero;

                    int x = (short)(lParam.ToInt32() & 0xFFFF);
                    int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

                    Point clientPoint = PointFromScreen(new Point(x, y));

                    // Hit test HeaderPanel
                    var resultHeader = VisualTreeHelper.HitTest(HeaderPanel, clientPoint);
                    if (resultHeader != null)
                    {
                        handled = false;
                        return IntPtr.Zero;
                    }

                    // For the split view, we want to check if the mouse is over any drawing toolbar items,
                    // the grid splitter column, or other controls. If so, they must remain interactive.
                    // Hit test our drawing area's size-changed grid or TextBox directly. If it is over
                    // the drawing canvas or note text area, make it click-through.
                    
                    // We check if mouse is over the TextBox (NotesContentBox) or DrawingCanvas:
                    var resultTextBox = VisualTreeHelper.HitTest(NotesContentBox, clientPoint);
                    if (resultTextBox != null)
                    {
                        // Inside note typing area - click-through when pinned
                        handled = true;
                        return new IntPtr(HTTRANSPARENT);
                    }

                    var resultCanvas = VisualTreeHelper.HitTest(DrawingCanvas, clientPoint);
                    if (resultCanvas != null)
                    {
                        // Inside image drawing area - click-through when pinned
                        handled = true;
                        return new IntPtr(HTTRANSPARENT);
                    }

                    // Otherwise, keep interactive (Header panel, Toolbar panel, Grid splitter)
                    handled = false;
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during hit-testing: {ex.Message}");
            }
            return IntPtr.Zero;
        }
        #endregion

        #region Drawing Toolbar Action Handlers
        private void PenRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (DrawingCanvas != null)
            {
                DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        private void EraserRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (DrawingCanvas != null)
            {
                DrawingCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                DrawingCanvas.DefaultDrawingAttributes.Color = (Color)ColorConverter.ConvertFromString(colorHex);
                PenRadio.IsChecked = true;
                DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DrawingCanvas != null && ThicknessSlider != null)
            {
                DrawingCanvas.DefaultDrawingAttributes.Width = ThicknessSlider.Value;
                DrawingCanvas.DefaultDrawingAttributes.Height = ThicknessSlider.Value;
            }
        }

        private void ClearInk_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmDialog("Are you sure you want to clear all drawings from this image?", "Clear Canvas");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                DrawingCanvas.Strokes.Clear();
                SaveAnnotations();
            }
        }

        private void DrawingAreaGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Adjust DrawingCanvas dimensions to snap to the layout sizing grid
            DrawingCanvas.Width = DrawingAreaGrid.ActualWidth;
            DrawingCanvas.Height = DrawingAreaGrid.ActualHeight;
        }
        #endregion

        #region Window Drag Move and Interactions
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
        #endregion
    }
}
