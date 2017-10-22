using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MP2Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetTVShowActors
  {
    public IList<WebActor> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(PersonAspect.ASPECT_ID);

      IFilter searchFilter = new RelationshipFilter(PersonAspect.ROLE_ACTOR, SeriesAspect.ROLE_SERIES, Guid.Empty);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, searchFilter);
      IList<MediaItem> items = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false, null, false);
         
      if (items.Count == 0)
        throw new BadRequestException("No Tv show actors found");

      var output = items.Select(a => new WebActor(a.GetAspect(PersonAspect.Metadata).GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME)))
        .Filter(filter);

      if (sort != null && order != null)
        output.SortWebActor(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
