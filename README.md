# PANApp — Project Analysis & Notes Application

A cross-platform desktop application for **analyzing software projects**, **visualizing module dependencies**, and **managing contextual notes** attached to specific modules. Designed to help developers understand complex codebases and document their findings directly in the project structure.

## Use Cases

1. **Legacy Code Exploration**: When working with unfamiliar or legacy codebases, PANApp helps you quickly understand how modules depend on each other using an interactive dependency graph.
2. **Code Review and Documentation**: Add notes directly to modules to document important findings, tasks, architectural decisions, or known issues—all tied to the specific module they belong to.
3. **Multi-Project Management**: Manage multiple projects simultaneously using separate profiles, each with its own analysis graph and set of notes.
4. **Architectural Analysis**: Visualize the dependency structure of C# projects to identify circular dependencies, tightly coupled modules, or architectural violations.

## Features

### 📊 Project Analysis
- **Multi-Language Support**: Currently supports
    1. C#
    2. C# Avalonia/WPF
    3. C# Blazor
    4. Python
- **Smart Dependency Detection**: Automatically detects `using` statements, type references, inheritance, and object creation to build a comprehensive dependency graph.
- **Filtered Scanning**: Automatically excludes `bin`, `obj`, and auto-generated files (`.g.cs`, `.g.i.cs`) from analysis.

### 🗺️ Interactive Dependency Graph
- **Visual Graph Layout**: Modules are displayed as nodes connected by directional arrows showing dependencies.
- **Pan & Zoom**: Navigate large graphs with mouse wheel zoom and right/middle-click panning.
- **Position Memory**: Each profile remembers its graph zoom level and pan position between switches.
- **Node Badges**: Each module displays a red badge showing the number of attached notes.
- **Tooltips**: Hover over any module to see its full path and note count.

### 📝 Notes System
- **Module-Scoped Notes**: Attach notes directly to specific modules within a project.
- **Profile Isolation**: Notes are tied to both the profile and the module — switching profiles shows only that profile's notes.
- **Auto-Numbering**: Each note receives a sequential number within its profile (e.g., #1, #2, #3) for easy reference.
- **Detailed View Popup**: Click any note to open a full-screen popup with complete details, including module name, creation date, and full description.
- **Inline Editing**: Edit note title and description directly in the popup without closing it.
- **All Notes View**: Browse all notes for a profile at once, with each note showing which module it belongs to.
- **SQLite Storage**: All notes are persisted in a local SQLite database, ensuring data survives application restarts.

### 📁 Profile Management
- **Multiple Profiles**: Create and manage multiple project profiles, each with its own configuration.
- **Graph Caching**: Analyzed graphs are cached per profile within the current session — switch between profiles without re-analyzing.
- **Analysis Indicator**: Green checkmark badge shows which profiles have been analyzed in the current session.
- **Persistent Storage**: Profiles are saved to a JSON file in the system's application data directory.

### ⚙️ Application Settings
- **Auto-Start with OS**: Configure PANApp to launch automatically when you log in (supports Windows, Linux, and macOS).
- **Cross-Platform Configuration**: Settings are stored in the appropriate location for each OS:
  - **Windows**: `%AppData%/PANApp/`
  - **Linux**: `~/.config/PANApp/`
  - **macOS**: `~/Library/Application Support/PANApp/`

### 🎨 User Interface
- **Modern Dark Theme**: Sleek dark interface inspired by Visual Studio Code.
- **Collapsible Panels**: Toggle the profile details panel to maximize graph viewing area.

## Usage Instructions

### First Launch

1. **Launch the Application**: Start PANApp. On Windows, you may be prompted for administrator rights (required for auto-start functionality).
2. **Create a Profile**:
   - Click the **➕ Create** button in the left panel.
   - Enter a name for your project.
   - Select the language (C# or C# Avalonia/WPF).
   - Click **📂 Browse...** to select the root folder of your project.
3. **Save the Profile**: Click **💾 Save** to persist your profile configuration.

### Analyzing a Project

1. **Select a Profile**: Click on the profile you want to analyze in the left panel.
2. **Start Analysis**: Click the **▶ Start analyze project** button.
3. **View the Graph**: The dependency graph will appear in the center panel. Each node represents a module (class/file), and arrows show dependencies.
4. **Navigate the Graph**:
   - **Zoom**: Use the mouse wheel to zoom in/out.
   - **Pan**: Hold the right or middle mouse button and drag to move around.
   - **Reset View**: Switch to another profile and back to reset the view.

### Managing Notes

1. **View Module Notes**: Click on any module in the graph to open the notes panel on the right.
2. **Create a Note**:
   - Enter a title and description in the form at the bottom of the notes panel.
   - Click **➕ Add** to save the note.
3. **Edit a Note**:
   - **From the list**: Click the ✏️ icon on any note card.
   - **From the popup**: Click a note to open the detailed view, then click **✏️ Edit**.
4. **Delete a Note**: Click the 🗑️ icon on a note card or in the popup.
5. **View All Notes**: Click the **📋 All notes** button above the graph to see all notes for the current profile, grouped by module.

### Application Settings

1. Navigate to **⚙️ Settings** using the top navigation bar.
2. Toggle **🚀 Start with Windows** to enable/disable auto-start.
3. Changes are saved automatically.

## Creating a Publish Version

### 🪟 Windows x64
```bash
dotnet publish -c Release -r win-x64 -o publish/releases/PANApp-Windows-x64
```

### 🪟 Windows ARM64
```bash
dotnet publish -c Release -r win-arm64 -o publish/releases/PANApp-Windows-ARM64
```

### 🐧 Linux x64
```bash
dotnet publish -c Release -r linux-x64 -o publish/releases/PANApp-Linux-x64
```

### 🐧 Linux ARM64
```bash
dotnet publish -c Release -r linux-arm64 -o publish/releases/PANApp-Linux-ARM64
```

### 🍎 macOS x64 (Intel)
```bash
dotnet publish -c Release -r osx-x64 -o publish/releases/PANApp-macOS-Intel
```

### 🍎 macOS ARM64 (Apple Silicon)
```bash
dotnet publish -c Release -r osx-arm64 -o publish/releases/PANApp-macOS-AppleSilicon
```