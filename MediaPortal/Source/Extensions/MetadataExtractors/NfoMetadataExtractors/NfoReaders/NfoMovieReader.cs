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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using System.Globalization;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders
{
  /// <summary>
  /// Reads the content of a nfo-file for movies into <see cref="MovieStub"/> objects and stores
  /// the appropriate values into the respective <see cref="MediaItemAspect"/>s
  /// </summary>
  /// <remarks>
  /// There is a TryRead method for any known child element of the nfo-file's root element and a
  /// TryWrite method for any MIA-Attribute we store values in.
  /// This class can parse much more information than we can currently store in our MediaLibrary.
  /// For performance reasons, the following long lasting operations have been temporarily disabled:
  /// - We do parse "set" (and therefore also "sets" elements); however, parsing and downloading
  ///   "setimage" child elements has been disabled. Reenable in <see cref="TryReadSetAsync"/>
  /// - We do parse "actor" and "procuder" elements, however, parsing and downloading "thumb"
  ///   child elements has been disabled. Reenable in <see cref="NfoReaderBase{T}.ParsePerson"/>
  /// - The following elements are completely ignored:
  ///   "fanart", "discart", "logo", "clearart", "banner", "Banner" and "Landscape"
  ///   Reenable in <see cref="InitializeSupportedElements"/>
  /// ToDo: Reenable the above once we can store the information in our MediaLibrary
  /// </remarks>
  class NfoMovieReader : NfoReaderBase<MovieStub>
  {
    #region Consts

    /// <summary>
    /// The name of the root element in a valid nfo-file for movies
    /// </summary>
    private const string MOVIE_ROOT_ELEMENT_NAME = "movie";

    #endregion

    #region Ctor

    /// <summary>
    /// Instantiates a <see cref="NfoMovieReader"/> object
    /// </summary>
    /// <param name="debugLogger">Debug logger to log to</param>
    /// <param name="miNumber">Unique number of the MediaItem for which the nfo-file is parsed</param>
    /// <param name="importOnly">If true, no long lasting operations such as parsing images are performed</param>
    /// <param name="httpClient"><see cref="HttpClient"/> used to download from http URLs contained in nfo-files</param>
    /// <param name="settings">Settings of the <see cref="NfoMovieMetadataExtractor"/></param>
    public NfoMovieReader(ILogger debugLogger, long miNumber, bool importOnly, HttpClient httpClient, NfoMovieMetadataExtractorSettings settings)
      : base(debugLogger, miNumber, importOnly, httpClient, settings)
    {
      InitializeSupportedElements();
      InitializeSupportedAttributes();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Treats the nfo-file's content as a string and parses it for a valid IMDB-ID; 
    /// if one is found and it represents a movie, it is stored in the stub object.
    /// </summary>
    /// <remarks>
    /// Used as a fallback, if the nfo-file cannot be parsed with XmlNfoReader. After calling
    /// this method, there is only one stub object with the IMDB-ID as only metadata. Any
    /// additional metadata must be fetched by the MDEs applied after the NfoMovieMDE.
    /// </remarks>
    /// <param name="nfoFsra"><see cref="IFileSystemResourceAccessor"/> pointing to the nfo-file</param>
    /// <returns><c>true</c> if a valid IMDB-ID for a movie was found; otherwise <c>false</c></returns>
    public async Task<bool> TryParseForImdbId(IFileSystemResourceAccessor nfoFsra)
    {
      // Make sure the nfo-file was read into _nfoBytes as byte array
      if (_nfoBytes == null && !await TryReadNfoFileAsync(nfoFsra).ConfigureAwait(false))
        return false;

      try
      {
        // ReSharper disable once AssignNullToNotNullAttribute
        // TryReadNfoFileAsync makes sure that _nfoBytes is not null
        using (var nfoMemoryStream = new MemoryStream(_nfoBytes))
        using (var nfoReader = new StreamReader(nfoMemoryStream, true))
        {
          // Convert the byte array to a string
          var nfoString = nfoReader.ReadToEnd();

          Match match = ((NfoMovieMetadataExtractorSettings)_settings).ImdbIdRegex.Regex.Match(nfoString);
          if (match.Success)
          {
            string imdbId = match.Groups[1].Value;
            _debugLogger.Debug("[#{0}]: Imdb-ID: '{1}' found when parsing the nfo-file as plain text.", _miNumber, imdbId);

            // Returns true, if the found IMDB-ID represents a movie (not a series)
            if (MovieTheMovieDbMatcher.Instance.FindAndUpdateMovie(new MovieInfo { ImdbId = imdbId }, false))
            {
              _debugLogger.Debug("[#{0}]: Imdb-ID: '{1}' confirmed online to represent a movie. Storing only Imdb-ID.", _miNumber, imdbId);
              var stub = new MovieStub { Id = imdbId };
              _stubs.Clear();
              _stubs.Add(stub);
              return true;
            }
            _debugLogger.Warn("[#{0}]: Cannot extract metadata; Imdb-ID: '{1}' could not be found or does not represent a movie on TheMovieDb.com", _miNumber, imdbId);
          }
          else
            _debugLogger.Warn("[#{0}]: Cannot extract metadata; no valid Imdb-ID was found in the nfo-file.", _miNumber);
        }
      }
      catch (Exception e)
      {
        _debugLogger.Warn("[#{0}]: Cannot extract metadata; error when trying to parse the nfo-file for a valid Imdb-ID", e, _miNumber);
      }
      return false;
    }

    #endregion

    #region Private methods

    #region Ctor helpers

    /// <summary>
    /// Adds a delegate for each xml element in a movie nfo-file that is understood by this MetadataExtractor to NfoReaderBase._supportedElements
    /// </summary>
    private void InitializeSupportedElements()
    {
      _supportedElements.Add("id", new TryReadElementDelegate(TryReadId));
      _supportedElements.Add("imdb", new TryReadElementDelegate(TryReadImdb));
      _supportedElements.Add("tmdbid", new TryReadElementDelegate(TryReadTmdbId));
      _supportedElements.Add("tmdbId", new TryReadElementDelegate(TryReadTmdbId)); // Tiny Media Manager (v2.6.5) uses <tmdbId> instead of <tmdbid>
      _supportedElements.Add("thmdb", new TryReadElementDelegate(TryReadThmdb));
      _supportedElements.Add("ids", new TryReadElementDelegate(TryReadIds)); // Used by Tiny MediaManager (as of v2.6.0)
      _supportedElements.Add("allocine", new TryReadElementDelegate(TryReadAllocine));
      _supportedElements.Add("cinepassion", new TryReadElementDelegate(TryReadCinepassion));
      _supportedElements.Add("title", new TryReadElementDelegate(TryReadTitle));
      _supportedElements.Add("originaltitle", new TryReadElementDelegate(TryReadOriginalTitle));
      _supportedElements.Add("sorttitle", new TryReadElementDelegate(TryReadSortTitle));
      _supportedElements.Add("set", new TryReadElementAsyncDelegate(TryReadSetAsync));
      _supportedElements.Add("sets", new TryReadElementAsyncDelegate(TryReadSetsAsync));
      _supportedElements.Add("premiered", new TryReadElementDelegate(TryReadPremiered));
      _supportedElements.Add("year", new TryReadElementDelegate(TryReadYear));
      _supportedElements.Add("country", new TryReadElementDelegate(TryReadCountry));
      _supportedElements.Add("company", new TryReadElementDelegate(TryReadCompany));
      _supportedElements.Add("studio", new TryReadElementDelegate(TryReadStudio));
      _supportedElements.Add("studios", new TryReadElementDelegate(TryReadStudio)); // Synonym for <studio>
      _supportedElements.Add("actor", new TryReadElementAsyncDelegate(TryReadActorAsync));
      _supportedElements.Add("producer", new TryReadElementAsyncDelegate(TryReadProducerAsync));
      _supportedElements.Add("director", new TryReadElementDelegate(TryReadDirector));
      _supportedElements.Add("directorimdb", new TryReadElementDelegate(TryReadDirectorImdb));
      _supportedElements.Add("credits", new TryReadElementDelegate(TryReadCredits));
      _supportedElements.Add("plot", new TryReadElementDelegate(TryReadPlot));
      _supportedElements.Add("outline", new TryReadElementDelegate(TryReadOutline));
      _supportedElements.Add("tagline", new TryReadElementDelegate(TryReadTagline));
      _supportedElements.Add("trailer", new TryReadElementDelegate(TryReadTrailer));
      _supportedElements.Add("genre", new TryReadElementDelegate(TryReadGenre));
      _supportedElements.Add("genres", new TryReadElementDelegate(TryReadGenres));
      _supportedElements.Add("language", new TryReadElementDelegate(TryReadLanguage));
      _supportedElements.Add("languages", new TryReadElementDelegate(TryReadLanguage)); // Synonym for <language>
      _supportedElements.Add("thumb", new TryReadElementAsyncDelegate(TryReadThumbAsync));
      _supportedElements.Add("fanart", new TryReadElementAsyncDelegate(TryReadFanArtAsync));
      _supportedElements.Add("discart", new TryReadElementAsyncDelegate(TryReadDiscArtAsync));
      _supportedElements.Add("logo", new TryReadElementAsyncDelegate(TryReadLogoAsync));
      _supportedElements.Add("clearart", new TryReadElementAsyncDelegate(TryReadClearArtAsync));
      _supportedElements.Add("banner", new TryReadElementAsyncDelegate(TryReadBannerAsync));
      _supportedElements.Add("Banner", new TryReadElementAsyncDelegate(TryReadBannerAsync)); // Used wrongly by XBNE instead of <banner>
      _supportedElements.Add("Landscape", new TryReadElementAsyncDelegate(TryReadLandscapeAsync)); // Used by XBNE (capital letter in the beginning correct, but not according to spec)
      _supportedElements.Add("certification", new TryReadElementDelegate(TryReadCertification));
      _supportedElements.Add("mpaa", new TryReadElementDelegate(TryReadMpaa));
      _supportedElements.Add("rating", new TryReadElementDelegate(TryReadRating));
      _supportedElements.Add("ratings", new TryReadElementDelegate(TryReadRatings));
      _supportedElements.Add("votes", new TryReadElementDelegate(TryReadVotes));
      _supportedElements.Add("review", new TryReadElementDelegate(TryReadReview));
      _supportedElements.Add("top250", new TryReadElementDelegate(TryReadTop250));
      _supportedElements.Add("runtime", new TryReadElementDelegate(TryReadRuntime));
      _supportedElements.Add("fps", new TryReadElementDelegate(TryReadFps));
      _supportedElements.Add("rip", new TryReadElementDelegate(TryReadRip));
      _supportedElements.Add("fileinfo", new TryReadElementDelegate(TryReadFileInfo));
      _supportedElements.Add("epbookmark", new TryReadElementDelegate(TryReadEpBookmark));
      _supportedElements.Add("watched", new TryReadElementDelegate(TryReadWatched));
      _supportedElements.Add("playcount", new TryReadElementDelegate(TryReadPlayCount));
      _supportedElements.Add("lastplayed", new TryReadElementDelegate(TryReadLastPlayed));
      _supportedElements.Add("dateadded", new TryReadElementDelegate(TryReadDateAdded));
      _supportedElements.Add("resume", new TryReadElementDelegate(TryReadResume));

      // The following element readers have been added above, but are replaced by the Ignore method here for performance reasons
      // ToDo: Reenable the below once we can store the information in the MediaLibrary
      _supportedElements["fanart"] = new TryReadElementDelegate(Ignore);
      _supportedElements["discart"] = new TryReadElementDelegate(Ignore);
      _supportedElements["logo"] = new TryReadElementDelegate(Ignore);
      _supportedElements["clearart"] = new TryReadElementDelegate(Ignore);
      _supportedElements["banner"] = new TryReadElementDelegate(Ignore);
      _supportedElements["Banner"] = new TryReadElementDelegate(Ignore);
      _supportedElements["Landscape"] = new TryReadElementDelegate(Ignore);

      // The following elements are contained in many movie.nfo files, but have no meaning
      // in the context of a movie. We add them here to avoid them being logged as
      // unknown elements, but we simply ignore them.
      // For reference see here: http://forum.kodi.tv/showthread.php?tid=40422&pid=244349#pid244349
      _supportedElements.Add("status", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("code", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("aired", new TryReadElementDelegate(Ignore));

      // We never need the following element as we get the same information in a more accurate way
      // from the filesystem itself; hence, we ignore it.
      _supportedElements.Add("filenameandpath", new TryReadElementDelegate(Ignore));
    }

    /// <summary>
    /// Adds a delegate for each Attribute in a MediaItemAspect into which this MetadataExtractor can write metadata to NfoReaderBase._supportedAttributes
    /// </summary>
    private void InitializeSupportedAttributes()
    {
      _supportedAttributes.Add(TryWriteMediaAspectTitle);
      _supportedAttributes.Add(TryWriteMediaAspectRecordingTime);
      _supportedAttributes.Add(TryWriteMediaAspectPlayCount);
      _supportedAttributes.Add(TryWriteMediaAspectLastPlayed);

      _supportedAttributes.Add(TryWriteVideoAspectGenres);
      _supportedAttributes.Add(TryWriteVideoAspectActors);
      _supportedAttributes.Add(TryWriteVideoAspectDirectors);
      _supportedAttributes.Add(TryWriteVideoAspectWriters);
      _supportedAttributes.Add(TryWriteVideoAspectStoryPlot);

      _supportedAttributes.Add(TryWriteMovieAspectCompanies);
      _supportedAttributes.Add(TryWriteMovieAspectMovieName);
      _supportedAttributes.Add(TryWriteMovieAspectOrigName);
      _supportedAttributes.Add(TryWriteMovieAspectTmdbId);
      _supportedAttributes.Add(TryWriteMovieAspectImdbId);
      _supportedAttributes.Add(TryWriteMovieAspectAllocineId);
      _supportedAttributes.Add(TryWriteMovieAspectCinePassionId);
      _supportedAttributes.Add(TryWriteMovieAspectCollectionName);
      _supportedAttributes.Add(TryWriteMovieAspectRuntime);
      _supportedAttributes.Add(TryWriteMovieAspectCertification);
      _supportedAttributes.Add(TryWriteMovieAspectTagline);
      _supportedAttributes.Add(TryWriteMovieAspectTotalRating);
      _supportedAttributes.Add(TryWriteMovieAspectRatingCount);

      _supportedAttributes.Add(TryWriteThumbnailLargeAspectThumbnail);
    }

    #endregion

    #region Reader methods for direct child elements of the root element

    #region Internet databases

    /// <summary>
    /// Tries to read the Imdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadId(XElement element)
    {
      // Example of a valid element:
      // <id>tt0111161</id>
      return ((_currentStub.Id = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the Imdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadImdb(XElement element)
    {
      // Example of a valid element:
      // <imdb>tt0810913</imdb>
      return ((_currentStub.Imdb = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the Tmdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTmdbId(XElement element)
    {
      // Examples of valid elements:
      // <tmdbId>52913<tmdbId>
      // <tmdbid>52913<tmdbid>
      return ((_currentStub.TmdbId = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the Tmdb ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadThmdb(XElement element)
    {
      // Example of a valid element:
      // <thmdb>52913</thmdb>
      return ((_currentStub.Thmdb = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the Allocine ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadAllocine(XElement element)
    {
      // Example of a valid element:
      // <allocine>52719</allocine>
      return ((_currentStub.Allocine = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the Cinepassion ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCinepassion(XElement element)
    {
      // Example of a valid element:
      // <cinepassion>52719</cinepassion>
      return ((_currentStub.Cinepassion = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read Ids values
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadIds(XElement element)
    {
      // Example of a valid element:
      // <ids>
      //   <entry>
      //     <key>imdbId</key>
      //     <value xsi:type="xs:string" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema" xmlns:xsi="8769http://www.w3.org/2001/XMLSchema-instance">8769</value>
      //   </entry>
      //   <entry>
      //     <key>tmdbId</key>
      //     <value xsi:type="xs:int" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsi="8769http://www.w3.org/2001/XMLSchema-instance">8769</value>
      //   </entry>
      // </ids>
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
      {
        if (childElement.Name == "entry")
        {
          var keyString = ParseSimpleString(childElement.Element("key"));
          switch (keyString)
          {
            case null:
              _debugLogger.Warn("[#{0}]: Key element missing {1}", _miNumber, childElement);
              break;
            case "imdbId":
              result = ((_currentStub.IdsImdbId = ParseSimpleString(childElement.Element("value"))) != null);
              break;
            case "tmdbId":
              result = ((_currentStub.IdsTmdbId = ParseSimpleInt(childElement.Element("value"))) != null) || result;
              break;
            default:
              _debugLogger.Warn("[#{0}]: Unknown Key element {1}", _miNumber, childElement);
              break;
          }
        }
        else
          _debugLogger.Warn("[#{0}]: Unknown child element: {1}", _miNumber, childElement);
      }
      return result;
    }

    #endregion

    #region Title information

    /// <summary>
    /// Tries to read the title
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTitle(XElement element)
    {
      // Example of a valid element:
      // <title>Harry Potter und der Orden des Phönix</title>
      return ((_currentStub.Title = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the original title
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadOriginalTitle(XElement element)
    {
      // Example of a valid element:
      // <originaltitle>Harry Potter and the Order of the Phoenix</originaltitle>
      return ((_currentStub.OriginalTitle = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the sort title
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadSortTitle(XElement element)
    {
      // Example of a valid element:
      // <sorttitle>Harry Potter Collection05</sorttitle>
      return ((_currentStub.SortTitle = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read the set information
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadSetAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // Examples of valid elements:
      // 1:
      // <set order = "1">Set Name</set>
      // 2:
      // <set order = "1">
      //   <setname>Harry Potter</setname>
      //   <setdescription>Magician ...</setdescription>
      //   <setrule></setrule>
      //   <setimage></setimage>
      // </set>
      // The order attribute in both cases is optional.
      // In example 2 only the <setname> child element is mandatory.
      if (element == null)
        return false;

      var value = new SetStub();
      // Example 1:
      if (!element.HasElements)
        value.Name = ParseSimpleString(element);
      // Example 2:
      else
      {
        value.Name = ParseSimpleString(element.Element("setname"));
        value.Description = ParseSimpleString(element.Element("setdescription"));
        value.Rule = ParseSimpleString(element.Element("setrule"));
        //ToDo: Reenable parsing <setimage> child elements once we can store them in the MediaLibrary
        value.Image = await Task.FromResult<byte[]>(null); // ParseSimpleImageAsync(element.Element("setimage"), nfoDirectoryFsra).ConfigureAwait(false);
      }
      value.Order = ParseIntAttribute(element, "order");

      if (value.Name == null)
        return false;

      if (_currentStub.Sets == null)
        _currentStub.Sets = new HashSet<SetStub>();
      _currentStub.Sets.Add(value);
      return true;
    }

    /// <summary>
    /// Tries to (asynchronously) read the sets information
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadSetsAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // Example of a valid element:
      // <sets>
      //   [any number of set elements that can be read by TryReadSetAsync]
      // </sets>
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
        if (childElement.Name == "set")
        {
          if (await TryReadSetAsync(childElement, nfoDirectoryFsra).ConfigureAwait(false))
            result = true;
        }
        else
          _debugLogger.Warn("[#{0}]: Unknown child element: {1}", _miNumber, childElement);
      return result;
    }

    #endregion

    #region Making-of information

    /// <summary>
    /// Tries to read the premiered value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadPremiered(XElement element)
    {
      // Examples of valid elements:
      // <premiered>1994-09-14</premiered>
      // <premiered>1994</premiered>
      return ((_currentStub.Premiered = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the year value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadYear(XElement element)
    {
      // Examples of valid elements:
      // <year>1994-09-14</year>
      // <year>1994</year>
      return ((_currentStub.Year = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read a country value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCountry(XElement element)
    {
      // Examples of valid elements:
      // <country>DE</country>
      // <country>US, GB</country>
      return ((_currentStub.Countries = ParseCharacterSeparatedStrings(element, _currentStub.Countries)) != null);
    }

    /// <summary>
    /// Tries to read a company value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCompany(XElement element)
    {
      // Examples of valid elements:
      // <company>Warner Bros. Pictures</company>
      // <company>Happy Madison Productions / Columbia Pictures</company>
      return ((_currentStub.Companies = ParseCharacterSeparatedStrings(element, _currentStub.Companies)) != null);
    }

    /// <summary>
    /// Tries to read a studio or studios value
    /// </summary>
    /// <param name="element">Element to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadStudio(XElement element)
    {
      // Examples of valid elements:
      // <studio>Warner Bros. Pictures</studio>
      // <studio>Happy Madison Productions / Columbia Pictures</studio>
      return ((_currentStub.Studios = ParseCharacterSeparatedStrings(element, _currentStub.Studios)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read an actor value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadActorAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment in NfoReaderBase.ParsePerson
      var person = await ParsePerson(element, nfoDirectoryFsra).ConfigureAwait(false);
      if (person == null)
        return false;
      if (_currentStub.Actors == null)
        _currentStub.Actors = new HashSet<PersonStub>();
      _currentStub.Actors.Add(person);
      return true;
    }

    /// <summary>
    /// Tries to (asynchronously) read a producer value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadProducerAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment in NfoReaderBase.ParsePerson
      var person = await ParsePerson(element, nfoDirectoryFsra).ConfigureAwait(false);
      if (person == null)
        return false;
      if (_currentStub.Producers == null)
        _currentStub.Producers = new HashSet<PersonStub>();
      _currentStub.Producers.Add(person);
      return true;
    }

    /// <summary>
    /// Tries to read the director value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDirector(XElement element)
    {
      // Example of a valid element:
      // <director>Dennis Dugan</director>
      return ((_currentStub.Director = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the directorimdb value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDirectorImdb(XElement element)
    {
      // Example of a valid element:
      // <directorimdb>nm0240797</directorimdb>
      return ((_currentStub.DirectorImdb = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read a credits value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCredits(XElement element)
    {
      // Examples of valid elements:
      // <credits>Adam Sandler</credits>
      // <credits>Adam Sandler / Steve Koren</credits>
      return ((_currentStub.Credits = ParseCharacterSeparatedStrings(element, _currentStub.Credits)) != null);
    }

    #endregion

    #region Content information

    /// <summary>
    /// Tries to read the plot value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadPlot(XElement element)
    {
      // Example of a valid element:
      // <plot>This movie tells a story about...</plot>
      return ((_currentStub.Plot = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the outline value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadOutline(XElement element)
    {
      // Example of a valid element:
      // <outline>This movie tells a story about...</outline>
      return ((_currentStub.Outline = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the tagline value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTagline(XElement element)
    {
      // Example of a valid element:
      // <tagline>This movie tells a story about...</tagline>
      return ((_currentStub.Tagline = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the trailer value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTrailer(XElement element)
    {
      // Example of a valid element:
      // <trailer>This movie tells a story about...</trailer>
      return ((_currentStub.Trailer = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read a genre value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadGenre(XElement element)
    {
      // Examples of valid elements:
      // <genre>Horror</genre>
      // <genre>Horror / Trash</genre>
      return ((_currentStub.Genres = ParseCharacterSeparatedStrings(element, _currentStub.Genres)) != null);
    }

    /// <summary>
    /// Tries to read a genres value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadGenres(XElement element)
    {
      // Example of a valid element:
      // <genres>
      //  <genre>[genre-value]</genre>
      // </genres>
      // There can be one of more <genre> child elements
      // [genre-value] can be any value that can be read by TryReadGenre
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
      {
        if (childElement.Name == "genre")
          result = TryReadGenre(childElement) || result;
        else
          _debugLogger.Warn("[#{0}]: Unknown child element: {1}", _miNumber, childElement);
      }
      return result;
    }

    /// <summary>
    /// Tries to read a language or languages value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadLanguage(XElement element)
    {
      // Examples of valid elements:
      // <language>de</language>
      // <language>de, en</language>
      // <languages>de</languages>
      // <languages>de, en</languages>
      return ((_currentStub.Languages = ParseCharacterSeparatedStrings(element, _currentStub.Languages)) != null);
    }

    #endregion

    #region Images

    /// <summary>
    /// Tries to (asynchronously) read the thumbnail image
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadThumbAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseSimpleImageAsync
      return ((_currentStub.Thumb = await ParseSimpleImageAsync(element, nfoDirectoryFsra).ConfigureAwait(false)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more fanart images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadFanArtAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values c of NfoReaderBase.ParseMultipleImagesAsync
      return ((_currentStub.FanArt = await ParseMultipleImagesAsync(element, _currentStub.FanArt, nfoDirectoryFsra).ConfigureAwait(false)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more discart images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadDiscArtAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((_currentStub.DiscArt = await ParseMultipleImagesAsync(element, _currentStub.DiscArt, nfoDirectoryFsra).ConfigureAwait(false)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more logo images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadLogoAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((_currentStub.Logos = await ParseMultipleImagesAsync(element, _currentStub.Logos, nfoDirectoryFsra).ConfigureAwait(false)) != null);
    }

    /// <summary>
    /// Tries to read one or more clearart images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadClearArtAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((_currentStub.ClearArt = await ParseMultipleImagesAsync(element, _currentStub.ClearArt, nfoDirectoryFsra).ConfigureAwait(false)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more banner images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadBannerAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((_currentStub.Banners = await ParseMultipleImagesAsync(element, _currentStub.Banners, nfoDirectoryFsra).ConfigureAwait(false)) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read one or more landscape images
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadLandscapeAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // For examples of valid element values see the comment of NfoReaderBase.ParseMultipleImagesAsync
      return ((_currentStub.Landscape = await ParseMultipleImagesAsync(element, _currentStub.Landscape, nfoDirectoryFsra).ConfigureAwait(false)) != null);
    }

    #endregion

    #region Certification and ratings

    /// <summary>
    /// Tries to read a certification value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCertification(XElement element)
    {
      // Examples of valid elements:
      // <certification>12</certification>
      // <certification>DE:FSK 12</certification>
      // <certification>DE:FSK 12 / DE:FSK12 / DE:12 / DE:ab 12</certification>
      return ((_currentStub.Certification = ParseCharacterSeparatedStrings(element, _currentStub.Certification)) != null);
    }

    /// <summary>
    /// Tries to read a mpaa value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadMpaa(XElement element)
    {
      // Examples of valid elements:
      // <mpaa>12</mpaa>
      // <mpaa>DE:FSK 12</mpaa>
      // <mpaa>DE:FSK 12 / DE:FSK12 / DE:12 / DE:ab 12</mpaa>
      return ((_currentStub.Mpaa = ParseCharacterSeparatedStrings(element, _currentStub.Mpaa)) != null);
    }

    /// <summary>
    /// Tries to read a ratings value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRatings(XElement element)
    {
      // Example of a valid element:
      // <ratings>
      //   <rating moviedb="imdb">8.7</rating>
      // </ratings>
      // The <ratings> element can contain one or more <rating> child elements
      // A value of 0 (zero) is ignored
      if (element == null || !element.HasElements)
        return false;
      var result = false;
      foreach (var childElement in element.Elements())
        if (childElement.Name == "rating")
        {
          var moviedb = ParseStringAttribute(childElement, "moviedb");
          var rating = ParseSimpleDecimal(element);
          if (moviedb != null && rating != null && rating.Value != decimal.Zero)
          {
            result = true;
            if (_currentStub.Ratings == null)
              _currentStub.Ratings = new Dictionary<string, decimal>();
            _currentStub.Ratings[moviedb] = rating.Value;
          }
        }
        else
          _debugLogger.Warn("[#{0}]: Unknown child element: {1}", _miNumber, childElement);
      return result;
    }

    /// <summary>
    /// Tries to read the rating value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRating(XElement element)
    {
      // Example of a valid element:
      // <rating>8.5</rating>
      // A value of 0 (zero) is ignored
      var value = ParseSimpleDecimal(element);
      if (value == null || value.Value == decimal.Zero)
        return false;
      _currentStub.Rating = value;
      return true;
    }

    /// <summary>
    /// Tries to read the votes value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadVotes(XElement element)
    {
      // Example of a valid element:
      // <votes>2941</votes>
      // A value of 0 (zero) is ignored
      var value = ParseSimpleInt(element);
      if (value == 0)
        value = null;
      return ((_currentStub.Votes = value) != null);
    }

    /// <summary>
    /// Tries to read the review value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadReview(XElement element)
    {
      // Example of a valid element:
      // <review>This movie is great...</review>
      return ((_currentStub.Review = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the top250 value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTop250(XElement element)
    {
      // Examples of valid elements:
      // <top250>250</top250>
      // <top250>0</top250>
      // A value of 0 (zero) is ignored
      var value = ParseSimpleInt(element);
      if (value == 0)
        value = null;
      return ((_currentStub.Top250 = value) != null);
    }

    #endregion

    #region Media file information

    /// <summary>
    /// Tries to read the runtime value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRuntime(XElement element)
    {
      // Examples of valid elements:
      // 1:
      // <runtime>120</runtime>
      // 2:
      // <runtime>2h 00mn</runtime>
      // 3:
      // <runtime>2h 00min</runtime>
      // The value in Example 1 may have decimal places and is interpreted as number of minutes
      // A value of less than 1 second is ignored
      var runtimeString = ParseSimpleString(element);
      if (runtimeString == null)
        return false;

      double runtimeDouble;
      if (double.TryParse(runtimeString, out runtimeDouble))
      {
        // Example 1
        if (runtimeDouble < (1.0 / 60.0))
          return false;
        _currentStub.Runtime = TimeSpan.FromMinutes(runtimeDouble);
        return true;
      }

      var match = Regex.Match(runtimeString, @"(\d+)\s*h\s*(\d+)\s*[mn|min]", RegexOptions.IgnoreCase);
      if (!match.Success)
        return false;
      
      // Examples 2 and 3
      var hours = int.Parse(match.Groups[1].Value);
      var minutes = int.Parse(match.Groups[2].Value);
      _currentStub.Runtime = new TimeSpan(hours, minutes, 0);
      return true;
    }

    /// <summary>
    /// Tries to read the fps value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadFps(XElement element)
    {
      // Examples of valid elements:
      // <fps>25</fps>
      // <fps>23.976<fps>
      return ((_currentStub.Fps = ParseSimpleDecimal(element)) != null);
    }

    /// <summary>
    /// Tries to read the rip value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadRip(XElement element)
    {
      // Example of a valid element:
      // <rip>Bluray</rip>
      return ((_currentStub.Rip = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the fileinfo value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadFileInfo(XElement element)
    {
      // Example of a valid element:
      // <fileinfo>
      //   <streamdetails>
      //     <video>
      //       <codec>h264</codec>
      //       <aspect>2.4</aspect>
      //       <width>1280</width>
      //       <height>534</height>
      //       <duration>149</duration>
      //       <durationinseconds>8940</durationinseconds>
      //       <stereomode></stereomode>
      //     </video>
      //     <audio>
      //       <codec>ac3</codec>
      //       <language>French</language>
      //       <channels>6</channels>
      //     </audio>
      //     <subtitle>
      //       <language>French</language>
      //     </subtitle>
      //   </streamdetails>
      // </fileinfo>
      // There can be multiple <streamdetails> elements in the <fileinfo> element.
      // There can be multiple <video>, <audio> and/or <subtitle> elements in one <streamdetails> element.
      // the <durationinseconds> element is preferred over the <duration> element.
      if (element == null || !element.HasElements)
        return false;

      var fileInfoFound = false;
      foreach (var stream in element.Elements())
      {
        if (stream.Name != "streamdetails" || !stream.HasElements)
        {
          _debugLogger.Warn("[#{0}]: Unknown or empty child element {1}", _miNumber, stream);
          continue;
        }
        var streamDetails = new StreamDetailsStub();
        var streamValueFound = false;
        foreach (var streamDetail in stream.Elements())
        {
          switch (streamDetail.Name.ToString())
          {
            case "video":
              var videoDetails = new VideoStreamDetailsStub();
              var videoDetailValueFound = ((videoDetails.Codec = ParseSimpleString(streamDetail.Element("codec"))) != null);
              videoDetailValueFound = ((videoDetails.Aspect = ParseSimpleDecimal(streamDetail.Element("aspect"))) != null) || videoDetailValueFound;
              videoDetailValueFound = ((videoDetails.Width = ParseSimpleInt(streamDetail.Element("width"))) != null) || videoDetailValueFound;
              videoDetailValueFound = ((videoDetails.Height = ParseSimpleInt(streamDetail.Element("height"))) != null) || videoDetailValueFound;
              var durationMinutes = ParseSimpleInt(streamDetail.Element("duration"));
              if (durationMinutes != null && durationMinutes > 0)
              {
                videoDetails.Duration = new TimeSpan(0, durationMinutes.Value, 0);
                videoDetailValueFound = true;
              }
              var durationSeconds = ParseSimpleInt(streamDetail.Element("durationinseconds"));
              if (durationSeconds != null && durationSeconds > 0)
              {
                videoDetails.Duration = new TimeSpan(0, 0, durationSeconds.Value);
                videoDetailValueFound = true;
              }
              videoDetailValueFound = ((videoDetails.Stereomode = ParseSimpleString(streamDetail.Element("stereomode"))) != null) || videoDetailValueFound;

              if (videoDetailValueFound)
              {
                if (streamDetails.VideoStreams == null)
                  streamDetails.VideoStreams = new HashSet<VideoStreamDetailsStub>();
                streamDetails.VideoStreams.Add(videoDetails);
                streamValueFound = true;
              }
              break;
            case "audio":
              var audioDetails = new AudioStreamDetailsStub();
              var audioDetailValueFound = ((audioDetails.Codec = ParseSimpleString(streamDetail.Element("codec"))) != null);
              audioDetailValueFound = ((audioDetails.Language = ParseSimpleString(streamDetail.Element("language"))) != null) || audioDetailValueFound;
              audioDetailValueFound = ((audioDetails.Channels = ParseSimpleInt(streamDetail.Element("channels"))) != null) || audioDetailValueFound;
              if (audioDetailValueFound)
              {
                if (streamDetails.AudioStreams == null)
                  streamDetails.AudioStreams = new HashSet<AudioStreamDetailsStub>();
                streamDetails.AudioStreams.Add(audioDetails);
                streamValueFound = true;
              }
              break;
            case "subtitle":
              var subtitleDetails = new SubtitleStreamDetailsStub();
              var subtitleDetailValueFound = ((subtitleDetails.Language = ParseSimpleString(streamDetail.Element("language"))) != null);
              if (subtitleDetailValueFound)
              {
                if (streamDetails.SubtitleStreams == null)
                  streamDetails.SubtitleStreams = new HashSet<SubtitleStreamDetailsStub>();
                streamDetails.SubtitleStreams.Add(subtitleDetails);
                streamValueFound = true;
              }
              break;
            default:
              _debugLogger.Warn("[#{0}]: Unknown child element: {1}", _miNumber, streamDetail);
              break;
          }
        }
        if (streamValueFound)
        {
          if (_currentStub.FileInfo == null)
            _currentStub.FileInfo = new HashSet<StreamDetailsStub>();
          _currentStub.FileInfo.Add(streamDetails);
          fileInfoFound = true;
        }
      }
      return fileInfoFound;
    }

    /// <summary>
    /// Tries to read the epbookmark value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadEpBookmark(XElement element)
    {
      // Example of a valid element:
      // <epbookmark>1200.000000</epbookmark>
      // The value is interpreted as number of seconds; a value of 0 is ignored
      var decimalSeconds = ParseSimpleDecimal(element);
      if (decimalSeconds.HasValue && decimalSeconds != decimal.Zero)
      {
        _currentStub.EpBookmark = TimeSpan.FromSeconds((double)decimalSeconds);
        return true;
      }
      return false;
    }

    #endregion

    #region User information

    /// <summary>
    /// Tries to read the watched value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadWatched(XElement element)
    {
      // Example of a valid element:
      // <watched>true</watched>
      var watchedString = ParseSimpleString(element);
      bool watched;
      if (bool.TryParse(watchedString, out watched))
      {
        _currentStub.Watched = watched;
        return true;
      }
      _debugLogger.Warn("[#{0}]: The following elelement was supposed to contain a bool value but it does not: {1}", _miNumber, element);
      return false;
    }

    /// <summary>
    /// Tries to read the playcount value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadPlayCount(XElement element)
    {
      // Example of a valid element:
      // <playcount>3</playcount>
      return ((_currentStub.PlayCount = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the lastplayed value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadLastPlayed(XElement element)
    {
      // Example of a valid element:
      // <lastplayed>2013-10-08 21:46</lastplayed>
      return ((_currentStub.LastPlayed = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the dateadded value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDateAdded(XElement element)
    {
      // Example of a valid element:
      // <dateadded>2013-10-08 21:46</dateadded>
      return ((_currentStub.DateAdded = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the resume value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadResume(XElement element)
    {
      // Example of a valid element:
      // <resume>
      //   <position>3600.000000</position>
      //   <total>5400.000000</total>
      // </resume>
      // The <total> child element (as well as any other child element of the <resume> element) is ignored.
      // THe <position> value is taken and interpreted as number of seconds; a value of less than 1 second is ignored.
      if (element == null || !element.HasElements)
        return false;
      var resumeDecimal = ParseSimpleDecimal(element.Element("position"));
      if (resumeDecimal == null || resumeDecimal < decimal.One)
        return false;
      _currentStub.ResumePosition = TimeSpan.FromSeconds((double)resumeDecimal);
      return true;
    }

    #endregion

    #endregion

    #region Writer methods to store metadata in MediaItemAspects

    // The following writer methods only write the first item found in the nfo-file
    // into the MediaItemAspects. This can be extended in the future once we support
    // multiple MediaItems in one media file.

    #region MediaAspect

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_TITLE"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectTitle(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Title != null)
      {
        string title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_stubs[0].Title);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
        if (_stubs[0].SortTitle != null)
        {
          title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_stubs[0].SortTitle);
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, title);
        }
        else
        {
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(title));
        }
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_RECORDINGTIME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectRecordingTime(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Premiered.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, _stubs[0].Premiered.Value);
        return true;
      }
      //priority 2:
      if (_stubs[0].Year.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, _stubs[0].Year.Value);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_PLAYCOUNT"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectPlayCount(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].PlayCount.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_PLAYCOUNT, _stubs[0].PlayCount.Value);
        return true;
      }
      //priority 2:
      if (_stubs[0].Watched.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_PLAYCOUNT, _stubs[0].Watched.Value ? 1 : 0);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_LASTPLAYED"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectLastPlayed(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].LastPlayed.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_LASTPLAYED, _stubs[0].LastPlayed.Value);
        return true;
      }
      return false;
    }

    #endregion

    #region VideoAspect

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_GENRES"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectGenres(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Genres != null && _stubs[0].Genres.Any())
      {
        List<GenreInfo> genres = _stubs[0].Genres.Select(s => new GenreInfo { Name = s }).ToList();
        OnlineMatcherService.Instance.AssignMissingMovieGenreIds(genres);
        foreach (GenreInfo genre in genres)
        {
          MultipleMediaItemAspect genreAspect = MediaItemAspect.CreateAspect(extractedAspectData, GenreAspect.Metadata);
          genreAspect.SetAttribute(GenreAspect.ATTR_ID, genre.Id);
          genreAspect.SetAttribute(GenreAspect.ATTR_GENRE, genre.Name);
        }
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_ACTORS"/> and <see cref="VideoAspect.ATTR_CHARACTERS"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectActors(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Actors != null && _stubs[0].Actors.Any())
      {
        foreach(PersonStub person in _stubs[0].Actors)
        {
          if (!string.IsNullOrEmpty(person.Name))
          {
            INfoRelationshipExtractor.StorePersonAndCharacter(extractedAspectData, person, PersonAspect.OCCUPATION_ACTOR, false);
          }
        }

        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_ACTORS, _stubs[0].Actors.OrderBy(actor => actor.Order).
          Where(actor => !string.IsNullOrEmpty(actor.Name)).Select(actor => actor.Name).ToList());
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_CHARACTERS, _stubs[0].Actors.OrderBy(actor => actor.Order).
          Where(actor => !string.IsNullOrEmpty(actor.Name) && !string.IsNullOrEmpty(actor.Role)).Select(actor => actor.Role).ToList());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_DIRECTORS"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectDirectors(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Director != null)
      {
        if (!string.IsNullOrEmpty(_stubs[0].Director))
        {
          PersonStub ps = new PersonStub();
          ps.ImdbId = _stubs[0].DirectorImdb;
          ps.Name = _stubs[0].Director;
          INfoRelationshipExtractor.StorePersonAndCharacter(extractedAspectData, ps, PersonAspect.OCCUPATION_DIRECTOR, false);
         }

        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_DIRECTORS, new List<string> { _stubs[0].Director });
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_WRITERS"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectWriters(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Credits != null && _stubs[0].Credits.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_WRITERS, _stubs[0].Credits.ToList());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="VideoAspect.ATTR_STORYPLOT"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteVideoAspectStoryPlot(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Plot != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, _stubs[0].Plot);
        return true;
      }
      // priority 2:
      if (_stubs[0].Outline != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, _stubs[0].Outline);
        return true;
      }
      return false;
    }

    #endregion

    #region MovieAspect

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_COMPANIES"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectCompanies(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Companies != null && _stubs[0].Companies.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, MovieAspect.ATTR_COMPANIES, _stubs[0].Companies.ToList());
        return true;
      }
      // priority 2:
      if (_stubs[0].Studios != null && _stubs[0].Studios.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, MovieAspect.ATTR_COMPANIES, _stubs[0].Studios.ToList());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_MOVIE_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectMovieName(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Title != null)
      {
        string title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_stubs[0].Title);
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_MOVIE_NAME, title);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_ORIG_MOVIE_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectOrigName(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].OriginalTitle != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_ORIG_MOVIE_NAME, _stubs[0].OriginalTitle);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="ExternalIdentifierAspect"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectTmdbId(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].TmdbId.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].TmdbId.Value.ToString());
        return true;
      }
      //priority 2:
      if (_stubs[0].Thmdb.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].Thmdb.Value.ToString());
        return true;
      }
      //priority 3:
      if (_stubs[0].IdsTmdbId.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].IdsTmdbId.Value.ToString());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="ExternalIdentifierAspect"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectImdbId(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].Id != null)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].Id);
        return true;
      }
      //priority 2:
      if (_stubs[0].Imdb != null)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].Imdb);
        return true;
      }
      //priority 3:
      if (_stubs[0].IdsImdbId != null)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].IdsImdbId);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="ExternalIdentifierAspect"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectAllocineId(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Allocine.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_ALLOCINE, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].Allocine.ToString());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="ExternalIdentifierAspect"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectCinePassionId(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Cinepassion.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_CINEPASSION, ExternalIdentifierAspect.TYPE_MOVIE, _stubs[0].Cinepassion.ToString());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_COLLECTION_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectCollectionName(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Sets != null && _stubs[0].Sets.Any())
      {
        string setName = _stubs[0].Sets.OrderBy(set => set.Order).First().Name;
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_COLLECTION_NAME, setName);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_RUNTIME_M"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectRuntime(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Runtime.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_RUNTIME_M, (int)_stubs[0].Runtime.Value.TotalMinutes);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_CERTIFICATION"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectCertification(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Certification != null && _stubs[0].Certification.Any())
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_CERTIFICATION, _stubs[0].Certification.First());
        return true;
      }
      // priority 2:
      if (_stubs[0].Mpaa != null && _stubs[0].Mpaa.Any())
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_CERTIFICATION, _stubs[0].Mpaa.First());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_TAGLINE"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectTagline(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Tagline != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TAGLINE, _stubs[0].Tagline);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_TOTAL_RATING"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectTotalRating(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Rating.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TOTAL_RATING, (double)_stubs[0].Rating.Value);
        return true;
      }
      // priority 2:
      if (_stubs[0].Ratings != null && _stubs[0].Ratings.Any())
      {
        decimal rating;
        if (!_stubs[0].Ratings.TryGetValue("imdb", out rating))
          rating = _stubs[0].Ratings.First().Value;
        MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_TOTAL_RATING, (double)rating);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MovieAspect.ATTR_RATING_COUNT"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMovieAspectRatingCount(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Votes.HasValue)
      {
        if (_stubs[0].Rating.HasValue || (_stubs[0].Ratings != null && _stubs[0].Ratings.Count == 1))
        {
          MediaItemAspect.SetAttribute(extractedAspectData, MovieAspect.ATTR_RATING_COUNT, _stubs[0].Votes.Value);
          return true;
        }
      }
      return false;
    }

    #endregion

    #region ThumbnailLargeAspect

    /// <summary>
    /// Tries to write metadata into <see cref="ThumbnailLargeAspect.ATTR_THUMBNAIL"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteThumbnailLargeAspectThumbnail(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Thumb != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, _stubs[0].Thumb);
        return true;
      }
      return false;
    }

    #endregion

    #endregion

    #region General helper methods

    /// <summary>
    /// Ignores the respective element
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to ignore</param>
    /// <returns><c>false</c></returns>
    /// <remarks>
    /// We use this method as TryReadElementDelegate for elements, of which we know that they are irrelevant in the context of a movie,
    /// but which are nevertheless contained in some movie's nfo-files. Having this method registered as handler delegate avoids that
    /// the respective xml element is logged as unknown element.
    /// </remarks>
    private static bool Ignore(XElement element)
    {
      return false;
    }

    #endregion

    #endregion

    #region BaseOverrides

    /// <summary>
    /// Checks whether the <paramref name="itemRootElement"/>'s name is "movie"
    /// </summary>
    /// <param name="itemRootElement">Element to check</param>
    /// <returns><c>true</c> if the element's name is "movie"; else <c>false</c></returns>
    protected override bool CanReadItemRootElementTree(XElement itemRootElement)
    {
      var itemRootElementName = itemRootElement.Name.ToString();
      if (itemRootElementName == MOVIE_ROOT_ELEMENT_NAME)
        return true;
      _debugLogger.Warn("[#{0}]: Cannot extract metadata; name of the item root element is {1} instead of {2}", _miNumber, itemRootElementName, MOVIE_ROOT_ELEMENT_NAME);
      return false;
    }

    #endregion
  }
}
