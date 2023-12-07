using System;

namespace Cinema.OnlineLibraries.Helper
{

  public static class StringExtensions
  {
    public static string Substring(this string value, string start, string ende)
    {
      try
      {
        var a = value.IndexOf(start) + start.Length;
        if (a - start.Length <= 0) return "";
        var b = value.IndexOf(ende, a, StringComparison.Ordinal);
        return value.Substring(a, b - a);
      }
      catch (Exception)
      {
        return "";
      }
    }

    public static string Substring(this string value, string start, string start2, string ende)
    {
      try
      {
        var a = value.IndexOf(start) + start.Length;
        var a2 = value.IndexOf(start2, a, StringComparison.Ordinal) + start2.Length;
        var b = value.IndexOf(ende, a2, StringComparison.Ordinal);
        return value.Substring(a2, b - a2);
      }
      catch (Exception)
      {
        return "";
      }
    }
  }
}
