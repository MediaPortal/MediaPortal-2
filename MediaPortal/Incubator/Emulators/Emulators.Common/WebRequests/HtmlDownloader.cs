using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.WebRequests
{
  public class HtmlDownloader : AbstractDownloader
  {
    protected override T Deserialize<T>(string response)
    {
      if (typeof(IHtmlDeserializable).IsAssignableFrom(typeof(T)))
      {
        ConstructorInfo constructor = typeof(T).GetConstructor(Type.EmptyTypes);
        if (constructor != null)
        {
          T item = (T)constructor.Invoke(null);
          if (((IHtmlDeserializable)item).Deserialize(response))
            return item;
        }
      }
      return default(T);
    }
  }
}
