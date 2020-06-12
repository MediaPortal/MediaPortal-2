using Emulators.Common.Games;
using Emulators.Common.Matchers;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb
{
  /// <summary>
  /// TheGamesDb has moved to a new API (see <see cref="TheGamesDbWrapperV2"/>), this wrapper has been
  /// kept to keep support for loading existing fanart downloaded using the old API.
  /// </summary>
  public class TheGamesDbLegacyWrapper : IOnlineMatcher
  {
    #region Consts
    
    protected static readonly Guid MATCHER_ID = new Guid("32047FBF-9080-4236-AE05-2E6DC1BF3A9F");
    protected static readonly string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheGamesDB\");
    protected const string COVERS_DIRECTORY = "Covers";
    protected const string COVERS_FRONT = "front";
    protected const string COVERS_BACK = "back";
    protected const string FANART_DIRECTORY = "Fanart";
    protected const string BANNERS_DIRECTORY = "Banners";
    protected const string CLEARLOGO_DIRECTORY = "ClearLogos";
    protected const string SCREENSHOT_DIRECTORY = "Screenshots";

    #endregion

    #region Public Properties

    public Guid MatcherId
    {
      get { return MATCHER_ID; }
    }

    #endregion

    #region Public Methods

    public bool TryGetImagePath(string id, ImageType imageType, out string path)
    {
      path = null;
      if (string.IsNullOrEmpty(id))
        return false;

      switch (imageType)
      {
        case ImageType.FrontCover:
          path = Path.Combine(CACHE_PATH, id, COVERS_DIRECTORY, COVERS_FRONT);
          return true;
        case ImageType.BackCover:
          path = Path.Combine(CACHE_PATH, id, COVERS_DIRECTORY, COVERS_BACK);
          return true;
        case ImageType.Fanart:
          path = Path.Combine(CACHE_PATH, id, FANART_DIRECTORY);
          return true;
        case ImageType.Screenshot:
          path = Path.Combine(CACHE_PATH, id, SCREENSHOT_DIRECTORY);
          return true;
        case ImageType.Banner:
          path = Path.Combine(CACHE_PATH, id, BANNERS_DIRECTORY);
          return true;
        case ImageType.ClearLogo:
          path = Path.Combine(CACHE_PATH, id, CLEARLOGO_DIRECTORY);
          return true;
        default:
          return false;
      }
    }

    public Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo)
    {
      return Task.FromResult(false);
    }

    public Task DownloadFanArtAsync(string itemId)
    {
      return Task.CompletedTask;
    }

    #endregion
  }
}
