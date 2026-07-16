# Security Policy

We take the security of **NoticeBoard - Note Tracker** seriously. If you believe you have found a security vulnerability, please report it to us using the instructions below.

---

## Supported Versions

Only the latest released version of NoticeBoard receives security patches and updates. 

| Version | Supported          |
| ------- | ------------------ |
| v0.7.x  | :white_check_mark: |
| < v0.7  | :x:                |

---

## Reporting a Vulnerability

**Please do not report security vulnerabilities via public GitHub issues.** Instead, report them privately using one of the following methods:

* **GitHub Private Vulnerability Reporting:** If available on this repository, please use the "Report a vulnerability" button under the **Security** tab.

### What to include in your report:
* A detailed description of the vulnerability.
* Step-by-step instructions or a Proof of Concept (PoC) to reproduce the issue.
* The potential impact (e.g., local file disclosure, application crash).

We will acknowledge receipt of your vulnerability report within **48 hours** and work to provide a resolution as quickly as possible.

---

## ⚠️ Security Guidelines for Users

NoticeBoard is a local desktop application that processes data entirely on your machine. However, to keep your environment secure, please follow these guidelines:

### 1. API Key Protection
NoticeBoard saves your Gemini API Key locally in the encrypted or plain user settings file under `%APPDATA%\NoticeBoard\`. 
* **Never** commit your `%APPDATA%\NoticeBoard\settings.json` file or share it publicly.
* If you suspect your API key has been exposed, revoke it immediately via the Google AI Studio dashboard and generate a new one.

### 2. Untrusted Workspaces and Cards
The Import feature allows you to open workspace packages (`.noticeworkspace`) and card files (`.noticecard`). 
* Only import workspaces or cards from **trusted sources**.
* Maliciously crafted packages could theoretically exploit file extraction path traversal vulnerabilities or reference external untrusted media. NoticeBoard mitigates path traversal by checking output paths, but caution is still recommended.

### 3. Local Offline Mode
If you require 100% privacy and no network connections, use **Local Ollama AI** instead of Gemini. Ollama runs entirely offline on your local network, ensuring your notes and storyboard data never leave your computer.
