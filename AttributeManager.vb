Imports System
Imports System.Collections.Generic
Imports System.Text.RegularExpressions

''' <summary>
''' Manages ISPF attribute definitions and provides efficient lookups for rendering.
''' </summary>
Namespace ISPFDesigner
    Public Class AttributeManager
        ' Constants for attribute keywords
        Private Const TYPE_TEXT As String = "TYPE(TEXT)"
        Private Const TYPE_INPUT As String = "TYPE(INPUT)"
        Private Const TYPE_NON_DISPLAY As String = "TYPE(INPUT) INTENS(NON)"
        Private Const INTENS_HIGH As String = "INTENS(HIGH)"
        Private Const INTENS_LOW As String = "INTENS(LOW)"
        Private Const INTENS_NON As String = "INTENS(NON)"
        Private Const CAPS_ON As String = "CAPS(ON)"
        
        ' Color constants
        Private Const COLOR_WHITE As String = "COLOR(WHITE)"
        Private Const COLOR_RED As String = "COLOR(RED)"
        Private Const COLOR_BLUE As String = "COLOR(BLUE)"
        Private Const COLOR_GREEN As String = "COLOR(GREEN)"
        Private Const COLOR_YELLOW As String = "COLOR(YELLOW)"
        Private Const COLOR_PINK As String = "COLOR(PINK)"
        Private Const COLOR_TURQ As String = "COLOR(TURQ)"

        ''' <summary>
        ''' Attribute type enumeration for efficient type checking.
        ''' </summary>
        Public Enum AttributeType
            Text
            Input
            Password
            Other
        End Enum

        ''' <summary>
        ''' Parsed attribute definition for efficient lookups.
        ''' </summary>
        Private Class AttributeDefinition
            Public Property RawDefinition As String
            Public Property UpperDefinition As String
            Public Property IsHidden As Boolean
            Public Property Color As ConsoleColor
            Public Property Type As AttributeType
            Public Property IsInputOrPassword As Boolean
            
            Public Sub New(definition As String)
                RawDefinition = definition
                UpperDefinition = definition.ToUpper()
                Type = ParseType()
                IsInputOrPassword = (Type = AttributeType.Input OrElse Type = AttributeType.Password)
                IsHidden = ParseIsHidden()
                Color = ParseColor()
            End Sub
            
            Private Function ParseType() As AttributeType
                If UpperDefinition.Contains("INTENS(NON)") Then Return AttributeType.Password
                If UpperDefinition.Contains(TYPE_INPUT) Then Return AttributeType.Input
                If UpperDefinition.Contains(TYPE_TEXT) Then Return AttributeType.Text
                Return AttributeType.Other
            End Function
            
            Private Function ParseIsHidden() As Boolean
                Return Type = AttributeType.Password OrElse UpperDefinition.Contains(INTENS_NON)
            End Function
            
            Private Function ParseColor() As ConsoleColor
                ' Priority order: explicit COLOR() > intensity-based > type-based
                If UpperDefinition.Contains(COLOR_WHITE) Then Return ConsoleColor.White
                If UpperDefinition.Contains(COLOR_RED) Then Return ConsoleColor.Red
                If UpperDefinition.Contains(COLOR_BLUE) Then Return ConsoleColor.Blue
                If UpperDefinition.Contains(COLOR_GREEN) Then Return ConsoleColor.Green
                If UpperDefinition.Contains(COLOR_YELLOW) Then Return ConsoleColor.Yellow
                If UpperDefinition.Contains(COLOR_PINK) Then Return ConsoleColor.Magenta
                If UpperDefinition.Contains(COLOR_TURQ) Then Return ConsoleColor.Cyan
                
                ' Fallback to intensity-based colors
                If UpperDefinition.Contains(INTENS_HIGH) Then Return ConsoleColor.White
                If UpperDefinition.Contains(INTENS_LOW) Then Return ConsoleColor.Cyan
                If Type = AttributeType.Input Then Return ConsoleColor.Red
                
                Return ConsoleColor.Cyan ' Default fallback
            End Function
        End Class

        ' Maps Char -> Parsed Attribute Definition
        Private ReadOnly _attributes As New Dictionary(Of Char, AttributeDefinition)

        ''' <summary>
        ''' Gets a read-only enumerable of attribute definitions.
        ''' Returns key-value pairs without allocating a new dictionary.
        ''' </summary>
        Public Function GetAttributeDefinitions() As IEnumerable(Of KeyValuePair(Of Char, String))
            Return _attributes.Select(Function(kvp) New KeyValuePair(Of Char, String)(kvp.Key, kvp.Value.RawDefinition))
        End Function

        ''' <summary>
        ''' Initializes the AttributeManager with default ISPF attributes.
        ''' </summary>
        Public Sub New()
            ' Default Attributes
            SetAttribute("%"c, INTENS_HIGH & " " & TYPE_TEXT)
            SetAttribute("+"c, INTENS_LOW & " " & TYPE_TEXT)
            SetAttribute("_"c, TYPE_INPUT & " " & CAPS_ON)
            SetAttribute("$"c, TYPE_NON_DISPLAY)
        End Sub

        ''' <summary>
        ''' Determines if an attribute should be hidden (password or non-display).
        ''' </summary>
        ''' <param name="c">The attribute character.</param>
        ''' <returns>True if the attribute is hidden; otherwise, false.</returns>
        Public Function IsHiddenAttribute(c As Char) As Boolean
            Dim def As AttributeDefinition = Nothing
            If _attributes.TryGetValue(c, def) Then
                Return def.IsHidden
            End If
            Return False
        End Function

        ''' <summary>
        ''' Sets or updates an attribute definition.
        ''' </summary>
        ''' <param name="c">The attribute character.</param>
        ''' <param name="def">The ISPF definition string.</param>
        Public Sub SetAttribute(c As Char, def As String)
            _attributes(c) = New AttributeDefinition(def)
        End Sub

        ''' <summary>
        ''' Removes an attribute definition.
        ''' </summary>
        ''' <param name="c">The attribute character to remove.</param>
        Public Sub RemoveAttribute(c As Char)
            _attributes.Remove(c)
        End Sub

        ''' <summary>
        ''' Gets the console color for an attribute character.
        ''' </summary>
        ''' <param name="c">The attribute character.</param>
        ''' <returns>The console color to use for rendering.</returns>
        Public Function GetColorForAttribute(c As Char) As ConsoleColor
            Dim def As AttributeDefinition = Nothing
            If _attributes.TryGetValue(c, def) Then
                Return def.Color
            End If
            Return ConsoleColor.Gray
        End Function

        ''' <summary>
        ''' Checks if a character is a defined attribute.
        ''' </summary>
        ''' <param name="c">The character to check.</param>
        ''' <returns>True if the character is a defined attribute; otherwise, false.</returns>
        Public Function IsAttributeChar(c As Char) As Boolean
            Return _attributes.ContainsKey(c)
        End Function

        ''' <summary>
        ''' Checks if an attribute character represents an input or password field.
        ''' This is optimized for frequent calls in test mode.
        ''' </summary>
        ''' <param name="c">The attribute character.</param>
        ''' <returns>True if the attribute is TYPE(INPUT) or TYPE(PASSWORD); otherwise, false.</returns>
        Public Function IsInputOrPasswordAttribute(c As Char) As Boolean
            Dim def As AttributeDefinition = Nothing
            If _attributes.TryGetValue(c, def) Then
                Return def.IsInputOrPassword
            End If
            Return False
        End Function
    End Class
End Namespace
