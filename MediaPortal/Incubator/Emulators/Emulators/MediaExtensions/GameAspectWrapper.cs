using Emulators.Common;
using Emulators.Common.Games;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

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
    protected AbstractProperty _mobyIDProperty;
    protected AbstractProperty _rawGIDProperty;
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

    public AbstractProperty MobyIDProperty
    {
      get { return _mobyIDProperty; }
    }

    public string MobyID
    {
      get { return (string)_mobyIDProperty.GetValue(); }
      set { _mobyIDProperty.SetValue(value); }
    }

    public AbstractProperty RAWGIDProperty
    {
      get { return _rawGIDProperty; }
    }

    public long? RAWGID
    {
      get { return (long?)_rawGIDProperty.GetValue(); }
      set { _rawGIDProperty.SetValue(value); }
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
      _mobyIDProperty = new SProperty(typeof(string));
      _rawGIDProperty = new SProperty(typeof(long?));
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

    private string GetESRBCertificationText(string esrbId)
    {
      if (string.IsNullOrEmpty(esrbId))
        return "";

      if (esrbId.Equals("ESRB_E10+", StringComparison.InvariantCultureIgnoreCase))
        return "ESRB E 10+";
      if (esrbId.Equals("ESRB_E", StringComparison.InvariantCultureIgnoreCase))
        return "ESRB E";
      if (esrbId.Equals("ESRB_T", StringComparison.InvariantCultureIgnoreCase))
        return "ESRB T";
      if (esrbId.Equals("ESRB_M", StringComparison.InvariantCultureIgnoreCase))
        return "ESRB M";
      if (esrbId.Equals("ESRB_A", StringComparison.InvariantCultureIgnoreCase))
        return "ESRB A";

      return "";
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
      Platform = (string)aspect[GameAspect.ATTR_PLATFORM];
      Developer = (string)aspect[GameAspect.ATTR_DEVELOPER];
      Year = (int?)aspect[GameAspect.ATTR_YEAR];
      Certification = GetESRBCertificationText((string)aspect[GameAspect.ATTR_CERTIFICATION]);
      Description = (string)aspect[GameAspect.ATTR_DESCRIPTION];
      Rating = (double?)aspect[GameAspect.ATTR_RATING];
      Genres = (IEnumerable<string>)aspect[GameAspect.ATTR_GENRES] ?? EMPTY_STRING_COLLECTION;

      IList<MultipleMediaItemAspect> externalIdAspects;
      if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ExternalIdentifierAspect.Metadata, out externalIdAspects))
      {
        foreach (MultipleMediaItemAspect externalId in externalIdAspects)
        {
          string source = externalId.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string id = externalId.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          string type = externalId.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          if (type == GameInfo.TYPE_GAME)
          {
            if (source == GameInfo.SOURCE_GAMESDB)
            {
              TGDBID = Convert.ToInt32(id);
            }
            else if (source == GameInfo.SOURCE_MOBY)
            {
              MobyID = id;
            }
            else if (source == GameInfo.SOURCE_RAWG)
            {
              RAWGID = Convert.ToInt64(id);
            }
          }
        }
      }
    }

    public void SetEmpty()
    {
      GameName = null;
      TGDBID = null;
      MobyID = null;
      RAWGID = null;
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
