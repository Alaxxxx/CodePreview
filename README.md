# CodePreview for Unity

[![GitHub license](https://img.shields.io/github/license/alaxxxx/CodePreview)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Alaxxxx/CodePreview?style=flat-square)](https://github.com/Alaxxxx/CodePreview/releases)
[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-green.svg)](https://unity3d.com/get-unity/download)
[![GitHub last commit](https://img.shields.io/github/last-commit/Alaxxxx/CodePreview)](https://github.com/Alaxxxx/CodePreview/commits/main)

A powerful and customizable preview for scripts and data files in the Unity inspector, designed to replace the default view.

<br>

<p align="center">
  <img height="500" alt="CodePreview Screenshot" src="https://github.com/user-attachments/assets/ae097fc2-f8c5-4bbb-8f5a-f6fde3af5ab7"/>
</p>

---
## 🎯 Supported Formats

Easily preview all your important files with dedicated syntax highlighting:

* **Scripts:** `C#` (.cs)
* **Data:** `JSON` (.json), `XML` (.xml), `YAML` (.yaml)
* **Documentation:** `Markdown` (.md)

---
## ✨ Features

* **Advanced Syntax Highlighting:** A clean and readable preview for all supported languages, compatible with Unity's Light and Dark editor themes.

* **Rendered Markdown Preview:** View your `.md` (README) files rendered directly in the inspector for a much more pleasant reading experience.

* **Powerful Navigation:** Instantly find what you need with a built-in search tool that highlights all matches. Jump to any line number with the "Go to Line" feature.

* **Detailed Insights:** Get a complete overview of your file: line, word, and character counts, file size, and last modification date.

* **Smart Header:** Automatically detects and displays the script type (`MonoBehaviour`, `ScriptableObject`, etc.) for immediate context.

* **Highly Customizable:** Adjust the font size, preview height, and other display options in a polished foldout menu.

* **Quick Actions:** A handy toolbar to edit the script in your IDE, ping the file in the project view, copy its path, or copy its entire content.

---
## 🚀 Installation

### Recommended Methods

<details>
<summary><strong>1. Install via Git URL (Package Manager)</strong></summary>
<br>

This method installs the package directly from GitHub and allows you to update it easily.

1.  In Unity, open the **Package Manager** (`Window > Package Manager`).
2.  Click the **+** button and select **"Add package from git URL..."**.
3.  Enter the following URL and click "Add":
    ```
    https://github.com/Alaxxxx/CodePreview.git
    ```
</details>

<details>
<summary><strong>2. Install via .unitypackage</strong></summary>
<br>

Ideal if you prefer a specific, stable version of the asset.

1.  Go to the [**Releases**](https://github.com/Alaxxxx/CodePreview/releases) page.
2.  Download the `.unitypackage` file from the latest release.
3.  In your Unity project, go to **`Assets > Import Package > Custom Package...`** and select the downloaded file.
</details>

### Alternative Method

<details>
<summary><strong>3. Manual Installation (from .zip)</strong></summary>
<br>

1.  Download this repository as a ZIP file by clicking **`Code > Download ZIP`**.
2.  Unzip the file.
3.  Drag the unzipped package folder into your project's `Assets` directory.
</details>

---
## 📄 License

This project is distributed under the **MIT License**. See the `LICENSE` file for more details.
