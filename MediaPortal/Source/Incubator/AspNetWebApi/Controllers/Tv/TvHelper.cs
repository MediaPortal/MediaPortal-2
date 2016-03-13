using System.Net;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers.Tv
{
  internal class TvHelper
  {
    /// <summary>
    /// Throws an ServiceUnavailable exception if no Tv Provider is available
    /// </summary>
    internal static void TvAvailable()
    {
      if(!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new HttpException(HttpStatusCode.ServiceUnavailable, "No Tv Provider available");
    }
  }
}
