using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.Utils
{
  public class NullableHelper
  {
    public delegate bool TryDelegate<T>(string s, out T result);

    public static bool TryParseNullable<T>(string s, out T? result, TryDelegate<T> tryDelegate) where T : struct
    {
      if (s == null)
      {
        result = null;
        return true;
      }

      T temp;
      bool success = tryDelegate(s, out temp);
      result = temp;
      return success;
    }

    public static T? ParseNullable<T>(string s, TryDelegate<T> tryDelegate) where T : struct
    {
      if (s == null)
      {
        return null;
      }

      T temp;
      return tryDelegate(s, out temp)
                 ? (T?)temp
                 : null;
    }
  }
}
