using MediaPortal.Extensions.OnlineLibraries.Matches;

namespace Emulators.Common.Matchers
{
  public class GameMatch<T> : BaseMediaMatch<T>
  {
    public string GameName;
    public string Platform;

    public override string ToString()
    {
      return string.Format("{0}: {1} [{2}]", GameName, Platform, Id);
    }
  }
}
