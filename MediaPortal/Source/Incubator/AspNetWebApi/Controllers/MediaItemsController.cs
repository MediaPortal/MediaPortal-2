#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Net;
using HttpServer.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers
{
  /// <summary>
  /// AspNet MVC Controller for <see cref="MediaItem"/>s
  /// </summary>
  [Route("v1/MediaLibrary/[Controller]")]
  public class MediaItemsController : Controller
  {
    #region Private fields

    private readonly ILogger _logger;

    /// <summary>
    /// Default mime type used as Content-Type header when returning an image
    /// </summary>
    /// <remarks>
    /// We currently don't store the mime type when storing images in the MediaLibrary; we only store the raw image data as byte[].
    /// But when returning an <see cref="FileContentResult"/>, it requires us to provide a <see cref="MediaTypeHeaderValue"/>.
    /// Although it is not recommended in productive systems, "image/*" would be correct, but has the disadvantage that both,
    /// Firefox and IE do not render the image, but offer to download and save it. When using "image/gif", both browsers correctly
    /// render the image - even if the image is not a gif, but e.g. a jpg.
    /// </remarks>
    private const string DEFAULT_IMAGE_CONTENT_TYPE_STRING = "image/gif";

    private const int DEFAULT_IMAGE_MAX_WIDTH = 3840;
    private const int DEFAULT_IMAGE_MAX_HEIGHT = 2160;

    #endregion

    #region Constructor

    public MediaItemsController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<MediaItemsController>();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/v1/MediaLibrary/MediaItems
    /// </summary>
    /// <param name="searchText">Text to search for in all string Attributes of the MediaItems</param>
    /// <param name="necessaryMiaIds">Ids of the MIAs that every returned MediaItem mus have</param>
    /// <param name="optionalMiaIds">Ids of the MIAs that are returned if present; if <c>null</c>, all MIAs are returned</param>
    /// <param name="sortInformationStrings">Strings representing <see cref="SortInformation"/> objects in the form "[MediaItemAspectId].[AttributeName].[SortDirection]"</param>
    /// <param name="offset">Offset in the search result, as of which MediaItems are returned</param>
    /// <param name="limit">Maximum number of MediaItems to return starting at <paramref name="offset"/></param>
    /// <param name="includeClobs">If <c>true</c>, the search is also performed in very large string Attributes (irrelevant for SQLiteDatabase)</param>
    /// <param name="caseSensitive">If <c>true</c>, the search is performed case sensistive</param>
    /// <param name="filterOnlyOnline">If <c>true</c>, the search only returns MediaItems that are currently accessible</param>
    /// <returns>Collection of MediaItems matching the search criteria</returns>
    [HttpGet]
    public IEnumerable<MediaItem> Get(string searchText = null,
                                      Guid[] necessaryMiaIds = null,
                                      Guid[] optionalMiaIds = null,
                                      string[] sortInformationStrings = null,
                                      uint? offset = null,
                                      uint? limit = null,
                                      bool includeClobs = false,
                                      bool caseSensitive = false,
                                      bool filterOnlyOnline = false)
    {
      // Logging for debugging purposes only;
      // ToDo: Remove this once the service is stable
      _logger.LogDebug("serachText = '{0}'", searchText);
      if(necessaryMiaIds != null)
        _logger.LogDebug("necessaryMiaIds = {0}", string.Join(",",necessaryMiaIds));
      if (optionalMiaIds != null)
        _logger.LogDebug("optionalMiaIds = {0}", string.Join(",", optionalMiaIds));
      if (sortInformationStrings != null)
        _logger.LogDebug("sortInformationStrings = {0}", string.Join(",", sortInformationStrings));
      _logger.LogDebug("offset = '{0}'; limit = '{1}'", offset, limit);
      _logger.LogDebug("includeClobs = {0}; caseSensitive = {1}; filterOnlyOnline = {2}", includeClobs, caseSensitive, filterOnlyOnline);

      ParameterValidator.ValidateMiaIds(ref necessaryMiaIds, ref optionalMiaIds, _logger);

      // BuildSimpleTextSearchQuery returns a query with a FalseFilter if the searchText is null or empty because it doesn't allow searches for empty searchTexts;
      // we want to allow this. We therefore replace null or empty searchStrings with a space as dummy searchText, let BuildSimpleTextSearchQuery build its
      // query and then replace the generated filter with a TrueFilter.
      if (string.IsNullOrEmpty(searchText))
        searchText = " ";
      var query = ServiceRegistration.Get<IMediaLibrary>().BuildSimpleTextSearchQuery(searchText, necessaryMiaIds, optionalMiaIds, null, includeClobs, caseSensitive);
      if (searchText == " ")
        query.Filter = new NotFilter(new FalseFilter());

      var sortInformation = ParameterValidator.ValidateSortInformation(sortInformationStrings, _logger);
      if (sortInformation.Any())
        query.SortInformation = sortInformation;

      query.Offset = offset;
      query.Limit = limit;

      return ServiceRegistration.Get<IMediaLibrary>().Search(query, filterOnlyOnline);
    }

    /// <summary>
    /// GET /api/v1/MediaLibrary/MediaItems/[mediaItemId]
    /// </summary>
    /// <param name="mediaItemId">ID of the <see cref="MediaItem"/></param>
    /// <param name="filterOnlyOnline">If <c>true</c>, the search only returns a MediaItem if it is currently accessible</param>
    /// <returns>
    /// Collection of <see cref="MediaItem"/>s either containing one <see cref="MediaItem"/> with all its MediaItemAspects or
    /// no <see cref="MediaItem"/> if there is no <see cref="MediaItem"/> with the given <paramref name="mediaItemId"/>
    /// </returns>
    [HttpGet("{mediaItemId}")]
    public IEnumerable<MediaItem> Get(Guid mediaItemId, bool filterOnlyOnline = false)
    {
      var filter = new MediaItemIdFilter(mediaItemId);
      var query = new MediaItemQuery(null, ServiceRegistration.Get<IMediaItemAspectTypeRegistration>().LocallyKnownMediaItemAspectTypes.Keys, filter);
      return ServiceRegistration.Get<IMediaLibrary>().Search(query, filterOnlyOnline);
    }

    /// <summary>
    /// GET /api/v1/MediaLibrary/MediaItems/[mediaItemId]/bin/[attributeString]
    /// GET /api/v1/MediaLibrary/MediaItems/[mediaItemId]/bin/[attributeString]/[index]
    /// </summary>
    /// <param name="mediaItemId">ID of the <see cref="MediaItem"/></param>
    /// <param name="attributeString">String identifying an Attribute in the Form "[MediaItemAspectId].[AttributeName]"</param>
    /// <param name="index">
    /// Currently only 0 is valid. This parameter was introduced with respect to the MIA-rework. When there are multiple MIAs of the
    /// same type for one MediaItem and the MIA contains an image, we need to uniquely identify the respective image so that a request
    /// to this method with a specific index always returns the same image. Multiple MIAs of the same type do not have a certain order,
    /// which is why we need to generate this index e.g. based on the size and content of the image.
    /// </param>
    /// <param name="maxWidth">Maximum width of the image returned; if it is larger, it will be downscaled</param>
    /// <param name="maxHeight">Maximum height of the image returned; if it is larger, it will be downscaled</param>
    /// <returns>The byte array in form of an image</returns>
    [HttpGet("{mediaItemId}/bin/{attributeString}/{index:int?}")]
    public IActionResult Get(Guid mediaItemId, string attributeString, int index = 0, int maxWidth = DEFAULT_IMAGE_MAX_WIDTH, int maxHeight = DEFAULT_IMAGE_MAX_HEIGHT)
    {
      var filter = new MediaItemIdFilter(mediaItemId);
      var miam = ParameterValidator.ValidateAttribute(attributeString, _logger);
      if (miam.AttributeType != typeof(byte[]))
        throw new HttpException(HttpStatusCode.BadRequest, $"{miam.ParentMIAM.Name}.{miam.AttributeName} is not of type byte[]");
      var query = new MediaItemQuery(new[] { miam.ParentMIAM.AspectId }, null, filter);
      var mi = ServiceRegistration.Get<IMediaLibrary>().Search(query, false).FirstOrDefault();
      if (mi == null)
        throw new HttpException(HttpStatusCode.NotFound, "MediaItem or Aspect not found");
      var bytes = mi.Aspects[miam.ParentMIAM.AspectId].GetAttributeValue<byte[]>(miam);
      if (bytes == null)
        throw new HttpException(HttpStatusCode.NotFound, $"{miam.ParentMIAM.Name}.{miam.AttributeName} is empty for MediaItem with ID {mediaItemId}");
      if (maxWidth == DEFAULT_IMAGE_MAX_WIDTH && maxHeight == DEFAULT_IMAGE_MAX_HEIGHT)
        return new FileContentResult(bytes, new MediaTypeHeaderValue(DEFAULT_IMAGE_CONTENT_TYPE_STRING));
      using (var originalImageStream = new MemoryStream(bytes))
      {
        var resizedBytes = FanArtImage.FromStream(originalImageStream, maxWidth, maxHeight, FanArtMediaTypes.Undefined, FanArtTypes.Undefined, mediaItemId.ToString(), attributeString).BinaryData;
        return new FileContentResult(resizedBytes, new MediaTypeHeaderValue(DEFAULT_IMAGE_CONTENT_TYPE_STRING));
      }
    }

    /// <summary>
    /// GET /api/v1/MediaLibrary/MediaItems/[mediaItemId]/FanArt/[mediaType]/[fanArtType]
    /// GET /api/v1/MediaLibrary/MediaItems/[mediaItemId]/FanArt/[mediaType]/[fanArtType]/[index]
    /// </summary>
    /// <param name="mediaItemId">ID of the <see cref="MediaItem"/></param>
    /// <param name="mediaType"><see cref="FanArtMediaTypes"/></param>
    /// <param name="fanArtType"><see cref="FanArtTypes"/></param>
    /// <param name="index">Zero based index for cases where multiple images are available</param>
    /// <param name="maxWidth">Maximum width of the image returned; if it is larger, it will be downscaled</param>
    /// <param name="maxHeight">Maximum height of the image returned; if it is larger, it will be downscaled</param>
    /// <returns>The requested FanArt</returns>
    /// <remarks>
    /// This method is temporary until the MIA rework is finished and all FanArt is in the MediaLibrary
    /// </remarks>
    [HttpGet("{mediaItemId}/FanArt/{mediaType}/{fanArtType}/{index:int?}")]
    public IActionResult Get(Guid mediaItemId, string mediaType, string fanArtType, int index = 0, int maxWidth = DEFAULT_IMAGE_MAX_WIDTH, int maxHeight = DEFAULT_IMAGE_MAX_HEIGHT)
    {
      var images = ServiceRegistration.Get<IFanArtService>().GetFanArt(mediaType, fanArtType, mediaItemId.ToString(), maxWidth, maxHeight, false);
      if(images == null || !images.Any())
        throw new HttpException(HttpStatusCode.NotFound, "No FanArt found");
      if(images.Count <= index)
        throw new HttpException(HttpStatusCode.NotFound, $"No FanArt with index {index} found");
      var bytes = images[index].BinaryData;
      return new FileContentResult(bytes, new MediaTypeHeaderValue(DEFAULT_IMAGE_CONTENT_TYPE_STRING));
    }

    #endregion
  }
}
