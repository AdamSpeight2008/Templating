Imports System.Runtime.CompilerServices
Imports Templates_V3.Parser

Module Module1

    Sub Main()
        '         0         1         2         3
        '         01234567890123456789012345678901
        Dim st =
"<# cdef<{ _First_Identifier_ }>  @<# @#>  <{ _Second_Identifier_ }> @@ #>"

        st =
"<#
  partial class <{ contextName }>
  {
    public const string ClassName = ""<{ contextName }>"" ; 
  }
#>"
        Dim s As New Source(st)
        Dim r0 = s.GetNewReader

        'Dim pr0 = Parse(r0)
        'Dim pr1 = Parse_Template_Ending(pr0.Next)
        Dim sw As New Diagnostics.Stopwatch
        Dim pr2 As Source.Span
        sw.Start()
        pr2 = Template.Parse(r0)
        sw.Stop()
        Console.WriteLine(pr2.ToString)
        Console.WriteLine($"Parsed in {sw.Elapsed}")
        Dim txt = pr2.ToText
        Dim t2 = pr2.ToString
        Dim tmp = TryCast(pr2, Template)
        If tmp IsNot Nothing Then
            Console.WriteLine()
            Console.WriteLine()
            Colorised(tmp)
            Console.WriteLine()
            Console.WriteLine()
            Dim Body = TryCast(tmp.T_Body, Template_Body)
            If Body IsNot Nothing Then
                For Each h In Body.Holes
                    Console.WriteLine(h.ID.ToText)
                Next
            End If
            Dim te As New Template_Engine(tmp)
            Dim args As New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase)
            args.Add("_First_Identifier_", "GHIJKLM")
            args.Add("_Second_Identifier_", "0123456789")
            args.Add("contextName", "FooBarBaz")
            Dim res As String
            sw.Restart()
            res = te.Process(args)
            sw.Stop()
            Console.WriteLine($"Processed in {sw.Elapsed}")

            Console.WriteLine()
            Console.WriteLine(res)
        End If
    End Sub

    Sub Colorised(tmp As Template)
        If tmp Is Nothing Then Exit Sub
        If tmp.T_Start Is Nothing Then Exit Sub
        Colored(ConsoleColor.DarkGray, tmp.T_Start.ToString)
        For Each p In DirectCast(tmp.T_Body, Template_Body).Parts
            Select Case True
                Case TypeOf p Is Quoted
                    Dim obj = DirectCast(p, Quoted)
                    Colored(ConsoleColor.Gray, obj.ToText)
                Case TypeOf p Is Hole
                    Dim obj = DirectCast(p, Hole)
                    Colored(ConsoleColor.Cyan, obj.HoleL.ToText)
                    Colored(ConsoleColor.White, obj.WS0.ToText)
                    Colored(ConsoleColor.Magenta, obj.ID.ToText)
                    Colored(ConsoleColor.White, obj.WS1.ToText)
                    Colored(ConsoleColor.Cyan, obj.HoleR.ToText)
                Case Else
                    Colored(ConsoleColor.Red, p.ToText)
            End Select
        Next
        Colored(ConsoleColor.DarkGray, tmp.T_End.ToText)
    End Sub
    Sub Colored(c As ConsoleColor, t As String)
        Dim orig = Console.ForegroundColor
        Console.ForegroundColor = c
        Console.Write(t)
        Console.ForegroundColor = orig
    End Sub

End Module






Public Class Template_Engine
    Public ReadOnly Property Tmp As Template
    Public Sub New(tmp As Template)
        Me.Tmp = tmp
    End Sub
    Public Function Process(args As Dictionary(Of String, String)) As String
        If args Is Nothing Then Return Nothing
        Dim Body = TryCast(Tmp.T_Body, Template_Body)
        If Body Is Nothing Then Return Nothing
        Dim sizeGuess = Body.Parts.Sum(Function(p) p.Size)
        Dim sb As New System.Text.StringBuilder(sizeGuess)
        For Each p In Body.Parts
            Select Case True
                Case TypeOf p Is Text,
                     TypeOf p Is Whitespace : sb.Append(p.ToText)
                Case TypeOf p Is Quoted : sb.Append(DirectCast(p, Quoted).Quoting.ToText)
                Case TypeOf p Is Hole
                    Dim id = DirectCast(DirectCast(p, Hole).ID, Identifier).ToText
                    Dim res As String = Nothing
                    If args.TryGetValue(id, res) = False Then Throw New Exception($"Key Not FOund {id}")
                    sb.Append(res)
                Case Else
                    Throw New Exception("Help")
            End Select
        Next
        Return sb.ToString
    End Function
