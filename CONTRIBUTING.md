# Contributing to NoticeBoard

Thank you for your interest in contributing to **NoticeBoard - Note Tracker**! Whether you are fixing a bug, optimizing performance, suggesting new features, or refining the user interface, your help is highly appreciated.

Please read through these guidelines to ensure a smooth and consistent workflow for everyone.

---

## License Compliance (Critical)

NoticeBoard is distributed under a custom license. By contributing to this project, you agree that your code will be licensed under the same terms:
1. **Non-Commercial:** The application and all derivative works are strictly non-commercial and must be free.
2. **Backwards Compatibility:** Any modification to file storage, export formats (`.noticecard` zip packages), or workspace formats (`.noticeworkspace` packages) **must maintain backwards compatibility**. Users must be able to import old format files into your build, and export files that are readable by original versions.

Refer to the full [LICENSE.md](file:///E:/ANTIGRAV%20projects/NoticeBoard/LICENSE.md) file for details.

---

## Development Setup

NoticeBoard is built using **WPF** and targetting **.NET 8.0**.

### Prerequisites
* Install the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
* Visual Studio 2022 (with the .NET Desktop Development workload) or VS Code (with C# Dev Kit extensions).

### Local Build Commands
To run the project in development:
```powershell
dotnet run
```

To compile a single-file, self-contained release executable:
```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained true -o "./Builds/V0.7"
```

---

## Architecture & Development Rules

To maintain high performance and stability (especially when the app is used as a background companion or gaming overlay), please adhere to the following rules:

### 1. GPU Stability & Software Rendering
* **Asset Freezing:** Always call `.Freeze()` on any newly created WPF resources (such as `SolidColorBrush`, `ImageBrush`, or `Pen`) that are declared in code-behind. This allows threads to share them safely, avoids unmanaged memory leaks, and prevents D3D driver crashes.
* **Hardware Acceleration Toggle:** Keep hardware rendering optional. If you implement new visual effects, make sure they degrade gracefully if the user selects the "Disable Hardware Acceleration" option.

### 2. Input Hooking (Low-Level Win32 Hooks)
* **No Input Swallowing:** When adding or modifying global hotkeys inside the keyboard hook thread (`WH_KEYBOARD_LL`), ensure hooks pass non-registered keystrokes to the next hook immediately to prevent input lag or keyboard freezes in other applications.

### 3. Data Serialization
* **JSON Serialization:** Any new card properties or settings properties added to models must have default values configured in [Models.cs](file:///E:/ANTIGRAV%20projects/NoticeBoard/Models.cs) to ensure that importing older workspaces does not throw serialization exceptions.

---

## How to Contribute

### 1. Reporting Bugs & Suggesting Features
* Open an **Issue** on GitHub.
* Describe the bug or feature request clearly, providing steps to reproduce, actual vs. expected results, and any relevant logs or screenshots.

### 2. Submitting Pull Requests
1. **Fork the Repository:** Create a personal fork.
2. **Create a Feature Branch:** Build your changes in a branch named `feature/your-feature-name` or `bugfix/issue-id`.
3. **Write Clean Code:** Maintain consistency with the existing codebase formatting (standard C# coding conventions and double-space indentation in XAML).
4. **Test Your Changes:** Build the project locally and verify the features run successfully without crashes.
5. **Open a PR:** Describe what your PR accomplishes, reference any related issues, and document your verification steps.

Thank you for helping make NoticeBoard better!
