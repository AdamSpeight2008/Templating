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
      Dim clr As New Coloriser
      clr.Colorised(tmp)
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



End Module
