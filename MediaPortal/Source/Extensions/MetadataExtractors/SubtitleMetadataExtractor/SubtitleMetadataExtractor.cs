#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Extensions.MetadataExtractors.SubtitleMetadataExtractor.Settings;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SubtitleMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for subtitle files. Supports several formats.
  /// </summary>
  public class SubtitleMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the subtitle metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "5BC72F45-3EC6-41CC-BE37-CDEF5507B023";

    /// <summary>
    /// Subtitle metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected const string MEDIA_CATEGORY_NAME_MOVIE = "Movie";
    protected const string MEDIA_CATEGORY_NAME_SERIES = "Series";

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static ICollection<string> SUBTITLE_FILE_EXTENSIONS = new HashSet<string>();
    protected static ICollection<string> SUBTITLE_FOLDERS = new HashSet<string>();

    protected MetadataExtractorMetadata _metadata;
    protected SettingsChangeWatcher<SubtitleMetadataExtractorSettings> _settingWatcher;

    #endregion

    #region Ctor

    static SubtitleMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
      SubtitleMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SubtitleMetadataExtractorSettings>();
      InitializeExtensions(settings);

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(TempSubtitleAspect.Metadata);
    }

    /// <summary>
    /// (Re)initializes the subtitle extensions for which this <see cref="SubtitleMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(SubtitleMetadataExtractorSettings settings)
    {
      SUBTITLE_FILE_EXTENSIONS = new HashSet<string>(settings.SubtitleFileExtensions.Select(e => e.ToLowerInvariant()));
      SUBTITLE_FOLDERS = new HashSet<string>(settings.SubtitleFolders);
    }

    public SubtitleMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Subtitle metadata extractor", MetadataExtractorPriority.FallBack, true,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                SubtitleAspect.Metadata,
              });

      _settingWatcher = new SettingsChangeWatcher<SubtitleMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;
    }

    #endregion

    #region Settings

    public static bool SkipOnlineSearches { get; private set; }
    public static IEnumerable<string> ImportLanguageCultures { get; private set; }

    private void LoadSettings()
    {
      SubtitleMetadataExtractorSettings settings = _settingWatcher.Settings;
      SkipOnlineSearches = settings.SkipOnlineSearches;
      ImportLanguageCultures = settings.ImportLanguageCultures.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Returns the information if the specified file name (or path) has a file extension which is
    /// supposed to be supported by this metadata extractor.
    /// </summary>
    /// <param name="fileName">Relative or absolute file path to check.</param>
    /// <returns><c>true</c>, if the file's extension is supposed to be supported, else <c>false</c>.</returns>

    protected static bool HasSubtitleExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return SUBTITLE_FILE_EXTENSIONS.Contains(ext);
    }

    protected string GetSubtitleFormat(string subtitleSource)
    {
      if (string.Compare(Path.GetExtension(subtitleSource), ".srt", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_SRT;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".smi", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_SMI;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".ass", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_ASS;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".ssa", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_SSA;
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".sub", true, CultureInfo.InvariantCulture) == 0)
      {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(subtitleSource), Path.GetFileNameWithoutExtension(subtitleSource) + ".idx")) == true)
        {
          //Only the idx file should be imported
          return null;
        }
        else
        {
          string subContent = File.ReadAllText(subtitleSource);
          if (subContent.Contains("[INFORMATION]")) return SubtitleAspect.FORMAT_SUBVIEW;
          else if (subContent.Contains("}{")) return SubtitleAspect.FORMAT_MICRODVD;
        }
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".idx", true, CultureInfo.InvariantCulture) == 0)
      {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(subtitleSource), Path.GetFileNameWithoutExtension(subtitleSource) + ".sub")) == true)
        {
          return SubtitleAspect.FORMAT_VOBSUB;
        }
      }
      else if (string.Compare(Path.GetExtension(subtitleSource), ".vtt", true, CultureInfo.InvariantCulture) == 0)
      {
        return SubtitleAspect.FORMAT_WEBVTT;
      }
      return null;
    }

    protected string GetSubtitleEncoding(string subtitleSource, string subtitleLanguage)
    {
      if (string.IsNullOrEmpty(subtitleSource))
      {
        return null;
      }

      byte[] buffer = File.ReadAllBytes(subtitleSource);

      //Use byte order mark if any
      if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0XFE && buffer[3] == 0XFF)
        return "UTF-32";
      else if (buffer[0] == 0XFF && buffer[1] == 0XFE && buffer[2] == 0x00 && buffer[3] == 0x00)
        return "UTF-32";
      else if (buffer[0] == 0XFE && buffer[1] == 0XFF)
        return "UNICODEBIG";
      else if (buffer[0] == 0XFF && buffer[1] == 0XFE)
        return "UNICODELITTLE";
      else if (buffer[0] == 0XEF && buffer[1] == 0XBB && buffer[2] == 0XBF)
        return "UTF-8";
      else if (buffer[0] == 0X2B && buffer[1] == 0X2F && buffer[2] == 0x76)
        return "UTF-7";

      //Detect encoding from language
      if (string.IsNullOrEmpty(subtitleLanguage) == false)
      {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        foreach (CultureInfo culture in cultures)
        {
          if (culture.TwoLetterISOLanguageName.ToUpperInvariant() == subtitleLanguage.ToUpperInvariant())
          {
            return Encoding.GetEncoding(culture.TextInfo.ANSICodePage).BodyName.ToUpperInvariant();
          }
        }
      }

      //Detect encoding from file
      Ude.CharsetDetector cdet = new Ude.CharsetDetector();
      cdet.Feed(buffer, 0, buffer.Length);
      cdet.DataEnd();
      if (cdet.Charset != null && cdet.Confidence >= 0.1)
      {
        return Encoding.GetEncoding(cdet.Charset).BodyName.ToUpperInvariant();
      }

      //Use windows encoding
      return Encoding.Default.BodyName.ToUpperInvariant();
    }

    protected string GetSubtitleLanguage(string subtitleSource, bool imageBased)
    {
      if (string.IsNullOrEmpty(subtitleSource))
      {
        return null;
      }

      CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

      //Language from file name
      string[] tags = subtitleSource.ToUpperInvariant().Split('.');
      if (tags.Length > 2)
      {
        tags = tags.Where((t, index) => index > 0 && index < tags.Length - 1).ToArray(); //Ignore first element (title) and last element (extension)
        foreach (CultureInfo culture in cultures)
        {
          string languageName = culture.EnglishName;
          if (culture.IsNeutralCulture == false)
          {
            languageName = culture.Parent.EnglishName;
          }
          if (tags.Contains(languageName.ToUpperInvariant()) ||
            tags.Contains(culture.ThreeLetterISOLanguageName.ToUpperInvariant()) ||
            tags.Contains(culture.ThreeLetterWindowsLanguageName.ToUpperInvariant()) ||
            tags.Contains(culture.TwoLetterISOLanguageName.ToUpperInvariant()))
          {
            return culture.TwoLetterISOLanguageName;
          }
        }
      }

      //Language from file encoding
      if (!imageBased)
      {
        string encoding = GetSubtitleEncoding(subtitleSource, null);
        if (encoding != null)
        {
          switch (encoding.ToUpperInvariant())
          {
            case "US-ASCII":
              return "EN";

            case "WINDOWS-1253":
              return "EL";
            case "ISO-8859-7":
              return "EL";

            case "WINDOWS-1254":
              return "TR";

            case "WINDOWS-1255":
              return "HE";
            case "ISO-8859-8":
              return "HE";

            case "WINDOWS-1256":
              return "AR";
            case "ISO-8859-6":
              return "AR";

            case "WINDOWS-1258":
              return "VI";
            case "VISCII":
              return "VI";

            case "WINDOWS-31J":
              return "JA";
            case "EUC-JP":
              return "JA";
            case "Shift_JIS":
              return "JA";
            case "ISO-2022-JP":
              return "JA";

            case "X-MSWIN-936":
              return "ZH";
            case "GB18030":
              return "ZH";
            case "X-EUC-CN":
              return "ZH";
            case "GBK":
              return "ZH";
            case "GB2312":
              return "ZH";
            case "X-WINDOWS-950":
              return "ZH";
            case "X-MS950-HKSCS":
              return "ZH";
            case "X-EUC-TW":
              return "ZH";
            case "BIG5":
              return "ZH";
            case "BIG5-HKSCS":
              return "ZH";

            case "EUC-KR":
              return "KO";
            case "ISO-2022-KR":
              return "KO";

            case "TIS-620":
              return "TH";
            case "ISO-8859-11":
              return "TH";

            case "KOI8-R":
              return "RU";
            case "KOI7":
              return "RU";

            case "KOI8-U":
              return "UK";
          }
        }
      }

      return null;
    }

    protected bool IsImageBasedSubtitle(string subtitleFormat)
    {
      if (subtitleFormat == SubtitleAspect.FORMAT_DVBTEXT)
        return true;
      if (subtitleFormat == SubtitleAspect.FORMAT_VOBSUB)
        return true;
      if (subtitleFormat == SubtitleAspect.FORMAT_PGS)
        return true;

      return false;
    }

    protected string GetSubtitleMime(string subtitleFormat)
    {
      if (subtitleFormat == SubtitleAspect.FORMAT_SRT)
        return "text/srt";
      if (subtitleFormat == SubtitleAspect.FORMAT_MICRODVD)
        return "text/microdvd";
      if (subtitleFormat == SubtitleAspect.FORMAT_SUBVIEW)
        return "text/plain";
      if (subtitleFormat == SubtitleAspect.FORMAT_ASS)
        return "text/x-ass";
      if (subtitleFormat == SubtitleAspect.FORMAT_SSA)
        return "text/x-ssa";
      if (subtitleFormat == SubtitleAspect.FORMAT_SMI)
        return "smi/caption";
      if (subtitleFormat == SubtitleAspect.FORMAT_WEBVTT)
        return "text/vtt";
      if (subtitleFormat == SubtitleAspect.FORMAT_PGS)
        return "image/pgs";
      if (subtitleFormat == SubtitleAspect.FORMAT_VOBSUB)
        return "image/vobsub";
      if (subtitleFormat == SubtitleAspect.FORMAT_DVBTEXT)
        return "image/vnd.dvb.subtitle";

      return null;
    }

    protected int FindExternalSubtitles(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      int subtitleCount = 0;
      try
      {
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (!MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerResourceAspects))
          return 0;

        int newResourceIndex = -1;
        foreach (MultipleMediaItemAspect providerResourceAspect in providerResourceAspects)
        {
          int resouceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
          if (newResourceIndex < resouceIndex)
          {
            newResourceIndex = resouceIndex;
          }
        }
        newResourceIndex++;

        using (lfsra.EnsureLocalFileSystemAccess())
        {
          foreach (MultipleMediaItemAspect mmia in providerResourceAspects)
          {
            string accessorPath = (string)mmia.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
            ResourcePath resourcePath = ResourcePath.Deserialize(accessorPath);

            if (mmia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) != ProviderResourceAspect.TYPE_PRIMARY)
              continue;

            string filePath = LocalFsResourceProviderBase.ToDosPath(resourcePath);
            if (string.IsNullOrEmpty(filePath))
              continue;

            List<string> subs = new List<string>();
            int videoResouceIndex = (int)mmia.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
            string[] subFiles = Directory.GetFiles(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "*.*");
            if (subFiles != null)
              subs.AddRange(subFiles);
            foreach (string folder in SUBTITLE_FOLDERS)
            {
              if (string.IsNullOrEmpty(Path.GetPathRoot(folder)) && Directory.Exists(Path.Combine(Path.GetDirectoryName(filePath), folder))) //Is relative path
                subFiles = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(filePath), folder), Path.GetFileNameWithoutExtension(filePath) + "*.*");
              else if (Directory.Exists(folder)) //Is absolute path
                subFiles = Directory.GetFiles(folder, Path.GetFileNameWithoutExtension(filePath) + "*.*");

              if (subFiles != null)
                subs.AddRange(subFiles);
            }
            foreach (string subFile in subFiles)
            {
              if (!HasSubtitleExtension(subFile))
                continue;

              LocalFsResourceAccessor fsra = new LocalFsResourceAccessor((LocalFsResourceProvider)lfsra.ParentProvider, LocalFsResourceProviderBase.ToProviderPath(subFile));

              //Check if already exists
              bool exists = false;
              foreach (MultipleMediaItemAspect providerResourceAspect in providerResourceAspects)
              {
                string subAccessorPath = (string)providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                ResourcePath subResourcePath = ResourcePath.Deserialize(subAccessorPath);
                if (subResourcePath.Equals(fsra.CanonicalLocalResourcePath))
                {
                  //Already exists
                  exists = true;
                  break;
                }
              }
              if (exists)
                continue;

              string subFormat = GetSubtitleFormat(subFile);
              if (!string.IsNullOrEmpty(subFormat))
              {
                MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, newResourceIndex);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_SECONDARY);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, GetSubtitleMime(subFormat));
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
                providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, fsra.CanonicalLocalResourcePath.Serialize());

                MultipleMediaItemAspect subtitleResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, SubtitleAspect.Metadata);
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, newResourceIndex);
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, videoResouceIndex);
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, -1); //External subtitle
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, subFormat);
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_INTERNAL, false);
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_DEFAULT, subFile.ToLowerInvariant().Contains(".default."));
                subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_FORCED, subFile.ToLowerInvariant().Contains(".forced."));

                bool imageBased = IsImageBasedSubtitle(subFormat);
                string language = GetSubtitleLanguage(subFile, imageBased);
                if (language != null) subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, language);
                if (imageBased == false)
                {
                  string encoding = GetSubtitleEncoding(subFile, language);
                  if (encoding != null) subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, encoding);
                }
                else
                {
                  subtitleResourceAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, SubtitleAspect.BINARY_ENCODING);
                }
                newResourceIndex++;
                subtitleCount++;
              }
            }
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("SubtitleMetadataExtractor: Exception finding external subtitles for resource '{0}' (Text: '{1}')", e, lfsra.CanonicalLocalResourcePath, e.Message);
      }
      return subtitleCount;
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null)
          return false;

        if (!extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID))
          return false;

        if (forceQuickMode)
          return false;

        if (fsra.IsFile)
        {
          string filePath = fsra.ResourcePathName;

          using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          {
            ILocalFsResourceAccessor lfsra = rah.LocalFsResourceAccessor;
            if (lfsra != null)
            {
              int extSubtitles = FindExternalSubtitles(lfsra, extractedAspectData);
              int existsSubtitles = extractedAspectData.Count(d => d.Key == SubtitleAspect.ASPECT_ID);

              if (!SkipOnlineSearches && extSubtitles == 0 && existsSubtitles == 0)
              {
                SubtitleInfo subtitle = new SubtitleInfo();
                subtitle.FromMetadata(extractedAspectData);

                var sys = ServiceRegistration.Get<ISystemResolver>();
                subtitle.MediaFiles.Add(new ResourceLocator(sys.LocalSystemId, mediaItemAccessor.CanonicalLocalResourcePath));

                IEnumerable<SubtitleInfo> matches = null;
                if (extractedAspectData.ContainsKey(MovieAspect.ASPECT_ID))
                  matches = await OnlineMatcherService.Instance.FindMatchingMovieSubtitlesAsync(subtitle, ImportLanguageCultures.ToList()).ConfigureAwait(false);
                if (extractedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
                  matches = await OnlineMatcherService.Instance.FindMatchingEpisodeSubtitlesAsync(subtitle, ImportLanguageCultures.ToList()).ConfigureAwait(false);

                if (matches != null)
                {
                  //Order by language and ranking
                  var subtitles = matches?.OrderBy(s => s.LanguageMatchRank).OrderByDescending(s => s.MatchPercentage);
                  if (subtitles?.Count() > 0)
                  {
                    //Download for each language
                    foreach (var lang in subtitles.Select(s => s.LanguageMatchRank).Distinct().OrderBy(i => i))
                    {
                      var sub = subtitles.FirstOrDefault(s => s.LanguageMatchRank == lang);
                      if (sub != null)
                        await OnlineMatcherService.Instance.DownloadSubtitleAsync(subtitle, false);
                    }
                  }
                }
              }

              return true;
            }
          }
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("SubtitleMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", e, mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    public async Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      try
      {
        if (!(searchCategories?.Contains(MEDIA_CATEGORY_NAME_MOVIE) ?? true) && !(searchCategories?.Contains(MEDIA_CATEGORY_NAME_SERIES) ?? true))
          return null;

        if (!searchAspectData.ContainsKey(MovieAspect.ASPECT_ID) && !searchAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
          return null;

        string searchData = null;
        var reimportAspect = MediaItemAspect.GetAspect(searchAspectData, ReimportAspect.Metadata);
        if (reimportAspect != null)
          searchData = reimportAspect.GetAttributeValue<string>(ReimportAspect.ATTR_SEARCH);

        ServiceRegistration.Get<ILogger>().Debug("SubtitleMetadataExtractor: Search aspects to use: '{0}'", string.Join(",", searchAspectData.Keys));

        //Prepare search info
        SubtitleInfo subtitleSearchinfo = new SubtitleInfo();
        subtitleSearchinfo.FromMetadata(searchAspectData);
        if (!string.IsNullOrEmpty(searchData))
        {
          subtitleSearchinfo.MediaTitle = searchData;
          subtitleSearchinfo.Year = null;
        }

        // Perform online search
        List<MediaItemSearchResult> searchResults = new List<MediaItemSearchResult>();
        IEnumerable<SubtitleInfo> matches = new List<SubtitleInfo>();
        if (searchAspectData.ContainsKey(MovieAspect.ASPECT_ID))
          matches = await OnlineMatcherService.Instance.FindMatchingMovieSubtitlesAsync(subtitleSearchinfo, subtitleSearchinfo.Language?.Split(',').ToList()).ConfigureAwait(false);
        if (searchAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
          matches = await OnlineMatcherService.Instance.FindMatchingEpisodeSubtitlesAsync(subtitleSearchinfo, subtitleSearchinfo.Language?.Split(',').ToList()).ConfigureAwait(false);
        ServiceRegistration.Get<ILogger>().Debug("SubtitleMetadataExtractor: Subtitle search returned {0} matches", matches.Count());
        foreach (var match in matches.OrderBy(m => m.LanguageMatchRank ?? int.MaxValue).ThenByDescending(m => m.MatchPercentage ?? 0))
        {
          var result = new MediaItemSearchResult
          {
            Name = $"{match.DisplayName}",
            Providers = new List<string>(match.DataProviders),
            MatchPercentage = match.MatchPercentage ?? 0,
            Language = match.Language
          };

          //Add external Ids
          if (!string.IsNullOrEmpty(match.ImdbId))
            result.ExternalIds.Add("imdb.com", match.ImdbId);
          if (match.MovieDbId > 0)
            result.ExternalIds.Add("themoviedb.org", match.MovieDbId.ToString());
          if (match.TvdbId > 0)
            result.ExternalIds.Add("thetvdb.com", match.TvdbId.ToString());

          //Assign aspects and remove unwanted aspects
          match.SetMetadata(result.AspectData, true);

          searchResults.Add(result);
        }
        return searchResults;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("SubtitleMetadataExtractor: Exception searching for matches (Text: '{0}')", e.Message);
      }
      return null;
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    public async Task<bool> DownloadMetadataAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      try
      {
        if (aspectData.ContainsKey(TempSubtitleAspect.ASPECT_ID) && aspectData.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        {
          SubtitleInfo info = new SubtitleInfo();
          info.FromMetadata(aspectData);
          return await OnlineMatcherService.Instance.DownloadSubtitleAsync(info, true).ConfigureAwait(false);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("SubtitleMetadataExtractor: Exception downloading subtitle (Text: '{0}')", e.Message);
      }
      return false;
    }

    #endregion
  }
}
