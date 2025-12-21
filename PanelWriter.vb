Imports System.IO

Namespace ISPFDesigner
    Public Class PanelWriter
        Public Shared Sub WriteToFile(filename As String, buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager, fieldProperties As Dictionary(Of (Integer, Integer), FieldProperty))
            Dim allBytes As New List(Of Byte)()
            Dim encoding As System.Text.Encoding = System.Text.Encoding.ASCII
            Dim crlf As Byte() = {13, 10} ' CRLF is mandatory for Vista TN3270 to recognize line boundaries

            ' Helper to add a line to the byte list
            Dim AddLine As Action(Of String) = Sub(text)
                allBytes.AddRange(encoding.GetBytes(text))
                allBytes.AddRange(crlf)
            End Sub

            ' 1. Write Attributes
            AddLine(")ATTR")
            For Each kvp In attrManager.GetAttributeDefinitions()
                AddLine($"  {kvp.Key} {kvp.Value}".TrimEnd())
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
                        
                        ' Priority 1: Get name from field properties (F6 editor)
                        If fieldProperties.ContainsKey((r, c)) Then
                            varName = fieldProperties((r, c)).Name
                            If varName = "(placeholder)" Then varName = ""
                        End If
                        
                        ' Priority 2: In-buffer name (if user typed directly in body)
                        If trimmedContent.Length > 0 AndAlso Not (trimmedContent.StartsWith("Z", StringComparison.OrdinalIgnoreCase) AndAlso trimmedContent.Length = 1) Then
                            lineBuilder.Append(fieldContent)
                            varName = trimmedContent ' Buffer name wins
                        Else
                            ' Priority 3: Auto-generate if still empty to avoid ISPP127
                            If String.IsNullOrWhiteSpace(varName) Then
                                varName = $"F{r + 1:D2}{c + 1:D2}"
                            End If
                            
                            ' Every "Z" field must be in .ZVARS
                            zVarNames.Add(varName)
                            
                            ' Pad the "Z" to maintain layout
                            If fieldContent.Length > 0 Then
                                lineBuilder.Append("Z".PadRight(fieldContent.Length))
                            Else
                                lineBuilder.Append("Z"c)
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
            AddLine(")BODY")
            For Each bLine In bodyLines
                AddLine(bLine)
            Next
            
            ' 4. Write INIT (.ZVARS)
            AddLine(")INIT")
            If zVarNames.Count > 0 Then
                AddLine($"  .ZVARS = '({String.Join(" ", zVarNames)})'".TrimEnd())
            End If
            
            ' 5. Write PROC
            AddLine(")PROC")
            For Each stmt In procStatements
                AddLine(stmt.TrimEnd())
            Next
            
            AddLine(")END")

            ' Final write as atomic byte array
            File.WriteAllBytes(filename, allBytes.ToArray())
        End Sub
    End Class
End Namespace
