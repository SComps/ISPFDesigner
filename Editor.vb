Imports System
Imports System.Text
Imports System.Collections.Generic
Imports System.IO

Namespace ISPFDesigner
    Public Class Editor
    Private Const ROWS As Integer = 24
    Private Const COLS As Integer = 80
    
    Private Buffer(ROWS - 1, COLS - 1) As Char
    Private CursorX As Integer = 0
    Private CursorY As Integer = 0
    Private IsRunning As Boolean = True
    Private IsTestMode As Boolean = False
    Private AttrManager As New AttributeManager()
    
    ' Track previous state to avoid redundant status bar updates
    Private LastCursorX As Integer = -1
    Private LastCursorY As Integer = -1
    Private LastTestMode As Boolean = False
    
    ' Field position cache for test mode performance
    Private FieldCache As New List(Of (Row As Integer, StartCol As Integer, EndCol As Integer))
    Private IsCacheDirty As Boolean = True
    
    ' Maps (Row, Col) of the attribute character to its field properties
    Private FieldProperties As New Dictionary(Of (Integer, Integer), FieldProperty)
    
    ' Backup buffer for test mode - restores design when exiting test mode
    Private TestModeBackupBuffer(ROWS - 1, COLS - 1) As Char

    ''' <summary>
    ''' Initializes a new instance of the Editor with an empty buffer.
    ''' </summary>
    Public Sub New()
        ' Initialize buffer with spaces
        For r As Integer = 0 To ROWS - 1
            For c As Integer = 0 To COLS - 1
                Buffer(r, c) = " "c
            Next
        Next
        

        ' Default size
        Try
            If OperatingSystem.IsWindows() Then
                If Console.WindowHeight < ROWS + 2 Then Console.WindowHeight = ROWS + 2
                If Console.WindowWidth < COLS Then Console.WindowWidth = COLS
            End If
        Catch
            ' Ignore resizing errors
        End Try

    End Sub

    ''' <summary>
    ''' Runs the main editor loop, handling user input until exit.
    ''' </summary>
    Public Sub Run()
        Console.CursorVisible = False ' Hide cursor during rendering
        Console.Clear()
        RenderAll() ' Initial full draw
        
        While IsRunning
            ' Only update status bar if cursor position or mode changed
            If CursorX <> LastCursorX OrElse CursorY <> LastCursorY OrElse IsTestMode <> LastTestMode Then
                Console.CursorVisible = False
                UpdateStatusBar()
                LastCursorX = CursorX
                LastCursorY = CursorY
                LastTestMode = IsTestMode
            End If
            
            Console.SetCursorPosition(CursorX, CursorY + 1)
            Console.CursorVisible = True ' Show cursor when waiting for input
            HandleInput()
        End While
        
        Console.CursorVisible = True ' Restore cursor visibility
        Console.Clear()
        
    End Sub

    Private Sub RenderAll()
        Console.CursorVisible = False
        Console.Clear()
        UpdateStatusBar()
        For r As Integer = 0 To ROWS - 1
            RenderLine(r)
        Next
        Console.SetCursorPosition(CursorX, CursorY + 1)
    End Sub

    Private Sub UpdateStatusBar()
        Console.SetCursorPosition(0, 0)
        If IsTestMode Then
            Console.BackgroundColor = ConsoleColor.DarkRed
        Else
            Console.BackgroundColor = ConsoleColor.Blue
        End If
        Console.ForegroundColor = ConsoleColor.White
        Dim modeStr As String = If(IsTestMode, "TEST", "DSGN")
        Dim status As String = $"Pos:{CursorY + 1:D2},{CursorX + 1:D2} | {modeStr} | F1:Help F2:Save F3:Load F4:Attr F5:Test F6:Fld F7:Exp ESC:Exit"
        Console.Write(status.PadRight(COLS))
        Console.ResetColor()
    End Sub

    Private Sub RenderLine(r As Integer)
        Console.SetCursorPosition(0, r + 1)
        
        Dim lineBuilder As New StringBuilder(COLS)
        Dim currentColor As ConsoleColor = ConsoleColor.Green
        Dim isHidden As Boolean = False
        Dim lastColor As ConsoleColor = currentColor
        
        For c As Integer = 0 To COLS - 1
            Dim ch As Char = Buffer(r, c)
            
            If AttrManager.IsAttributeChar(ch) Then
                ' Flush previous segment before writing attribute
                If lineBuilder.Length > 0 Then
                    Console.ForegroundColor = lastColor
                    Console.Write(lineBuilder.ToString())
                    lineBuilder.Clear()
                End If
                
                ' In test mode, hide attribute chars (like production ISPF)
                ' In design mode, show them in DarkYellow for editing
                If IsTestMode Then
                    Console.ForegroundColor = currentColor
                    Console.Write(" "c)
                Else
                    Console.ForegroundColor = ConsoleColor.DarkYellow
                    Console.Write(ch)
                End If
                
                ' Update color state for following characters
                currentColor = AttrManager.GetColorForAttribute(ch)
                isHidden = AttrManager.IsHiddenAttribute(ch)
                lastColor = currentColor
            Else
                ' If color changed, flush previous segment
                If currentColor <> lastColor Then
                    If lineBuilder.Length > 0 Then
                        Console.ForegroundColor = lastColor
                        Console.Write(lineBuilder.ToString())
                        lineBuilder.Clear()
                    End If
                    lastColor = currentColor
                End If
                
                ' Append character to current segment
                lineBuilder.Append(If(isHidden, " "c, ch))
            End If
        Next
        
        ' Flush final segment
        If lineBuilder.Length > 0 Then
            Console.ForegroundColor = lastColor
            Console.Write(lineBuilder.ToString())
        End If
    End Sub
    
    ''' <summary>
    ''' Rebuilds the field position cache by scanning the buffer for input/password fields.
    ''' Called when IsCacheDirty is true, typically after buffer modifications.
    ''' </summary>
    Private Sub RebuildFieldCache()
        FieldCache.Clear()
        
        For r As Integer = 0 To ROWS - 1
            Dim c As Integer = 0
            While c < COLS
                Dim ch As Char = Buffer(r, c)
                If AttrManager.IsInputOrPasswordAttribute(ch) Then
                    Dim startCol As Integer = c + 1
                    Dim endCol As Integer = startCol
                    
                    ' Find end of field (next attribute or end of line)
                    While endCol < COLS AndAlso Not AttrManager.IsAttributeChar(Buffer(r, endCol))
                        endCol += 1
                    End While
                    
                    If startCol < COLS Then
                        FieldCache.Add((r, startCol, endCol - 1))
                    End If
                    
                    c = endCol
                Else
                    c += 1
                End If
            End While
        Next
        
        IsCacheDirty = False
    End Sub

    ''' <summary>
    ''' Moves the cursor by the specified delta, clamping to valid bounds.
    ''' </summary>
    Private Sub MoveCursor(dx As Integer, dy As Integer)
        CursorX = Math.Max(0, Math.Min(COLS - 1, CursorX + dx))
        CursorY = Math.Max(0, Math.Min(ROWS - 1, CursorY + dy))
    End Sub

    Private Sub HandleInput()


        Dim key As ConsoleKeyInfo = Console.ReadKey(True)
        Dim needsRedraw As Boolean = False
        Dim currentRowForRedraw As Integer = CursorY

        If IsTestMode Then
             HandleTestInput(key)
             Return
        End If

        Select Case key.Key
            Case ConsoleKey.LeftArrow
                MoveCursor(-1, 0)
            
            Case ConsoleKey.RightArrow
                MoveCursor(1, 0)
            
            Case ConsoleKey.UpArrow
                MoveCursor(0, -1)
            
            Case ConsoleKey.DownArrow
                MoveCursor(0, 1)
            
            Case ConsoleKey.Backspace
                If CursorX > 0 Then
                    CursorX -= 1
                    Buffer(CursorY, CursorX) = " "c
                    IsCacheDirty = True
                    RenderLine(CursorY) 
                End If

            Case ConsoleKey.Delete
                Buffer(CursorY, CursorX) = " "c
                IsCacheDirty = True
                RenderLine(CursorY)

            Case ConsoleKey.Enter
                Dim field = FindFieldInCache(CursorY, CursorX)
                ' Only open description if specifically on the attribute character
                If field.Row <> -1 AndAlso CursorX = field.StartCol - 1 Then
                    ShowFieldPropertyEditor()
                    RenderAll()
                Else
                    ' Standard newline behavior
                    CursorX = 0
                    CursorY = Math.Min(ROWS - 1, CursorY + 1)
                End If

            Case ConsoleKey.Escape
                IsRunning = False

            Case ConsoleKey.F1
                ShowHelp()
                RenderAll() ' Restore screen after Help

            Case ConsoleKey.F2
                SaveProject()
                RenderAll() 

            Case ConsoleKey.F3
                LoadProject()
                RenderAll()

            Case ConsoleKey.F4
                ShowAttributeEditor()
                RenderAll()
            
            Case ConsoleKey.F5
                IsTestMode = Not IsTestMode
                If IsTestMode Then
                    ' Entering test mode - backup the current buffer
                    Array.Copy(Buffer, TestModeBackupBuffer, Buffer.Length)
                End If
                RenderAll()

            Case ConsoleKey.F6
                ShowFieldPropertyEditor()
                RenderAll()

            Case ConsoleKey.F7
                ExportPanel()
                RenderAll()

            Case Else
                If Not Char.IsControl(key.KeyChar) Then
                    Buffer(CursorY, CursorX) = key.KeyChar
                    IsCacheDirty = True
                    ' We must redraw the WHOLE line because if the user typed an attribute, 
                    ' it changes colors for the rest of the line.
                    RenderLine(CursorY)
                    
                    If CursorX < COLS - 1 Then
                        CursorX += 1
                    End If
                End If
        End Select
    End Sub

    ''' <summary>
    ''' Reads input from the console with support for ESC to cancel.
    ''' Returns the input string, or Nothing if canceled.
    ''' </summary>
    Private Function ReadInputWithCancel(Optional prompt As String = "", Optional defaultValue As String = "") As String
        If Not String.IsNullOrEmpty(prompt) Then Console.Write(prompt)
        
        Dim input As New StringBuilder(defaultValue)
        If Not String.IsNullOrEmpty(defaultValue) Then Console.Write(defaultValue)
        
        While True
            If Not Console.KeyAvailable Then
                System.Threading.Thread.Sleep(50)
                Continue While
            End If

            Dim key = Console.ReadKey(True)
            
            If key.Key = ConsoleKey.Escape Then
                Return Nothing
            ElseIf key.Key = ConsoleKey.Enter Then
                Console.WriteLine()
                Return input.ToString()
            ElseIf key.Key = ConsoleKey.Backspace Then
                If input.Length > 0 Then
                    input.Remove(input.Length - 1, 1)
                    Console.Write(vbBack & " " & vbBack)
                End If
            ElseIf Not Char.IsControl(key.KeyChar) Then
                input.Append(key.KeyChar)
                Console.Write(key.KeyChar)
            End If
        End While
    End Function

    Private Sub SaveProject()
        Console.SetCursorPosition(0, ROWS + 2)
        Console.ForegroundColor = ConsoleColor.Gray
        Console.Write("Save Project (.ispfd): ".PadRight(COLS))
        Console.SetCursorPosition(23, ROWS + 2)
        
        Dim filename As String = ReadInputWithCancel()
        If filename Is Nothing Then Return ' Canceled
        
        filename = filename.Trim()
        If String.IsNullOrWhiteSpace(filename) Then Return
        If Not filename.EndsWith(".ispfd", StringComparison.OrdinalIgnoreCase) Then filename &= ".ispfd"
        
        If Not String.IsNullOrWhiteSpace(filename) Then
             Try
                 Dim model As New ProjectModel()
                 model.Rows = ROWS
                 model.Cols = COLS
                 
                 ' Convert buffer to string array
                 Dim strBuffer(ROWS - 1) As String
                 For r As Integer = 0 To ROWS - 1
                     Dim sb As New StringBuilder(COLS)
                     For c As Integer = 0 To COLS - 1
                         sb.Append(Buffer(r, c))
                     Next
                     strBuffer(r) = sb.ToString()
                 Next
                 model.Buffer = strBuffer
                 
                 ' Attributes
                 For Each kvp In AttrManager.GetAttributeDefinitions()
                     model.Attributes(kvp.Key.ToString()) = kvp.Value
                 Next
                 
                 ' Field Properties
                 model.FieldProperties = New List(Of FieldProperty)(FieldProperties.Values)
                 
                 ProjectManager.SaveProject(filename, model)
                 
                 Console.SetCursorPosition(0, ROWS + 2)
                 Console.Write("Project saved successfully! Press any key.".PadRight(COLS))
             Catch ex As Exception
                 Console.SetCursorPosition(0, ROWS + 2)
                 Console.Write($"Save Error: {ex.Message}".PadRight(COLS))
             End Try
             Console.ReadKey()
        End If
    End Sub

    Private Sub ExportPanel()
        Console.SetCursorPosition(0, ROWS + 2)
        Console.ForegroundColor = ConsoleColor.Gray
        Console.Write("Export to Panel (.panel): ".PadRight(COLS))
        Console.SetCursorPosition(26, ROWS + 2)
        
        Dim filename As String = ReadInputWithCancel()
        If filename Is Nothing Then Return ' Canceled
        
        filename = filename.Trim()
        If String.IsNullOrWhiteSpace(filename) Then Return
        If Not filename.EndsWith(".panel", StringComparison.OrdinalIgnoreCase) Then filename &= ".panel"
        
        If Not String.IsNullOrWhiteSpace(filename) Then
             Try
                 PanelWriter.WriteToFile(filename, Buffer, ROWS, COLS, AttrManager, FieldProperties)
                 Console.SetCursorPosition(0, ROWS + 2)
                 Console.Write("Exported successfully! Press any key.".PadRight(COLS))
             Catch ex As Exception
                 Console.SetCursorPosition(0, ROWS + 2)
                 Console.Write($"Export Error: {ex.Message}".PadRight(COLS))
             End Try
             Console.ReadKey()
        End If
    End Sub

    Private Sub ShowHelp()
        Console.CursorVisible = False
        Console.Clear()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("================ ISPF DESIGNER HELP ================")
        Console.WriteLine()
        Console.WriteLine("NAVIGATION:")
        Console.WriteLine("  Arrow Keys  : Move Cursor")
        Console.WriteLine("  Bksp/Del    : Delete Characters")
        Console.WriteLine("  Enter       : Carriage Return")
        Console.WriteLine()
        Console.WriteLine("FORMATTING CODES (WYSIWYG):")
        Console.WriteLine("  %  : High Intensity Text (White)")
        Console.WriteLine("  +  : Low Intensity Text  (Cyan)")
        Console.WriteLine("  _  : Input Field         (Red)")
        Console.WriteLine()
        Console.WriteLine("COMMANDS:")
        Console.WriteLine("  F1 : This Help Screen")
        Console.WriteLine("  F2 : Save PROJECT (.ispfd)")
        Console.WriteLine("  F3 : Load PROJECT (.ispfd)")
        Console.WriteLine("  F4 : Edit Attributes")
        Console.WriteLine("  F5 : Test Mode (Runtime)")
        Console.WriteLine("  F6 : Edit Field Properties")
        Console.WriteLine("  F7 : EXPORT to ISPF Panel (.panel)")
        Console.WriteLine("  ESC: Exit Application")
        Console.WriteLine()
        Console.WriteLine("====================================================")
        Console.Write("Press any key to return to editor...")
        Console.ReadKey()
    End Sub
    
    Private Sub ShowAttributeEditor()
        Console.CursorVisible = False
        Console.Clear()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("================ ATTRIBUTE EDITOR (F4) ================")
        Console.WriteLine("Existing Attributes:")
        Console.WriteLine()
        
        For Each kvp In AttrManager.GetAttributeDefinitions()
            Console.WriteLine($"  [{kvp.Key}] : {kvp.Value}")
        Next
        
        Console.WriteLine()
        Console.WriteLine("-------------------------------------------------------")
        Console.WriteLine("To ADD or UPDATE, type a character and definition.")
        Console.WriteLine("Example: ? TYPE(TEXT) COLOR(YELLOW)")
        Console.WriteLine("To DELETE, type the character followed by DELETE.")
        Console.WriteLine("Example: % DELETE")
        Console.WriteLine("Press ENTER (empty) to return.")
        Console.WriteLine("-------------------------------------------------------")
        Console.CursorVisible = True
        
        While True
            Dim input As String = ReadInputWithCancel("> ")
            
            If input Is Nothing OrElse String.IsNullOrWhiteSpace(input) Then Exit While
            
            Dim parts As String() = input.Split(" "c, 2)
            If parts.Length > 0 AndAlso parts(0).Length = 1 Then
                Dim c As Char = parts(0)(0)
                
                If parts.Length = 2 Then
                    Dim def As String = parts(1).Trim()
                    If def.Equals("DELETE", StringComparison.OrdinalIgnoreCase) Then
                        AttrManager.RemoveAttribute(c)
                        Console.WriteLine($"Removed '{c}'")
                    Else
                        AttrManager.SetAttribute(c, def)
                        Console.WriteLine($"Set '{c}' = {def}")
                    End If
                Else
                     Console.WriteLine("Invalid format. Usage: Char Definition")
                End If
            Else
                Console.WriteLine("Invalid Input. Must start with a single character.")
            End If
        End While
    End Sub

    Private Sub ShowFieldPropertyEditor()
        Dim field = FindFieldInCache(CursorY, CursorX)
        If field.Row = -1 Then
            Console.SetCursorPosition(0, ROWS + 2)
            Console.ForegroundColor = ConsoleColor.Red
            Console.Write("Error: Not in an input field. Press any key.".PadRight(COLS))
            Console.ReadKey()
            Return
        End If

        Dim attrCol As Integer = field.StartCol - 1
        Dim key = (field.Row, attrCol)
        
        If Not FieldProperties.ContainsKey(key) Then
            ' Initialize with defaults if not exists
            Dim isZVar As Boolean = (Buffer(field.Row, field.StartCol) = "Z"c OrElse Buffer(field.Row, field.StartCol) = "z"c)
            FieldProperties(key) = New FieldProperty With {
                .Row = field.Row,
                .Col = attrCol,
                .Length = field.EndCol - field.StartCol + 1,
                .Name = If(isZVar, "(placeholder)", ""),
                .Type = "TEXT"
            }
        End If

        Dim fp = FieldProperties(key)

        Console.CursorVisible = False
        Console.Clear()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("================ FIELD PROPERTY EDITOR (F6) ================")
        Console.WriteLine($"Field at Row {field.Row + 1}, Col {attrCol + 1}")
        Console.WriteLine($"Current Content: {If(GetFieldContent(field).Length > 60, GetFieldContent(field).Substring(0, 57) & "...", GetFieldContent(field))}")
        Console.WriteLine()
        Console.CursorVisible = True

        While True
            ' Redraw menu with padding to clear old text
            Console.SetCursorPosition(0, 5)
            Console.WriteLine($"1. Name/Variable: {fp.Name.PadRight(40)}")
            Console.WriteLine($"2. Length       : {fp.Length.ToString().PadRight(40)}")
            Console.WriteLine($"3. Type         : {fp.Type.PadRight(40)}")
            Console.WriteLine()
            Console.WriteLine("Enter Number (1-3) to edit, or ENTER to return.".PadRight(COLS))
            Console.WriteLine("------------------------------------------------------------".PadRight(COLS))
            
            Console.SetCursorPosition(0, 11) ' Position for the prompt
            Console.Write("> ".PadRight(COLS))
            Console.SetCursorPosition(2, 11) ' Position for input after prompt
            
            Dim input As String = ReadInputWithCancel()
            If input Is Nothing OrElse String.IsNullOrWhiteSpace(input) Then Exit While

            Select Case input.Trim()
                Case "1"
                    Console.SetCursorPosition(0, 12)
                    Console.Write("New Name: ".PadRight(COLS))
                    Console.SetCursorPosition(10, 12)
                    Dim newName = ReadInputWithCancel()
                    If newName IsNot Nothing Then fp.Name = newName.Trim()
                Case "2"
                    Console.SetCursorPosition(0, 12)
                    Console.Write("New Length: ".PadRight(COLS))
                    Console.SetCursorPosition(12, 12)
                    Dim lenStr As String = ReadInputWithCancel()
                    Dim newLen As Integer
                    If lenStr IsNot Nothing AndAlso Integer.TryParse(lenStr, newLen) Then fp.Length = newLen
                Case "3"
                    Console.SetCursorPosition(0, 12)
                    Console.Write("New Type (TEXT/ALPHA/NUM): ".PadRight(COLS))
                    Console.SetCursorPosition(27, 12)
                    Dim newType = ReadInputWithCancel()
                    If newType IsNot Nothing Then fp.Type = newType.Trim().ToUpper()
                Case Else
                    Console.SetCursorPosition(0, 12)
                    Console.Write("Invalid option. Press any key.".PadRight(COLS))
                    Console.ReadKey()
            End Select
            
            ' Clear the prompt/input area for next iteration
            Console.SetCursorPosition(0, 11)
            Console.Write(" ".PadRight(COLS))
            Console.SetCursorPosition(0, 12)
            Console.Write(" ".PadRight(COLS))
        End While
    End Sub

    Private Function FindFieldInCache(r As Integer, c As Integer) As (Row As Integer, StartCol As Integer, EndCol As Integer)
        If IsCacheDirty Then RebuildFieldCache()
        For Each field In FieldCache
            ' Field starts at StartCol, Attribute is at StartCol-1
            If field.Row = r AndAlso c >= field.StartCol - 1 AndAlso c <= field.EndCol Then
                Return field
            End If
        Next
        Return (-1, -1, -1)
    End Function

    Private Function GetFieldContent(field As (Row As Integer, StartCol As Integer, EndCol As Integer)) As String
        Dim sb As New StringBuilder()
        For c As Integer = field.StartCol To field.EndCol
            sb.Append(Buffer(field.Row, c))
        Next
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Returns a dictionary of field variable names and their current values in the buffer.
    ''' </summary>
    Public Function GetFieldValues() As Dictionary(Of String, String)
        Dim result As New Dictionary(Of String, String)
        If IsCacheDirty Then RebuildFieldCache()

        ' We want to process fields in order of appearance (Top -> Bottom, Left -> Right)
        ' FieldCache is naturally ordered because of how RebuildFieldCache works.
        
        Dim zVarNames As New List(Of String)
        For Each fp In FieldProperties.Values
             ' We need to find if this is a Z-var or regular var
             ' Implementation detail: if user named it, we use it.
             ' If it's a Z-placeholder, we'll handle it below.
        Next

        ' Actually, let's keep it simple: 
        ' 1. Identify all fields in order.
        ' 2. If a field has a defined name in FieldProperties, use that name.
        ' 3. If a field contains 'Z' and has no name or "(placeholder)", it's a candidate for .ZVARS ordering?
        ' NO, the user should define the order. ISPF .ZVARS is an ordered list.
        
        ' Revised approach for this designer:
        ' Store ZVARS as a specific list or just use the ordered appearance of fields starting with 'Z'.
        
        For Each field In FieldCache
            Dim fp As FieldProperty = Nothing
            Dim key = (field.Row, field.StartCol - 1)
            Dim content As String = GetFieldContent(field).Trim()
            
            If FieldProperties.TryGetValue(key, fp) AndAlso Not String.IsNullOrWhiteSpace(fp.Name) AndAlso fp.Name <> "(placeholder)" Then
                result(fp.Name) = content
            End If
        Next
        
        Return result
    End Function

    Public Sub LoadFile(filename As String)
        If Not String.IsNullOrWhiteSpace(filename) Then
            Try
                PanelReader.ReadFromFile(filename, Buffer, ROWS, COLS, AttrManager, FieldProperties)
                IsCacheDirty = True
            Catch ex As Exception
                Console.SetCursorPosition(0, ROWS + 2)
                Console.Write($"Error: {ex.Message}".PadRight(COLS))
                Console.ReadKey
            End Try
        End If
    End Sub

    Private Sub LoadProject()
        Console.Clear()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("================ LOAD PROJECT / IMPORT PANEL ================")
        Console.WriteLine()
        
        Dim files As New List(Of String)
        files.AddRange(Directory.GetFiles(".", "*.ispfd"))
        files.AddRange(Directory.GetFiles(".", "*.panel"))
        
        If files.Count > 0 Then
            Console.WriteLine("Available files in current directory:")
            Console.WriteLine("-------------------------------------")
            For Each f In files
                Console.WriteLine($"  {Path.GetFileName(f)}")
            Next
            Console.WriteLine("-------------------------------------")
        Else
            Console.WriteLine("(No .ispfd or .panel files found in current directory)")
        End If
        
        Console.WriteLine()
        Console.Write("Enter filename (or ESC to cancel): ")
        Dim filename As String = ReadInputWithCancel()
        
        ' Restore screen before processing or returning
        RenderAll()

        If filename Is Nothing Then Return ' Canceled
        filename = filename.Trim()
            ' Smart extension detection
            If Not File.Exists(filename) Then
                If File.Exists(filename & ".ispfd") Then
                    filename &= ".ispfd"
                ElseIf File.Exists(filename & ".panel") Then
                    filename &= ".panel"
                ElseIf Not filename.Contains(".") Then
                    ' Default to .ispfd if no dots and neither exists (to show clear error)
                    filename &= ".ispfd"
                End If
            End If

            Try
                If filename.EndsWith(".panel", StringComparison.OrdinalIgnoreCase) Then
                    ' Legacy Import
                    LoadFile(filename)
                    Console.SetCursorPosition(0, ROWS + 2)
                    Console.Write("Panel imported successfully! Press any key.".PadRight(COLS))
                Else
                    ' Native Project Load
                    Dim model = ProjectManager.LoadProject(filename)
                    
                    ' Restore attributes
                    For Each kvp In model.Attributes
                        AttrManager.SetAttribute(kvp.Key(0), kvp.Value)
                    Next
                    
                    ' Restore buffer
                    For r As Integer = 0 To Math.Min(ROWS, model.Buffer.Length) - 1
                        Dim line = model.Buffer(r)
                        For c As Integer = 0 To Math.Min(COLS, line.Length) - 1
                            Buffer(r, c) = line(c)
                        Next
                    Next
                    
                    ' Restore field properties
                    FieldProperties.Clear()
                    For Each fp In model.FieldProperties
                        FieldProperties((fp.Row, fp.Col)) = fp
                    Next

                    IsCacheDirty = True
                    Console.SetCursorPosition(0, ROWS + 2)
                    Console.Write("Project loaded successfully! Press any key.".PadRight(COLS))
                End If
            Catch ex As Exception
                Console.SetCursorPosition(0, ROWS + 2)
                Console.Write($"Load Error: {ex.Message}".PadRight(COLS))
            End Try
            Console.ReadKey()
    End Sub


    Private Sub HandleTestInput(key As ConsoleKeyInfo)
         Select Case key.Key
            Case ConsoleKey.F5
                ' Exiting test mode - restore the design buffer
                Array.Copy(TestModeBackupBuffer, Buffer, Buffer.Length)
                IsTestMode = Not IsTestMode
                IsCacheDirty = True
                RenderAll()
            
            Case ConsoleKey.Tab
                If key.Modifiers.HasFlag(ConsoleModifiers.Shift) Then
                    JumpToPrevField()
                Else
                    JumpToNextField()
                End If
            
            Case ConsoleKey.LeftArrow
                MoveCursor(-1, 0)
            Case ConsoleKey.RightArrow
                MoveCursor(1, 0)
            Case ConsoleKey.UpArrow
                MoveCursor(0, -1)
            Case ConsoleKey.DownArrow
                MoveCursor(0, 1)

            Case ConsoleKey.Enter
                CursorX = 0
                CursorY = Math.Min(ROWS - 1, CursorY + 1)
            Case ConsoleKey.Backspace
                 If IsInputField(CursorY, CursorX) AndAlso CursorX > 0 Then
                     ' Ensure we don't delete the start attribute itself
                     If Not AttrManager.IsAttributeChar(Buffer(CursorY, CursorX - 1)) Then
                         IsCacheDirty = True
                         CursorX -= 1
                         Buffer(CursorY, CursorX) = " "c
                         RenderLine(CursorY)
                     End If
                 End If

            Case Else
                ' Allow typing only if in input field
                 If Not Char.IsControl(key.KeyChar) Then
                     If IsInputField(CursorY, CursorX) Then
                         Buffer(CursorY, CursorX) = key.KeyChar
                         IsCacheDirty = True
                         RenderLine(CursorY)
                         If CursorX < COLS - 1 Then CursorX += 1
                    End If
                End If
        End Select
    End Sub

    Private Sub JumpToNextField()
        If IsCacheDirty Then RebuildFieldCache()
        
        If FieldCache.Count = 0 Then Return
        
        ' Find first field after current position
        For Each field In FieldCache
            If field.Row > CursorY OrElse (field.Row = CursorY AndAlso field.StartCol > CursorX) Then
                CursorY = field.Row
                CursorX = field.StartCol
                Return
            End If
        Next
        
        ' Wrap to first field
        If FieldCache.Count > 0 Then
            CursorY = FieldCache(0).Row
            CursorX = FieldCache(0).StartCol
        End If
    End Sub

    Private Sub JumpToPrevField()
        If IsCacheDirty Then RebuildFieldCache()
        
        If FieldCache.Count = 0 Then Return
        
        ' Find last field before current position
        For i As Integer = FieldCache.Count - 1 To 0 Step -1
            Dim field = FieldCache(i)
            If field.Row < CursorY OrElse (field.Row = CursorY AndAlso field.StartCol < CursorX) Then
                CursorY = field.Row
                CursorX = field.StartCol
                Return
            End If
        Next
        
        ' Wrap to last field
        If FieldCache.Count > 0 Then
            Dim lastField = FieldCache(FieldCache.Count - 1)
            CursorY = lastField.Row
            CursorX = lastField.StartCol
        End If
    End Sub



    Private Function IsInputField(r As Integer, c As Integer) As Boolean
        If IsCacheDirty Then RebuildFieldCache()
        
        For Each field In FieldCache
            If field.Row = r AndAlso c >= field.StartCol AndAlso c <= field.EndCol Then
                Return True
            End If
        Next
        
        Return False
    End Function

End Class
End Namespace
