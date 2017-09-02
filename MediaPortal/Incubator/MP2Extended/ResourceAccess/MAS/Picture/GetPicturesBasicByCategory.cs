using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture.BaseClasses;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetPicturesBasicByCategory : BasePictureBasic
  {
    public IList<WebPictureBasic> Process(string id)
    {
      DateTime recordingTime = (DateTime)JsonConvert.DeserializeObject(id, typeof(DateTime));
      if (recordingTime == null)
        throw new BadRequestException("GetPicturesBasicByCategory: couldn't convert id to DateTime");
      
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);

      IFilter searchFilter = new BetweenFilter(MediaAspect.ATTR_RECORDINGTIME, new DateTime(recordingTime.Year, 1, 1), new DateTime(recordingTime.Year, 12, 31));
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, searchFilter);

      IList<MediaItem> items = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false);

      var output = items.Select(item => PictureBasic(item)).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}