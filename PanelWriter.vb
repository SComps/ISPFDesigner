Imports System.IO

Namespace ISPFDesigner
    Public Class PanelWriter
        Public Shared Sub WriteToFile(filename As String, buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager, fieldProperties As Dictionary(Of (Integer, Integer), FieldProperty))
            ' Explicitly use ASCII encoding to avoid BOM issues (common in UTF-8)
            Using writer As New StreamWriter(filename, False, System.Text.Encoding.ASCII)
                ' We use vbCrLf explicitly for cross-platform consistency.
                ' IMPORTANT: To avoid record overflow on the mainframe (FB 80), 
                ' we should NOT pad lines to exactly 80 characters if we are also adding 
                ' line delimiters. The transfer tool will handle padding on the host.
                
                ' 1. Write Attributes
                writer.Write(")ATTR" & vbCrLf)
                For Each kvp In attrManager.GetAttributeDefinitions()
                    writer.Write($"  {kvp.Key} {kvp.Value}".TrimEnd() & vbCrLf)
                Next
                
                ' 2. Scan Body and collect Variable Meta
                Dim zVarNames As New List(Of String)
                Dim procStatements As New List(Of String)
                Dim bodyLines(rows - 1) As String
                
                For r As Integer = 0 To rows - 1
                    Dim lineBuilder As New System.Text.StringBuilder(cols)
                    For c As Integer = 0 To cols - 1
                        Dim ch As Char = buffer(r, c)
                        
                        If attrManager.IsInputOrPasswordAttribute(ch) Then
                            lineBuilder.Append(ch)
                            
                            Dim fieldContent As String = ""
                            Dim tempC As Integer = c + 1
                            While tempC < cols AndAlso Not attrManager.IsAttributeChar(buffer(r, tempC))
                                fieldContent &= buffer(r, tempC)
                                tempC += 1
                            End While
                            
                            Dim trimmedContent As String = fieldContent.Trim()
                            Dim varName As String = ""
                            
                            If fieldProperties.ContainsKey((r, c)) Then
                                varName = fieldProperties((r, c)).Name
                                If varName = "(placeholder)" Then varName = ""
                            End If
                            
                            If trimmedContent.Length > 0 AndAlso Not (trimmedContent.StartsWith("Z", StringComparison.OrdinalIgnoreCase) AndAlso trimmedContent.Length = 1) Then
                                lineBuilder.Append(fieldContent)
                                varName = trimmedContent
                            Else
                                If Not String.IsNullOrWhiteSpace(varName) Then
                                    zVarNames.Add(varName)
                                    lineBuilder.Append("Z".PadRight(fieldContent.Length))
                                Else
                                    lineBuilder.Append(fieldContent)
                                End If
                            End If
                            
                            If Not String.IsNullOrWhiteSpace(varName) AndAlso fieldProperties.ContainsKey((r, c)) Then
                                Dim fp = fieldProperties((r, c))
                                If fp.Type = "NUM" OrElse fp.Type = "ALPHA" Then
                                    procStatements.Add($"  VER(&{varName}, NB, {fp.Type})")
                                End If
                            End If
                            
                            c = tempC - 1
                        Else
                            lineBuilder.Append(ch)
                        End If
                    Next
                    bodyLines(r) = lineBuilder.ToString().TrimEnd()
                Next
                
                ' 3. Write Body
                writer.Write(")BODY" & vbCrLf)
                For Each bLine In bodyLines
                    writer.Write(bLine & vbCrLf)
                Next
                
                ' 4. Write INIT (.ZVARS)
                writer.Write(")INIT" & vbCrLf)
                If zVarNames.Count > 0 Then
                    writer.Write($"  .ZVARS = '({String.Join(" ", zVarNames)})'".TrimEnd() & vbCrLf)
                End If
                
                ' 5. Write PROC
                writer.Write(")PROC" & vbCrLf)
                For Each stmt In procStatements
                    writer.Write(stmt.TrimEnd() & vbCrLf)
                Next
                
                writer.Write(")END" & vbCrLf)
            End Using
        End Sub
    End Class
End Namespace
