using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  internal class GetMusicTrackCount : GetMusicTracksBasic
  {
    public WebIntResult Process(string filter)
    {
      var output = Process(filter, null, null);

      return new WebIntResult() { Result = output.Count };
    }
  }
}
