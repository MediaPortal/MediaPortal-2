using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.Games
{
  public class GameCategory
  {
    public const string CATEGORY_NAME = "Game";

    public static string CategoryNameToMimeType(string categoryName)
    {
      categoryName = categoryName != null ? categoryName.Replace(' ', '-').ToLowerInvariant() : string.Empty;
      return "game/" + categoryName;
    }
  }
}
