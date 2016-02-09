using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.MediaServer.Tree;
using MediaPortal.Plugins.Transcoding.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  class MediaLibraryMusicGenreItem : BasicContainer, IDirectoryMusicGenre
  {
    protected string ObjectId { get; set; }
    protected string BaseKey { get; set; }

    private readonly string _title;

    public MediaLibraryMusicGenreItem(string id, string title, EndPointSettings client)
      : base(id, client)
    {
      ServiceRegistration.Get<ILogger>().Debug("Create music genre {0}={1}", id, title);
      _title = title;
      BaseKey = MediaLibraryHelper.GetBaseKey(Key);
    }

    public override string Class
    {
      get { return "object.container.genre.musicGenre"; }
    }

    public override void Initialise()
    {
      Title = _title;
    }

    private IList<MediaItem> GenreAudio()
    {
      var necessaryMiaTypeIDs = new Guid[] {
                                    MediaAspect.ASPECT_ID,
                                    AudioAspect.ASPECT_ID,
                                    TranscodeItemAudioAspect.ASPECT_ID,
                                    ProviderResourceAspect.ASPECT_ID
                                  };
      var optionalMIATypeIDs = new Guid[]
                                 {
                                 };
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();

      ServiceRegistration.Get<ILogger>().Debug("Looking for music genre " + _title);
      IFilter searchFilter = new RelationalFilter(AudioAspect.ATTR_GENRES, RelationalOperator.EQ, _title);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMiaTypeIDs, optionalMIATypeIDs, searchFilter);

      return library.Search(searchQuery, true);
    }

    public override int ChildCount
    {
      get { return GenreAudio().Count; }
      set { }
    }

    public override TreeNode<object> FindNode(string key)
    {
      if (!key.StartsWith(Key)) return null;
      if (key == Key) return this;

      return null;
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      List<IDirectoryObject> result = new List<IDirectoryObject>();

      try
      {
        var parent = new BasicContainer(Id, Client);
        IList<MediaItem> items = GenreAudio();
        result.AddRange(items.Select(item => MediaLibraryHelper.InstansiateMediaLibraryObject(item, BaseKey, parent)));
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Cannot search for music genre " + ObjectId, e);
      }

      return result;
    }

    public string LongDescription{ get; set; }
    public string Description{ get; set; }
  }
}
