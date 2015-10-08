using System;
using System.Collections.Generic;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class GetMediaItems
  {
    #region ById

    internal static MediaItem GetMediaItemById(string id, ISet<Guid> necessaryMIATypes)
    {
      return GetMediaItemById(id, necessaryMIATypes, null);
    }

    internal static MediaItem GetMediaItemById(string id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      return GetMediaItemById(Guid.Parse(id), necessaryMIATypes, optionalMIATypes);
    }

    internal static MediaItem GetMediaItemById(Guid id, ISet<Guid> necessaryMIATypes)
    {
      return GetMediaItemById(id, necessaryMIATypes, null);
    }

    internal static MediaItem GetMediaItemById(Guid id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      IList<MediaItem> items = GetMediaItemsById(id, necessaryMIATypes, optionalMIATypes, 1);
      if (items.Count != 0)
        return items[0];
      return null;
    }

    internal static IList<MediaItem> GetMediaItemsById(string id, ISet<Guid> necessaryMIATypes, uint limit)
    {
      return GetMediaItemsById(id, necessaryMIATypes, null, limit);
    }

    internal static IList<MediaItem> GetMediaItemsById(string id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint limit)
    {
      return GetMediaItemsById(Guid.Parse(id), necessaryMIATypes, optionalMIATypes, limit);
    }

    internal static IList<MediaItem> GetMediaItemsById(Guid id, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint limit)
    {
      IFilter searchFilter = new MediaItemIdFilter(id);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = limit };

      return ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);
    }

    #endregion ByID

    #region ByName

    internal static MediaItem GetMediaItemByName(string name, ISet<Guid> necessaryMIATypes)
    {
      return GetMediaItemByName(name, necessaryMIATypes, null);
    }


    /// <summary>
    /// Filters by MediaAspect.ATTR_TITLE
    /// </summary>
    /// <param name="name"></param>
    /// <param name="necessaryMIATypes">Must contain MediaAspect</param>
    /// <param name="optionalMIATypes"></param>
    /// <returns></returns>
    internal static MediaItem GetMediaItemByName(string name, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes)
    {
      IList<MediaItem> items = GetMediaItemsByName(name, necessaryMIATypes, optionalMIATypes, 1);
      if (items.Count != 0)
        return items[0];
      return null;
    }

    internal static IList<MediaItem> GetMediaItemsByName(string name, ISet<Guid> necessaryMIATypes, uint limit)
    {
      if (necessaryMIATypes == null)
      {
        necessaryMIATypes = new HashSet<Guid> { MediaAspect.ASPECT_ID };
      }

      return GetMediaItemsByName(name, necessaryMIATypes, null, limit);
    }

    /// <summary>
    /// Filters by MediaAspect.ATTR_TITLE
    /// </summary>
    /// <param name="name"></param>
    /// <param name="necessaryMIATypes">Must contain MediaAspect</param>
    /// <param name="optionalMIATypes"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    internal static IList<MediaItem> GetMediaItemsByName(string name, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint? limit)
    {
      return GetMediaItemsByString(name, necessaryMIATypes, optionalMIATypes, MediaAspect.ATTR_TITLE, limit);
    }

    internal static IList<MediaItem> GetMediaItemsByString(string name, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, MediaItemAspectMetadata.AttributeSpecification attributeSpecification, uint? limit)
    {
      IFilter searchFilter = new RelationalFilter(attributeSpecification, RelationalOperator.EQ, name);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = limit };

      return ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);
    }

    internal static IList<MediaItem> GetMediaItemsByInt(int number, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, MediaItemAspectMetadata.AttributeSpecification attributeSpecification, uint? limit)
    {
      IFilter searchFilter = new RelationalFilter(attributeSpecification, RelationalOperator.EQ, number);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = limit };

      return ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);
    }

    #endregion ByName

    #region ByAspect

    internal static IList<MediaItem> GetMediaItemsByAspect(ISet<Guid> necessaryMIATypes)
    {
      return GetMediaItemsByAspect(necessaryMIATypes, null, null);
    }

    /// <summary>
    /// Returns all MediaItems which have the necessary MediaAspects
    /// </summary>
    /// <param name="necessaryMIATypes"></param>
    /// <param name="optionalMIATypes"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    internal static IList<MediaItem> GetMediaItemsByAspect(ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, uint? limit)
    {
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, null) { Limit = limit };

      return ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);
    }

    #endregion
  }
}