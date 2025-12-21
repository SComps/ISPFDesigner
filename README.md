# ISPF Designer

> [!NOTE]
> This is a [mostly] AI generated application done as a lark. It seems to work, and was an experiment. Take it for what it's worth.

ISPF Designer is a WYSIWYG terminal-based editor for creating ISPF Panels. It uses a project-based workflow to separate design-time metadata from the final exported panel.

## Features
- **WYSIWYG Editing**: Real-time rendering of ISPF attributes.
- **Project Model**: Saves complete design state (including field names and types) to `.ispfd` files.
- **Clean Export**: Generates standard ISPF `.panel` files without designer-specific metadata.
- **Native AOT Compatible**: Optimized for high-performance, zero-dependency execution on Windows and Linux.

## Build Instructions

### Prerequisites
- .NET 9.0 SDK

### Windows
To build a standalone executable for Windows:
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

### Linux (Native AOT)
To build a high-performance Native AOT executable for Linux:
```bash
# Ensure build.sh is executable
chmod +x build.sh
./build.sh
```
Or manually:
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishAot=true
```

## Operating Instructions (Key Bindings)

| **Arrow Keys** | Move cursor around the buffer |
| **ENTER** | **Field Editor**: Open properties ONLY when on the attribute character (`_`, `.`, etc.) |
| **F1** | Show Help Screen |
| **F2** | **Save Project**: Persists design state to a `.ispfd` file |
| **F3** | **Load Project**: Restores design state from a `.ispfd` file |
| **F4** | **Attribute Editor**: Define custom attribute characters and colors |
| **F5** | **Test Mode**: Preview the panel with functional input fields |
| **F6** | **Field Properties**: Define variable names and types for the current field |
| **F7** | **Export Panel**: Generates a clean `.panel` file for mainframe use |
| **ESC** | Exit Application |

## Project Structure
- `.ispfd`: Project files containing full design metadata.
- `.panel`: Standard ISPF panel files (output format).
- `ProjectManager.vb`: Handles custom robust I/O for Native AOT compatibility.
