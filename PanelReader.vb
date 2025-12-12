Imports System.IO

Public Class PanelReader
    Public Shared Sub ReadFromFile(filename As String, ByRef buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager)
        If Not File.Exists(filename) Then
            Throw New FileNotFoundException("File not found.")
        End If

        Dim lines As String() = File.ReadAllLines(filename)
        Dim inBody As Boolean = False
        Dim inAttr As Boolean = False
        Dim currentRow As Integer = 0
        
        ' Clear buffer first
        For r As Integer = 0 To rows - 1
            For c As Integer = 0 To cols - 1
                buffer(r, c) = " "c
            Next
        Next

        For Each line As String In lines
            Dim trimLine As String = line.Trim()
            
            If trimLine.StartsWith(")") Then
                inBody = False
                inAttr = False
                
                If trimLine.Contains(")ATTR") Then
                    inAttr = True
                    ' We might want to clear existing attributes or merge?
                    ' Let's clear defaults if we are loading a full file definition
                    ' but usually we want to keep defaults unless overwritten.
                    ' Let's assume merge/overwrite behavior.
                ElseIf trimLine.Contains(")BODY") Then
                    inBody = True
                End If
                Continue For
            End If

            If inAttr Then
                ' Parse Attribute Line: "  % TYPE(TEXT)..."
                ' Simple parser: First char is key, rest is value
                If trimLine.Length > 2 Then
                    Dim key As Char = trimLine(0)
                    Dim value As String = trimLine.Substring(1).Trim()
                    attrManager.SetAttribute(key, value)
                End If
            End If

            If inBody Then
                If currentRow < rows Then
                    Dim charArray As Char() = line.ToCharArray()
                    For c As Integer = 0 To Math.Min(charArray.Length, cols) - 1
                        buffer(currentRow, c) = charArray(c)
                    Next
                    currentRow += 1
                End If
            End If
        Next
    End Sub
End Class
