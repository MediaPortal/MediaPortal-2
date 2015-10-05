using System.Collections.Generic;
using System.Net;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General
{
  class GetServiceDescription : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      WebMediaServiceDescription webMediaServiceDescription = new WebMediaServiceDescription();
      webMediaServiceDescription.ApiVersion = 4;
      webMediaServiceDescription.AvailableFileSystemLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 10,
          Name = "MP Movie Shares",
          Version = "0.4.99.1"
        },
        new WebBackendProvider
        {
          Id = 11,
          Name = "MP Picture Shares",
          Version = "0.4.99.1"
        },
        new WebBackendProvider
        {
          Id = 5,
          Name = "MP Shares",
          Version = "0.4.99.1"
        },
        new WebBackendProvider
        {
          Id = 9,
          Name = "MP Music Shares",
          Version = "0.4.99.1"
        }
      };
      webMediaServiceDescription.AvailableMovieLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 7,
          Name = "MP MyVideo",
          Version = "0.4.99.1"
        }
      };
      webMediaServiceDescription.AvailableMusicLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 4,
          Name = "MP MyMusic",
          Version = "0.4.99.1"
        }
      };
      webMediaServiceDescription.AvailablePictureLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 8,
          Name = "MP Picture Shares",
          Version = "0.4.99.1"
        }
      };
      webMediaServiceDescription.AvailableTvShowLibraries = new List<WebBackendProvider>
      {
        new WebBackendProvider
        {
          Id = 6,
          Name = "MP-TVSeries",
          Version = "0.4.99.1"
        }
      };
      webMediaServiceDescription.DefaultFileSystemLibrary = 5;
      webMediaServiceDescription.DefaultMovieLibrary = 0;
      webMediaServiceDescription.DefaultMusicLibrary = 4;
      webMediaServiceDescription.DefaultPictureLibrary = 0;
      webMediaServiceDescription.DefaultTvShowLibrary = 6;
      webMediaServiceDescription.ServiceVersion = "0.4.99.1";

      return webMediaServiceDescription;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
