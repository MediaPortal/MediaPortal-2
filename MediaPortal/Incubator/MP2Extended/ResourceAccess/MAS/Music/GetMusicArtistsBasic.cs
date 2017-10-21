using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetMusicArtistsBasic
  {
    public IList<WebMusicArtistBasic> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      HomogenousMap items = ServiceRegistration.Get<IMediaLibrary>().GetValueGroups(AudioAspect.ATTR_ARTISTS, null, ProjectionFunction.None, necessaryMIATypes, null, true);
      HomogenousMap itemsAlbum = ServiceRegistration.Get<IMediaLibrary>().GetValueGroups(AudioAspect.ATTR_ALBUMARTISTS, null, ProjectionFunction.None, necessaryMIATypes, null, true);

      List<WebMusicArtistBasic> output = new List<WebMusicArtistBasic>();

      if (items.Count == 0)
        return output;

      output = (from item in items
        where item.Key is string
        select new WebMusicArtistBasic
        {
          Id = Convert.ToBase64String((new UTF8Encoding().GetBytes(item.Key.ToString()))), Title = item.Key.ToString(), PID = 0, HasAlbums = itemsAlbum.ContainsKey(item.Key)
        }).ToList();

      
      // sort and filter
      if (sort != null && order != null)
      {
       output = output.AsQueryable().Filter(filter).SortMediaItemList(sort, order).ToList();
      }
      else
        output = output.AsQueryable().Filter(filter).ToList();


      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}