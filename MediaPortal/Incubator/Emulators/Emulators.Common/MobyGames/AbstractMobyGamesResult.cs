using Emulators.Common.WebRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Emulators.Common.MobyGames
{
  abstract class AbstractMobyGamesResult : IHtmlDeserializable
  {
    public abstract bool Deserialize(string response);
        
    protected string Decode(string input)
    {
      return HttpUtility.HtmlDecode(input).Trim();
    }
  }
}
