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
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers
{
  public class Item
  {
    public int Id;
    public string Name;
  }

  [Route("v1/MediaLibrary/[Controller]")]
  public class MediaItemsController : Controller
  {
    public static List<Item> Items = new List<Item>
    {
      new Item { Id = 1, Name = "First Test Item" },
      new Item { Id = 2, Name = "Second Test Item" },
    };

    private readonly ILogger _logger;

    public MediaItemsController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<MediaItemsController>();
    }

    [HttpGet]
    public IEnumerable<MediaItem> Get(string searchText, Guid[] necessaryMiaIds = null, Guid[] optionalMiaIds = null, string[] sortInformationStrings = null, uint? offset = null, uint? limit = null)
    {
      _logger.LogDebug("serachText = '{0}'", searchText);
      if(necessaryMiaIds != null)
        _logger.LogDebug("necessaryMiaIds = {0}", string.Join(",",necessaryMiaIds));
      if (optionalMiaIds != null)
        _logger.LogDebug("optionalMiaIds = {0}", string.Join(",", optionalMiaIds));
      if (sortInformationStrings != null)
        _logger.LogDebug("sortInformationStrings = {0}", string.Join(",", sortInformationStrings));
      _logger.LogDebug("offset = '{0}'; limit = '{1}'", offset, limit);

      ParameterValidator.ValidateMiaIds(ref necessaryMiaIds, ref optionalMiaIds, _logger);
      var query = ServiceRegistration.Get<IMediaLibrary>().BuildSimpleTextSearchQuery(searchText, necessaryMiaIds, optionalMiaIds, null, false, false);

      var sortInformation = ParameterValidator.ValidateSortInformation(sortInformationStrings, _logger);
      if (sortInformation.Any())
        query.SortInformation = sortInformation;

      query.Offset = offset;
      query.Limit = limit;

      return ServiceRegistration.Get<IMediaLibrary>().Search(query, false);
    }

    [HttpGet("{id}")]
    public Item Get(int id)
    {
      return Items.FirstOrDefault(item => item.Id == id);
    }
  }
}