End Class

Public Class Source

    Public ReadOnly Property Text As String
    Public ReadOnly Property Size As Integer
    Public ReadOnly Property ID As Guid = Guid.NewGuid

    Sub New(Text As String)
        Me.Text = Text
        Me.Size = Text.Length
    End Sub

    Default Public ReadOnly Property Chars(Index As Integer) As Char?
        Get
            Return If((Index < 0) OrElse (Index >= Size), New Char?(), New Char?(Text(Index)))
        End Get
    End Property

    Public Function GetNewReader() As Reader
        Return New Reader(Me, 0)
    End Function

    <DebuggerDisplay("Index:{Index}='{Value}'")>
    Public Class Reader
        Private _V As Lazy(Of Char?)
        Public ReadOnly Property Source As Source
        Public ReadOnly Property Index As Integer
        Public ReadOnly Property OK As Boolean

        Public ReadOnly Property Value As Char?
            Get
                Return _V.Value
            End Get
        End Property

        Protected Friend Sub New(Source As Source, Index As Integer)
            Me.Source = Source
            Me.Index = Index
            Me._V = New Lazy(Of Char?)(Function() Me.Source(Index), isThreadSafe:=True)
            Me.OK = (0 <= Index) AndAlso (Index < Source.Size)
        End Sub

        Protected Friend Sub New(Source As Source, Reader As Source.Reader)
            Me.Source = Source
            Me.Index = Reader.Index
            Me._V = New Lazy(Of Char?)(Function() Me.Source(Index), isThreadSafe:=True)
            Me.OK = (0 <= Index) AndAlso (Index < Source.Size)
        End Sub

        Public Function Copy() As Reader
            Return New Reader(Me.Source, Index)
        End Function

        Public Shared Operator =(a As Reader, b As Reader) As Boolean
            Return (a.Source = b.Source) AndAlso (a.Index = b.Index)
        End Operator

        Public Shared Operator <>(a As Reader, b As Reader) As Boolean
            Return (a.Source = b.Source) AndAlso (a.Index <> b.Index)
        End Operator

        Public Shared Operator +(x As Reader, offset As Integer) As Reader
            If offset = 0 Then Return x.Copy
            Dim nx As Integer
            If offset < 0 Then
                Try
                    nx = x.Index + offset
                Catch ex As OverflowException
                    nx = Integer.MinValue
                End Try
            Else
                Try
                    nx = x.Index + offset
                Catch ex As OverflowException
                    nx = Integer.MaxValue
                End Try
            End If
            Return New Reader(x.Source, nx)
        End Operator

        Public Shared Operator -(x As Reader, offset As Integer) As Reader
            If offset = 0 Then Return x.Copy
            Dim nx As Integer
            If offset < 0 Then
                Try
                    nx = x.Index - offset
                Catch ex As OverflowException
                    nx = Integer.MaxValue
                End Try
            Else
                Try
                    nx = x.Index - offset
                Catch ex As OverflowException
                    nx = Integer.MinValue
                End Try
            End If
            Return New Reader(x.Source, nx)
        End Operator

        Public Shared Operator =(a As Reader, ch As Char) As Boolean
            Return (a.Value = ch)
        End Operator

        Public Shared Operator <>(a As Reader, ch As Char) As Boolean
            Return (a.Value <> ch)
        End Operator

        Public Function [To](other As Source.Reader) As Source.Span
            If Me.Source <> other.Source Then Return Nothing
            Return New Source.Span(Me, other)
        End Function

        'Public Overrides Function ToString() As String
        '    '   Return $"{Index}'{Value}'"
        'End Function

    End Class

    Public Shared Operator =(a As Source, b As Source) As Boolean
        Return a.ID = b.ID
    End Operator

    Public Shared Operator <>(a As Source, b As Source) As Boolean
        Return a.ID <> b.ID
    End Operator

    Public Function GetSpan(x0 As Source.Reader, x1 As Source.Reader) As Source.Span
        If x0.Source <> x1.Source Then Return Nothing
        Return New Span(x0, x1)
    End Function

    <DebuggerDisplay("[{X0}-{X1}")>
    Public Class Span
        Public ReadOnly Property X0 As Source.Reader
        Public ReadOnly Property X1 As Source.Reader

        Protected Friend Sub New(X0 As Source.Reader, X1 As Source.Reader)
            Me.X0 = X0
            Me.X1 = X1
        End Sub

        Public Function [Next]() As Source.Reader
            Return New Source.Reader(X0.Source, (X1 + 1))
        End Function

        Public Overrides Function ToString() As String
            Return ToText()
            '  Return $"{X0} {X1}"
        End Function

        Public ReadOnly Property Size() As Integer
            Get
                Return (X1.Index - X0.Index) + 1
            End Get
        End Property

        Private _Initial As Boolean = True
        Private _Text As String

        Public Function ToText() As String
            If (X0.OK = False) OrElse (X1.OK = False) OrElse (Size <= 0) Then
                _Text = String.Empty
            Else
                _Text = Me.X0.Source.Text.Substring(X0.Index, Size)
            End If
            _Initial = False
            Return _Text

        End Function

    End Class

