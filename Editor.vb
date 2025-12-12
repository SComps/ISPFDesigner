Imports System
Imports System.Text

Public Class Editor
    Private Const ROWS As Integer = 24
    Private Const COLS As Integer = 80
    
    Private Buffer(ROWS - 1, COLS - 1) As Char
    Private CursorX As Integer = 0
    Private CursorY As Integer = 0
    Private IsRunning As Boolean = True
    Private AttrManager As New AttributeManager() 

    Public Sub New()
        ' Initialize buffer with spaces
        For r As Integer = 0 To ROWS - 1
            For c As Integer = 0 To COLS - 1
                Buffer(r, c) = " "c
            Next
        Next
        
        ' Default size
        Try
            If Console.WindowHeight < ROWS + 2 Then Console.WindowHeight = ROWS + 2
            If Console.WindowWidth < COLS Then Console.WindowWidth = COLS
        Catch
            ' Ignore resizing errors
        End Try
    End Sub

    Public Sub Run()
        Console.Clear()
        RenderAll() ' Initial full draw
        
        While IsRunning
            UpdateStatusBar()
            Console.SetCursorPosition(CursorX, CursorY + 1)
            HandleInput()
        End While
        
        Console.Clear()
        Console.WriteLine("Exited ISPF Designer.")
    End Sub

    Private Sub RenderAll()
        Console.Clear()
        UpdateStatusBar()
        For r As Integer = 0 To ROWS - 1
            RenderLine(r)
        Next
        Console.SetCursorPosition(CursorX, CursorY + 1)
    End Sub

    Private Sub UpdateStatusBar()
        Console.SetCursorPosition(0, 0)
        Console.BackgroundColor = ConsoleColor.Blue
        Console.ForegroundColor = ConsoleColor.White
        Console.Write($"Pos: {CursorY + 1:D2},{CursorX + 1:D2} | F1:Help | F2:Save | F3:Load | F4:Attrs | ESC:Exit".PadRight(COLS))
        Console.ResetColor()
    End Sub

    Private Sub RenderLine(r As Integer)
        Console.SetCursorPosition(0, r + 1)
        
        Dim currentColor As ConsoleColor = ConsoleColor.Green ' Default to Low Intensity
        
        For c As Integer = 0 To COLS - 1
            Dim ch As Char = Buffer(r, c)
            
            If AttrManager.IsAttributeChar(ch) Then
                Console.ForegroundColor = ConsoleColor.DarkYellow ' Control char color
                Console.Write(ch)
                currentColor = AttrManager.GetColorForAttribute(ch)
            Else
                Console.ForegroundColor = currentColor
                Console.Write(ch)
            End If
        Next
    End Sub

    Private Sub HandleInput()
        If Not Console.KeyAvailable Then
             return
        End If

        Dim key As ConsoleKeyInfo = Console.ReadKey(True)
        Dim needsRedraw As Boolean = False
        Dim currentRowForRedraw As Integer = CursorY

        Select Case key.Key
            Case ConsoleKey.LeftArrow
                CursorX = Math.Max(0, CursorX - 1)
            
            Case ConsoleKey.RightArrow
                CursorX = Math.Min(COLS - 1, CursorX + 1)
            
            Case ConsoleKey.UpArrow
                CursorY = Math.Max(0, CursorY - 1)
            
            Case ConsoleKey.DownArrow
                CursorY = Math.Min(ROWS - 1, CursorY + 1)
            
            Case ConsoleKey.Backspace
                If CursorX > 0 Then
                    CursorX -= 1
                    Buffer(CursorY, CursorX) = " "c
                    RenderLine(CursorY) 
                End If

            Case ConsoleKey.Delete
                Buffer(CursorY, CursorX) = " "c
                RenderLine(CursorY)

            Case ConsoleKey.Enter
                CursorX = 0
                CursorY = Math.Min(ROWS - 1, CursorY + 1)

            Case ConsoleKey.Escape
                IsRunning = False

            Case ConsoleKey.F1
                ShowHelp()
                RenderAll() ' Restore screen after Help

            Case ConsoleKey.F2
                SavePanel()
                RenderAll() ' Restore status bar msg/screen

            Case ConsoleKey.F3
                LoadPanel()
                RenderAll()

            Case ConsoleKey.F4
                ShowAttributeEditor()
                RenderAll()

            Case Else
                If Not Char.IsControl(key.KeyChar) Then
                    Buffer(CursorY, CursorX) = key.KeyChar
                    ' We must redraw the WHOLE line because if the user typed an attribute, 
                    ' it changes colors for the rest of the line.
                    RenderLine(CursorY)
                    
                    If CursorX < COLS - 1 Then
                        CursorX += 1
                    End If
                End If
        End Select
    End Sub

    Private Sub SavePanel()
        Console.SetCursorPosition(0, ROWS + 2)
        Console.ForegroundColor = ConsoleColor.Gray
        Console.Write("Filename: ".PadRight(COLS))
        Console.SetCursorPosition(10, ROWS + 2)
        
        Dim filename As String = Console.ReadLine()
        
        If Not String.IsNullOrWhiteSpace(filename) Then
             PanelWriter.WriteToFile(filename, Buffer, ROWS, COLS, AttrManager)
             Console.SetCursorPosition(0, ROWS + 2)
             Console.Write("Saved successfully! Press any key.".PadRight(COLS))
             Console.ReadKey()
        End If
    End Sub

    Private Sub ShowHelp()
        Console.Clear()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("================ ISPF DESIGNER HELP ================")
        ' ... (Same content as before, abbreviated for brevity in this replace?) ...
        ' Actually I should preserve the content. I'll paste the full content again to be safe.
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
        Console.WriteLine("  F2 : Save to .panel file")
        Console.WriteLine("  F3 : Load existing .panel file")
        Console.WriteLine("  ESC: Exit Application")
        Console.WriteLine()
        Console.WriteLine("====================================================")
        Console.Write("Press any key to return to editor...")
        Console.ReadKey()
    End Sub
    
    Private Sub ShowAttributeEditor()
        Console.Clear()
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("================ ATTRIBUTE EDITOR (F4) ================")
        Console.WriteLine("Existing Attributes:")
        Console.WriteLine()
        
        For Each kvp In AttrManager.Attributes
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
        
        While True
            Console.Write("> ")
            Dim input As String = Console.ReadLine()
            
            If String.IsNullOrWhiteSpace(input) Then Exit While
            
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

    Private Sub LoadPanel()
        Console.SetCursorPosition(0, ROWS + 2)
        Console.ForegroundColor = ConsoleColor.Gray
        Console.Write("Load File: ".PadRight(COLS))
        Console.SetCursorPosition(11, ROWS + 2)
        Dim filename As String = Console.ReadLine()
        
        If Not String.IsNullOrWhiteSpace(filename) Then
             Try
                 PanelReader.ReadFromFile(filename, Buffer, ROWS, COLS, AttrManager)
                 Console.SetCursorPosition(0, ROWS + 2)
                 Console.Write("Loaded successfully! Press any key.".PadRight(COLS))
             Catch ex As Exception
                 Console.SetCursorPosition(0, ROWS + 2)
                 Console.Write($"Error: {ex.Message}".PadRight(COLS))
             End Try
             Console.ReadKey()
        End If
    End Sub

End Class
