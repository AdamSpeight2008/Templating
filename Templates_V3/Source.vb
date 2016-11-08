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
