Imports Templates_V3.Parser

Public Class Coloriser
  Public Sub Colorised(tmp As Template)
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
  Private Sub Colored(c As ConsoleColor, t As String)
    Dim orig = Console.ForegroundColor
    Console.ForegroundColor = c
    Console.Write(t)
    Console.ForegroundColor = orig
  End Sub

End Class