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
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;

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
    /// GET /api/v1/MediaLibrary/MediaItems/[MediaItemId]
    /// </summary>
    /// <param name="id">ID of the <see cref="MediaItem"/></param>
    /// <param name="filterOnlyOnline">If <c>true</c>, the search only returns a MediaItem if it is currently accessible</param>
    /// <returns>
    /// Collection of <see cref="MediaItem"/>s either containing one <see cref="MediaItem"/> with all its MediaItemAspects or
    /// no <see cref="MediaItem"/> if there is no <see cref="MediaItem"/> with the given <paramref name="id"/>
    /// </returns>
    [HttpGet("{id}")]
    public IEnumerable<MediaItem> Get(Guid id, bool filterOnlyOnline = false)
    {
      var filter = new MediaItemIdFilter(id);
      var query = new MediaItemQuery(null, ServiceRegistration.Get<IMediaItemAspectTypeRegistration>().LocallyKnownMediaItemAspectTypes.Keys, filter);
      return ServiceRegistration.Get<IMediaLibrary>().Search(query, filterOnlyOnline);
    }

    #endregion
  }
}
