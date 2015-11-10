using System;
using System.Text;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  internal static class Base64
  {
    internal static string Encode(string toEncode)
    {
      byte[] toEncodeAsBytes = Encoding.UTF8.GetBytes(toEncode);
      return Convert.ToBase64String(toEncodeAsBytes);
    }

    internal static string Decode(string encodedData)
    {
      try
      {
        byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
        return Encoding.UTF8.GetString(encodedDataAsBytes);
      }
      catch (FormatException)
      {
        return String.Empty;
      }
    }
  }
}
