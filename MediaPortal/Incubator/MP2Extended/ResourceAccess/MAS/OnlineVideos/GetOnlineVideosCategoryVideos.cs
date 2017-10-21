using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosVideo>), Summary = "This function returns a list of Subcategories available in a selected Category.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetOnlineVideosCategoryVideos
  {
    public List<WebOnlineVideosVideo> Process(string id)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosCategoryVideos: id is null");

      string siteName;
      string categoryRecursiveName;
      OnlineVideosIdGenerator.DecodeCategoryId(id, out siteName, out categoryRecursiveName);

      List<WebOnlineVideosVideo> output = new List<WebOnlineVideosVideo>();

      foreach (var video in MP2Extended.OnlineVideosManager.GetCategoryVideos(siteName, categoryRecursiveName))
      {
        output.Add(new WebOnlineVideosVideo
        {
          Id = OnlineVideosIdGenerator.BuildVideoId(siteName, categoryRecursiveName, video.VideoUrl),
          Title = video.Title,
          Description = video.Description,
          AirDate = video.Airdate,
          Length = video.Length,
          StartTime = video.StartTime,
          SubtitleText = video.SubtitleText,
          SubtitleUrl = video.SubtitleUrl,
          VideoUrl = video.VideoUrl,
          ThumbUrl = video.Thumb
        });
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}