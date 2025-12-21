Imports System.IO
Imports System.Text
Imports System.Collections.Generic

Namespace ISPFDesigner
    Public Class ProjectManager
        Private Const CURRENT_VERSION As String = "1"

        ''' <summary>
        ''' Saves the project state to a custom text-based format (.ispfd).
        ''' </summary>
        Public Shared Sub SaveProject(filename As String, model As ProjectModel)
            Using writer As New StreamWriter(filename, False, Encoding.UTF8)
                writer.WriteLine($"VER|{CURRENT_VERSION}")
                writer.WriteLine($"SIZE|{model.Rows}|{model.Cols}")
                
                ' Save Attributes
                For Each kvp In model.Attributes
                    writer.WriteLine($"ATTR|{kvp.Key}|{kvp.Value}")
                Next
                
                ' Save Field Properties
                For Each fp In model.FieldProperties
                    writer.WriteLine($"FIELD|{fp.Row}|{fp.Col}|{fp.Name}|{fp.Length}|{fp.Type}")
                Next
                
                ' Save Body
                For Each line In model.Buffer
                    writer.WriteLine($"BODY|{line}")
                Next
            End Using
        End Sub

        ''' <summary>
        ''' Loads the project state from a custom text-based format (.ispfd).
        ''' </summary>
        Public Shared Function LoadProject(filename As String) As ProjectModel
            Dim model As New ProjectModel()
            Dim lines As String() = File.ReadAllLines(filename)
            Dim bodyLines As New List(Of String)

            For Each line In lines
                If String.IsNullOrWhiteSpace(line) Then Continue For
                
                Dim parts As String() = line.Split("|"c)
                If parts.Length < 2 Then Continue For
                
                Dim tag As String = parts(0).ToUpper()
                
                Select Case tag
                    Case "SIZE"
                        If parts.Length >= 3 Then
                            model.Rows = Integer.Parse(parts(1))
                            model.Cols = Integer.Parse(parts(2))
                        End If
                    
                    Case "ATTR"
                        If parts.Length >= 3 Then
                            model.Attributes(parts(1)) = parts(2)
                        End If
                    
                    Case "FIELD"
                        If parts.Length >= 6 Then
                            Dim fp As New FieldProperty With {
                                .Row = Integer.Parse(parts(1)),
                                .Col = Integer.Parse(parts(2)),
                                .Name = parts(3),
                                .Length = Integer.Parse(parts(4)),
                                .Type = parts(5)
                            }
                            model.FieldProperties.Add(fp)
                        End If
                    
                    Case "BODY"
                        ' Reconstruct body line (handle if content itself contains |)
                        Dim content As String = line.Substring(5)
                        bodyLines.Add(content)
                End Select
            Next
            
            model.Buffer = bodyLines.ToArray()
            Return model
        End Function
    End Class
End Namespace
