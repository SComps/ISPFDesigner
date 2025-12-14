# ISPF Designer User Manual

**Version 1.0**

## Introduction
ISPF Designer is a simplified "What You See Is What You Get" (WYSIWYG) editor for creating Mainframe ISPF Panels on Windows or Linux. It mimics the behavior of the z/OS ISPF editor but runs in a standard local console.

## Installation
1.  Ensure you have **.NET 9.0 SDK** installed.
2.  Navigate to the project directory:
    ```bash
    cd scratch/ispf_designer/ISPFDesigner
    ```
3.  Build the project (Windows):
    ```bash
    dotnet publish -c Release
    ```
    Or for a single-file executable:
    ```bash
    dotnet publish -c Release -r win-x64
    ```
4.  **Linux**: Use the included build script:
    ```bash
    ./build.sh
    ```
    This produces a single-file executable in the `Publish/` directory.

## Running the Application
To start the editor:
```bash
dotnet run
```
You will be presented with a blank 80x24 character canvas.

## Designing Panels
You can type anywhere on the screen. The editor supports standard ISPF attribute characters which are rendered in color to help you visualize the final result.

### Attribute Codes
| Character | Meaning | Display Color |
| :--- | :--- | :--- |
| **`%`** | **High Intensity Text** | **White** |
| **`+`** | **Low Intensity Text** | **Cyan** |
| **`_`** | **Input Field** | **Red** (and Underlined concept) |
| **`$`** | **Password Field** | **Invisible** (Hidden Input) |

**Example:**
To create a text field that says "Employee ID:" followed by an input box:
1.  Type `+` (Color turns Cyan).
2.  Type `Employee ID:`.
3.  Type `_` (Color turns Red).
4.  Type `      ` (Spaces for the input length).

## Short-Cut Keys
*   **Arrow Keys**: Move the cursor around the grid without deleting text.
*   **F1**: Display the **Help Screen**.
*   **F2**: **Save** the current design to a file.
*   **F3**: **Load** an existing file (warning: overwrites current screen).
*   **F4**: Open the **Attribute Editor** to define custom attribute characters.
*   **F5**: Toggle **Test Mode** (Simulation).
*   **ESC**: Exit the program.

## Custom Attributes (F4)
You can define your own attribute characters.
1.  Press **F4**.
2.  Type a mapping, e.g., `? TYPE(TEXT) COLOR(YELLOW)`.
3.  The editor immediately updates to render `?` in Yellow.
4.  These definitions are saved to the `)ATTR` section of your panel file.

## Test Mode (F5)
Press **F5** to switch between **Design Mode** (Blue status bar) and **Test Mode** (Dark Red status bar).

In Test Mode, the application simulates the runtime behavior of the ISPF panel:
*   **Input Protection**: You can strictly only type inside defined **Input Fields** (like `_` or `$`). Typing over protected text is blocked.
*   **Tab Navigation**: Press **TAB** to jump to the start of the next input field. Press **SHIFT+TAB** to jump to the previous one.
*   **Validation**: Useful for verifying your tab order and ensuring fields are correctly defined.

## Output Format
The application saves files with a structure compatible with z/OS ISPF:

```text
)ATTR
  % TYPE(TEXT) INTENS(HIGH)
  + TYPE(TEXT) INTENS(LOW)
  _ TYPE(INPUT) CAPS(ON)
)BODY
... content of your screen ...
)INIT
)PROC
)END
```

## Uploading to Mainframe
1.  Save your file, e.g., `MYPANEL.PANEL`.
2.  Transfer the file to your z/OS system (e.g., via FPS or Zowe).
3.  Place it in your PDS allocated to `ISPPLIB`.
