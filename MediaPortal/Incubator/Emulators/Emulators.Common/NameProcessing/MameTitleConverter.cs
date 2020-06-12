using Emulators.Common.Games;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emulators.Common.NameProcessing
{
  class MameTitleConverter : ITitleConverter
  {
    protected const string PLATFORM = "arcade";
    protected const string MAME_TITLES_LIST = "Emulators.Common.NameProcessing.MameTitlesList.txt";
    protected static readonly Dictionary<string, string> MAME_TITLES_DICTIONARY = new Dictionary<string, string>();
    protected static readonly Regex REGEX = new Regex(@"(.*?)\s+""([^\(""]*)");

    static MameTitleConverter()
    {
      BuildDictionary();
    }

    static void BuildDictionary()
    {
      using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(MAME_TITLES_LIST)))
      {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
          Match m = REGEX.Match(line);
          if (m.Success)
            MAME_TITLES_DICTIONARY[m.Groups[1].Value.Trim()] = m.Groups[2].Value.Trim();
        }
      }
    }
    
    public bool ConvertTitle(GameInfo gameInfo)
    {
      if (gameInfo.Platform.ToLowerInvariant() != PLATFORM)
        return false;

      string newTitle;
      if (MAME_TITLES_DICTIONARY.TryGetValue(gameInfo.GameName, out newTitle))
      {
        gameInfo.GameName = newTitle;
        return true;
      }
      return false;
    }
  }
}
