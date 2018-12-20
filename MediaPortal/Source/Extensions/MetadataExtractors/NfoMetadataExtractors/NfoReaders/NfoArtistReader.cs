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

using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;
using MediaPortal.Utilities.Cache;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders
{
  /// <summary>
  /// Reads the content of a nfo-file for artists into <see cref="ArtistStub"/> objects and stores
  /// the appropriate values into the respective <see cref="MediaItemAspect"/>s
  /// </summary>
  /// <remarks>
  /// There is a TryRead method for any known child element of the nfo-file's root element and a
  /// TryWrite method for any MIA-Attribute we store values in.
  /// </remarks>
  public class NfoArtistReader : NfoReaderBase<ArtistStub>
  {
    #region Consts

    /// <summary>
    /// The name of the root element in a valid nfo-file for albums
    /// </summary>
    private const string ARTIST_ROOT_ELEMENT_NAME = "artist";

    /// <summary>
    /// Default timeout for the cache is 5 minutes
    /// </summary>
    private static readonly TimeSpan CACHE_TIMEOUT = new TimeSpan(0, 5, 0);

    /// <summary>
    /// Cache used to temporarily store <see cref="SeriesStub"/> objects so that the same tvshow.nfo file
    /// doesn't have to be parsed once for every episode
    /// </summary>
    private static readonly AsyncStaticTimeoutCache<ResourcePath, List<ArtistStub>> CACHE = new AsyncStaticTimeoutCache<ResourcePath, List<ArtistStub>>(CACHE_TIMEOUT);

    #endregion

    #region Ctor

    /// <summary>
    /// Instantiates a <see cref="NfoArtistReader"/> object
    /// </summary>
    /// <param name="debugLogger">Debug logger to log to</param>
    /// <param name="miNumber">Unique number of the MediaItem for which the nfo-file is parsed</param>
    /// <param name="forceQuickMode">If true, no long lasting operations such as parsing images are performed</param>
    /// <param name="httpClient"><see cref="HttpClient"/> used to download from http URLs contained in nfo-files</param>
    /// <param name="settings">Settings of the <see cref="NfoMovieMetadataExtractor"/></param>
    public NfoArtistReader(ILogger debugLogger, long miNumber, bool forceQuickMode, HttpClient httpClient, NfoAudioMetadataExtractorSettings settings)
      : base(debugLogger, miNumber, forceQuickMode, httpClient, settings)
    {
      _settings = settings;
      InitializeSupportedElements();
      InitializeSupportedAttributes();
    }

    #endregion

    #region Private methods

    #region Ctor helpers

    /// <summary>
    /// Adds a delegate for each xml element in a movie nfo-file that is understood by this MetadataExtractor to NfoReaderBase._supportedElements
    /// </summary>
    private void InitializeSupportedElements()
    {
      _supportedElements.Add("musicBrainzArtistID", new TryReadElementDelegate(TryReadMbArtistId));
      _supportedElements.Add("audioDbID", new TryReadElementDelegate(TryReadAudiodbId));
      _supportedElements.Add("name", new TryReadElementDelegate(TryReadName));
      _supportedElements.Add("formed", new TryReadElementDelegate(TryReadFormedDate));
      _supportedElements.Add("born", new TryReadElementDelegate(TryReadBirthDate));
      _supportedElements.Add("died", new TryReadElementDelegate(TryReadDeathDate));
      _supportedElements.Add("disbanded", new TryReadElementDelegate(TryReadDisbandedDate));
      //_supportedElements.Add("thumb", new TryReadElementAsyncDelegate(TryReadThumbAsync));
      _supportedElements.Add("biography", new TryReadElementDelegate(TryReadBiography));

      //Ignored. No attribute in aspects to store them or irrelevant
      _supportedElements.Add("genre", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("style", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("mood", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("yearsactive", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("instruments", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("album", new TryReadElementDelegate(Ignore));
      _supportedElements.Add("thumb", new TryReadElementDelegate(Ignore));
    }

    /// <summary>
    /// Adds a delegate for each Attribute in a MediaItemAspect into which this MetadataExtractor can write metadata to NfoReaderBase._supportedAttributes
    /// </summary>
    private void InitializeSupportedAttributes()
    {
      _supportedAttributes.Add(TryWriteMediaAspectTitle);

      _supportedAttributes.Add(TryWritePersonAspectPersonName);
      _supportedAttributes.Add(TryWritePersonAspectOccupation);
      _supportedAttributes.Add(TryWritePersonAspectBiography);
      _supportedAttributes.Add(TryWritePersonAspectDateOfBirth);
      _supportedAttributes.Add(TryWritePersonAspectDateOfDeath);
      _supportedAttributes.Add(TryWritePersonAspectGroup);

      _supportedAttributes.Add(TryWriteExternalIdentifierAspectAudioDbId);
      _supportedAttributes.Add(TryWriteExternalIdentifierAspectMusicBrainzArtistId);

      _supportedAttributes.Add(TryWriteThumbnailLargeAspectThumbnail);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the <see cref="ArtistStub"/> objects generated by this class
    /// </summary>
    /// <returns>List of <see cref="ArtistStub"/> objects</returns>
    public List<ArtistStub> GetArtistStubs()
    {
      return _stubs;
    }

    #endregion

    #region Reader methods for direct child elements of the root element

    #region Internet databases

    /// <summary>
    /// Tries to read the MusicBrainz artist ID
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadMbArtistId(XElement element)
    {
      // Example of a valid element:
      // <musicBrainzArtistID>be62a5b3-2236-38e7-bcb0-c87f75be2259</musicBrainzArtistID>
      return ((_currentStub.MusicBrainzArtistId = ParseSimpleString(element)) != null);
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

    #region Artist information

    /// <summary>
    /// Tries to read the name
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadName(XElement element)
    {
      // Example of a valid element:
      // <title>Album Title</title>
      return ((_currentStub.Name = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the biography
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadBiography(XElement element)
    {
      // Example of a valid element:
      // <biography>Biography text</biography>
      return ((_currentStub.Biography = ParseSimpleString(element)) != null);
    }

    /// <summary>
    /// Tries to read the formed date value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadFormedDate(XElement element)
    {
      // Examples of valid elements:
      // <formed>1994-09-14</formed>
      // <formed>1994</formed>
      return ((_currentStub.Formeddate = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the birth date value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadBirthDate(XElement element)
    {
      // Examples of valid elements:
      // <born>1994-09-14</born>
      // <born>1994</born>
      return ((_currentStub.Birthdate = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the death date value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDeathDate(XElement element)
    {
      // Examples of valid elements:
      // <died>1994-09-14</died>
      // <died>1994</died>
      return ((_currentStub.Formeddate = ParseSimpleDateTime(element)) != null);
    }

    /// <summary>
    /// Tries to read the disbanded date value
    /// </summary>
    /// <param name="element"><see cref="XElement"/> to read from</param>
    /// <returns><c>true</c> if a value was found in <paramref name="element"/>; otherwise <c>false</c></returns>
    private bool TryReadDisbandedDate(XElement element)
    {
      // Examples of valid elements:
      // <disbanded>1994-09-14</disbanded>
      // <disbanded>1994</disbanded>
      return ((_currentStub.Disbandeddate = ParseSimpleDateTime(element)) != null);
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
      if (_stubs[0].Name != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, _stubs[0].Name);
        return true;
      }
      return false;
    }

    #endregion

    #region PersonAspect

    /// <summary>
    /// Tries to write metadata into <see cref="PersonAspect.ATTR_PERSON_NAME"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWritePersonAspectPersonName(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Name != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_PERSON_NAME, _stubs[0].Name);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="PersonAspect.ATTR_OCCUPATION"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWritePersonAspectOccupation(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_OCCUPATION, PersonAspect.OCCUPATION_ARTIST);
      return true;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="PersonAspect.ATTR_BIOGRAPHY"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWritePersonAspectBiography(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Biography != null)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_BIOGRAPHY, _stubs[0].Biography);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="PersonAspect.ATTR_DATEOFBIRTH"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWritePersonAspectDateOfBirth(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Birthdate.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_DATEOFBIRTH, _stubs[0].Birthdate);
        return true;
      }
      // priority 2:
      if (_stubs[0].Formeddate.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_DATEOFBIRTH, _stubs[0].Formeddate);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="PersonAspect.ATTR_DATEOFDEATH"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWritePersonAspectDateOfDeath(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // priority 1:
      if (_stubs[0].Deathdate.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_DATEOFDEATH, _stubs[0].Deathdate);
        return true;
      }
      // priority 2:
      if (_stubs[0].Disbandeddate.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_DATEOFDEATH, _stubs[0].Disbandeddate);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="PersonAspect.ATTR_GROUP"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWritePersonAspectGroup(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].Birthdate.HasValue || _stubs[0].Deathdate.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_GROUP, false);
        return true;
      }
      if (_stubs[0].Formeddate.HasValue || _stubs[0].Disbandeddate.HasValue)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, PersonAspect.ATTR_GROUP, true);
        return true;
      }
      return false;
    }

    #endregion

    #region ExternalIdentifierAspect

    /// <summary>
    /// Tries to write metadata into <see cref="ExternalIdentifierAspect.ATTR_ID"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteExternalIdentifierAspectAudioDbId(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].AudioDbId.HasValue)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_PERSON, _stubs[0].AudioDbId.ToString());
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tries to write metadata into <see cref="ExternalIdentifierAspect.ATTR_ID"/>
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s to write into</param>
    /// <returns><c>true</c> if any information was written; otherwise <c>false</c></returns>
    private bool TryWriteExternalIdentifierAspectMusicBrainzArtistId(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      if (_stubs[0].MusicBrainzArtistId != null)
      {
        MediaItemAspect.AddOrUpdateExternalIdentifier(extractedAspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_PERSON, _stubs[0].MusicBrainzArtistId);
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
    /// Checks whether the <paramref name="itemRootElement"/>'s name is "artist"
    /// </summary>
    /// <param name="itemRootElement">Element to check</param>
    /// <returns><c>true</c> if the element's name is "artist"; else <c>false</c></returns>
    protected override bool CanReadItemRootElementTree(XElement itemRootElement)
    {
      var itemRootElementName = itemRootElement.Name.ToString();
      if (itemRootElementName == ARTIST_ROOT_ELEMENT_NAME)
        return true;
      _debugLogger.Warn("[#{0}]: Cannot extract metadata; name of the item root element is {1} instead of {2}", _miNumber, itemRootElementName, ARTIST_ROOT_ELEMENT_NAME);
      return false;
    }

    /// <summary>
    /// Tries to read a series nfo-file into <see cref="ArtistStub"/> objects (or gets them from cache)
    /// </summary>
    /// <param name="nfoFsra"><see cref="IFileSystemResourceAccessor"/> pointing to the nfo-file</param>
    /// <returns><c>true</c> if any usable metadata was found; else <c>false</c></returns>
    public override async Task<bool> TryReadMetadataAsync(IFileSystemResourceAccessor nfoFsra)
    {
      var stubs = await CACHE.GetValue(nfoFsra.CanonicalLocalResourcePath, async path =>
      {
        _debugLogger.Info("[#{0}]: ArtistStub object for series nfo-file not found in cache; parsing nfo-file {1}", _miNumber, nfoFsra.CanonicalLocalResourcePath);
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
