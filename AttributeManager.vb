Imports System
Imports System.Collections.Generic

Public Class AttributeManager
    ' Maps Char -> ISPF Definition String
    Public Property Attributes As New Dictionary(Of Char, String)

    Public Sub New()
        ' Default Attributes
        Attributes.Add("%"c, "TYPE(TEXT) INTENS(HIGH)")
        Attributes.Add("+"c, "TYPE(TEXT) INTENS(LOW)")
        Attributes.Add("_"c, "TYPE(INPUT) CAPS(ON)")
        Attributes.Add("$"c, "TYPE(PASSWORD)")
    End Sub

    Public Function IsHiddenAttribute(c As Char) As Boolean
        If Not Attributes.ContainsKey(c) Then Return False
        Dim def As String = Attributes(c).ToUpper()
        Return def.Contains("TYPE(PASSWORD)") OrElse def.Contains("INTENS(NON)")
    End Function

    Public Sub SetAttribute(c As Char, def As String)
        If Attributes.ContainsKey(c) Then
            Attributes(c) = def
        Else
            Attributes.Add(c, def)
        End If
    End Sub

    Public Sub RemoveAttribute(c As Char)
        If Attributes.ContainsKey(c) Then
            Attributes.Remove(c)
        End If
    End Sub

    Public Function GetColorForAttribute(c As Char) As ConsoleColor
        If Not Attributes.ContainsKey(c) Then Return ConsoleColor.Gray

        Dim def As String = Attributes(c).ToUpper()
        
        ' Note: This heuristics can be expanded
        If def.Contains("INTENS(HIGH)") Then Return ConsoleColor.White
        If def.Contains("INTENS(LOW)") Then Return ConsoleColor.Cyan
        If def.Contains("TYPE(INPUT)") Then Return ConsoleColor.Red
        
        If def.Contains("COLOR(WHITE)") Then Return ConsoleColor.White
        If def.Contains("COLOR(RED)") Then Return ConsoleColor.Red
        If def.Contains("COLOR(BLUE)") Then Return ConsoleColor.Blue
        If def.Contains("COLOR(GREEN)") Then Return ConsoleColor.Green
        If def.Contains("COLOR(YELLOW)") Then Return ConsoleColor.Yellow
        If def.Contains("COLOR(PINK)") Then Return ConsoleColor.Magenta
        If def.Contains("COLOR(TURQ)") Then Return ConsoleColor.Cyan
        
        Return ConsoleColor.Cyan ' Default fallback
    End Function

    Public Function IsAttributeChar(c As Char) As Boolean
        Return Attributes.ContainsKey(c)
    End Function
End Class
