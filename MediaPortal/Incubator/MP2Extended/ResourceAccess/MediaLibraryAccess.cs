#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;
using Microsoft.Owin;
using static MediaPortal.Common.MediaManagement.MediaItemAspectMetadata;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class MediaLibraryAccess
  {
    #region MediaItem By Id

    internal static MediaItem GetMediaItemById(IOwinContext context, string id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      return GetMediaItemById(context, Guid.Parse(id), necessaryMIATypes, optionalMIATypes);
    }

    internal static MediaItem GetMediaItemById(IOwinContext context, Guid id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      IList<MediaItem> items = GetMediaItemsById(context, id, necessaryMIATypes, optionalMIATypes, 1);
      if (items.Count != 0)
        return items[0];
      return null;
    }

    internal static IList<MediaItem> GetMediaItemsById(IOwinContext context, string id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint? limit = null)
    {
      return GetMediaItemsById(context, Guid.Parse(id), necessaryMIATypes, optionalMIATypes, limit);
    }

    internal static IList<MediaItem> GetMediaItemsById(IOwinContext context, Guid id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint? limit = null)
    {
      IFilter searchFilter = new MediaItemIdFilter(id);
      return Search(context, necessaryMIATypes, optionalMIATypes, searchFilter, limit);
    }

    #endregion ByID

    #region MediaItem By Name

    /// <summary>
    /// Filters by MediaAspect.ATTR_TITLE
    /// </summary>
    /// <param name="name"></param>
    /// <param name="necessaryMIATypes">Must contain MediaAspect</param>
    /// <param name="optionalMIATypes"></param>
    /// <returns></returns>
    internal static MediaItem GetMediaItemByName(IOwinContext context, string name, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      if (necessaryMIATypes == null)
        necessaryMIATypes = new HashSet<Guid> { MediaAspect.ASPECT_ID };

      IList<MediaItem> items = GetMediaItemsByString(context, name, necessaryMIATypes, optionalMIATypes, MediaAspect.ATTR_TITLE, 1);
      if (items.Count != 0)
        return items[0];
      return null;
    }

    internal static IList<MediaItem> GetMediaItemsByName(IOwinContext context, string name, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint? limit = null)
    {
      if (necessaryMIATypes == null)
        necessaryMIATypes = new HashSet<Guid> { MediaAspect.ASPECT_ID };

      return GetMediaItemsByString(context, name, necessaryMIATypes, optionalMIATypes, MediaAspect.ATTR_TITLE, limit);
    }

    internal static IList<MediaItem> GetMediaItemsByString(IOwinContext context, string name, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, AttributeSpecification attributeSpecification, uint? limit = null)
    {
      IFilter searchFilter = new RelationalFilter(attributeSpecification, RelationalOperator.EQ, name);
      return Search(context, necessaryMIATypes, optionalMIATypes, searchFilter, limit);
    }

    internal static IList<MediaItem> GetMediaItemsByInt(IOwinContext context, int number, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, AttributeSpecification attributeSpecification, uint? limit = null)
    {
      IFilter searchFilter = new RelationalFilter(attributeSpecification, RelationalOperator.EQ, number);
      return Search(context, necessaryMIATypes, optionalMIATypes, searchFilter, limit);
    }

    #endregion ByName

    #region MediaItem By Aspect

    internal static IList<MediaItem> GetMediaItemsByAspect(IOwinContext context, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint? limit = null)
    {
      return Search(context, necessaryMIATypes, optionalMIATypes, null, limit);
    }

    #endregion

    #region MediaItem By Time

    internal static IList<MediaItem> GetMediaItemsByRecordingTime(IOwinContext context, DateTime start, DateTime end, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      IFilter searchFilter = new BetweenFilter(MediaAspect.ATTR_RECORDINGTIME, start, end);
      return Search(context, necessaryMIATypes, optionalMIATypes, searchFilter);
    }

    #endregion

    #region MediaItem By Group

    internal static IList<MediaItem> GetMediaItemsByGroup(IOwinContext context, Guid itemRole, Guid groupRole, Guid groupId, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      IFilter filter = new RelationshipFilter(itemRole, groupRole, groupId);
      return Search(context, necessaryMIATypes, optionalMIATypes, filter);
    }

    #endregion

    #region Queries

    internal static int CountMediaItems(IOwinContext context, ISet<Guid> necessaryMIATypes, IFilter filter = null)
    {
      Guid? user = ResourceAccessUtils.GetUser(context);
      IFilter searchFilter = ResourceAccessUtils.AppendUserFilter(user, filter, necessaryMIATypes);
      return MediaLibrary.CountMediaItems(necessaryMIATypes, searchFilter, false, false);
    }

    internal static HomogenousMap GetGroups(IOwinContext context, ISet<Guid> necessaryMIATypes, AttributeSpecification attribute, IFilter filter = null)
    {
      Guid? user = ResourceAccessUtils.GetUser(context);
      IFilter searchFilter = ResourceAccessUtils.AppendUserFilter(user, filter, necessaryMIATypes);
      return MediaLibrary.GetValueGroups(attribute, filter, ProjectionFunction.None, necessaryMIATypes, searchFilter, true, false);
    }

    internal static IList<MediaItem> Search(IOwinContext context, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, IFilter filter, uint? limit = null)
    {
      Guid? user = ResourceAccessUtils.GetUser(context);
      IFilter searchFilter = ResourceAccessUtils.AppendUserFilter(user, filter, necessaryMIATypes);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = limit };
      return MediaLibrary.Search(searchQuery, false, user, false);
    }

    internal static bool Delete(IOwinContext context, MediaItem item)
    {
      Guid? user = ResourceAccessUtils.GetUser(context);
      IList<MultipleMediaItemAspect> providerResourceAspects;
      if (MediaItemAspect.TryGetAspects(item.Aspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
      {
        foreach (var res in providerResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY))
        {
          var systemId = res.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID);
          var resourcePathStr = res.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());
          MediaLibrary.DeleteMediaItemOrPath(systemId, resourcePath, true);
        }
      }
      return true;
    }

    #endregion

    internal static IMediaLibrary MediaLibrary
    {
      get { return ServiceRegistration.Get<IMediaLibrary>(); }
    }
  }
}
