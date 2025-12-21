Imports System.Collections.Generic

Namespace ISPFDesigner
    Public Class ProjectModel
        Public Property Buffer As String()
        Public Property Attributes As New Dictionary(Of String, String)
        Public Property FieldProperties As New List(Of FieldProperty)
        Public Property Rows As Integer = 24
        Public Property Cols As Integer = 80
    End Class
End Namespace
