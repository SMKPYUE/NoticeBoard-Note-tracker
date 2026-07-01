# NoticeBoard 📌

NoticeBoard is a modern, dark-themed local desktop application built with WPF and .NET 8. It functions as a visual storyboarding & notes tracker, with custom workspaces, tag categorization, custom color highlights, local media attachments, floating image references, card sharing (import/export), and an optional streaming Gemini AI assistant.

## Features

- 📌 **Separate Workspaces**: Organize projects or storylines into separate, isolated boards.
- 🏷️ **Workspace Tags & Dim Filtering**: Tag cards with custom labels (e.g. `#Crime`, `#Report`, `#Cow`, `#Sheep`). Filter the board by any tag; non-matching cards are dimmed and moved to the end, keeping the context visible.
- 📝 **Detailed Card Flyout**: Open a large flyout panel for detailed titles, notes, border highlight customization, and attached images.
- 🎨 **Visual Highlighting**: Color-code cards with pre-configured color schemes in the application style.
- 🖼️ **Visual Reference Images**: Attach images to cards (saved locally at `%APPDATA%\NoticeBoard\Card Images\`). Click any image thumbnail to pop it out into a draggable, resizable, borderless window to keep on your desktop.
- 📤 **Import & Export (.noticecard)**: Package any card with its details and attached images into a portable `.noticecard` zip archive to share with others.
- 🤖 **Gemini AI Assistant**: Stream responses directly inside a collapsible, fully hideable panel. (Toggle in settings and supply your own Gemini API Key).
- ⚙️ **Premium Dark Theme**: Sleek, customizable modern styling, with dark-themed custom confirmations.

## How to Build & Run

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run in Development
Run the project using the dotnet CLI from the root folder:
```powershell
dotnet run
```

### Build a Release Executable
To publish a self-contained single-file executable (includes the .NET runtime, so no prerequisites are needed on target machines):
```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained true -o "./Builds/V0.3"
```

## License
Refer to the `LICENSE` file for permissions and mandatory backwards compatibility guidelines.
