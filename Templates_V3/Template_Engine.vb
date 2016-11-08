Imports Templates_V3.Parser






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
