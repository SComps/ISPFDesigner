Imports System.IO

Public Class PanelWriter
    Public Shared Sub WriteToFile(filename As String, buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager)
        Using writer As New StreamWriter(filename)
            writer.WriteLine(")ATTR")
            
            ' Write dynamic attributes
            For Each kvp In attrManager.Attributes
                writer.WriteLine($"  {kvp.Key} {kvp.Value}")
            Next
            
            writer.WriteLine(")BODY")
            
            For r As Integer = 0 To rows - 1
                Dim line As New System.Text.StringBuilder()
                For c As Integer = 0 To cols - 1
                    line.Append(buffer(r, c))
                Next
                writer.WriteLine(line.ToString().TrimEnd())
            Next
            
            writer.WriteLine(")INIT")
            writer.WriteLine(")PROC")
            writer.WriteLine(")END")
        End Using
    End Sub
End Class
