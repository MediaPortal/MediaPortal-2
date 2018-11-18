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

using System;
using System.Collections.Generic;
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
using MediaPortal.Utilities;
using System.Globalization;
using MediaPortal.Utilities.Cache;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Common;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders
{
  /// <summary>
  /// Reads the content of a nfo-file for an episode of a series into <see cref="SeriesEpisodeStub"/> objects and stores
  /// the appropriate values from the <see cref="SeriesEpisodeStub"/> objects and (if set before via <see cref="SetSeriesStubs"/>)
  /// from the <see cref="SeriesStub"/> objects into the respective <see cref="MediaItemAspect"/>s
  /// </summary>
  /// <remarks>
  /// There is a TryRead method for any known child element of the nfo-file's root element and a
  /// TryWrite method for any MIA-Attribute we store values in.
  /// This class can parse much more information than we can currently store in our MediaLibrary.
  /// For performance reasons, the following long lasting operations have been temporarily disabled:
  /// - We do parse "set" (and therefore also "sets" elements); however, parsing and downloading
  ///   "setimage" child elements has been disabled. Reenable in <see cref="TryReadSetAsync"/>
  /// - We do parse "actor" elements, however, parsing and downloading "thumb"
  ///   child elements has been disabled. Reenable in <see cref="NfoReaderBase{T}.ParsePerson"/>
  /// ToDo: Reenable the above once we can store the information in our MediaLibrary
  /// </remarks>
  public class NfoSeriesEpisodeReader : NfoReaderBase<SeriesEpisodeStub>
  {
    #region Consts

    /// <summary>
    /// The name of the root element in a valid nfo-file for episodes
    /// </summary>
    private const string EPISODE_ROOT_ELEMENT_NAME = "episodedetails";

    #endregion

    #region Private fields

    /// <summary>
    /// List of <see cref="SeriesStub"/> objects to be set via <see cref="SetSeriesStubs"/>
    /// </summary>
    private List<SeriesStub> _seriesStubs;

    /// <summary>
    /// <c>false</c> if <see cref="_seriesStubs"/> is <c>null</c> or empty; otherwise <c>true</c>
    /// </summary>
    private bool _useSeriesStubs;

    /// <summary>
    /// If true, file details will also be read from the nfo-file
    /// </summary>
    private bool _readFileDetails;

    /// <summary>
    /// Default timeout for the cache is 5 minutes
    /// </summary>
    private static readonly TimeSpan CACHE_TIMEOUT = new TimeSpan(0, 5, 0);

    /// <summary>
    /// Cache used to temporarily store <see cref="SeriesEpisodeStub"/> objects so that the same episode.nfo file
    /// doesn't have to be parsed once for every episode
    /// </summary>
    private static readonly AsyncStaticTimeoutCache<ResourcePath, List<SeriesEpisodeStub>> CACHE = new AsyncStaticTimeoutCache<ResourcePath, List<SeriesEpisodeStub>>(CACHE_TIMEOUT);

    #endregion

    #region Ctor

    /// <summary>
    /// Instantiates a <see cref="NfoSeriesEpisodeReader"/> object
    /// </summary>
    /// <param name="debugLogger">Debug logger to log to</param>
    /// <param name="miNumber">Unique number of the MediaItem for which the nfo-file is parsed</param>
    /// <param name="forceQuickMode">If true, no long lasting operations such as parsing images are performed</param>
    /// <param name="readFileDetails">If true, file details will also be read from the nfo-file</param>
    /// <param name="httpClient"><see cref="HttpClient"/> used to download from http URLs contained in nfo-files</param>
    /// <param name="settings">Settings of the <see cref="NfoSeriesMetadataExtractor"/></param>
    /// 
    public NfoSeriesEpisodeReader(ILogger debugLogger, long miNumber, bool forceQuickMode, bool readFileDetails, HttpClient httpClient, NfoSeriesMetadataExtractorSettings settings)
      : base(debugLogger, miNumber, forceQuickMode, httpClient, settings)
    {
      _readFileDetails = readFileDetails;
      InitializeSupportedElements();
      InitializeSupportedAttributes();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the <see cref="SeriesEpisodeStub"/> objects generated by this class
    /// </summary>
    /// <returns>List of <see cref="SeriesEpisodeStub"/> objects</returns>
    public List<SeriesEpisodeStub> GetEpisodeStubs()
    {
      return _stubs;
    }

    #endregion

    #region Private methods

    #region Ctor helpers

    /// <summary>
    /// Adds a delegate for each xml element in a movie nfo-file that is understood by this MetadataExtractor to NfoReaderBase._supportedElements
    /// </summary>
    private void InitializeSupportedElements()
    {
      _supportedElements.Add("uniqueid", new TryReadElementDelegate(TryReadUniqueId));
      _supportedElements.Add("code", new TryReadElementDelegate(TryReadCode));
      _supportedElements.Add("id", new TryReadElementDelegate(TryReadId));

      _supportedElements.Add("title", new TryReadElementDelegate(TryReadTitle));
      _supportedElements.Add("showtitle", new TryReadElementDelegate(TryReadShowTitle));
      _supportedElements.Add("season", new TryReadElementDelegate(TryReadSeason));
      _supportedElements.Add("episode", new TryReadElementDelegate(TryReadEpisode));
      _supportedElements.Add("dvd_episode", new TryReadElementDelegate(TryReadDvdEpisode));
      _supportedElements.Add("displayseason", new TryReadElementDelegate(TryReadDisplaySeason));
      _supportedElements.Add("displayepisode", new TryReadElementDelegate(TryReadDisplayEpisode));
      _supportedElements.Add("set", new TryReadElementAsyncDelegate(TryReadSetAsync));
      _supportedElements.Add("sets", new TryReadElementAsyncDelegate(TryReadSetsAsync));

      _supportedElements.Add("premiered", new TryReadElementDelegate(TryReadPremiered));
      _supportedElements.Add("aired", new TryReadElementDelegate(TryReadAired));
      _supportedElements.Add("year", new TryReadElementDelegate(TryReadYear));
      _supportedElements.Add("studio", new TryReadElementDelegate(TryReadStudio));
      _supportedElements.Add("actor", new TryReadElementAsyncDelegate(TryReadActorAsync));
      _supportedElements.Add("director", new TryReadElementDelegate(TryReadDirector));
      _supportedElements.Add("credits", new TryReadElementDelegate(TryReadCredits));
      _supportedElements.Add("runtime", new TryReadElementDelegate(TryReadRuntime));
      _supportedElements.Add("status", new TryReadElementDelegate(TryReadStatus));

      _supportedElements.Add("plot", new TryReadElementDelegate(TryReadPlot));
      _supportedElements.Add("outline", new TryReadElementDelegate(TryReadOutline));
      _supportedElements.Add("tagline", new TryReadElementDelegate(TryReadTagline));
      _supportedElements.Add("trailer", new TryReadElementDelegate(TryReadTrailer));

      _supportedElements.Add("thumb", new TryReadElementAsyncDelegate(TryReadThumbAsync));

      _supportedElements.Add("mpaa", new TryReadElementDelegate(TryReadMpaa));
      _supportedElements.Add("rating", new TryReadElementDelegate(TryReadRating));
      _supportedElements.Add("votes", new TryReadElementDelegate(TryReadVotes));
      _supportedElements.Add("top250", new TryReadElementDelegate(TryReadTop250));

      _supportedElements.Add("fileinfo", new TryReadElementDelegate(TryReadFileInfo));
      _supportedElements.Add("epbookmark", new TryReadElementDelegate(TryReadEpBookmark));

      _supportedElements.Add("watched", new TryReadElementDelegate(TryReadWatched));
      _supportedElements.Add("playcount", new TryReadElementDelegate(TryReadPlayCount));
      _supportedElements.Add("lastplayed", new TryReadElementDelegate(TryReadLastPlayed));
      _supportedElements.Add("resume", new TryReadElementDelegate(TryReadResume));

      // The following element is contained in many episode nfo-files, but we don't need its information
      // We add it here to avoid it being logged as unknown element, but we simply ignore it.
      // For reference see here: http://forum.team-mediaportal.com/threads/mp2-459-implementation-of-a-movienfometadataextractor-and-a-seriesnfometadataextractor.128805/page-13#post-1130414
      _supportedElements.Add("dateadded", new TryReadElementDelegate(Ignore));
      
      // Used by MKVBuddy to store the language of the first audio stream found by mediainfo.lib; we read that
      // anyway in the VideoMetadataExtractor before.
      _supportedElements.Add("language", new TryReadElementDelegate(Ignore));
    }

    /// <summary>
    /// Adds a delegate for each Attribute in a MediaItemAspect into which this MetadataExtractor can write metadata to NfoReaderBase._supportedAttributes
    /// </summary>
    private void InitializeSupportedAttributes()
    {
      _supportedAttributes.Add(TryWriteMediaAspectTitle);
      _supportedAttributes.Add(TryWriteMediaAspectPlayCount);
      _supportedAttributes.Add(TryWriteMediaAspectLastPlayed);

      _supportedAttributes.Add(TryWriteVideoAspectGenres);
      _supportedAttributes.Add(TryWriteVideoAspectActors);
      _supportedAttributes.Add(TryWriteVideoAspectDirectors);
      _supportedAttributes.Add(TryWriteVideoAspectWriters);
      _supportedAttributes.Add(TryWriteVideoAspectStoryPlot);

      _supportedAttributes.Add(TryWriteEpisodeAspectTvDbId);
      _supportedAttributes.Add(TryWriteEpisodeAspectSeriesName);
      _supportedAttributes.Add(TryWriteEpisodeAspectSeriesYear);
      _supportedAttributes.Add(TryWriteEpisodeAspectSeason);
      _supportedAttributes.Add(TryWriteEpisodeAspectSeriesSeason);
      _supportedAttributes.Add(TryWriteEpisodeAspectEpisode);
      _supportedAttributes.Add(TryWriteEpisodeAspectDvdEpisode);
      _supportedAttributes.Add(TryWriteEpisodeAspectEpisodeName);
      _supportedAttributes.Add(TryWriteEpisodeAspectFirstAired);
      _supportedAttributes.Add(TryWriteEpisodeAspectTotalRating);
      _supportedAttributes.Add(TryWriteEpisodeAspectRatingCount);

      _supportedAttributes.Add(TryWriteThumbnailLargeAspectThumbnail);
    }

    #endregion

    #region Reader methods for direct child elements of the root element

    #region Internet databases

    /// <summary>
    /// Tries to read the episode's ID at thetvdb.com
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadUniqueId(XElement element)
    {
      // Example of a valid element:
      // <uniqueid>2111911</uniqueid>
      return ((_currentStub.UniqueId = ParseSimpleInt(element)) != null);
    }

    /// <summary>
    /// Tries to read the Production Code Number of the episode
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadCode(XElement element)
    {
      // Example of a valid element:
      // <code>A12301</code>
      return ((_currentStub.ProductionCodeNumber = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the series' ID at thetvdb.com
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadId(XElement element)
    {
      // Example of a valid element:
      // <id>158661</id>
      return ((_currentStub.Id = ParseSimpleInt(element)) != null);
    }

    #endregion

    #region Title information

    /// <summary>
    /// Tries to read the title of the episode
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTitle(XElement element)
    {
      // Example of a valid element:
      // <title>Blumen für Dein Grab</title>
      return ((_currentStub.Title = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the title of the series
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadShowTitle(XElement element)
    {
      // Example of a valid element:
      // <showtitle>Castle</showtitle>
      return ((_currentStub.ShowTitle = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the season value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadSeason(XElement element)
    {
      // Examples of valid elements:
      // <season>2</season>
      // <season>0</season>
      // A value of 0 (zero) is valid for specials
      // A value of < 0 is ignored
      var value = ParseSimpleInt(element);
      if (value < 0)
        value = null;
      return ((_currentStub.Season = value) != null);
    }

    /// <summary>
    /// Tries to read the episode value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadEpisode(XElement element)
    {
      // Examples of valid elements:
      // <episode>12</episode>
      // <episode>0</episode>
      // A value of 0 (zero) is valid for specials within a season
      // A value of < 0 is ignored
      var value = ParseSimpleInt(element);
      if (value < 0 || !value.HasValue)
        return false;

      if (_currentStub.Episodes == null)
        _currentStub.Episodes = new HashSet<int>();
      _currentStub.Episodes.Add(value.Value);
      return _currentStub.Episodes.Count > 0;
    }

    /// <summary>
    /// Tries to read the dvd episode value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDvdEpisode(XElement element)
    {
      // Examples of valid elements:
      // <episode>3.0</episode>
      // <episode>4.0</episode>
      // A value of 0 (zero) is valid for specials within a season
      // A value of < 0 is ignored
      var value = ParseSimpleDecimal(element);
      if (value < 0 || !value.HasValue)
        return false;

      if (_currentStub.DvdEpisodes == null)
        _currentStub.DvdEpisodes = new HashSet<decimal>();
      _currentStub.DvdEpisodes.Add(value.Value);
      return _currentStub.DvdEpisodes.Count > 0;
    }

    /// <summary>
    /// Tries to read the displayseason value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDisplaySeason(XElement element)
    {
      // Examples of valid elements:
      // <displayseason>2</displayseason>
      // <displayseason>0</displayseason>
      // A value of 0 (zero) is valid for specials
      // A value of < 0 is ignored
      var value = ParseSimpleInt(element);
      if (value < 0)
        value = null;
      return ((_currentStub.DisplaySeason = value) != null);
    }

    /// <summary>
    /// Tries to read the displayepisode value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDisplayEpisode(XElement element)
    {
      // Examples of valid elements:
      // <displayepisode>12</displayepisode>
      // <displayepisode>0</displayepisode>
      // A value of 0 (zero) is valid for specials within a season
      // A value of < 0 is ignored
      var value = ParseSimpleInt(element);
      if (value < 0)
        value = null;
      return ((_currentStub.DisplayEpisode = value) != null);
    }

    /// <summary>
    /// Tries to (asynchronously) read the set information
    /// We have not found an example for this element, yet, and assume it has the same structure as for movies.
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <param name="nfoDirectoryFsra"><see cref="IFileSystemResourceAccessor"/> to the parent directory of the nfo-file</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private async Task<bool> TryReadSetAsync(XElement element, IFileSystemResourceAccessor nfoDirectoryFsra)
    {
      // Examples of valid elements:
      // 1:
      // <set order = "1">Star Trek</set>
      // 2:
      // <set order = "1">
      //   <setname>Star Trek</setname>
      //   <setdescription>This is ...</setdescription>
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
    /// Tries to read the aired value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadAired(XElement element)
    {
      // Examples of valid elements:
      // <aired>1994-09-14</aired>
      // <aired>1994</aired>
      return ((_currentStub.Aired = ParseSimpleDateTime(element)) != null);
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
    /// Tries to read a studio value
    /// </summary>
    /// <param name="element">Element to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadStudio(XElement element)
    {
      // Example of a valid element:
      // <studio>SyFy</studio>
      return ((_currentStub.Studio = ParseSimpleString(element)) != null);
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
    /// Tries to read a status value
    /// </summary>
    /// <param name="element">Element to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadStatus(XElement element)
    {
      // Example of a valid element:
      // <status>Continuing</status>
      return ((_currentStub.Status = ParseSimpleString(element)) != null);
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
      // <plot>This episode tells a story about...</plot>
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
      // <outline>This episode tells a story about...</outline>
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
      // <tagline>This episode tells a story about...</tagline>
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
      // <trailer>[URL to a trailer]</trailer>
      return ((_currentStub.Trailer = ParseSimpleString(element)) != null);
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

    #endregion

    #region Certification and ratings

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
    /// Tries to read the file info value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadFileInfo(XElement element)
    {
      if (!_readFileDetails)
        return false;

      var fileInfo = ParseFileInfo(element);
      if (fileInfo != null && fileInfo.Count > 0)
      {
        _currentStub.FileInfo = fileInfo;
        return true;
      }
      return false;
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
    // into the MediaItemAspects. The only exception is the Episode Attribute, where
    // we store the episode numbers of all items in the episode nfo-file.
    // This can be extended in the future once we really support multiple MediaItems in one media file.

    #region MediaAspect

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_TITLE"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteMediaAspectTitle(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      string seriesName = _stubs[0].ShowTitle;
      if (seriesName == null && _useSeriesStubs && _seriesStubs[0].ShowTitle != null)
        seriesName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_seriesStubs[0].ShowTitle);

      var season = _stubs[0].Season;
      if (!season.HasValue)
        season = _stubs[0].DisplaySeason;

      if (seriesName != null && season.HasValue && _stubs.Count > 0)
      {
        string name = String.Format(EpisodeInfo.EPISODE_FORMAT_STR,
          seriesName,
          season.Value.ToString().PadLeft(2, '0'),
          StringUtils.Join(", ", _stubs.OrderBy(e => e.Episodes.First()).Select(e => e.Episodes.First().ToString().PadLeft(2, '0'))),
          string.Join("; ", _stubs.OrderBy(e => e.Episodes.First()).Select(e => e.Title).ToArray()));
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, name);

        string episodeName = string.Join("; ", _stubs.OrderBy(e => e.Episodes.First()).Select(e => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(e.Title)));
        if (episodeName != null)
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(episodeName));
        else
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(name));
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
      if (_useSeriesStubs && _seriesStubs[0].Genres != null && _seriesStubs[0].Genres.Any())
      {
        List<GenreInfo> genres = _seriesStubs[0].Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();
        IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
        foreach (var genre in genres)
        {
          if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Series, null, out int genreId))
            genre.Id = genreId;
        }
        foreach(GenreInfo genre in genres)
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
      List<string> actors = null;
      List<string> characters = null;
      if (_stubs[0].Actors != null)
      {
        actors = _stubs.SelectMany(e => e.Actors).OrderBy(actor => actor.Order).Where(actor => !string.IsNullOrEmpty(actor.Name)).
          Select(actor => actor.Name).Distinct().ToList();
        characters = _stubs.SelectMany(e => e.Actors).OrderBy(actor => actor.Order).Where(actor => !string.IsNullOrEmpty(actor.Role)).
          Select(actor => actor.Role).Distinct().ToList();
      }
      if (_useSeriesStubs && _seriesStubs[0].Actors != null)
      {
        actors = actors != null ?
          actors.Union(_seriesStubs[0].Actors.OrderBy(actor => actor.Order).Where(actor => !string.IsNullOrEmpty(actor.Name)).
          Select(actor => actor.Name)).ToList() :
          _seriesStubs[0].Actors.OrderBy(actor => actor.Order).Where(actor => !string.IsNullOrEmpty(actor.Name)).
          Select(actor => actor.Name).ToList();
        characters = characters != null ?
          characters.Union(_seriesStubs[0].Actors.OrderBy(actor => actor.Order).Where(actor => !string.IsNullOrEmpty(actor.Role)).
          Select(actor => actor.Role)).ToList() :
          _seriesStubs[0].Actors.OrderBy(actor => actor.Order).Where(actor => !string.IsNullOrEmpty(actor.Role)).
          Select(actor => actor.Role).ToList();
      }
      if (actors != null && actors.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_ACTORS, actors);
        if (characters != null && characters.Any())
        {
          MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_CHARACTERS, characters);
        }
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
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_DIRECTORS, _stubs.Select(e => e.Director).Distinct());
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
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_WRITERS, _stubs.SelectMany(e => e.Credits).Distinct());
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
        if (_stubs.Count == 1)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, _stubs[0].Plot);
        else
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, string.Join("\r\n\r\n", _stubs.OrderBy(e => e.Episodes.First()).Select(e => string.Format("{0,02}) {1}", e.Episodes.First(), e.Plot)).ToArray()));
        return true;
      }
      // priority 2:
      if (_stubs[0].Outline != null)
      {
        if (_stubs.Count == 1)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, _stubs[0].Outline);
        else
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, string.Join("\r\n\r\n", _stubs.OrderBy(e => e.Episodes.First()).Select(e => string.Format("{0,02}) {1}", e.Episodes.First(), e.Outline)).ToArray()));
        return true;
      }
      // priority 3:
      if (_useSeriesStubs && _seriesStubs[0].Plot != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, _seriesStubs[0].Plot);
        return true;
      }
      // priority 4:
      if (_useSeriesStubs && _seriesStubs[0].Outline != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, _seriesStubs[0].Outline);
        return true;
      }
      return false;
    }

    #endregion

    #region EpisodeAspect

    /// <summary>
    /// Tries to write metadata into external id.
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectTvDbId(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].Id.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, _stubs[0].Id.Value.ToString());
        return true;
      }
      //priority 2:
      if (_useSeriesStubs && _seriesStubs[0].Id.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, _seriesStubs[0].Id.Value.ToString());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_SERIES_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectSeriesName(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].ShowTitle != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_SERIES_NAME,
          CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_stubs[0].ShowTitle));
        return true;
      }
      //priority 2:
      if (_useSeriesStubs && _seriesStubs[0].ShowTitle != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_SERIES_NAME,
          CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_seriesStubs[0].ShowTitle));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_RECORDINGTIME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectSeriesYear(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].Premiered != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, _stubs[0].Premiered);
        return true;
      }
      //priority 2:
      if (_useSeriesStubs && _seriesStubs[0].Year != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, _seriesStubs[0].Year);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_SEASON"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectSeason(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].Season.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_SEASON, _stubs[0].Season.Value);
        return true;
      }
      //priority 2:
      if (_stubs[0].DisplaySeason.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_SEASON, _stubs[0].DisplaySeason.Value);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_SERIES_SEASON"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectSeriesSeason(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      var series = _stubs[0].ShowTitle;
      if (_useSeriesStubs && series == null && _seriesStubs[0].ShowTitle != null)
        series = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_seriesStubs[0].ShowTitle);

      var season = _stubs[0].Season;
      if (!season.HasValue)
        season = _stubs[0].DisplaySeason;

      if (series != null && season.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_SERIES_SEASON, String.Format(EpisodeInfo.SERIES_SEASON_FORMAT_STR, series, season.ToString().PadLeft(2, '0')));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_EPISODE"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectEpisode(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      var episodes = _stubs.Select(episodeStub => episodeStub.Episodes).Where(episode => episode.Count > 0).ToList();
      if (episodes.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, EpisodeAspect.ATTR_EPISODE, episodes.SelectMany(e => e).Distinct());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_DVDEPISODE"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectDvdEpisode(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      var episodes = _stubs.Select(episodeStub => episodeStub.DvdEpisodes).Where(episode => episode?.Count > 0).ToList();
      if (episodes.Any())
      {
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, EpisodeAspect.ATTR_DVDEPISODE, episodes.SelectMany(e => e).Select(episode => Convert.ToDouble(episode)).Distinct());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_EPISODE_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectEpisodeName(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Title != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_EPISODE_NAME, string.Join("; ", _stubs.OrderBy(e => e.Episodes.First()).
          Select(e => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(e.Title)).ToArray()));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="MediaAspect.ATTR_RECORDINGTIME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectFirstAired(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Aired.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, _stubs[0].Aired.Value);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_TOTAL_RATING"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectTotalRating(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Rating.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_TOTAL_RATING, (double)_stubs.Where(e => e.Rating.HasValue).Average(e => e.Rating.Value));
        return true;
      }
      // priority 2:
      if (_useSeriesStubs && _seriesStubs[0].Rating.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_TOTAL_RATING, (double)_seriesStubs[0].Rating.Value);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="EpisodeAspect.ATTR_RATING_COUNT"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteEpisodeAspectRatingCount(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      //priority 1:
      if (_stubs[0].Votes.HasValue && _stubs[0].Rating.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_RATING_COUNT, _stubs[0].Votes.Value);
        return true;
      }
      //priority 2:
      if (_useSeriesStubs && _seriesStubs[0].Votes.HasValue && _seriesStubs[0].Rating.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, EpisodeAspect.ATTR_RATING_COUNT, _seriesStubs[0].Votes.Value);
        return true;
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

    #region Relationships

    private bool TryWriteActorAspect(PersonStub person, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      return TryWritePersonAspect(person, PersonAspect.OCCUPATION_ACTOR, extractedAspectData);
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
    /// We use this method as TryReadElementDelegate for elements, of which we know that they are irrelevant in the context of an episode,
    /// but which are nevertheless contained in some episode's nfo-files. Having this method registered as handler delegate avoids that
    /// the respective xml element is logged as unknown element.
    /// </remarks>
    private static bool Ignore(XElement element)
    {
      return false;
    }

    #endregion

    #endregion

    #region Public methods

    /// <summary>
    /// Sets the <see cref="SeriesStub"/> objects to be used by the TryWrite methods
    /// </summary>
    /// <param name="seriesStubs">List of <see cref="SeriesStub"/> objects to be used</param>
    public void SetSeriesStubs(List<SeriesStub> seriesStubs)
    {
      _seriesStubs = seriesStubs;
      if (_seriesStubs == null || _seriesStubs.Count == 0)
      {
        _debugLogger.Warn("[#{0}]: SeriesStub is null or empty; only information from the episode nfo-file is used.", _miNumber);
        _useSeriesStubs = false;
        return;
      }
      _useSeriesStubs = true;
      if (_seriesStubs.Count > 1)
        _debugLogger.Warn("[#{0}]: There were multiple series contained in the series nfo-file; only information from the first series is used.", _miNumber);
    }

    /// <summary>
    /// Sets the <see cref="SeriesEpisodeStub"/> objects to be used by the TryWrite methods
    /// </summary>
    /// <param name="spisodeStubs">List of <see cref="SeriesEpisodeStub"/> objects to be used</param>
    public void SetEpisodeStubs(List<SeriesEpisodeStub> episodeStubs)
    {
      _stubs = episodeStubs;
      if (_stubs == null || _stubs.Count == 0)
      {
        _debugLogger.Warn("[#{0}]: Stub is null or empty.", _miNumber);
        _useSeriesStubs = false;
        return;
      }
      if (_stubs.Count > 1)
        _debugLogger.Warn("[#{0}]: There were multiple episodes contained in the episode stub; only information from the first episode is used.", _miNumber);
    }

    public bool TryWriteActorMetadata(IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspects)
    {
      IEnumerable<PersonStub> stubActors = null;
      if (_stubs[0].Actors != null)
        stubActors = _stubs[0].Actors;
      else if (_useSeriesStubs && _seriesStubs[0].Actors != null)
        stubActors = _seriesStubs[0].Actors;
      else
        return false;
      //Distinct actor names, ordered by Order
      IEnumerable<PersonStub> actors = stubActors
          .Where(a => !string.IsNullOrEmpty(a.Name))
          .GroupBy(a => a.Name).Select(g => g.First())
          .OrderBy(a => a.Order);
      return TryWriteRelationshipMetadata(TryWriteActorAspect, actors, extractedAspects);
    }

    public bool TryWriteCharacterMetadata(IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspects)
    {
      IEnumerable<PersonStub> stubActors = null;
      if (_stubs[0].Actors != null)
        stubActors = _stubs[0].Actors;
      else if (_useSeriesStubs && _seriesStubs[0].Actors != null)
        stubActors = _seriesStubs[0].Actors;
      else
        return false;

      //Distinct actor names, ordered by Order
      IEnumerable<PersonStub> actors = stubActors
        .Where(a => !string.IsNullOrEmpty(a.Name))
        .GroupBy(a => a.Name).Select(g => g.First())
        .OrderBy(a => a.Order);
      return TryWriteRelationshipMetadata(TryWriteCharacterAspect, actors, extractedAspects);
    }

    #endregion

    #region BaseOverrides

    /// <summary>
    /// Checks whether the <paramref name="itemRootElement"/>'s name is "episodedetails"
    /// </summary>
    /// <param name="itemRootElement">Element to check</param>
    /// <returns><c>true</c> if the element's name is "episodedetails"; else <c>false</c></returns>
    protected override bool CanReadItemRootElementTree(XElement itemRootElement)
    {
      var itemRootElementName = itemRootElement.Name.ToString();
      if (itemRootElementName == EPISODE_ROOT_ELEMENT_NAME)
        return true;
      _debugLogger.Warn("[#{0}]: Cannot extract metadata; name of the item root element is {1} instead of {2}", _miNumber, itemRootElementName, EPISODE_ROOT_ELEMENT_NAME);
      return false;
    }

    /// <summary>
    /// Tries to read a album nfo-file into <see cref="AlbumStub"/> objects (or gets them from cache)
    /// </summary>
    /// <param name="nfoFsra"><see cref="IFileSystemResourceAccessor"/> pointing to the nfo-file</param>
    /// <returns><c>true</c> if any usable metadata was found; else <c>false</c></returns>
    public override async Task<bool> TryReadMetadataAsync(IFileSystemResourceAccessor nfoFsra)
    {
      var stubs = await CACHE.GetValue(nfoFsra.CanonicalLocalResourcePath, async path =>
      {
        _debugLogger.Info("[#{0}]: SeriesEpisodeStub object for episode nfo-file not found in cache; parsing nfo-file {1}", _miNumber, nfoFsra.CanonicalLocalResourcePath);
        if (await base.TryReadMetadataAsync(nfoFsra).ConfigureAwait(false))
        {
          if (_settings.EnableDebugLogging && _settings.WriteStubObjectIntoDebugLog)
            LogStubObjects();
          return _stubs;
        }
        return null;
      }).ConfigureAwait(false);
      if (stubs == null)
        return false;
      _stubs = stubs;
      return true;
    }

    #endregion
  }
}
