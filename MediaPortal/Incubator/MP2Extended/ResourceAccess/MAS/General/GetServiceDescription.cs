using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetServiceDescription : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      WebMediaServiceDescription webMediaServiceDescription = new WebMediaServiceDescription();
      webMediaServiceDescription.ApiVersion = 4;
      webMediaServiceDescription.AvailableFileSystemLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 10,
          Name = "MP Movie Shares",
          Version = GlobalVersion.VERSION
        },
        new WebBackendProvider
        {
          Id = 11,
          Name = "MP Picture Shares",
          Version = GlobalVersion.VERSION
        },
        new WebBackendProvider
        {
          Id = 5,
          Name = "MP Shares",
          Version = GlobalVersion.VERSION
        },
        new WebBackendProvider
        {
          Id = 9,
          Name = "MP Music Shares",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.AvailableMovieLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 7,
          Name = "MP MyVideo",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.AvailableMusicLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 4,
          Name = "MP MyMusic",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.AvailablePictureLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 8,
          Name = "MP Picture Shares",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.AvailableTvShowLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 6,
          Name = "MP-TVSeries",
          Version = GlobalVersion.VERSION
        }
      };
      webMediaServiceDescription.DefaultFileSystemLibrary = 5;
      webMediaServiceDescription.DefaultMovieLibrary = 0;
      webMediaServiceDescription.DefaultMusicLibrary = 4;
      webMediaServiceDescription.DefaultPictureLibrary = 0;
      webMediaServiceDescription.DefaultTvShowLibrary = 6;
      webMediaServiceDescription.ServiceVersion = GlobalVersion.VERSION;

      return webMediaServiceDescription;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}