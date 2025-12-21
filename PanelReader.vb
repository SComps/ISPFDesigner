Imports System.IO
Imports System.Text.RegularExpressions

Namespace ISPFDesigner
    Public Class PanelReader
        Public Shared Sub ReadFromFile(filename As String, ByRef buffer(,) As Char, rows As Integer, cols As Integer, attrManager As AttributeManager, fieldProperties As Dictionary(Of (Integer, Integer), FieldProperty))
            If Not File.Exists(filename) Then
                Throw New FileNotFoundException("File not found.")
            End If

            Dim lines As String() = File.ReadAllLines(filename)
            Dim inBody As Boolean = False
            Dim inAttr As Boolean = False
            Dim inInit As Boolean = False
            Dim inField As Boolean = False
            Dim currentRow As Integer = 0
            Dim zVarNames As New List(Of String)
            
            ' Clear buffer and properties first
            For r As Integer = 0 To rows - 1
                For c As Integer = 0 To cols - 1
                    buffer(r, c) = " "c
                Next
            Next
            fieldProperties.Clear()

            For Each line As String In lines
                Dim trimLine As String = line.Trim()
                
                If trimLine.StartsWith(")") Then
                    inBody = False
                    inAttr = False
                    inInit = False
                    inField = False
                    
                    If trimLine.Contains(")ATTR") Then
                        inAttr = True
                    ElseIf trimLine.Contains(")BODY") Then
                        inBody = True
                    ElseIf trimLine.Contains(")INIT") Then
                        inInit = True
                    End If
                    Continue For
                End If

                If inAttr Then
                    If trimLine.Length > 2 Then
                        Dim key As Char = trimLine(0)
                        Dim value As String = trimLine.Substring(1).Trim()
                        attrManager.SetAttribute(key, value)
                    End If
                End If

                If inInit Then
                    ' Parse .ZVARS = '(NAME1 NAME2)'
                    If trimLine.ToUpper().Contains(".ZVARS") Then
                        Dim match = Regex.Match(trimLine, "\.ZVARS\s*=\s*'\((.*?)\)'", RegexOptions.IgnoreCase)
                        If match.Success Then
                            Dim names = match.Groups(1).Value.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
                            zVarNames.AddRange(names)
                        End If
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

            ' If we have .ZVARS, map them to 'Z' fields in the buffer
            If zVarNames.Count > 0 Then
                Dim nameIdx As Integer = 0
                For r As Integer = 0 To rows - 1
                    For c As Integer = 0 To cols - 1
                        Dim ch As Char = buffer(r, c)
                        If attrManager.IsInputOrPasswordAttribute(ch) Then
                            If c + 1 < cols AndAlso (buffer(r, c + 1) = "Z"c OrElse buffer(r, c + 1) = "z"c) Then
                                If nameIdx < zVarNames.Count Then
                                    Dim key = (r, c)
                                    If Not fieldProperties.ContainsKey(key) Then
                                        ' Create new metadata if )FIELD didn't already cover it (standard ISPF case)
                                        Dim fp As New FieldProperty With {
                                            .Row = r,
                                            .Col = c,
                                            .Name = zVarNames(nameIdx),
                                            .Type = "TEXT"
                                        }
                                        ' Find length
                                        Dim endC As Integer = c + 1
                                        While endC < cols AndAlso Not attrManager.IsAttributeChar(buffer(r, endC))
                                            endC += 1
                                        End While
                                        fp.Length = endC - (c + 1)
                                        fieldProperties(key) = fp
                                    Else
                                        ' Update existing if name was placeholder
                                        Dim existingFp = fieldProperties(key)
                                        If existingFp.Name = "(placeholder)" Then
                                            existingFp.Name = zVarNames(nameIdx)
                                        End If
                                    End If
                                    nameIdx += 1
                                End If
                            End If
                        End If
                    Next
                Next
            End If
        End Sub
    End Class
End Namespace
