# Re4QuadRemake

[![Version](https://img.shields.io/badge/Version-1.0.1-red?style=flat-square)](https://github.com/YEMENI-modder/Re4QuadRemake/releases/latest)
![Status](https://img.shields.io/badge/Status-Active-success?style=flat-square)
[![Downloads](https://img.shields.io/github/downloads/YEMENI-modder/Re4QuadRemake/total.svg?cacheSeconds=0)](https://github.com/YEMENI-modder/Re4QuadRemake/releases)

# About

**Re4QuadRemake** is a fork of [Re4QuadExtremeEditor](https://github.com/JADERLINK/Re4QuadExtremeEditor), created to continue and modernize the development of the original project.

<img width="1294" height="708" alt="Screenshot_20260721_180350" src="https://github.com/user-attachments/assets/ab3b31ec-c47a-4391-be39-1a0d514d5b49" />


Since the original project by **[@JADERLINK](https://github.com/JADERLINK)** is no longer actively maintained, this fork focuses on improving stability, adding missing features, supporting NewAge file formats, and making the editor more practical for everyday Resident Evil 4 modding.

The goal is **not to replace the original project**, but to extend and modernize it while keeping it familiar for existing users.

---

# Supported Platforms

- Windows 11
- Windows 10
- Winlator (with Wine-Mono)
- Linux/Wine (with Wine-Mono)

---

# Fork Features

### New functionality

- Added support for NewAge file formats.
- Added SMD + ImagePackHD support.
- Added Dark Mode and Light Mode.
- Added a built-in FPS counter.
- Integrated the EFFBLOB tool directly into the editor.
- Added **Save All**.
- Added **Clear All**.
- Added a project system.
- Added automatic room loading. Selecting a room automatically loads its associated files (AEV, ITA, CAM, LIT, etc.).
- Projects now save:
  - Current map.
  - Whether the map uses SMD.
  - Camera position.
  - Save directory.
  - All opened files.
  - Unsaved modifications.

### Improvements

- Improved the application exit dialog for a faster workflow.
- Documented and named several previously unknown fields, including AEV Type 6 and ESL Types 40 & 41.
- Added partial Arabic localization.
- Added various quality-of-life improvements.
- Fixed numerous bugs and improved overall stability.

---

# Supported Game

Currently supported:

- **Resident Evil 4 Ultimate HD Edition (2014)**

Although some formats (such as ITA, ETS, and AEV) are compatible with the 2007 version, this project is designed specifically for the UHD release.

---

# Notes

- The project is stable but still under active development.
- The Arabic translation is not yet complete.
- This editor is intended for the UHD version only.
- Currently, only **English** and **Arabic** are available. The original Portuguese translation has been removed.
- In the **3D View**, right-click actions are currently unavailable for the following file types:
  - SAR
  - EAR
  - FSE
- The same issue may also affect **EMI**, **EFFBLOB**, and **ESE**, although this has not been fully verified yet.
- If you encounter any bugs or unexpected behavior, please open an Issue.

---

# Credits

### Original Project

- **[@JADERLINK](https://github.com/JADERLINK)** — Creator of **Re4QuadExtremeEditor**.

Repository: https://github.com/JADERLINK/Re4QuadExtremeEditor

### Fork Maintainer

- **YEMENI**

### AI-Assisted Development

Approximately **98%** of this project's codebase was implemented using **Claude Sonnet 5**.

AI was used as the primary implementation tool, while all testing, validation, debugging, integration, manual adjustments, and final project decisions were performed by **YEMENI**.

### Special Thanks

- **[@mualzahrani-wq](https://github.com/mualzahrani-wq)** — For the Dark Mode implementation that inspired this project.

  Repository: https://github.com/mualzahrani-wq/Re4QuadExtremeEditor

- **[@r3nzk](https://github.com/r3nzk)** — For **Re4QuadX** and several ideas that inspired this fork.

  Repository: https://github.com/r3nzk/Re4QuadX

---

# License

This project is licensed under the **MIT License**.

See the [LICENSE](LICENSE) file for more information.
