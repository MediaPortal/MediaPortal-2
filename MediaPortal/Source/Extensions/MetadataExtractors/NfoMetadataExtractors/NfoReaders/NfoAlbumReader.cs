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
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using MediaPortal.Utilities.Cache;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders
{
  /// <summary>
  /// Reads the content of a nfo-file for albums into <see cref="AlbumStub"/> objects and stores
  /// the appropriate values into the respective <see cref="MediaItemAspect"/>s
  /// </summary>
  /// <remarks>
  /// There is a TryRead method for any known child element of the nfo-file's root element and a
  /// TryWrite method for any MIA-Attribute we store values in.
  /// </remarks>
  class NfoAlbumReader : NfoReaderBase<AlbumStub>
  {
    #region Consts

    /// <summary>
    /// The name of the root element in a valid nfo-file for albums
    /// </summary>
    private const string ALBUM_ROOT_ELEMENT_NAME = "album";

    /// <summary>
    /// Default timeout for the cache is 5 minutes
    /// </summary>
    private static readonly TimeSpan CACHE_TIMEOUT = new TimeSpan(0, 5, 0);

    /// <summary>
    /// Cache used to temporarily store <see cref="SeriesStub"/> objects so that the same tvshow.nfo file
    /// doesn't have to be parsed once for every episode
    /// </summary>
    private static readonly AsyncStaticTimeoutCache<ResourcePath, List<AlbumStub>> CACHE = new AsyncStaticTimeoutCache<ResourcePath, List<AlbumStub>>(CACHE_TIMEOUT);

    #endregion

    #region Ctor

    /// <summary>
    /// Instantiates a <see cref="NfoAlbumReader"/> object
    /// </summary>
    /// <param name="debugLogger">Debug logger to log to</param>
    /// <param name="miNumber">Unique number of the MediaItem for which the nfo-file is parsed</param>
    /// <param name="importOnly">If true, this is an import only cycle meaning no refresh of existing media</param>
    /// <param name="forceQuickMode">If true, no long lasting operations such as parsing images are performed</param>
    /// <param name="httpClient"><see cref="HttpClient"/> used to download from http URLs contained in nfo-files</param>
    /// <param name="settings">Settings of the <see cref="NfoMovieMetadataExtractor"/></param>
    public NfoAlbumReader(ILogger debugLogger, long miNumber, bool importOnly, bool forceQuickMode, HttpClient httpClient, NfoAudioMetadataExtractorSettings settings)
      : base(debugLogger, miNumber, importOnly, forceQuickMode, httpClient, settings)
    {
      _settings = settings;
      InitializeSupportedElements();
    }

    #endregion

    #region Private methods

    #region Ctor helpers

    /// <summary>
    /// Adds a delegate for each xml element in a movie nfo-file that is understood by this MetadataExtractor to NfoReaderBase._supportedElements
    /// </summary>
    private void InitializeSupportedElements()
    {
      _supportedElements.Add("musicBrainzReleaseGroupID", new TryReadElementDelegate(TryReadMbReleaseGroupId));
      _supportedElements.Add("musicBrainzAlbumID", new TryReadElementDelegate(TryReadMbReleaseId));
      _supportedElements.Add("audioDbID", new TryReadElementDelegate(TryReadAudiodbId));
      _supportedElements.Add("title", new TryReadElementDelegate(TryReadTitle));
      _supportedElements.Add("artist", new TryReadElementDelegate(TryReadArtists));
      _supportedElements.Add("releasedate", new TryReadElementDelegate(TryReadReleaseDate));
      _supportedElements.Add("year", new TryReadElementDelegate(TryReadYear));
      _supportedElements.Add("label", new TryReadElementDelegate(TryReadLabels));
      _supportedElements.Add("genre", new TryReadElementDelegate(TryReadGenre));
      _supportedElements.Add("genres", new TryReadElementDelegate(TryReadGenres));
      _supportedElements.Add("thumb", new TryReadElementAsyncDelegate(TryReadThumbAsync));
      _supportedElements.Add("rating", new TryReadElementDelegate(TryReadRating));
      _supportedElements.Add("review", new TryReadElementDelegate(TryReadReview));
      _supportedElements.Add("track", new TryReadElementDelegate(TryReadTrack));
      _supportedElements.Add("stub", new TryReadElementDelegate(TryReadStub));
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the <see cref="AlbumStub"/> objects generated by this class
    /// </summary>
    /// <returns>List of <see cref="AlbumStub"/> objects</returns>
    public List<AlbumStub> GetAlbumStubs()
    {
      return _stubs;
    }

    #endregion

    #region Reader methods for direct child elements of the root element

    #region Internet databases

    /// <summary>
    /// Tries to read the MusicBrainz release group ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadMbReleaseGroupId(XElement element)
    {
      // Example of a valid element:
      // <musicBrainzReleaseGroupID>be62a5b3-2236-38e7-bcb0-c87f75be2259</musicBrainzReleaseGroupID>
      return ((_currentStub.MusicBrainzReleaseGroupId = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the MusicBrainz release ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadMbReleaseId(XElement element)
    {
      // Example of a valid element:
      // <musicBrainzAlbumID>c2af1418-6f1d-45a8-b8ed-1761d97b1e22</musicBrainzAlbumID>
      return ((_currentStub.MusicBrainzAlbumId = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the AudioDB ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadAudiodbId(XElement element)
    {
      // Example of a valid element:
      // <audiodbid>52719</audiodbid>
      return ((_currentStub.AudioDbId = ParseSimpleLong(element)) != null);
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
      // <title>Album Title</title>
      return ((_currentStub.Title = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the stub value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadStub(XElement element)
    {
      // Example of a valid element:
      // <stub>Album</stub>
      return ((_currentStub.StubLabel = ParseSimpleString(element)) != null);
    }

    #endregion

    #region Making-of information

    /// <summary>
    /// Tries to read the release date value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadReleaseDate(XElement element)
    {
      // Examples of valid elements:
      // <releasedate>1994-09-14</releasedate>
      // <releasedate>1994</releasedate>
      return ((_currentStub.ReleaseDate = ParseSimpleDateTime(element)) != null);
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
    /// Tries to read a record artists
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadArtists(XElement element)
    {
      // Examples of valid elements:
      // <artist>Artists name</artist>
      // <artist>Artist name 1 / Artist name 2</artist>
      return ((_currentStub.Artists = ParseCharacterSeparatedStrings(element, _currentStub.Artists)) != null);
    }

    /// <summary>
    /// Tries to read a record label value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadLabels(XElement element)
    {
      // Examples of valid elements:
      // <company>Record label</company>
      // <company>Record label 1 / Record label 2</company>
      return ((_currentStub.Labels = ParseCharacterSeparatedStrings(element, _currentStub.Labels)) != null);
    }

    #endregion

    #region Content information

    /// <summary>
    /// Tries to read a genre value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadGenre(XElement element)
    {
      // Examples of valid elements:
      // <genre>Dance</genre>
      // <genre>Rock / Trash</genre>
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
    /// Tries to (asynchronously) read a track value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadTrack(XElement element)
    {
      // For examples of valid element values see the comment in NfoReaderBase.ParseTrack
      var track = ParseTrack(element);
      if (track == null)
        return false;
      if (_currentStub.Tracks == null)
        _currentStub.Tracks = new HashSet<AlbumTrackStub>();
      _currentStub.Tracks.Add(track);
      return true;
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

    #region Ratings

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
    /// Checks whether the <paramref name="itemRootElement"/>'s name is "album"
    /// </summary>
    /// <param name="itemRootElement">Element to check</param>
    /// <returns><c>true</c> if the element's name is "album"; else <c>false</c></returns>
    protected override bool CanReadItemRootElementTree(XElement itemRootElement)
    {
      var itemRootElementName = itemRootElement.Name.ToString();
      if (itemRootElementName == ALBUM_ROOT_ELEMENT_NAME)
        return true;
      _debugLogger.Warn("[#{0}]: Cannot extract metadata; name of the item root element is {1} instead of {2}", _miNumber, itemRootElementName, ALBUM_ROOT_ELEMENT_NAME);
      return false;
    }

    /// <summary>
    /// Tries to read a series nfo-file into <see cref="AlbumStub"/> objects (or gets them from cache)
    /// </summary>
    /// <param name="nfoFsra"><see cref="IFileSystemResourceAccessor"/> pointing to the nfo-file</param>
    /// <returns><c>true</c> if any usable metadata was found; else <c>false</c></returns>
    public override async Task<bool> TryReadMetadataAsync(IFileSystemResourceAccessor nfoFsra)
    {
      var stubs = await CACHE.GetValue(nfoFsra.CanonicalLocalResourcePath, async path =>
      {
        _debugLogger.Info("[#{0}]: AlbumStub object for series nfo-file not found in cache; parsing nfo-file {1}", _miNumber, nfoFsra.CanonicalLocalResourcePath);
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
