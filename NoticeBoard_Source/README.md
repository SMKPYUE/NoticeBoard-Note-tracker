# NoticeBoard 

NoticeBoard is a modern, dark-themed local desktop application built with WPF and .NET 8. It functions as a visual storyboarding & notes tracker, with custom workspaces, tag categorization, custom color highlights, local media attachments, floating image references, card sharing (import/export), and an optional streaming Gemini AI assistant.

## Features

- **Separate Workspaces**: Organize projects or storylines into separate, isolated boards.
- **Workspace Tags & Dim Filtering**: Tag cards with custom labels (e.g. `#Crime`, `#Report`, `#Cow`, `#Sheep`). Filter the board by any tag; non-matching cards are dimmed and moved to the end, keeping the context visible.
- **Detailed Card Flyout**: Open a large flyout panel for detailed titles, notes, border highlight customization, custom backgrounds, and attached images.
- **Visual Highlighting**: Color-code cards with pre-configured color schemes in the application style, including font size/family customizations.
- **Visual Reference Images**: Attach images to cards (saved locally at `%APPDATA%\NoticeBoard\Card Images\`).
  - Click any image thumbnail to pop it out into a draggable, resizable, borderless window.
  - **Non-Destructive Image Annotation**: Draw markings, paths, and circles directly on attached images using a vector pen canvas (colors: Red, Yellow, Green, Blue, White; size slider) and stroke eraser. Strokes are serialized in card data to preserve the original image.
- **Split Popout Note Window (`📑`)**: Pop out a split layout containing the reference image with active drawing controls on the left, a resizable splitter, and the note editor text box on the right.
- **Side-by-Side Card View (`📖`)**: Toggle layout inside the main app dashboard to display your reference image directly side-by-side with your note text inside the card.
- **Import & Export (.noticecard)**: Share any card with its details, attached images, and drawing annotations packaged in a portable `.noticecard` zip archive.
- **Dual AI Assistants (Gemini & Ollama)**:
  - **Gemini AI**: Stream responses directly inside a collapsible, fully hideable panel using your Google API Key.
  - **Local Ollama Integration**: Connect to local self-hosted AI models (Llama3, Mistral, Gemma, etc.) running on your computer. Discovers installed models automatically, running 100% locally and offline without data leaving your machine.
- **Pop Out Standalone Note Window**: Focus and work on a single note in its own borderless window that supports custom scaling, opacity adjustment, and click-through pin overlay behavior.
- **Global System Hotkeys (Low-Level Hooks)**:
  - Global triggers to create a **New Note** (default `Ctrl+Shift+N`), **Focus Popout** (default `Ctrl+Shift+F`), and **Toggle Lock** (default `Ctrl+Shift+K`) anywhere, even while in full-screen games.
  - Implemented using a Win32 Low-Level Keyboard Hook (`WH_KEYBOARD_LL`) so shortcuts are never swallowed inside blocked applications.
- **Focus-Stealing Thread Attachment Bypass**: Instantly pulls active window focus from background applications (like Spotify, Explorer, or Skyrim) directly to the popped-out note using Win32 thread input attachment, preventing flashing taskbar locks.
- **PC-Style Keybind Recorder**: Interactive recorder buttons in the Settings menu that capture modifiers and key combinations dynamically with duplicate collision warnings.
- **Hotkey App Blocklist**: Comma-separated blocklist (e.g., `discord, chrome, notepad`) to ignore hotkeys when typing in specific productivity apps.
- **GPU Stability & Software Rendering**:
  - Implements static texture caching and `.Freeze()` calls on custom backgrounds to eliminate unmanaged DX memory leaks and prevent GPU driver crashes.
  - Adds a **Disable GPU Hardware Acceleration** option to force software-only rendering, avoiding hardware contention during 3D gaming.

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
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained true -o "./Builds/V0.6"
```

## License
Refer to the `LICENSE` file for permissions and mandatory backwards compatibility guidelines.

---

**Disclaimer:** I'm open with it, I do use AI in my workflow for structuring (cleanup, error checking, and output formatting). My main editor for coding is VSCode. (If you see any messy lines, give me a shout!)
