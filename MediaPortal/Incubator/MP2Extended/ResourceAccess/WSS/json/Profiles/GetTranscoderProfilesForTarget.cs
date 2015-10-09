using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.WSS.StreamInfo;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles
{
  internal class GetTranscoderProfilesForTarget : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string target = httpParam["target"].Value;
      if (target == null)
        throw new BadRequestException("GetTranscoderProfilesForTarget: target is null");

      List<WebTranscoderProfile> output = new List<WebTranscoderProfile>();

      WebTranscoderProfile webTranscoderProfile = new WebTranscoderProfile
      {
        Bandwidth = 2280,
        Description = "HD-quality Android profile based on ffmpeg",
        HasVideoStream = true,
        MIME = "videoMP2T",
        MaxOutputHeight = 1280,
        MaxOutputWidth = 720,
        Name = "Android FFmpeg HD",
        Targets = new List<string> { "android" },
        Transport = "http"
      };

      output.Add(webTranscoderProfile);


      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
