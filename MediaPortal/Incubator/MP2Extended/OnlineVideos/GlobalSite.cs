using OnlineVideos.OnlineVideosWebservice;

namespace MediaPortal.Plugins.MP2Extended.OnlineVideos
{
  class GlobalSite
  {
    /// <summary>
    /// Holds all other oinformation about the Site like Owner, Name, Description, State, ...
    /// </summary>
    internal Site Site { get; set; }
    /// <summary>
    /// Is the site already locally installed or only available online? true = locally available, false = only online
    /// </summary>
    internal bool Added { get; set; }
  }
}