End Class

Public Class Parser

    Public Class Hole
        Inherits Source.Span

        Public ReadOnly Property HoleL As Source.Span
        Public ReadOnly Property WS0 As Source.Span
        Public ReadOnly Property ID As Source.Span
        Public ReadOnly Property WS1 As Source.Span
        Public ReadOnly Property HoleR As Source.Span

        Public Sub New(holeL As Source.Span, ws0 As Source.Span, id As Source.Span, ws1 As Source.Span, holeR As Source.Span)
            MyBase.New(holeL.X0, holeR.X1)
            Me.HoleL = holeL
            Me.WS0 = ws0
            Me.ID = id
            Me.WS1 = ws1
            Me.HoleR = holeR
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr.OK = False Then Return AnError.EoT(sr)
            Dim sx = sr
            Dim HoleL = Hole_L.Parse(sr)
            If HoleL Is Nothing OrElse TypeOf HoleL IsNot Hole_L Then Return AnError.Expected("Expecting An Hole Opening", HoleL)
            Dim ws0 = Whitespace.Parse(HoleL.Next) : If ws0 Is Nothing Then Return Nothing
            Dim id = Identifier.Parse(ws0.Next)
            If id Is Nothing OrElse TypeOf id IsNot Identifier Then Return AnError.Expected("Expecting A Identifier", id)

            Dim ws1 = Whitespace.Parse(id.Next) : If ws1 Is Nothing Then Return Nothing

            Dim HoleR = Hole_R.Parse(ws1.Next)
            If HoleR Is Nothing OrElse TypeOf HoleR IsNot Hole_R Then Return AnError.Expected("Expecting A Hole Closing", HoleR)
            Return New Hole(HoleL, ws0, id, ws1, HoleR)
        End Function

    End Class

    Public Class Hole_L
        Inherits Source.Span

        Sub New(s As Source.Span)
            MyBase.New(s.X0, s.X1)
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr.OK = False Then Return AnError.EoT(sr)
            Dim x0 = sr
            If sr <> "<"c Then Return AnError.UExChar(sr) Else sr += 1
            If sr <> "{"c Then Return AnError.UExChar(sr)
            Return New Hole_L(x0.To(sr))
        End Function

    End Class

    Public Class Hole_R
        Inherits Source.Span

        Sub New(s As Source.Span)
            MyBase.New(s.X0, s.X1)
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr.OK = False Then Return AnError.EoT(sr)
            Dim x0 = sr
            If sr <> "}"c Then Return AnError.UExChar(sr) Else sr += 1
            If sr <> ">"c Then Return AnError.UExChar(sr)
            Return New Hole_R(x0.To(sr))
        End Function

    End Class

    Public Class Whitespace
        Inherits Source.Span

        Public Sub New(s As Source.Span)
            MyBase.New(s.X0, s.X1)
        End Sub
        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            Dim sx = sr
            While sr.OK AndAlso (sr = " "c)
                sr += 1
            End While
            Return New Whitespace(sx.To(sr - 1))
        End Function



    End Class

    Public Class Identifier
        Inherits Source.Span

        Public Sub New(s As Source.Span)
            MyBase.New(s.X0, s.X1)
        End Sub


        Private Shared Function IsFirst_IdentifierCharacter(ch As Char?) As Boolean
            Return (ch = "_"c) OrElse Char.IsLetter(ch)
        End Function

        Private Shared Function IsTail_IdentifierCharacter(ch As Char?) As Boolean
            Return IsFirst_IdentifierCharacter(ch) OrElse Char.IsDigit(ch)
        End Function

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr Is Nothing Then Return Nothing
            If sr.OK = False Then Return AnError.EoT(sr)
            If IsFirst_IdentifierCharacter(sr.Value) Then
                Dim sx = sr
                Dim sy = sr + 1
                While sy.OK AndAlso IsTail_IdentifierCharacter(sy.Value)
                    sy += 1
                End While
                Return New Identifier(sx.To(sy - 1))

            Else
                Return Nothing
            End If

        End Function

    End Class

    Public Class Template
        Inherits Source.Span

        Public ReadOnly Property T_Start As Source.Span
        Public ReadOnly Property T_Body As Source.Span
        Public ReadOnly Property T_End As Source.Span

        Sub New(T_Start As Source.Span, T_Body As Source.Span, T_End As Source.Span)
            MyBase.New(T_Start.X0, T_End.X1)
            Me.T_Start = T_Start
            Me.T_Body = T_Body
            Me.T_End = T_End
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr Is Nothing Then Return Nothing
            If sr.OK = False Then Return AnError.EoT(sr)

            Dim T_Start = Template_Start.Parse(sr)
            If T_Start Is Nothing Then Return Nothing
            If TypeOf (T_Start) Is AnError Then Return T_Start

            Dim T_Body = Template_Body.Parse(T_Start.Next)
            If T_Body Is Nothing Then Return Nothing
            If TypeOf (T_Body) Is AnError Then Return T_Body

            Dim T_End = Template_End.Parse(T_Body.Next)
            If T_End Is Nothing Then Return Nothing
            If TypeOf (T_End) Is AnError Then Return T_End

            Return New Template(T_Start, T_Body, T_End)
        End Function

    End Class

    Public Class Template_Start
        Inherits Source.Span

        Sub New(s As Source.Span)
            MyBase.New(s.X0, s.X1)
        End Sub

        Public Shared Function Parse(Curr As Source.Reader) As Source.Span
            If Curr.OK = False Then Return AnError.EoT(Curr)
            Dim x0 = Curr
            If Curr <> "<"c Then Return AnError.UExChar(Curr) Else Curr += 1
            If Curr <> "#"c Then Return AnError.UExChar(Curr)
            Return New Template_Start(x0.To(Curr))
        End Function

    End Class

    Public Class Template_Body
        Inherits Source.Span

        Public ReadOnly Property Parts As IEnumerable(Of Source.Span)
        Public ReadOnly Property Holes As IEnumerable(Of Hole)
            Get
                Return P_Holes.Value
            End Get
        End Property

        Private P_Holes As Lazy(Of IEnumerable(Of Hole))

        Sub New(s As Source.Span, parts As List(Of Source.Span))
            MyBase.New(s.X0, s.X1)
            Me.Parts = parts
            Me.P_Holes = New Lazy(Of IEnumerable(Of Hole))(Function() Me.Parts.OfType(Of Hole))
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr.OK = False Then Return AnError.EoT(sr)

            Dim sx = sr
            Dim parts As New List(Of Source.Span)
            While sr.OK
                Dim res = Text.Parse(sr)
                If TypeOf res Is Text Then
                    parts.Add(res)
                    sr = res.X1 + 1
                ElseIf TypeOf res Is Text.Preceeding Then
                    Dim pre = DirectCast(res, Text.Preceeding)
                    If pre.Text.Size > 0 Then parts.Add(pre.Text)
                    If TypeOf pre.What Is Template_End AndAlso TypeOf pre.What IsNot Whitespace Then
                        sr = pre.Text.X1
                        Exit While
                    End If
                    parts.Add(pre.What)
                    sr = res.X1 + 1
                ElseIf TypeOf res Is AnError Then
                    Return res
                Else

                    ' error
                    'Return Nothing
                End If
            End While

            Return New Template_Body(sx.To(sr), parts)
        End Function

    End Class

    Public Class Template_End
        Inherits Source.Span

        Sub New(s As Source.Span)
            MyBase.New(s.X0, s.X1)
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr.OK = False Then Return AnError.EoT(sr)
            Dim x0 = sr
            If sr <> "#"c Then Return AnError.UExChar(sr) Else sr += 1
            If sr <> ">"c Then Return AnError.UExChar(sr)
            Return New Template_End(x0.To(sr))
        End Function

    End Class


    Public Class Text
        Inherits Source.Span

        Sub New(s As Source.Span)
            MyBase.New(s.X0, s.X1)
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr.OK = False Then Return AnError.EoT(sr)
            Dim sx = sr

            While sr.OK

                Dim ch = sr.Value
                Select Case ch
                    Case " "c
                        Dim WS = Whitespace.Parse(sr)
                        If TypeOf WS Is Whitespace Then Return New Preceeding(New Text(sx.To(sr - 1)), WS)
                    Case "<"c
                        ' Could be the start of template or hole.
                        Dim T_Start = Template_Start.Parse(sr)
                        'If TypeOf T_Start Is AnError Then Return T_Start
                        If TypeOf T_Start Is Template_Start Then Return New Preceeding(New Text(sx.To(sr - 1)), T_Start)

                        Dim HoleL = Hole.Parse(sr)
                        If TypeOf HoleL Is AnError Then Return HoleL
                        If TypeOf HoleL Is Hole Then Return New Preceeding(New Text(sx.To(sr - 1)), HoleL)

                    Case "#"
                        Dim T_End = Template_End.Parse(sr)
                        'If TypeOf T_End Is AnError Then Return T_End
                        If TypeOf T_End Is Template_End Then Return New Preceeding(New Text(sx.To(sr - 1)), T_End)

                    Case "}"
                        Dim HoleR = Hole_R.Parse(sr)
                        'If TypeOf HoleR Is AnError Then Return HoleR
                        If TypeOf HoleR Is Hole_R Then Return New Preceeding(New Text(sx.To(sr - 1)), HoleR)

                    Case "@"c
                        ' Quotation
                        Dim q = Quoted.Parse(sr)
                        'If TypeOf q Is AnError Then Return q
                        If TypeOf q Is Quoted Then Return New Preceeding(New Text(sx.To(sr - 1)), q)

                End Select
                sr = sr + 1
            End While
            Return New Text(sx.To(sr - 1))
        End Function

        Public Class Preceeding
            Inherits Source.Span

            Public ReadOnly Property Text As Text
            Public ReadOnly Property What As Source.Span

            Friend Sub New(pre As Text, what As Source.Span)
                MyBase.New(pre.X0, what.X1)
                Me.Text = pre
                Me.What = what
            End Sub
        End Class

    End Class

    Public Class Quoted
        Inherits Source.Span

        Public ReadOnly Property Quoting As Source.Span

        Sub New(qs As Source.Reader, q As Source.Span)
            MyBase.New(qs, q.X1)
            Me.Quoting = q
        End Sub

        Public Shared Function Parse(sr As Source.Reader) As Source.Span
            If sr Is Nothing Then Return Nothing
            If sr.OK = False Then Return AnError.EoT(sr)
            If sr <> "@"c Then Return AnError.UExChar(sr)
            Dim sx = sr
            ' Quotation
            Dim nx = (sr + 1)
            If nx = "@"c Then
                Dim tx As New Text(nx.To(nx))
                Return New Quoted(sx, tx)
            Else
                Dim T_Start = Template_Start.Parse(nx)
                If TypeOf T_Start Is Template_Start Then Return New Quoted(sx, T_Start)

                Dim T_End = Template_End.Parse(nx)
                If TypeOf T_End Is Template_End Then Return New Quoted(sx, T_End)

                Dim HoleL = Hole_L.Parse(nx)
                If TypeOf HoleL Is Hole_L Then Return New Quoted(sx, HoleL)

                Dim HoleR = Hole_R.Parse(nx)
                If TypeOf HoleR Is Hole_R Then Return New Quoted(sx, HoleR)

            End If
            Return Nothing
            'Dim x0 = sr
            'Return New Text(x0.To(sr))
        End Function

    End Class

    Public MustInherit Class AnError
        Inherits Source.Span

        ReadOnly Property MSG As String

        Protected Friend Sub New(msg As String, x As Source.Span)
            MyBase.New(x.X0, x.X1)
            Me.MSG = If(msg, String.Empty)
        End Sub

        Public Shared Function EoT(x As Source.Reader) As UnexpectedEndOfText
            Return New UnexpectedEndOfText(x.To(x))
        End Function
        Public Shared Function UExChar(x As Source.Reader) As UnexpectedEndOfText
            Return New UnexpectedEndOfText(x.To(x))
        End Function
        Public Shared Function Expected(msg As String, x As Source.Span)
            Return New Expected(msg, x)
        End Function

        Public Overrides Function ToString() As String
            Return $"Error '{MSG}' at {X0} - {X1}"
        End Function
    End Class

    Public Class UnexpectedEndOfText
        Inherits AnError

        Protected Friend Sub New(x As Source.Span)
            MyBase.New("Unexpected End Of Text", x)
        End Sub
    End Class
    Public Class UnexpectedCharacter
        Inherits AnError

        Protected Friend Sub New(x As Source.Span)
            MyBase.New("Unexpected Character", x)
        End Sub
    End Class
    Public Class Expected
        Inherits AnError

        Protected Friend Sub New(msg As String, x As Source.Span)
            MyBase.New(msg, x)
        End Sub
    End Class

End Class
