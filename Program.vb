Imports System

Namespace ISPFDesigner
    Module Program
        Sub Main(args As String())
            Try
                Console.OutputEncoding = System.Text.Encoding.UTF8
                
                Dim editor As New Editor()
                
                ' Support loading a file on startup if provided as argument
                ' Usage: ISPFDesigner.exe myfile.panel
                If args.Length > 0 AndAlso Not String.IsNullOrWhiteSpace(args(0)) Then
                    editor.LoadFile(args(0))
                End If
                
                editor.Run()
                
            Catch ex As Exception
                Console.Clear()
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("ISPF Designer encountered an error:")
                Console.WriteLine(ex.Message)
                Console.WriteLine()
                Console.WriteLine("Press any key to exit...")
                Console.ResetColor()
                Console.ReadKey()
            Finally
                ' Ensure console is properly reset on exit
                Console.ResetColor()
                Console.Clear()
            End Try
        End Sub
    End Module
End Namespace
