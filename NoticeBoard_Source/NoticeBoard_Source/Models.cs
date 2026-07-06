using System.Collections.ObjectModel;
using System.ComponentModel;

namespace StoryBoardAI
{
    public class StoryBoardCard : INotifyPropertyChanged
    {
        private string _title = "New Card";
        private string _content = "";
        private string _cardTag = "";
        private bool _isDimmed = false;
        private string _borderColor = ""; // Hex code, e.g. #ff0000, empty means default
        private ObservableCollection<string> _images = new ObservableCollection<string>();
        private string _backgroundImagePath = ""; // empty, "parchment", or file path
        private string _fontFamilyName = "Segoe UI";
        private double _scaleFactor = 1.0;
        private double _contentOpacity = 1.0;
        private bool _isPinned = false;
        private string _foregroundColor = ""; // Hex code, empty means default

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        public string CardTag
        {
            get => _cardTag;
            set
            {
                if (_cardTag != value)
                {
                    _cardTag = value;
                    OnPropertyChanged(nameof(CardTag));
                }
            }
        }

        public bool IsDimmed
        {
            get => _isDimmed;
            set
            {
                if (_isDimmed != value)
                {
                    _isDimmed = value;
                    OnPropertyChanged(nameof(IsDimmed));
                }
            }
        }

        public string BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    OnPropertyChanged(nameof(BorderColor));
                }
            }
        }

        public ObservableCollection<string> Images
        {
            get => _images;
            set
            {
                _images = value;
                OnPropertyChanged(nameof(Images));
            }
        }

        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set
            {
                if (_backgroundImagePath != value)
                {
                    _backgroundImagePath = value;
                    OnPropertyChanged(nameof(BackgroundImagePath));
                }
            }
        }

        public string FontFamilyName
        {
            get => _fontFamilyName;
            set
            {
                if (_fontFamilyName != value)
                {
                    _fontFamilyName = value;
                    OnPropertyChanged(nameof(FontFamilyName));
                }
            }
        }

        public double ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (_scaleFactor != value)
                {
                    _scaleFactor = value;
                    OnPropertyChanged(nameof(ScaleFactor));
                }
            }
        }

        public double ContentOpacity
        {
            get => _contentOpacity;
            set
            {
                if (_contentOpacity != value)
                {
                    _contentOpacity = value;
                    OnPropertyChanged(nameof(ContentOpacity));
                }
            }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    OnPropertyChanged(nameof(IsPinned));
                }
            }
        }

        public string ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    OnPropertyChanged(nameof(ForegroundColor));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Workspace : INotifyPropertyChanged
    {
        private string _name = "Default Workspace";
        private ObservableCollection<StoryBoardCard> _cards = new ObservableCollection<StoryBoardCard>();
        private ObservableCollection<string> _availableTags = new ObservableCollection<string>();

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public ObservableCollection<StoryBoardCard> Cards
        {
            get => _cards;
            set
            {
                _cards = value;
                OnPropertyChanged(nameof(Cards));
            }
        }

        public ObservableCollection<string> AvailableTags
        {
            get => _availableTags;
            set
            {
                _availableTags = value;
                OnPropertyChanged(nameof(AvailableTags));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AppSettings
    {
        public string ApiKey { get; set; } = "";
        public bool EnableAiAssistant { get; set; } = false; // Hidden/Disabled by default
        public bool EnableDragDrop { get; set; } = false; // Disabled by default per user request
        public string ActiveWorkspaceName { get; set; } = "Default Workspace";
        public string ThemeName { get; set; } = "Midnight Purple";
        public string GlobalHotkey { get; set; } = "Ctrl+Shift+N";
        public string HotkeyNewNote { get; set; } = "Ctrl+Shift+N";
        public string HotkeyFocusNote { get; set; } = "Ctrl+Shift+F";
        public string HotkeyToggleLock { get; set; } = "Ctrl+Shift+K";
        public bool EnableGlobalHotkeys { get; set; } = true;
        public string BlockedProcesses { get; set; } = "";
        public bool DisableHardwareAcceleration { get; set; } = false;
    }
}
