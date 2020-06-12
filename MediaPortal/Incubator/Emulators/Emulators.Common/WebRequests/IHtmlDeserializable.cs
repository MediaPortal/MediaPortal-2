using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.WebRequests
{
  public interface IHtmlDeserializable
  {
    bool Deserialize(string html);
  }
}
