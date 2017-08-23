#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common;
using MediaPortal.Common.Genres;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using System.Text.RegularExpressions;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class OnlineLibrarySettings
  {
    #region Genre Mappings

    public static readonly GenreMapping[] DEFAULT_MUSIC_GENRES = new GenreMapping[]
        {
          new GenreMapping(MusicGenre.CLASSIC, new SerializableRegex(@"Classic|Opera|Orchestral|Choral|Avant|Baroque|Chant", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.SOUNDTRACK, new SerializableRegex(@"Soundtrack|Cinema|Musical|Score", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.NEW_AGE, new SerializableRegex(@"New Age|Environment|Healing|Meditation|Nature|Relax|Travel", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ROCK, new SerializableRegex(@"Rock|Grunge|Punk", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.METAL, new SerializableRegex(@"Metal", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COUNTRY, new SerializableRegex(@"Country|Americana|Bluegrass|Cowboy|Honky|Hokum", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.JAZZ, new SerializableRegex(@"Jazz|Big Band|Fusion|Ragtime", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.RB_SOUL, new SerializableRegex(@"R&B|Soul|Disco|Funk|Swing|Blues", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.HIP_HOP_RAP, new SerializableRegex(@"Hop|Rap|Bounce|Turntablism", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.RAGGAE, new SerializableRegex(@"Reggae|Dancehall", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.POP, new SerializableRegex(@"Pop|Beat", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.DANCE, new SerializableRegex(@"Dance|Club|House|Step|Garage|Trance|NRG|Core|Techno", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ELECTRONIC, new SerializableRegex(@"Electronic|Electro|Experimental|8bit|Chiptune|Downtempo|Industrial", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COMEDY, new SerializableRegex(@"Comedy|Novelty|Parody", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.FOLK, new SerializableRegex(@"Folk", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.EASY_LISTENING, new SerializableRegex(@"Easy|Lounge|Background|Swing|Bop|Ambient", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.HOLIDAY, new SerializableRegex(@"Holiday|Chanukah|Christmas|Easter|Halloween|Thanksgiving", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.WORLD, new SerializableRegex(@"World|Africa|Afro|Asia|Australia|Cajun|Latin|Calypso|Caribbean|Celtic|Europe|France|America|Polka|Japanese|Indian|Korean|German|Danish|Ballad|Ethnic|Indie", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.ALTERNATIVE, new SerializableRegex(@"Alternative|New Wave|Progressive", RegexOptions.IgnoreCase)),
          new GenreMapping(MusicGenre.COMPILATION, new SerializableRegex(@"Compilation|Top", RegexOptions.IgnoreCase)),
        };

    public static readonly GenreMapping[] DEFAULT_MOVIE_GENRES = new GenreMapping[]
        {
          new GenreMapping(MovieGenre.ACTION, new SerializableRegex(@"Action", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ADVENTURE, new SerializableRegex(@"Adventure", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ANIMATION, new SerializableRegex(@"Animation|Cartoon|Anime", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.COMEDY, new SerializableRegex(@"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.CRIME, new SerializableRegex(@"Crime", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.DOCUMENTARY, new SerializableRegex(@"Documentary|Biography", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.DRAMA, new SerializableRegex(@"Drama", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.FAMILY, new SerializableRegex(@"Family", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.FANTASY, new SerializableRegex(@"Fantasy", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.HISTORY, new SerializableRegex(@"History", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.HORROR, new SerializableRegex(@"Horror", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.MUSIC, new SerializableRegex(@"Music", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.MYSTERY, new SerializableRegex(@"Mystery", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.ROMANCE, new SerializableRegex(@"Romance", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.SCIENCE_FICTION, new SerializableRegex(@"Science Fiction|Science-Fiction|Sci-Fi", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.TV_MOVIE, new SerializableRegex(@"TV", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.THRILLER, new SerializableRegex(@"Thriller|Disaster|Suspense", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.WAR, new SerializableRegex(@"War", RegexOptions.IgnoreCase)),
          new GenreMapping(MovieGenre.WESTERN, new SerializableRegex(@"Western", RegexOptions.IgnoreCase)),
        };

    public static readonly GenreMapping[] DEFAULT_SERIES_GENRES = new GenreMapping[]
        {
          new GenreMapping(SeriesGenre.ACTION, new SerializableRegex(@"Action", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ADVENTURE, new SerializableRegex(@"Adventure", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ANIMATION, new SerializableRegex(@"Animation|Cartoon|Anime", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.COMEDY, new SerializableRegex(@"Comedy", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.CRIME, new SerializableRegex(@"Crime", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.DOCUMENTARY, new SerializableRegex(@"Documentary|Biography", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.DRAMA, new SerializableRegex(@"Drama", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.FAMILY, new SerializableRegex(@"Family", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.FANTASY, new SerializableRegex(@"Fantasy", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.HISTORY, new SerializableRegex(@"History", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.HORROR, new SerializableRegex(@"Horror", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.MUSIC, new SerializableRegex(@"Music", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.MYSTERY, new SerializableRegex(@"Mystery", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.ROMANCE, new SerializableRegex(@"Romance", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.SCIENCE_FICTION, new SerializableRegex(@"Science Fiction|Science-Fiction|Sci-Fi", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.THRILLER, new SerializableRegex(@"Thriller|Disaster|Suspense", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.WAR, new SerializableRegex(@"War", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.WESTERN, new SerializableRegex(@"Western", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.KIDS, new SerializableRegex(@"Kids|Children|Teen", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.NEWS, new SerializableRegex(@"News", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.REALITY, new SerializableRegex(@"Reality", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.SOAP, new SerializableRegex(@"Soap", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.TALK, new SerializableRegex(@"Talk", RegexOptions.IgnoreCase)),
          new GenreMapping(SeriesGenre.POLITICS, new SerializableRegex(@"Politic", RegexOptions.IgnoreCase)),
        };

    #endregion

    protected bool _onlyBasicFanArt = false;
    protected bool _useHttps = true;
    protected MatcherSetting[] _musicMatchers = new MatcherSetting[0];
    protected MatcherSetting[] _seriesMatchers = new MatcherSetting[0];
    protected MatcherSetting[] _movieMatchers = new MatcherSetting[0];
    protected GenreMapping[] _musicGenreMap = new GenreMapping[0];
    protected GenreMapping[] _seriesGenreMap = new GenreMapping[0];
    protected GenreMapping[] _movieGenreMap = new GenreMapping[0];
    protected string _musicLanguageCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture.Name;
    protected string _seriesLanguageCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture.Name;
    protected string _movieLanguageCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture.Name;

    public OnlineLibrarySettings()
    {
      if (string.IsNullOrEmpty(_musicLanguageCulture))
        _musicLanguageCulture = "en-US";
      if (string.IsNullOrEmpty(_movieLanguageCulture))
        _movieLanguageCulture = "en-US";
      if (string.IsNullOrEmpty(_seriesLanguageCulture))
        _seriesLanguageCulture = "en-US";
    }

    //Only download basic FanArt like backdrops, banners, posters and thumbnails
    //Not DiscArt,  ClearArt, Logos etc. 
    [Setting(SettingScope.Global)]
    public bool OnlyBasicFanArt
    {
      get { return _onlyBasicFanArt; }
      set { _onlyBasicFanArt = value; }
    }

    //Use HTTPS when available
    [Setting(SettingScope.Global)]
    public bool UseSecureWebCommunication
    {
      get { return _useHttps; }
      set { _useHttps = value; }
    }

    //Music matcher settings
    [Setting(SettingScope.Global)]
    public MatcherSetting[] MusicMatchers
    {
      get { return _musicMatchers; }
      set { _musicMatchers = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] MusicGenreMappings
    {
      get { return _musicGenreMap; }
      set { _musicGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public string MusicLanguageCulture
    {
      get { return _musicLanguageCulture; }
      set { _musicLanguageCulture = value; }
    }

    //Series matcher settings
    [Setting(SettingScope.Global)]
    public MatcherSetting[] SeriesMatchers
    {
      get { return _seriesMatchers; }
      set { _seriesMatchers = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] SeriesGenreMappings
    {
      get { return _seriesGenreMap; }
      set { _seriesGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public string SeriesLanguageCulture
    {
      get { return _seriesLanguageCulture; }
      set { _seriesLanguageCulture = value; }
    }

    //Movie matcher settings
    [Setting(SettingScope.Global)]
    public MatcherSetting[] MovieMatchers
    {
      get { return _movieMatchers; }
      set { _movieMatchers = value; }
    }

    [Setting(SettingScope.Global)]
    public GenreMapping[] MovieGenreMappings
    {
      get { return _movieGenreMap; }
      set { _movieGenreMap = value; }
    }

    [Setting(SettingScope.Global)]
    public string MovieLanguageCulture
    {
      get { return _movieLanguageCulture; }
      set { _movieLanguageCulture = value; }
    }
  }
}
