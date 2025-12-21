Imports System.IO

Namespace ISPFDesigner
    Public Class PanelWriter
        Public Shared Sub WriteToFile(filename As String, buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager, fieldProperties As Dictionary(Of (Integer, Integer), FieldProperty))
            ' Use FileStream directly to control every byte precisely.
            ' This avoids platform-specific line ending conversions.
            Dim crlf As Byte() = {13, 10}
            Dim encoding As System.Text.Encoding = System.Text.Encoding.ASCII

            Using fs As New FileStream(filename, FileMode.Create, FileAccess.Write)
                ' Helper to write a string as ASCII bytes followed by CRLF
                Dim WriteLine As Action(Of String) = Sub(text)
                    Dim bytes = encoding.GetBytes(text)
                    fs.Write(bytes, 0, bytes.Length)
                    fs.Write(crlf, 0, crlf.Length)
                End Sub

                ' 1. Write Attributes
                WriteLine(")ATTR")
                For Each kvp In attrManager.GetAttributeDefinitions()
                    WriteLine($"  {kvp.Key} {kvp.Value}".TrimEnd())
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
                WriteLine(")BODY")
                For Each bLine In bodyLines
                    WriteLine(bLine)
                Next
                
                ' 4. Write INIT (.ZVARS)
                WriteLine(")INIT")
                If zVarNames.Count > 0 Then
                    WriteLine($"  .ZVARS = '({String.Join(" ", zVarNames)})'".TrimEnd())
                End If
                
                ' 5. Write PROC
                WriteLine(")PROC")
                For Each stmt In procStatements
                    WriteLine(stmt.TrimEnd())
                Next
                
                WriteLine(")END")
            End Using
        End Sub
    End Class
End Namespace
