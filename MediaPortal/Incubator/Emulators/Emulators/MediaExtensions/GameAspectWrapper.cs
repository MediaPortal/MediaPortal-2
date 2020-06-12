using Emulators.Common;
using Emulators.Common.Games;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaExtensions
{
  public class GameAspectWrapper : Control
  {
    #region Constants

    public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

    #endregion

    #region Fields

    protected AbstractProperty _gameNameProperty;
    protected AbstractProperty _tGDBIDProperty;
    protected AbstractProperty _platformProperty;
    protected AbstractProperty _developerProperty;
    protected AbstractProperty _yearProperty;
    protected AbstractProperty _certificationProperty;
    protected AbstractProperty _descriptionProperty;
    protected AbstractProperty _ratingProperty;
    protected AbstractProperty _genresProperty;
    protected AbstractProperty _mediaItemProperty;

    #endregion

    #region Properties

    public AbstractProperty GameNameProperty
    {
      get { return _gameNameProperty; }
    }

    public string GameName
    {
      get { return (string)_gameNameProperty.GetValue(); }
      set { _gameNameProperty.SetValue(value); }
    }

    public AbstractProperty TGDBIDProperty
    {
      get { return _tGDBIDProperty; }
    }

    public int? TGDBID
    {
      get { return (int?)_tGDBIDProperty.GetValue(); }
      set { _tGDBIDProperty.SetValue(value); }
    }

    public AbstractProperty PlatformProperty
    {
      get { return _platformProperty; }
    }

    public string Platform
    {
      get { return (string)_platformProperty.GetValue(); }
      set { _platformProperty.SetValue(value); }
    }

    public AbstractProperty DeveloperProperty
    {
      get { return _developerProperty; }
    }

    public string Developer
    {
      get { return (string)_developerProperty.GetValue(); }
      set { _developerProperty.SetValue(value); }
    }

    public AbstractProperty YearProperty
    {
      get { return _yearProperty; }
    }

    public int? Year
    {
      get { return (int?)_yearProperty.GetValue(); }
      set { _yearProperty.SetValue(value); }
    }

    public AbstractProperty CertificationProperty
    {
      get { return _certificationProperty; }
    }

    public string Certification
    {
      get { return (string)_certificationProperty.GetValue(); }
      set { _certificationProperty.SetValue(value); }
    }

    public AbstractProperty DescriptionProperty
    {
      get { return _descriptionProperty; }
    }

    public string Description
    {
      get { return (string)_descriptionProperty.GetValue(); }
      set { _descriptionProperty.SetValue(value); }
    }

    public AbstractProperty RatingProperty
    {
      get { return _ratingProperty; }
    }

    public double? Rating
    {
      get { return (double?)_ratingProperty.GetValue(); }
      set { _ratingProperty.SetValue(value); }
    }

    public AbstractProperty GenresProperty
    {
      get { return _genresProperty; }
    }

    public IEnumerable<string> Genres
    {
      get { return (IEnumerable<string>)_genresProperty.GetValue(); }
      set { _genresProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem)_mediaItemProperty.GetValue(); }
      set { _mediaItemProperty.SetValue(value); }
    }

    #endregion

    #region Constructor

    public GameAspectWrapper()
    {
      _gameNameProperty = new SProperty(typeof(string));
      _tGDBIDProperty = new SProperty(typeof(int?));
      _platformProperty = new SProperty(typeof(string));
      _developerProperty = new SProperty(typeof(string));
      _yearProperty = new SProperty(typeof(int?));
      _certificationProperty = new SProperty(typeof(string));
      _descriptionProperty = new SProperty(typeof(string));
      _ratingProperty = new SProperty(typeof(double?));
      _genresProperty = new SProperty(typeof(IEnumerable<string>));
      _mediaItemProperty = new SProperty(typeof(MediaItem));
      _mediaItemProperty.Attach(MediaItemChanged);
    }

    #endregion

    #region Members

    private void MediaItemChanged(AbstractProperty property, object oldvalue)
    {
      Init(MediaItem);
    }

    public void Init(MediaItem mediaItem)
    {
      SingleMediaItemAspect aspect;
      if (mediaItem == null || !MediaItemAspect.TryGetAspect(mediaItem.Aspects, GameAspect.Metadata, out aspect))
      {
        SetEmpty();
        return;
      }

      GameName = (string)aspect[GameAspect.ATTR_GAME_NAME];
      TGDBID = (int?)aspect[GameAspect.ATTR_TGDB_ID];
      Platform = (string)aspect[GameAspect.ATTR_PLATFORM];
      Developer = (string)aspect[GameAspect.ATTR_DEVELOPER];
      Year = (int?)aspect[GameAspect.ATTR_YEAR];
      Certification = (string)aspect[GameAspect.ATTR_CERTIFICATION];
      Description = (string)aspect[GameAspect.ATTR_DESCRIPTION];
      Rating = (double?)aspect[GameAspect.ATTR_RATING];
      Genres = (IEnumerable<string>)aspect[GameAspect.ATTR_GENRES] ?? EMPTY_STRING_COLLECTION;
    }

    public void SetEmpty()
    {
      GameName = null;
      TGDBID = null;
      Platform = null;
      Developer = null;
      Year = null;
      Certification = null;
      Description = null;
      Rating = null;
      Genres = EMPTY_STRING_COLLECTION;
    }

    #endregion
  }
}