Imports System.IO

Namespace ISPFDesigner
    Public Class PanelWriter
        Public Shared Sub WriteToFile(filename As String, buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager, fieldProperties As Dictionary(Of (Integer, Integer), FieldProperty))
            Using writer As New StreamWriter(filename)
                writer.WriteLine(")ATTR")
                
                ' Write dynamic attributes
                For Each kvp In attrManager.GetAttributeDefinitions()
                    writer.WriteLine($"  {kvp.Key} {kvp.Value}")
                Next
                
                writer.WriteLine(")BODY")
                
                ' Reuse StringBuilder instance for all lines
                Dim line As New System.Text.StringBuilder(cols)
                For r As Integer = 0 To rows - 1
                    line.Clear()
                    For c As Integer = 0 To cols - 1
                        line.Append(buffer(r, c))
                    Next
                    writer.WriteLine(line.ToString().TrimEnd())
                Next
                
                writer.WriteLine(")INIT")
                
                ' .ZVARS logic
                ' Find all 'Z' placeholders in body scan order
                Dim zVarNames As New List(Of String)
                For r As Integer = 0 To rows - 1
                    For c As Integer = 0 To cols - 1
                        Dim ch As Char = buffer(r, c)
                        If attrManager.IsInputOrPasswordAttribute(ch) Then
                            ' Found a field starting at r, c+1
                            If c + 1 < cols AndAlso (buffer(r, c + 1) = "Z"c OrElse buffer(r, c + 1) = "z"c) Then
                                Dim key = (r, c)
                                If fieldProperties.ContainsKey(key) Then
                                    Dim fp = fieldProperties(key)
                                    If Not String.IsNullOrWhiteSpace(fp.Name) AndAlso fp.Name <> "(placeholder)" Then
                                        zVarNames.Add(fp.Name)
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next

                If zVarNames.Count > 0 Then
                    writer.WriteLine($"  .ZVARS = '({String.Join(" ", zVarNames)})'")
                End If

                writer.WriteLine(")PROC")
                writer.WriteLine(")END")
            End Using
        End Sub
    End Class
End Namespace
