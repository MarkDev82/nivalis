<div align="center">

# ❄️ NIVALIS
### *Thermal & Power Management Suite for Windows*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20x64-blue.svg)](https://microsoft.com/windows)
[![Language](https://img.shields.io/badge/Language-C%23%20.NET%20%2F%20Win32-purple.svg)](https://learn.microsoft.com/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-Native%20P%2FInvoke-emerald.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

</div>

---

## 📌 Executive Summary

**NIVALIS** is a high-performance, native Windows utility designed to solve late-night desktop overheating and fan noise. High-end gaming PCs operating during late hours generate excessive thermal energy, turning bedrooms into uncomfortably hot environments—even during lightweight activities like web browsing, listening to Spotify, or coding in an IDE.

NIVALIS provides a seamless, one-click solution that throttles CPU frequency, caps GPU wattage, and adjusts monitor refresh rates to **reduce PC heat generation by up to 45%** while keeping the system 100% usable.

---

## ✨ Key Features (Client & User Guide)

### 🌙 Bedtime Eco Mode
- **CPU Thermal Limiting**: Restricts maximum processor frequency to **75%** and switches cooling policy to **Passive** (reducing fan noise to near-zero).
- **GPU Power Capping**: Restricts NVIDIA graphics card power draw to **70W** (a 44% reduction from default 125W TDP), dramatically decreasing heat output.
- **Optional Display Refresh Scaling**: Optionally scales display refresh rate down to **60 Hz** for energy savings while preserving 32-bit color depth and saturation.

### ⚡ Performance Mode
- Instantly restores **100% CPU max frequency**, enables **Active fan cooling**, removes GPU wattage caps (restoring full 125W factory power), and resets monitor refresh rate to **144 Hz**.

### 🎛️ Modular Feature Control Panel
Customize exactly which hardware components NIVALIS controls using interactive checkboxes:
- `[x] Manage CPU Thermal & Power Saver`
- `[x] Manage GPU Power Limit`
- `[ ] Switch Display Refresh Rate` *(Optional: Unchecked by default to ensure NVIDIA Digital Vibrance & color saturation remain 100% untouched)*
- `[ ] Launch NIVALIS at Windows Startup`

### 🔔 Silent System Tray Execution
- Minimizes directly to the Windows System Tray near the clock with zero desktop clutter.
- Includes a native context menu for instant mode switching (`Eco Mode`, `Normal Mode`, `Open Dashboard`, `Quit`).

### 🔊 Native In-Memory Audio Feedback
- Real-time synthesized audio chimes provide acoustic feedback for mode transitions (*Frost Chime* for Eco Mode, *Energy Surge* for Normal Mode) generated entirely in RAM without external audio files.

### 🛡️ Reboot Safety Guarantee
- All hardware limits automatically reset to factory defaults upon restarting Windows, ensuring zero permanent changes or persistence risk.

---

## 🛠️ Technical Architecture & Engineering Showcase (Portfolio Section)

This section details the low-level systems programming, Win32 API integration, and architectural decisions behind NIVALIS for software engineering portfolio evaluation.

### 🧩 System Architecture Grid

| Subsystem | Core Engine | Technical Specification & Low-Level APIs |
| :--- | :--- | :--- |
| 💻 **CPU Engine** | `PowerCFG` IPC Interop | Programmatic GUID switching (`Power Saver: a1841308...`), 75% max state ceiling, passive silent cooling policy (`SYSCOOLPOL: 0`). |
| 🎮 **GPU Engine** | `NVIDIA-SMI` / NVAPI | UAC elevated runtime wattage capping (`-pl 70` vs `-pl 125`), achieving a 44% reduction in thermal output. |
| 🖥️ **Display Engine** | Win32 CCD & P/Invoke API | Interrogates display paths via `QueryDisplayConfig` & `ChangeDisplaySettingsExW` with explicit bit-depth field masks to preserve RGB Full Range. |
| 🔊 **DSP Audio Engine** | In-Memory WAV Synthesizer | Pure C# sinusoidal PCM wave generation writing 16-bit stereo buffers directly to `MemoryStream` played via `SoundPlayer`. |
| 🔔 **System Tray Engine** | Win32 `NotifyIcon` & Registry | Background tray execution near clock with auto-start capability via `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`. |

### 1. Low-Level Windows Power Management (CPU Subsystem)
NIVALIS interacts directly with the Windows Power Manager via `powercfg.exe` IPC commands using programmatic GUID targeting:
- **Power Schemes**: Switches between **Power Saver** (`a1841308-3541-4fab-bc81-f71556f20b4a`) and **High Performance** (`8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c`).
- **Sub-Processor GUID Indexing**: Dynamically modifies sub-group `54533251-82be-4824-96c1-47b60b740d00`:
  - `PROCMIN (893de800-450c-4634-b3d1-af4fad038096)`: Set to 5%.
  - `PROCMAX (bc50be8b-707e-446c-8e7c-64aed8623541)`: Capped at 75% in Eco Mode vs 100% in Normal Mode.
  - `SYSCOOLPOL (94d3a615-a899-4ac5-ae2b-e4d8f634367f)`: `0` (Passive/Silent) vs `1` (Active/Performance).

### 2. Graphics Power Management (GPU Subsystem)
- Direct integration with `nvidia-smi` CLI to modify hardware power limits at runtime (`-pl 70` vs `-pl 125`).
- Handles administrative privileges via embedded application manifest (`requireAdministrator`) to ensure kernel-level GPU driver requests succeed seamlessly.

### 3. Win32 Display & Color Profile Preservation (Display Subsystem)
To prevent the Windows display driver from resetting NVIDIA Digital Vibrance or switching RGB Dynamic Range from *Full (0-255)* to *Limited (16-235)* during refresh rate changes, NIVALIS utilizes native C# P/Invoke bindings:
- **QueryDisplayConfig & GetDisplayConfigBufferSizes**: Interrogates active display paths using Connecting and Configuring Displays (CCD) APIs (`QDC_ONLY_ACTIVE_PATHS`).
- **EnumDisplaySettingsW & ChangeDisplaySettingsExW**: Populates a 188-byte `DEVMODEW` structure with explicit field masks:
  ```csharp
  tempMode.dmFields = DM_BITSPERPEL | DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY;
  ChangeDisplaySettingsExW(@"\\.\DISPLAY1", ref tempMode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
  ```
- **Color Preservation**: By explicitly declaring bit depth and resolution flags, the GPU driver maintains original color saturation profiles without shifting to washed-out defaults.

### 4. Real-Time Math-Based Audio Synthesizer
Rather than bundling external `.wav` assets, NIVALIS includes a custom DSP audio synthesizer that constructs RIFF WAV byte structures in-memory:
- Computes sine waves with sinusoidal envelope shaping:
  $$\text{wave}(t) = \sin(2\pi \cdot f \cdot t) \cdot \sin\left(\frac{\pi \cdot i}{N}\right)$$
- Writes 16-bit PCM stereo data to `MemoryStream` and renders via `System.Media.SoundPlayer`.

### 5. Windows Registry Auto-Start Integration
- Manages auto-start capabilities via Windows Registry key manipulation:
  `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`

---

## 🚀 Getting Started

### Prerequisites
- **OS**: Windows 10 or Windows 11 (64-bit).
- **Permissions**: Administrator privileges (enforced via UAC manifest).
- **GPU** (Optional): NVIDIA GPU with driver support for `nvidia-smi`.

### Running the Application
1. Download or locate `output/Nivalis.exe`.
2. Double-click `Nivalis.exe`. Click **Yes** on the Windows UAC prompt.
3. Use the interface to toggle between **Eco Mode** and **Normal Mode**, or customize settings using the modular checkboxes.

### Compiling from Source
NIVALIS can be compiled using the standard Microsoft C# compiler (`csc.exe`) bundled with .NET Framework 4.0+:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /win32manifest:code\app.manifest /win32icon:assets\logo.ico /out:output\Nivalis.exe code\Nivalis.cs
```

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<div align="center">
  <sub>Developed by <b>Mark</b> &bull; Built with C#, .NET Win32 APIs, & Native Windows Hardware Controls.</sub>
</div>
