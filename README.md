# FileCraft

**FileCraft** is a powerful and extensible desktop application built with WPF (.NET 9, C#) designed to assist users with managing and processing file and folder content in a structured, customizable, and user-friendly way.

## 🚀 Overview

FileCraft is a tool that provides multiple utilities for file and folder operations. The application is designed with long-term scalability in mind, following clean architecture principles to ensure new features can be added with minimal impact on existing functionality.

The application features a clean tabbed interface where each tab represents a specific function, such as content exporting, folder tree visualization, or file renaming.

> **Note:** The app is fully written in English, including code comments, naming conventions, and UI.

## 🧩 Features

### ✅ Current Features

- **File Content Export**
  - Export the contents of selected files from selected folders based on their extensions.
  - Generate a `.txt` file with readable, concatenated contents of all selected files.
  
- **Folder Content Export**
  - Export metadata of files (e.g., size, creation/modification/access dates, format, full path) from selected folders to a CSV-style `.txt` file.

- **Tree Structure Generator**
  - Generate a visual tree representation of the folder structure, with support for excluding specific directories.
  - Output is saved as a plain text `.txt` file.

### 🛠️ Planned Features

- **File Renamer (Coming Soon)**
  - Rename files in bulk based on unified naming patterns or metadata.
  
- **Future Functionalities**
  - The application is architected for easy feature extension such as duplicate finder, file organizer, metadata editor, and more.

## 🧱 Architecture

FileCraft is built with long-term maintainability in mind:
- MVVM (Model-View-ViewModel) pattern for clean separation of concerns.
- Dependency injection for service abstraction.
- Clear division into `Views`, `ViewModels`, `Services`, `Shared`, and `Interfaces`.
- Settings persist across sessions using JSON storage.

## 🖥️ Tech Stack

- **Framework:** WPF (.NET 9)
- **Language:** C#
- **UI:** XAML
- **Data Storage (optional):** MS SQL (for future expansion, not required currently)
- **IDE:** Visual Studio 2022

## 📂 Project Structure

FileCraft/

├── Views/ # XAML UI components

├── ViewModels/ # View logic and bindings

├── Services/ # File operations, settings, dialog handling

├── Shared/ # Shared utilities, commands, validation

├── App.xaml # Application startup definition

├── MainWindow.xaml # Root window with tab control

└── FileCraft.sln # Visual Studio solution file

## ⚙️ Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/FileCraft.git
2. Open FileCraft.sln in Visual Studio 2022.

3. Build and run the application using .NET 9 SDK.

Note: Ensure you have .NET 9 installed and your system meets the prerequisites for WPF development.

## 📬 Contribution
This project is still in active development. Feature suggestions and contributions are welcome! Please open an issue or submit a pull request.
