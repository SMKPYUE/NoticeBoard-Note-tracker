using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Mscc.GenerativeAI;

using System.IO.Compression;
using System.Collections.Generic;
using Microsoft.Win32;

namespace StoryBoardAI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Workspace> _workspaces = new ObservableCollection<Workspace>();
        public ObservableCollection<Workspace> Workspaces
        {
            get => _workspaces;
            set
            {
                _workspaces = value;
                OnPropertyChanged(nameof(Workspaces));
            }
        }

        private Workspace? _activeWorkspace;
        private StoryBoardCard? _activeEditingCard;
        public Workspace? ActiveWorkspace
        {
            get => _activeWorkspace;
            set
            {
                if (_activeWorkspace != null)
                {
                    _activeWorkspace.Cards.CollectionChanged -= Cards_CollectionChanged;
                    UnsubscribeFromCardChanges(_activeWorkspace.Cards);
                    _activeWorkspace.AvailableTags.CollectionChanged -= AvailableTags_CollectionChanged;
                }

                _activeWorkspace = value;

                if (_activeWorkspace != null)
                {
                    _activeWorkspace.Cards.CollectionChanged += Cards_CollectionChanged;
                    SubscribeToCardChanges(_activeWorkspace.Cards);
                    _activeWorkspace.AvailableTags.CollectionChanged += AvailableTags_CollectionChanged;
                }

                OnPropertyChanged(nameof(ActiveWorkspace));
                OnPropertyChanged(nameof(DisplayCards));
                OnPropertyChanged(nameof(AvailableTagsWithNone));
                UpdateFilterComboBoxItems();
            }
        }

        private string _selectedFilterTag = "All Tags";
        public string SelectedFilterTag
        {
            get => _selectedFilterTag;
            set
            {
                if (_selectedFilterTag != value)
                {
                    _selectedFilterTag = value;
                    OnPropertyChanged(nameof(SelectedFilterTag));
                    OnPropertyChanged(nameof(DisplayCards));
                }
            }
        }

        public ObservableCollection<string> AvailableTagsWithNone
        {
            get
            {
                var list = new ObservableCollection<string> { "" };
                if (ActiveWorkspace != null && ActiveWorkspace.AvailableTags != null)
                {
                    foreach (var tag in ActiveWorkspace.AvailableTags)
                    {
                        list.Add(tag);
                    }
                }
                return list;
            }
        }

        public ObservableCollection<StoryBoardCard> DisplayCards
        {
            get
            {
                if (ActiveWorkspace == null)
                    return new ObservableCollection<StoryBoardCard>();

                string filter = SelectedFilterTag;
                if (string.IsNullOrEmpty(filter) || filter == "All Tags")
                {
                    foreach (var card in ActiveWorkspace.Cards)
                    {
                        card.IsDimmed = false;
                    }
                    return ActiveWorkspace.Cards;
                }

                var matching = new ObservableCollection<StoryBoardCard>();
                var nonMatching = new ObservableCollection<StoryBoardCard>();

                foreach (var card in ActiveWorkspace.Cards)
                {
                    if (card.CardTag == filter)
                    {
                        card.IsDimmed = false;
                        matching.Add(card);
                    }
                    else
                    {
                        card.IsDimmed = true;
                        nonMatching.Add(card);
                    }
                }

                var combined = new ObservableCollection<StoryBoardCard>();
                foreach (var c in matching) combined.Add(c);
                foreach (var c in nonMatching) combined.Add(c);

                return combined;
            }
        }

        private void Cards_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) UnsubscribeFromCardChanges(e.OldItems);
            if (e.NewItems != null) SubscribeToCardChanges(e.NewItems);
            OnPropertyChanged(nameof(DisplayCards));
        }

        private void AvailableTags_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(AvailableTagsWithNone));
            UpdateFilterComboBoxItems();
        }

        private void SubscribeToCardChanges(System.Collections.IEnumerable cards)
        {
            foreach (StoryBoardCard card in cards)
            {
                card.PropertyChanged += Card_PropertyChanged;
            }
        }

        private void UnsubscribeFromCardChanges(System.Collections.IEnumerable cards)
        {
            foreach (StoryBoardCard card in cards)
            {
                card.PropertyChanged -= Card_PropertyChanged;
            }
        }

        private void Card_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StoryBoardCard.CardTag))
            {
                OnPropertyChanged(nameof(DisplayCards));
            }
        }

        private void UpdateFilterComboBoxItems()
        {
            if (FilterComboBox == null) return;

            string currentFilter = SelectedFilterTag;

            FilterComboBox.Items.Clear();
            FilterComboBox.Items.Add("All Tags");

            if (ActiveWorkspace != null && ActiveWorkspace.AvailableTags != null)
            {
                foreach (var tag in ActiveWorkspace.AvailableTags)
                {
                    FilterComboBox.Items.Add(tag);
                }
            }

            // Restore selection
            if (FilterComboBox.Items.Contains(currentFilter))
            {
                FilterComboBox.SelectedItem = currentFilter;
            }
            else
            {
                FilterComboBox.SelectedIndex = 0;
                SelectedFilterTag = "All Tags";
            }
        }

        private AppSettings _currentSettings = new AppSettings();
        public AppSettings CurrentSettings
        {
            get => _currentSettings;
            set
            {
                _currentSettings = value;
                OnPropertyChanged(nameof(CurrentSettings));
            }
        }

        public ObservableCollection<string> SystemFontsList { get; } = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Populate system fonts list
            try
            {
                foreach (var font in Fonts.SystemFontFamilies.OrderBy(f => f.Source))
                {
                    SystemFontsList.Add(font.Source);
                }
            }
            catch
            {
                SystemFontsList.Add("Segoe UI");
                SystemFontsList.Add("Arial");
                SystemFontsList.Add("Courier New");
                SystemFontsList.Add("Georgia");
            }

            // Ensure parchment textures are created
            EnsureParchmentTexture();

            // Restrict window size when maximized to avoid covering taskbar
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            // Load saved data and settings
            LoadData();

            // Register global hotkey Loaded event
            this.Loaded += MainWindow_Loaded;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveData();
            base.OnClosing(e);
        }

        #region Custom Title Bar Operations
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximize();
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeBtn.Content = "▢";
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeBtn.Content = "❐";
            }
        }
        #endregion

        #region Data Persistence & Serialization
        private string GetAppDataDirectory()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoticeBoard");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            return appData;
        }

        private string GetSettingsFilePath()
        {
            return Path.Combine(GetAppDataDirectory(), "settings.json");
        }

        private string GetWorkspacesFilePath()
        {
            return Path.Combine(GetAppDataDirectory(), "workspaces.json");
        }

        #region Parchment Texture Generator
        private string EnsureParchmentTexture()
        {
            string dir = Path.Combine(GetAppDataDirectory(), "Textures");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "parchment.jpg");
            if (File.Exists(path)) return path;

            try
            {
                int width = 512;
                int height = 512;
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f5ebd0")),
                        null,
                        new Rect(0, 0, width, height)
                    );

                    var edgeColor = (Color)ColorConverter.ConvertFromString("#d3c299");
                    var centerColor = Colors.Transparent;
                    var grad = new RadialGradientBrush(centerColor, edgeColor)
                    {
                        Center = new Point(0.5, 0.5),
                        GradientOrigin = new Point(0.5, 0.5),
                        RadiusX = 0.7,
                        RadiusY = 0.7
                    };
                    dc.DrawRectangle(grad, null, new Rect(0, 0, width, height));

                    var rand = new Random(42);
                    var fiberPen = new Pen(new SolidColorBrush(Color.FromArgb(10, 139, 90, 43)), 1.0);
                    for (int i = 0; i < 150; i++)
                    {
                        double y = rand.NextDouble() * height;
                        dc.DrawLine(fiberPen, new Point(0, y), new Point(width, y + (rand.NextDouble() * 10 - 5)));
                    }
                    for (int i = 0; i < 150; i++)
                    {
                        double x = rand.NextDouble() * width;
                        dc.DrawLine(fiberPen, new Point(x, 0), new Point(x + (rand.NextDouble() * 10 - 5), height));
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        double x = rand.NextDouble() * width;
                        double y = rand.NextDouble() * height;
                        double radius = rand.NextDouble() * 12 + 4;
                        var spotBrush = new RadialGradientBrush(
                            Color.FromArgb((byte)rand.Next(15, 30), 120, 90, 50),
                            Colors.Transparent
                        )
                        {
                            Center = new Point(0.5, 0.5),
                            GradientOrigin = new Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5
                        };
                        dc.DrawGeometry(spotBrush, null, new EllipseGeometry(new Point(x, y), radius, radius));
                    }
                }

                var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(dv);

                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using (var stream = File.Create(path))
                {
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate parchment texture: {ex.Message}");
            }

            return path;
        }
        #endregion

        #region Global Hotkey Win32 Support
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private const int HOTKEY_NEW_NOTE = 9001;
        private const int HOTKEY_FOCUS_NOTE = 9002;
        private const int HOTKEY_TOGGLE_LOCK = 9003;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        public static SingleNoteWindow? LastActivePopout { get; set; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RegisterAllGlobalHotkeys();

            var helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc? _proc;
        private IntPtr _hookID = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private void RegisterAllGlobalHotkeys()
        {
            UnregisterAllGlobalHotkeys();

            // Check if global hotkeys are enabled in settings
            if (!CurrentSettings.EnableGlobalHotkeys)
            {
                return;
            }

            _proc = HookCallback;
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private void UnregisterAllGlobalHotkeys()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                _proc = null;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                System.Windows.Input.Key key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(vkCode);

                bool ctrl = (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || 
                             System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl));
                bool shift = (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || 
                              System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift));
                bool alt = (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt) || 
                            System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightAlt));
                bool win = (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LWin) || 
                            System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RWin));

                // Format current pressed hotkey combination
                var parts = new List<string>();
                if (ctrl) parts.Add("Ctrl");
                if (shift) parts.Add("Shift");
                if (alt) parts.Add("Alt");
                if (win) parts.Add("Win");

                // Exclude modifier-only key presses
                if (key != System.Windows.Input.Key.LeftCtrl && key != System.Windows.Input.Key.RightCtrl &&
                    key != System.Windows.Input.Key.LeftShift && key != System.Windows.Input.Key.RightShift &&
                    key != System.Windows.Input.Key.LeftAlt && key != System.Windows.Input.Key.RightAlt &&
                    key != System.Windows.Input.Key.LWin && key != System.Windows.Input.Key.RWin)
                {
                    parts.Add(key.ToString());
                    string pressedCombo = string.Join("+", parts);

                    // If currently recording a keybind, intercept and record it!
                    if (_recordingField != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ProcessKeybindRecord(pressedCombo);
                        }));
                        return new IntPtr(1); // Swallow the keypress
                    }

                    // Check foreground app blocklist
                    if (IsHotkeyAllowedInForegroundWindow())
                    {
                        // Check against configured hotkeys
                        if (pressedCombo == CurrentSettings.HotkeyNewNote)
                        {
                            Dispatcher.BeginInvoke(new Action(() => { AddNewCardAndPopOut(); }));
                            return new IntPtr(1); // Swallow
                        }
                        else if (pressedCombo == CurrentSettings.HotkeyFocusNote)
                        {
                            Dispatcher.BeginInvoke(new Action(() => { FocusLastActivePopout(); }));
                            return new IntPtr(1); // Swallow
                        }
                        else if (pressedCombo == CurrentSettings.HotkeyToggleLock)
                        {
                            Dispatcher.BeginInvoke(new Action(() => { ToggleLastActivePopoutLock(); }));
                            return new IntPtr(1); // Swallow
                        }
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WM_HOTKEY = 0x0312;

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // We use SetWindowsHookEx for global hotkeys instead of RegisterHotKey.
            // But we keep HwndHook registered to prevent breaking visual setup.
            return IntPtr.Zero;
        }

        private bool IsHotkeyAllowedInForegroundWindow()
        {
            if (!CurrentSettings.EnableGlobalHotkeys) return false;

            IntPtr fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero) return true;

            try
            {
                GetWindowThreadProcessId(fgWnd, out uint pid);
                if (pid == 0) return true;

                using (var proc = System.Diagnostics.Process.GetProcessById((int)pid))
                {
                    string procName = proc.ProcessName.ToLower();
                    string title = proc.MainWindowTitle.ToLower();

                    // If user specified blocked process names
                    if (!string.IsNullOrEmpty(CurrentSettings.BlockedProcesses))
                    {
                        string[] blockedList = CurrentSettings.BlockedProcesses.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string item in blockedList)
                        {
                            string term = item.Trim().ToLower();
                            if (term.Length > 0 && (procName.Contains(term) || title.Contains(term)))
                            {
                                return false; // Hotkey is BLOCKED in this foreground app!
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to verify foreground process blocklist: {ex.Message}");
            }

            return true;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_SHOW = 5;

        public static void ForceWindowToForeground(Window window)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                IntPtr targetHwnd = helper.Handle;
                if (targetHwnd == IntPtr.Zero) return;

                IntPtr fgHwnd = GetForegroundWindow();
                if (fgHwnd == IntPtr.Zero || fgHwnd == targetHwnd)
                {
                    window.Activate();
                    window.Focus();
                    return;
                }

                uint fgThreadId = GetWindowThreadProcessId(fgHwnd, out _);
                uint appThreadId = GetCurrentThreadId();

                if (fgThreadId != appThreadId)
                {
                    AttachThreadInput(appThreadId, fgThreadId, true);
                    BringWindowToTop(targetHwnd);
                    ShowWindow(targetHwnd, SW_SHOW);
                    SetForegroundWindow(targetHwnd);
                    AttachThreadInput(appThreadId, fgThreadId, false);
                }
                else
                {
                    BringWindowToTop(targetHwnd);
                    ShowWindow(targetHwnd, SW_SHOW);
                    SetForegroundWindow(targetHwnd);
                }

                window.Activate();
                window.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to force window to foreground: {ex.Message}");
            }
        }

        private void FocusLastActivePopout()
        {
            var win = LastActivePopout;
            if (win != null && win.IsLoaded)
            {
                ForceWindowToForeground(win);
                win.FocusContentBox();
            }
        }

        private void ToggleLastActivePopoutLock()
        {
            var win = LastActivePopout;
            if (win != null && win.IsLoaded)
            {
                win.ToggleLockState();
            }
        }

        private string? _recordingField; // "NewNote", "FocusNote", "ToggleLock"

        private void HotkeyRecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset all other recording buttons Content to their current settings
                HotkeyNewNoteBtn.Content = CurrentSettings.HotkeyNewNote;
                HotkeyFocusNoteBtn.Content = CurrentSettings.HotkeyFocusNote;
                HotkeyToggleLockBtn.Content = CurrentSettings.HotkeyToggleLock;

                if (btn == HotkeyNewNoteBtn) _recordingField = "NewNote";
                else if (btn == HotkeyFocusNoteBtn) _recordingField = "FocusNote";
                else if (btn == HotkeyToggleLockBtn) _recordingField = "ToggleLock";

                btn.Content = "Press keys... (Esc to cancel)";
                this.Focus();
            }
        }

        private void ProcessKeybindRecord(string pressedCombo)
        {
            if (_recordingField == null) return;

            // If user pressed Escape key, cancel recording
            if (pressedCombo.EndsWith("Escape"))
            {
                _recordingField = null;
                HotkeyNewNoteBtn.Content = CurrentSettings.HotkeyNewNote;
                HotkeyFocusNoteBtn.Content = CurrentSettings.HotkeyFocusNote;
                HotkeyToggleLockBtn.Content = CurrentSettings.HotkeyToggleLock;
                return;
            }

            // Check for overlapping key combos!
            string other1 = "";
            string other2 = "";
            if (_recordingField == "NewNote")
            {
                other1 = CurrentSettings.HotkeyFocusNote;
                other2 = CurrentSettings.HotkeyToggleLock;
            }
            else if (_recordingField == "FocusNote")
            {
                other1 = CurrentSettings.HotkeyNewNote;
                other2 = CurrentSettings.HotkeyToggleLock;
            }
            else if (_recordingField == "ToggleLock")
            {
                other1 = CurrentSettings.HotkeyNewNote;
                other2 = CurrentSettings.HotkeyFocusNote;
            }

            if (pressedCombo == other1 || pressedCombo == other2)
            {
                MessageBox.Show("This key combination is already bound to another action! Please choose a different shortcut.", "Duplicate Keybind Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Do not apply, keep waiting for another key combination
            }

            // Save the new keybind
            if (_recordingField == "NewNote")
            {
                CurrentSettings.HotkeyNewNote = pressedCombo;
                HotkeyNewNoteBtn.Content = pressedCombo;
            }
            else if (_recordingField == "FocusNote")
            {
                CurrentSettings.HotkeyFocusNote = pressedCombo;
                HotkeyFocusNoteBtn.Content = pressedCombo;
            }
            else if (_recordingField == "ToggleLock")
            {
                CurrentSettings.HotkeyToggleLock = pressedCombo;
                HotkeyToggleLockBtn.Content = pressedCombo;
            }

            _recordingField = null;
            SaveData();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_recordingField != null)
            {
                e.Handled = true;
                
                var modifiers = System.Windows.Input.Keyboard.Modifiers;
                System.Windows.Input.Key key = (e.Key == System.Windows.Input.Key.System) ? e.SystemKey : e.Key;

                if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                    key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                    key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                    key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
                {
                    return;
                }

                var parts = new List<string>();
                if ((modifiers & System.Windows.Input.ModifierKeys.Control) != 0) parts.Add("Ctrl");
                if ((modifiers & System.Windows.Input.ModifierKeys.Shift) != 0) parts.Add("Shift");
                if ((modifiers & System.Windows.Input.ModifierKeys.Alt) != 0) parts.Add("Alt");
                if ((modifiers & System.Windows.Input.ModifierKeys.Windows) != 0) parts.Add("Win");
                parts.Add(key.ToString());

                ProcessKeybindRecord(string.Join("+", parts));
            }
        }

        private void EnableHotkeys_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (HotkeyNewNoteBtn == null || HotkeyFocusNoteBtn == null || HotkeyToggleLockBtn == null || BlockedProcessesBox == null)
            {
                return;
            }

            bool isEnabled = EnableGlobalHotkeysCheckbox.IsChecked == true;
            HotkeyNewNoteBtn.IsEnabled = isEnabled;
            HotkeyFocusNoteBtn.IsEnabled = isEnabled;
            HotkeyToggleLockBtn.IsEnabled = isEnabled;
            BlockedProcessesBox.IsEnabled = isEnabled;

            // Adjust styling to look dimmed when disabled
            double opacity = isEnabled ? 1.0 : 0.45;
            HotkeyNewNoteBtn.Opacity = opacity;
            HotkeyFocusNoteBtn.Opacity = opacity;
            HotkeyToggleLockBtn.Opacity = opacity;
            BlockedProcessesBox.Opacity = opacity;
        }

        private void AddNewCardAndPopOut()
        {
            if (ActiveWorkspace == null) return;

            var newCard = new StoryBoardCard
            {
                Title = "Quick Note",
                Content = "",
                CardTag = "",
                FontFamilyName = "Segoe UI",
                ScaleFactor = 1.0,
                ContentOpacity = 1.0,
                BackgroundImagePath = ""
            };

            ActiveWorkspace.Cards.Add(newCard);
            SaveData();

            var win = new SingleNoteWindow(newCard);
            win.Show();

            // Force window to foreground focus
            ForceWindowToForeground(win);
            win.FocusContentBox();
        }

        protected override void OnClosed(EventArgs e)
        {
            UnregisterAllGlobalHotkeys();
            base.OnClosed(e);
        }
        #endregion

        private void LoadData()
        {
            try
            {
                // 1. Load Settings
                string settingsPath = GetSettingsFilePath();
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    CurrentSettings = new AppSettings();
                }

                // Force drag and drop to false per user request
                CurrentSettings.EnableDragDrop = false;

                // Apply Software Rendering if checked
                if (CurrentSettings.DisableHardwareAcceleration)
                {
                    RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                }

                if (CurrentSettings.HotkeyNewNote == null) CurrentSettings.HotkeyNewNote = "Ctrl+Shift+N";
                if (CurrentSettings.HotkeyFocusNote == null) CurrentSettings.HotkeyFocusNote = "Ctrl+Shift+F";
                if (CurrentSettings.HotkeyToggleLock == null) CurrentSettings.HotkeyToggleLock = "Ctrl+Shift+K";

                // Apply settings to UI
                EnableAiCheckbox.IsChecked = CurrentSettings.EnableAiAssistant;
                EnableDragDropCheckbox.IsChecked = false; // Force visual unchecked
                DisableHardwareAccelerationCheckbox.IsChecked = CurrentSettings.DisableHardwareAcceleration;
                SettingsApiKeyBox.Password = CurrentSettings.ApiKey;
                ApiKeyBox.Password = CurrentSettings.ApiKey;
                ApplyAiPanelVisibility(CurrentSettings.EnableAiAssistant);
                
                // Apply the saved Color Theme
                ApplyTheme(CurrentSettings.ThemeName);

                // 2. Load Workspaces
                string workspacesPath = GetWorkspacesFilePath();
                if (File.Exists(workspacesPath))
                {
                    string json = File.ReadAllText(workspacesPath);
                    Workspaces = JsonSerializer.Deserialize<ObservableCollection<Workspace>>(json) ?? new ObservableCollection<Workspace>();
                }
                else
                {
                    Workspaces = new ObservableCollection<Workspace>();
                }

                foreach (var ws in Workspaces)
                {
                    if (ws.AvailableTags == null)
                    {
                        ws.AvailableTags = new ObservableCollection<string>();
                    }
                    if (ws.Cards == null)
                    {
                        ws.Cards = new ObservableCollection<StoryBoardCard>();
                    }
                    else
                    {
                        foreach (var card in ws.Cards)
                        {
                            if (card.CardTag == null) card.CardTag = "";
                        }
                    }
                }

                // If no workspaces loaded, seed with default structure
                if (Workspaces.Count == 0)
                {
                    var defaultWs = new Workspace { Name = "Default Workspace" };
                    defaultWs.Cards.Add(new StoryBoardCard 
                    { 
                        Title = "1. Introduction", 
                        Content = "Establish the main protagonist, Leo, a young watchmaker who discovers a pocket watch that can freeze time for 10 seconds." 
                    });
                    defaultWs.Cards.Add(new StoryBoardCard 
                    { 
                        Title = "2. Inciting Incident", 
                        Content = "Leo uses the watch to save someone from an accident, but realizes that every time he uses it, he ages by one day." 
                    });
                    defaultWs.Cards.Add(new StoryBoardCard 
                    { 
                        Title = "3. Rising Action", 
                        Content = "A mysterious organization discovers Leo's watch and begins pursuing him to steal the time-manipulation technology." 
                    });
                    Workspaces.Add(defaultWs);
                }

                // Determine which workspace is active
                var active = Workspaces.FirstOrDefault(w => w.Name == CurrentSettings.ActiveWorkspaceName) ?? Workspaces[0];
                ActiveWorkspace = active;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CurrentSettings = new AppSettings();
                CurrentSettings.EnableDragDrop = false; // Force drag and drop to false
                Workspaces = new ObservableCollection<Workspace> { new Workspace { Name = "Default Workspace" } };
                ActiveWorkspace = Workspaces[0];
            }
        }

        public void SaveData()
        {
            try
            {
                // Update settings values before serialization
                if (CurrentSettings != null)
                {
                    CurrentSettings.ApiKey = ApiKeyBox.Password.Trim();
                    CurrentSettings.ActiveWorkspaceName = ActiveWorkspace?.Name ?? "Default Workspace";
                    
                    string settingsPath = GetSettingsFilePath();
                    string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(settingsPath, json);
                }

                // Save workspaces
                if (Workspaces != null)
                {
                    string workspacesPath = GetWorkspacesFilePath();
                    string json = JsonSerializer.Serialize(Workspaces, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(workspacesPath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save data: {ex.Message}");
            }
        }
        #endregion

        #region Settings Panel Handlers
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            EnableAiCheckbox.IsChecked = CurrentSettings.EnableAiAssistant;
            EnableDragDropCheckbox.IsChecked = CurrentSettings.EnableDragDrop;
            SettingsApiKeyBox.Password = CurrentSettings.ApiKey;

            // Populate hotkey config
            EnableGlobalHotkeysCheckbox.IsChecked = CurrentSettings.EnableGlobalHotkeys;
            HotkeyNewNoteBtn.Content = CurrentSettings.HotkeyNewNote;
            HotkeyFocusNoteBtn.Content = CurrentSettings.HotkeyFocusNote;
            HotkeyToggleLockBtn.Content = CurrentSettings.HotkeyToggleLock;
            BlockedProcessesBox.Text = CurrentSettings.BlockedProcesses ?? "";

            // Populate hardware acceleration
            DisableHardwareAccelerationCheckbox.IsChecked = CurrentSettings.DisableHardwareAcceleration;

            // Adjust disabled states
            EnableHotkeys_CheckedChanged(this, new RoutedEventArgs());

            // Set selected theme in ComboBox
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Content.ToString() == CurrentSettings.ThemeName)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            CurrentSettings.EnableAiAssistant = EnableAiCheckbox.IsChecked == true;
            CurrentSettings.EnableDragDrop = false; // Always force disable drag & drop
            CurrentSettings.ApiKey = SettingsApiKeyBox.Password.Trim();
            
            // Save hotkey settings
            CurrentSettings.EnableGlobalHotkeys = EnableGlobalHotkeysCheckbox.IsChecked == true;
            CurrentSettings.HotkeyNewNote = HotkeyNewNoteBtn.Content.ToString() ?? "Ctrl+Shift+N";
            CurrentSettings.HotkeyFocusNote = HotkeyFocusNoteBtn.Content.ToString() ?? "Ctrl+Shift+F";
            CurrentSettings.HotkeyToggleLock = HotkeyToggleLockBtn.Content.ToString() ?? "Ctrl+Shift+K";
            CurrentSettings.BlockedProcesses = BlockedProcessesBox.Text.Trim();

            // Save hardware rendering setting
            CurrentSettings.DisableHardwareAcceleration = DisableHardwareAccelerationCheckbox.IsChecked == true;
            if (CurrentSettings.DisableHardwareAcceleration)
            {
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }
            else
            {
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
            }

            // Re-register hotkeys immediately
            RegisterAllGlobalHotkeys();

            // Read and apply Theme setting
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedThemeItem)
            {
                CurrentSettings.ThemeName = selectedThemeItem.Content.ToString() ?? "Midnight Purple";
                ApplyTheme(CurrentSettings.ThemeName);
            }

            // Apply updates
            ApiKeyBox.Password = CurrentSettings.ApiKey;
            ApplyAiPanelVisibility(CurrentSettings.EnableAiAssistant);

            SaveData();
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void ResetFlyoutScale_Click(object sender, RoutedEventArgs e)
        {
            // Reset the scale factor of the editing card
            if (_activeEditingCard != null)
            {
                _activeEditingCard.ScaleFactor = 1.0;
            }

            // Reset the flyout border dimensions to defaults (Width=780, Height=580)
            var border = CardDetailOverlay.Children[0] as Border;
            if (border != null)
            {
                border.Width = 780;
                border.Height = 580;
            }
        }

        private void ApplyTheme(string themeName)
        {
            Color primaryColor;
            Color secBgColor;
            Color secBorderColor;
            Color secTextColor;

            switch (themeName)
            {
                case "Neon Teal":
                    primaryColor = (Color)ColorConverter.ConvertFromString("#00bfa5");
                    secBgColor = (Color)ColorConverter.ConvertFromString("#102522");
                    secBorderColor = (Color)ColorConverter.ConvertFromString("#1d473e");
                    secTextColor = (Color)ColorConverter.ConvertFromString("#00ffcc");
                    break;
                case "Sunset Orange":
                    primaryColor = (Color)ColorConverter.ConvertFromString("#ff6d00");
                    secBgColor = (Color)ColorConverter.ConvertFromString("#2b1b10");
                    secBorderColor = (Color)ColorConverter.ConvertFromString("#5c381c");
                    secTextColor = (Color)ColorConverter.ConvertFromString("#ffab40");
                    break;
                case "Forest Green":
                    primaryColor = (Color)ColorConverter.ConvertFromString("#00c853");
                    secBgColor = (Color)ColorConverter.ConvertFromString("#102515");
                    secBorderColor = (Color)ColorConverter.ConvertFromString("#1c4c28");
                    secTextColor = (Color)ColorConverter.ConvertFromString("#69f0ae");
                    break;
                case "Cyberpunk Pink":
                    primaryColor = (Color)ColorConverter.ConvertFromString("#ff007f");
                    secBgColor = (Color)ColorConverter.ConvertFromString("#2c0a1a");
                    secBorderColor = (Color)ColorConverter.ConvertFromString("#5e163b");
                    secTextColor = (Color)ColorConverter.ConvertFromString("#ff66b2");
                    break;
                case "Midnight Purple":
                default:
                    primaryColor = (Color)ColorConverter.ConvertFromString("#5c2dd5");
                    secBgColor = (Color)ColorConverter.ConvertFromString("#16122c");
                    secBorderColor = (Color)ColorConverter.ConvertFromString("#3a2a6b");
                    secTextColor = (Color)ColorConverter.ConvertFromString("#b39ddb");
                    break;
            }

            this.Resources["PrimaryAccentBrush"] = new SolidColorBrush(primaryColor);
            this.Resources["SecondaryAccentBGBrush"] = new SolidColorBrush(secBgColor);
            this.Resources["SecondaryAccentBorderBrush"] = new SolidColorBrush(secBorderColor);
            this.Resources["SecondaryAccentTextBrush"] = new SolidColorBrush(secTextColor);
        }

        private void ApplyAiPanelVisibility(bool enabled)
        {
            if (enabled)
            {
                DividerPanel.Visibility = Visibility.Visible;
                ChatPanel.Visibility = Visibility.Visible;
            }
            else
            {
                DividerPanel.Visibility = Visibility.Collapsed;
                ChatPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveKey_Click(object sender, RoutedEventArgs e)
        {
            CurrentSettings.ApiKey = ApiKeyBox.Password.Trim();
            SaveData();
            MessageBox.Show("Gemini API Key saved successfully!", "Key Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Workspace Actions
        private void WorkspaceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveWorkspace != null)
            {
                SaveData(); // Auto save old workspace state
            }
        }

        private void AddWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Enter new workspace name:", "New Workspace");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                string name = dialog.InputText.Trim();
                if (string.IsNullOrEmpty(name)) return;

                if (Workspaces.Any(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    var err = new ConfirmDialog("A workspace with that name already exists.", "Workspace Exists", "OK", "Cancel");
                    err.Owner = this;
                    err.ShowDialog();
                    return;
                }

                var newWs = new Workspace { Name = name };
                Workspaces.Add(newWs);
                ActiveWorkspace = newWs;
                SaveData();
            }
        }

        private void DeleteWorkspace_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveWorkspace == null) return;

            if (Workspaces.Count <= 1)
            {
                var err = new ConfirmDialog("You must keep at least one workspace.", "Cannot Delete", "OK", "Cancel");
                err.Owner = this;
                err.ShowDialog();
                return;
            }

            var dialog = new ConfirmDialog($"Are you sure you want to delete workspace '{ActiveWorkspace.Name}' and all its note cards?", "Confirm Workspace Delete");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var toRemove = ActiveWorkspace;
                int index = Workspaces.IndexOf(toRemove);
                int nextIndex = index == 0 ? 1 : index - 1;

                ActiveWorkspace = Workspaces[nextIndex];
                Workspaces.Remove(toRemove);
                SaveData();
            }
        }
        #endregion

        #region Card Actions
        private void AddCard_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveWorkspace != null)
            {
                ActiveWorkspace.Cards.Add(new StoryBoardCard 
                { 
                    Title = $"New Card {ActiveWorkspace.Cards.Count + 1}", 
                    Content = "Type your scene notes here..." 
                });
                SaveData();
            }
        }

        private void DeleteCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is StoryBoardCard card)
            {
                var dialog = new ConfirmDialog($"Are you sure you want to delete card '{card.Title}'?", "Confirm Card Delete");
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    ActiveWorkspace?.Cards.Remove(card);
                    SaveData();
                }
            }
        }

        private void MoveCardLeft_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is StoryBoardCard card && ActiveWorkspace != null)
            {
                int index = ActiveWorkspace.Cards.IndexOf(card);
                if (index > 0)
                {
                    ActiveWorkspace.Cards.Move(index, index - 1);
                    SaveData();
                }
            }
        }

        private void MoveCardRight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is StoryBoardCard card && ActiveWorkspace != null)
            {
                int index = ActiveWorkspace.Cards.IndexOf(card);
                if (index >= 0 && index < ActiveWorkspace.Cards.Count - 1)
                {
                    ActiveWorkspace.Cards.Move(index, index + 1);
                    SaveData();
                }
            }
        }
        #endregion

        #region Drag and Drop Reordering
        private Point _dragStartPoint;
        private FrameworkElement? _dragSourceElement;

        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Skip if drag-and-drop is disabled in settings
            if (!CurrentSettings.EnableDragDrop) return;

            // Ignore if clicking editable text controls or buttons
            DependencyObject? obj = e.OriginalSource as DependencyObject;
            while (obj != null && obj != sender)
            {
                if (obj is TextBox || obj is Button || obj is ComboBox || obj is PasswordBox)
                {
                    return;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }

            _dragStartPoint = e.GetPosition(null);
        }

        private void Card_MouseMove(object sender, MouseEventArgs e)
        {
            // Skip if drag-and-drop is disabled in settings
            if (!CurrentSettings.EnableDragDrop) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is FrameworkElement element && element.DataContext is StoryBoardCard card)
                    {
                        // Visual feedback: fade the source card while dragging
                        _dragSourceElement = element;
                        element.Opacity = 0.5;

                        DragDrop.DoDragDrop(element, card, DragDropEffects.Move);

                        // Restore after drag completes (drop or cancel)
                        element.Opacity = 1.0;
                        _dragSourceElement = null;
                    }
                }
            }
        }

        private void Card_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(StoryBoardCard)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Card_DragEnter(object sender, DragEventArgs e)
        {
            // Scale up the target card when a dragged card hovers over it
            if (sender is Border border && border != _dragSourceElement &&
                e.Data.GetDataPresent(typeof(StoryBoardCard)))
            {
                if (border.RenderTransform is ScaleTransform scale)
                {
                    scale.ScaleX = 1.05;
                    scale.ScaleY = 1.05;
                }
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5a5a6a"));
            }
        }

        private void Card_DragLeave(object sender, DragEventArgs e)
        {
            // Reset the target card when the dragged card leaves
            if (sender is Border border)
            {
                if (border.RenderTransform is ScaleTransform scale)
                {
                    scale.ScaleX = 1.0;
                    scale.ScaleY = 1.0;
                }
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2d3d"));
            }
        }

        private void Card_Drop(object sender, DragEventArgs e)
        {
            // Reset visual feedback on the drop target
            if (sender is Border border)
            {
                if (border.RenderTransform is ScaleTransform scale)
                {
                    scale.ScaleX = 1.0;
                    scale.ScaleY = 1.0;
                }
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2d3d"));
            }

            if (e.Data.GetDataPresent(typeof(StoryBoardCard)))
            {
                var sourceCard = e.Data.GetData(typeof(StoryBoardCard)) as StoryBoardCard;
                var targetCard = (sender as FrameworkElement)?.DataContext as StoryBoardCard;

                if (sourceCard != null && targetCard != null && sourceCard != targetCard)
                {
                    // Defer the move so it executes after DoDragDrop returns,
                    // preventing a visual tree rebuild while the drag is still active.
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var cardsCollection = ActiveWorkspace?.Cards;
                        if (cardsCollection != null)
                        {
                            int sourceIndex = cardsCollection.IndexOf(sourceCard);
                            int targetIndex = cardsCollection.IndexOf(targetCard);

                            if (sourceIndex >= 0 && targetIndex >= 0)
                            {
                                cardsCollection.Move(sourceIndex, targetIndex);
                                SaveData();
                            }
                        }
                    }));
                }
            }
        }
        #endregion

        #region Gemini Chat Logic
        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            string prompt = ChatInputBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            string apiKey = ApiKeyBox.Password.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                ChatHistoryBox.Text += "System: [Error: Please input your Gemini API Key in Settings or the key box before sending messages.]\n\n";
                ChatHistoryBox.ScrollToEnd();
                return;
            }

            ChatHistoryBox.Text += $"User: {prompt}\n\n";
            ChatInputBox.Text = string.Empty;
            ChatHistoryBox.ScrollToEnd();

            SendBtn.IsEnabled = false;
            ChatInputBox.IsEnabled = false;

            ChatHistoryBox.Text += "Gemini AI: ";
            ChatHistoryBox.ScrollToEnd();

            try
            {
                StringBuilder contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("You are a helpful Gemini AI integrated into NoticeBoard, a notes and story planning application.");
                contextBuilder.AppendLine("Here is the current context of the user's active workspace cards:");
                contextBuilder.AppendLine("==========================================");
                if (ActiveWorkspace != null)
                {
                    foreach (var card in ActiveWorkspace.Cards)
                    {
                        contextBuilder.AppendLine($"[Card: {card.Title}]");
                        contextBuilder.AppendLine(card.Content);
                        contextBuilder.AppendLine("------------------------------------------");
                    }
                }
                contextBuilder.AppendLine("==========================================");
                contextBuilder.AppendLine("Based on the storyboard context above, please answer the following user question:");
                contextBuilder.AppendLine(prompt);

                string fullPrompt = contextBuilder.ToString();

                var googleAI = new GoogleAI(apiKey: apiKey);
                var model = googleAI.GenerativeModel(model: "gemini-1.5-flash");

                var responseStream = model.GenerateContentStream(fullPrompt);
                await foreach (var chunk in responseStream)
                {
                    if (chunk != null && !string.IsNullOrEmpty(chunk.Text))
                    {
                        ChatHistoryBox.Text += chunk.Text;
                        ChatHistoryBox.ScrollToEnd();
                    }
                }
                ChatHistoryBox.Text += "\n\n";
            }
            catch (Exception ex)
            {
                ChatHistoryBox.Text += $"\n[Error: {ex.Message}]\n\n";
            }
            finally
            {
                SendBtn.IsEnabled = true;
                ChatInputBox.IsEnabled = true;
                ChatInputBox.Focus();
                ChatHistoryBox.ScrollToEnd();
            }
        }

        private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                SendBtn_Click(sender, e);
            }
        }

        private void ChatInputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PlaceholderText != null)
            {
                PlaceholderText.Visibility = string.IsNullOrEmpty(ChatInputBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void TogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (ChatPanel.Visibility == Visibility.Visible)
            {
                ChatPanel.Visibility = Visibility.Collapsed;
                TogglePanelBtn.Content = "‹";
            }
            else
            {
                ChatPanel.Visibility = Visibility.Visible;
                TogglePanelBtn.Content = "›";
            }
        }
        #endregion

        #region Property Changed Notification
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Tag Management Handlers
        private void Tags_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveWorkspace == null) return;
            TagsListControl.ItemsSource = ActiveWorkspace.AvailableTags;
            TagsOverlay.Visibility = Visibility.Visible;
            NewTagTextBox.Text = "";
            NewTagTextBox.Focus();
        }

        private void CloseTags_Click(object sender, RoutedEventArgs e)
        {
            TagsOverlay.Visibility = Visibility.Collapsed;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveWorkspace == null) return;
            string text = NewTagTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Prepend '#' if it doesn't already start with it
            if (!text.StartsWith("#"))
            {
                text = "#" + text;
            }

            if (ActiveWorkspace.AvailableTags.Contains(text))
            {
                MessageBox.Show("Tag already exists.", "Duplicate Tag", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ActiveWorkspace.AvailableTags.Add(text);
            NewTagTextBox.Text = "";
            NewTagTextBox.Focus();
            SaveData();
        }

        private void DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveWorkspace == null || sender is not Button button || button.Tag is not string tagToDelete) return;

            var dialog = new ConfirmDialog($"Are you sure you want to delete tag '{tagToDelete}'? This will remove it from all cards using it.", "Delete Tag");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                // Remove from workspace's available tags
                ActiveWorkspace.AvailableTags.Remove(tagToDelete);

                // Clear from all cards that have this tag
                foreach (var card in ActiveWorkspace.Cards)
                {
                    if (card.CardTag == tagToDelete)
                    {
                        card.CardTag = "";
                    }
                }

                // If the deleted tag was the active filter, reset it
                if (SelectedFilterTag == tagToDelete)
                {
                    SelectedFilterTag = "All Tags";
                }

                SaveData();
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterComboBox != null && FilterComboBox.SelectedItem is string selectedTag)
            {
                SelectedFilterTag = selectedTag;
            }
        }
        #endregion

        #region Card Details & Images & Sharing Handlers
        private void EditCardDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StoryBoardCard card)
            {
                _activeEditingCard = card;
                CardDetailOverlay.DataContext = card;
                CardDetailOverlay.Visibility = Visibility.Visible;
            }
        }

        private void CloseCardDetail_Click(object sender, RoutedEventArgs e)
        {
            CardDetailOverlay.Visibility = Visibility.Collapsed;
            _activeEditingCard = null;
            SaveData();
        }

        private void ColorCircle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && _activeEditingCard != null)
            {
                _activeEditingCard.BorderColor = btn.Tag?.ToString() ?? "";
                SaveData();
            }
        }

        private void TextColorCircle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && _activeEditingCard != null)
            {
                _activeEditingCard.ForegroundColor = btn.Tag?.ToString() ?? "";
                SaveData();
            }
        }

        private string GetCardImagesDirectory()
        {
            string dir = Path.Combine(GetAppDataDirectory(), "Card Images");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        private void AddCardImage_Click(object sender, RoutedEventArgs e)
        {
            if (_activeEditingCard == null) return;

            var openDlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp"
            };

            if (openDlg.ShowDialog() == true)
            {
                try
                {
                    string destDir = GetCardImagesDirectory();
                    string fileExt = Path.GetExtension(openDlg.FileName);
                    string newFileName = Guid.NewGuid().ToString() + fileExt;
                    string destPath = Path.Combine(destDir, newFileName);

                    File.Copy(openDlg.FileName, destPath, true);
                    _activeEditingCard.Images.Add(destPath);
                    SaveData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteCardImage_Click(object sender, RoutedEventArgs e)
        {
            if (_activeEditingCard == null || sender is not Button btn || btn.Tag is not string imgPath) return;

            var dialog = new ConfirmDialog("Are you sure you want to remove this image?", "Remove Image");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _activeEditingCard.Images.Remove(imgPath);
                
                // Try to delete the file from local storage if no other card is referencing it
                try
                {
                    bool isReferenced = false;
                    foreach (var ws in Workspaces)
                    {
                        foreach (var card in ws.Cards)
                        {
                            if (card != _activeEditingCard && card.Images.Contains(imgPath))
                            {
                                isReferenced = true;
                                break;
                            }
                        }
                        if (isReferenced) break;
                    }

                    if (!isReferenced && File.Exists(imgPath))
                    {
                        File.Delete(imgPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to clean up image file: {ex.Message}");
                }

                SaveData();
            }
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

        private void BackgroundDefault_Click(object sender, RoutedEventArgs e)
        {
            if (_activeEditingCard != null)
            {
                _activeEditingCard.BackgroundImagePath = "";
                SaveData();
            }
        }

        private void BackgroundParchment_Click(object sender, RoutedEventArgs e)
        {
            if (_activeEditingCard != null)
            {
                _activeEditingCard.BackgroundImagePath = "parchment";
                SaveData();
            }
        }

        private void BackgroundUpload_Click(object sender, RoutedEventArgs e)
        {
            if (_activeEditingCard == null) return;

            var dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string dir = Path.Combine(GetAppDataDirectory(), "Card Backgrounds");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string ext = Path.GetExtension(dialog.FileName);
                    string uniqueName = $"{Guid.NewGuid()}{ext}";
                    string destPath = Path.Combine(dir, uniqueName);

                    File.Copy(dialog.FileName, destPath, true);
                    _activeEditingCard.BackgroundImagePath = destPath;
                    SaveData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to upload background: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PopOutCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StoryBoardCard card)
            {
                var win = new SingleNoteWindow(card);
                win.Show();
            }
        }

        private void ExportCard_Click(object sender, RoutedEventArgs e)
        {
            if (_activeEditingCard == null) return;

            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "NoticeBoard Card (*.noticecard)|*.noticecard",
                FileName = $"{CleanFileName(_activeEditingCard.Title)}.noticecard"
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);

                    // Create card export model with relative image filenames
                    var exportCard = new ExportCardModel
                    {
                        Title = _activeEditingCard.Title,
                        Content = _activeEditingCard.Content,
                        CardTag = _activeEditingCard.CardTag,
                        BorderColor = _activeEditingCard.BorderColor,
                        FontFamilyName = _activeEditingCard.FontFamilyName,
                        ScaleFactor = _activeEditingCard.ScaleFactor,
                        ContentOpacity = _activeEditingCard.ContentOpacity,
                        ImageNames = new List<string>()
                    };

                    // Handle background image export
                    if (_activeEditingCard.BackgroundImagePath == "parchment")
                    {
                        exportCard.BackgroundImagePath = "parchment";
                    }
                    else if (!string.IsNullOrEmpty(_activeEditingCard.BackgroundImagePath) && File.Exists(_activeEditingCard.BackgroundImagePath))
                    {
                        string bgFileName = "bg_" + Path.GetFileName(_activeEditingCard.BackgroundImagePath);
                        string destBgPath = Path.Combine(tempDir, bgFileName);
                        File.Copy(_activeEditingCard.BackgroundImagePath, destBgPath, true);
                        exportCard.BackgroundImagePath = bgFileName;
                        exportCard.ImageNames.Add(bgFileName);
                    }
                    else
                    {
                        exportCard.BackgroundImagePath = "";
                    }

                    foreach (var imgPath in _activeEditingCard.Images)
                    {
                        if (File.Exists(imgPath))
                        {
                            string fileName = Path.GetFileName(imgPath);
                            string destPath = Path.Combine(tempDir, fileName);
                            File.Copy(imgPath, destPath, true);
                            exportCard.ImageNames.Add(fileName);
                        }
                    }

                    string json = JsonSerializer.Serialize(exportCard, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(tempDir, "card.json"), json);

                    if (File.Exists(saveDlg.FileName))
                    {
                        File.Delete(saveDlg.FileName);
                    }

                    ZipFile.CreateFromDirectory(tempDir, saveDlg.FileName);
                    Directory.Delete(tempDir, true);

                    MessageBox.Show("Card shared successfully!", "Export Card", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export card: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportCard_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveWorkspace == null)
            {
                MessageBox.Show("Please select or create a workspace first.", "Import Card", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openDlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "NoticeBoard Card (*.noticecard)|*.noticecard"
            };

            if (openDlg.ShowDialog() == true)
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                    // Zip Slip Protection during extraction
                    using (ZipArchive archive = ZipFile.OpenRead(openDlg.FileName))
                    {
                        string targetDir = Path.GetFullPath(tempDir);
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string destPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
                            if (!destPath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException("Malicious path detected in import package (Zip Slip).");
                            }
                        }

                        Directory.CreateDirectory(tempDir);
                        archive.ExtractToDirectory(tempDir);
                    }

                    string jsonPath = Path.Combine(tempDir, "card.json");
                    if (!File.Exists(jsonPath))
                    {
                        MessageBox.Show("Invalid card archive: card.json missing.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string json = File.ReadAllText(jsonPath);
                    var exportCard = JsonSerializer.Deserialize<ExportCardModel>(json);
                    if (exportCard == null)
                    {
                        MessageBox.Show("Failed to parse card data.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var newCard = new StoryBoardCard
                    {
                        Title = exportCard.Title,
                        Content = exportCard.Content,
                        CardTag = exportCard.CardTag,
                        BorderColor = exportCard.BorderColor,
                        FontFamilyName = exportCard.FontFamilyName ?? "Segoe UI",
                        ScaleFactor = exportCard.ScaleFactor > 0 ? exportCard.ScaleFactor : 1.0,
                        ContentOpacity = exportCard.ContentOpacity > 0 ? exportCard.ContentOpacity : 1.0
                    };

                    // Handle background image import
                    if (exportCard.BackgroundImagePath == "parchment")
                    {
                        newCard.BackgroundImagePath = "parchment";
                    }
                    else if (!string.IsNullOrEmpty(exportCard.BackgroundImagePath) && exportCard.BackgroundImagePath.StartsWith("bg_"))
                    {
                        string cleanBgName = Path.GetFileName(exportCard.BackgroundImagePath);
                        string srcBgPath = Path.Combine(tempDir, cleanBgName);
                        if (File.Exists(srcBgPath))
                        {
                            string bgDir = Path.Combine(GetAppDataDirectory(), "Card Backgrounds");
                            if (!Directory.Exists(bgDir)) Directory.CreateDirectory(bgDir);

                            string newBgName = Guid.NewGuid().ToString() + Path.GetExtension(cleanBgName);
                            string destBgPath = Path.Combine(bgDir, newBgName);
                            File.Copy(srcBgPath, destBgPath, true);
                            newCard.BackgroundImagePath = destBgPath;
                        }
                    }
                    else
                    {
                        newCard.BackgroundImagePath = "";
                    }

                    string destImagesDir = GetCardImagesDirectory();

                    foreach (var imgName in exportCard.ImageNames)
                    {
                        // Skip if it's the background image (already handled)
                        if (imgName.StartsWith("bg_")) continue;

                        string cleanImgName = Path.GetFileName(imgName);
                        string srcPath = Path.Combine(tempDir, cleanImgName);
                        if (File.Exists(srcPath))
                        {
                            string newFileName = Guid.NewGuid().ToString() + Path.GetExtension(cleanImgName);
                            string destPath = Path.Combine(destImagesDir, newFileName);
                            File.Copy(srcPath, destPath, true);
                            newCard.Images.Add(destPath);
                        }
                    }

                    Directory.Delete(tempDir, true);

                    // If the card tag is not present in active workspace available tags, add it
                    if (!string.IsNullOrEmpty(newCard.CardTag) && !ActiveWorkspace.AvailableTags.Contains(newCard.CardTag))
                    {
                        ActiveWorkspace.AvailableTags.Add(newCard.CardTag);
                    }

                    ActiveWorkspace.Cards.Add(newCard);
                    SaveData();

                    MessageBox.Show($"Card '{newCard.Title}' imported successfully!", "Import Card", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import card: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string CleanFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
        private void FlyoutResize_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var border = CardDetailOverlay.Children[0] as Border;
            if (border != null)
            {
                double newWidth = border.Width;
                double newHeight = border.Height;

                if (double.IsNaN(newWidth)) newWidth = 780;
                if (double.IsNaN(newHeight)) newHeight = 580;

                newWidth += e.HorizontalChange;
                newHeight += e.VerticalChange;

                if (newWidth >= 500) border.Width = newWidth;
                if (newHeight >= 400) border.Height = newHeight;
            }
        }
        #endregion
    }

    #region Custom Input Dialog Helper
    public class InputDialog : Window
    {
        private TextBox _inputBox;
        public string InputText => _inputBox.Text;

        public InputDialog(string prompt, string title = "Enter Name", string defaultVal = "")
        {
            Title = title;
            Width = 340;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a24")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a3a4c")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = prompt,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13.5,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            _inputBox = new TextBox
            {
                Text = defaultVal,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1e28")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a3a4c")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                CaretBrush = Brushes.White
            };
            Grid.SetRow(_inputBox, 1);
            grid.Children.Add(_inputBox);

            var buttonGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Height = 30,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22222c")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0b5")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a3a4c")),
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.SemiBold
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            Grid.SetColumn(cancelBtn, 0);
            buttonGrid.Children.Add(cancelBtn);

            var confirmBtn = new Button
            {
                Content = "OK",
                Height = 30,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5c2dd5")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold
            };
            confirmBtn.Click += (s, e) => { DialogResult = true; Close(); };
            Grid.SetColumn(confirmBtn, 2);
            buttonGrid.Children.Add(confirmBtn);

            Grid.SetRow(buttonGrid, 2);
            grid.Children.Add(buttonGrid);

            border.Child = grid;
            Content = border;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left) DragMove();
            };

            Loaded += (s, e) => { _inputBox.Focus(); _inputBox.SelectAll(); };
        }
    }
    #endregion

    #region Custom Converters
    public class BorderColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? hex = value as string;
            if (string.IsNullOrEmpty(hex))
                return DependencyProperty.UnsetValue;
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BackgroundImageConverter : System.Windows.Data.IValueConverter
    {
        private static readonly System.Collections.Generic.Dictionary<string, ImageBrush> _brushCache = 
            new System.Collections.Generic.Dictionary<string, ImageBrush>();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? path = value as string;
            if (string.IsNullOrEmpty(path))
                return DependencyProperty.UnsetValue;

            try
            {
                string actualPath = path;
                if (path == "parchment")
                {
                    string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoticeBoard");
                    actualPath = Path.Combine(appData, "Textures", "parchment.jpg");
                }

                lock (_brushCache)
                {
                    if (_brushCache.TryGetValue(actualPath, out var cachedBrush))
                    {
                        return cachedBrush;
                    }
                }

                if (File.Exists(actualPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(actualPath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze(); 

                    var brush = new ImageBrush(bitmap);
                    if (path == "parchment")
                    {
                        brush.TileMode = TileMode.Tile;
                        brush.ViewportUnits = BrushMappingMode.Absolute;
                        brush.Viewport = new Rect(0, 0, 256, 256);
                    }
                    else
                    {
                        brush.Stretch = Stretch.UniformToFill;
                    }

                    brush.Freeze(); 

                    lock (_brushCache)
                    {
                        _brushCache[actualPath] = brush;
                    }
                    return brush;
                }
            }
            catch
            {
                // ignore
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int count && count > 0)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Export Models
    public class ExportCardModel
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string CardTag { get; set; } = "";
        public string BorderColor { get; set; } = "";
        public List<string> ImageNames { get; set; } = new List<string>();
        public string BackgroundImagePath { get; set; } = "";
        public string FontFamilyName { get; set; } = "Segoe UI";
        public double ScaleFactor { get; set; } = 1.0;
        public double ContentOpacity { get; set; } = 1.0;
    }
    #endregion
}