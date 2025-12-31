<p align="center">
  <img src="src/Assets/icon.png" width="128" height="128" alt="FreeMyRam Logo">
</p>

<h1 align="center">FreeMyRam</h1>

<p align="center">
  <b>Simple, fast, and powerful RAM optimizer for Windows</b>
</p>

<p align="center">
  <a href="https://github.com/rainaku/FreeMyRam/releases">
    <img src="https://img.shields.io/github/v/release/rainaku/FreeMyRam?style=for-the-badge&color=3B82F6&logo=github" alt="Latest Release">
  </a>
  <img src="https://img.shields.io/badge/platform-Windows-lightgrey?style=for-the-badge&logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt="Framework">
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/rainaku/FreeMyRam?style=for-the-badge" alt="License">
  </a>
</p>

<p align="center">
  FreeMyRam is a lightweight Windows utility designed to help you regain control over your system memory. With a single click or through automated rules, it flushes various memory lists to keep your PC running smoothly without the bloat.
</p>

<p align="center">
  💖 This project is entirely <b>non-profit</b> and <b>free</b>. <br>
  If you'd like to support my work, you can donate at my PayPal page: <a href="https://www.paypal.me/PhuocLe678"><b>Donate</b></a>
</p>

---

## ✨ Features

- ⚡ **One-Click Optimization** - Instantly clean all memory types with a single button
- 🚀 **Auto-Clean on Startup** - Automatically free up memory every time you launch the app
- ⏲️ **Periodic Auto-Clean** - Set intervals (5m to 180m) to keep your RAM fresh automatically
- 📉 **Smart Threshold** - Automatically trigger cleaning when RAM usage exceeds 70%
- 📊 **Real-time Monitoring** - Live tracking of used vs. total RAM and percentage
- 🔧 **Advanced Control** - Precision cleaning for Working Sets, Standby Lists, and Modified Pages
- 🧹 **Disk Cleanup** - Integrated tool to clean Temp files and the Windows Recycle Bin
- 🎨 **Modern Interface** - Beautiful WPF UI with Dark/Light mode support
- 🌐 **Bilingual** - Full support for both Vietnamese and English
- 💨 **Ultra Lightweight** - Minimal CPU and RAM footprint while running in the background

## 📸 Screenshot

*Coming soon*

## 📥 Download & Installation

### Requirements
- Windows 10/11 (64-bit)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Administrator Privileges** (Required for system memory operations)

### Installation
1. Download the latest release from [Releases](https://github.com/rainaku/FreeMyRam/releases)
2. Extract the files to your preferred location
3. Run `FreeMyRam.exe` as **Administrator**
4. (Optional) Enable "Clean on Startup" to automate your optimization

## 🎛️ Memory Operations

| Operation | Description |
|-----------|-------------|
| **Clean All Memory** | Executes all memory flushing operations for maximum free space |
| **Flush Working Sets** | Trims the memory used by all running processes |
| **Flush System Working Set** | Clears the memory used by the Windows kernel and system services |
| **Flush Standby List** | Frees cached memory (Standby list) back to the system |
| **Flush Modified Page List** | Writes modified memory pages to disk to free up RAM |
| **Flush Priority 0 Standby** | Clears the lowest-priority cached pages |
| **Disk Clean** | Deletes temporary application files and empties the Recycle Bin |

## ⚙️ Settings & Automation

- **Clean on Startup**: Automatically performs a full clean when the application starts.
- **Auto Clean Every**: Sets a timer to periodically clean memory (Options: Off, 5, 10, 15, 30, 45, 60, 120, 180 minutes).
- **RAM > 70% Threshold**: Monitors your usage and cleans automatically when it gets too high (with a 10-minute cooldown).
- **Theme**: Switch between **Dark** (default) and **Light** themes.
- **Language**: Toggle between **English** and **Tiếng Việt**.

## 🔧 Technical Details

- Built with **WPF (.NET 8)**
- Uses **Windows Native APIs** (`EmptyWorkingSet`, `NtSetSystemInformation`)
- **System Tray Integration** for background operation
- **JSON-based settings** stored in `%AppData%\FreeMyRam\settings.json`
- **Asynchronous tasks** to ensure a responsive UI during memory operations

## ⌨️ Usage

| Action | Result |
|--------|--------|
| **Double-click Tray Icon** | Show/Open main window |
| **Right-click Tray Icon** | Access quick clean options and exit |
| **Minimize / Close** | App continues running in System Tray |
| **Wait for Notification** | Get balloon alerts when auto-clean completes |

## 📝 Changelog

### v1.2.0
- Added "Start with Windows" option to launch app on system startup
- Added custom app icon for exe and system tray
- Faster RAM usage updates (every 0.5 seconds)
- Fixed single instance - reopening exe shows existing window instead of new instance
- Improved button hover effects with text color transitions
- Removed focus border on buttons when using Tab navigation
- Performance optimizations for faster startup

### v1.1.0
- Added Auto-Clean based on RAM threshold (>70%)
- Added Periodic Auto-Clean with customizable intervals
- Added Light Mode theme
- Redesigned "Clean on Startup" from checkbox to toggle button
- Improved UI layout and animations
- Optimized memory monitoring performance

### v1.0.0
- Initial release
- Basic memory cleaning operations
- System tray support
- English & Vietnamese localization

## 🤝 Contributing

Contributions are welcome! Feel free to open an issue or submit a pull request.

## 📄 License

MIT License - Feel free to use, modify, and distribute.

---

**Made with ❤️ by [rainaku](https://rainaku.id.vn)**

[![Facebook](https://img.shields.io/badge/Facebook-1877F2?logo=facebook&logoColor=white)](https://www.facebook.com/rain.107/)
[![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=white)](https://github.com/rainaku/)
[![Website](https://img.shields.io/badge/Website-FF7139?logo=firefox&logoColor=white)](https://rainaku.id.vn)
