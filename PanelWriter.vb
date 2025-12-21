Imports System.IO

Namespace ISPFDesigner
    Public Class PanelWriter
        Public Shared Sub WriteToFile(filename As String, buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager, fieldProperties As Dictionary(Of (Integer, Integer), FieldProperty))
            Using writer As New StreamWriter(filename)
                ' 1. Write Attributes
                writer.WriteLine(")ATTR")
                For Each kvp In attrManager.GetAttributeDefinitions()
                    writer.WriteLine($"  {kvp.Key} {kvp.Value}")
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
                            ' It's an input field. Determine if it's a Body Variable or Placeholder.
                            lineBuilder.Append(ch)
                            
                            Dim fieldContent As String = ""
                            Dim tempC As Integer = c + 1
                            While tempC < cols AndAlso Not attrManager.IsAttributeChar(buffer(r, tempC))
                                fieldContent &= buffer(r, tempC)
                                tempC += 1
                            End While
                            
                            Dim trimmedContent As String = fieldContent.Trim()
                            Dim varName As String = ""
                            Dim isPlaceholder As Boolean = False
                            
                            ' Check metadata first
                            If fieldProperties.ContainsKey((r, c)) Then
                                varName = fieldProperties((r, c)).Name
                                If varName = "(placeholder)" Then varName = ""
                            End If
                            
                            ' Decision Logic:
                            ' If it looks like a variable (starts with A-Z, @, #, $), it's a Body Variable.
                            ' Otherwise (starts with space or 'Z'), it's a Placeholder for .ZVARS.
                            If trimmedContent.Length > 0 AndAlso Not (trimmedContent.StartsWith("Z", StringComparison.OrdinalIgnoreCase) AndAlso trimmedContent.Length = 1) Then
                                ' Likely a Body Variable (e.g. _ADDR1)
                                lineBuilder.Append(fieldContent)
                                varName = trimmedContent ' Buffer takes precedence for Body Variables
                            Else
                                ' Placeholder (e.g. _Z or _  )
                                If Not String.IsNullOrWhiteSpace(varName) Then
                                    zVarNames.Add(varName)
                                    lineBuilder.Append("Z".PadRight(fieldContent.Length))
                                Else
                                    lineBuilder.Append(fieldContent)
                                End If
                            End If
                            
                            ' Add to PROC if validation is needed
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
                writer.WriteLine(")BODY")
                For Each bLine In bodyLines
                    writer.WriteLine(bLine)
                Next
                
                ' 4. Write INIT (.ZVARS)
                writer.WriteLine(")INIT")
                If zVarNames.Count > 0 Then
                    writer.WriteLine($"  .ZVARS = '({String.Join(" ", zVarNames)})'")
                End If
                
                ' 5. Write PROC
                writer.WriteLine(")PROC")
                For Each stmt In procStatements
                    writer.WriteLine(stmt)
                Next
                
                writer.WriteLine(")END")
            End Using
        End Sub
    End Class
End Namespace
