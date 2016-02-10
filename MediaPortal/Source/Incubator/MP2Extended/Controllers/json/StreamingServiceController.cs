using System;
using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Controllers.Interfaces;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.StreamInfo;
using MediaPortal.Plugins.MP2Extended.WSS;
using MediaPortal.Plugins.MP2Extended.WSS.General;
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.WSS.StreamInfo;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [Route("[Controller]/json/[Action]")]
  public class StreamingServiceController : Controller, IStreamingServiceController
  {

    #region General

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebStreamServiceDescription GetServiceDescription()
    {
      return new GetServiceDescription().Process();
    }

    #endregion

    #region Profiles

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public IList<WebTranscoderProfile> GetTranscoderProfiles()
    {
      return new GetTranscoderProfiles().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public IList<WebTranscoderProfile> GetTranscoderProfilesForTarget(string target)
    {
      return new GetTranscoderProfilesForTarget().Process(target);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebTranscoderProfile GetTranscoderProfileByName(string name)
    {
      return new GetTranscoderProfileByName().Process(name);
    }

    #endregion

    #region StreamInfo

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebMediaInfo GetMediaInfo(string itemId, WebMediaType type)
    {
      return new GetMediaInfo().Process(itemId, type);
    }

    #endregion

    #region Control

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebTranscodingInfo GetTranscodingInfo(string identifier, long? playerPosition)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebBoolResult InitStream(string itemId, string clientDescription, string identifier, WebMediaType type, int? idleTimeout)
    {
      return new InitStream().Process(HttpContext, itemId, clientDescription, identifier, type, idleTimeout);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebStringResult StartStream(string identifier, string profileName, long startPosition)
    {
      return new StartStream().Process(HttpContext, identifier, profileName, startPosition);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebStringResult StartStreamWithStreamSelection(string identifier, string profileName, long startPosition, int audioId, int subtitleId)
    {
      return new StartStreamWithStreamSelection().Process(HttpContext, identifier, profileName, startPosition, audioId, subtitleId);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebBoolResult StopStream(string identifier)
    {
      return new StopStream().Process(identifier);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebBoolResult FinishStream(string identifier)
    {
      return new FinishStream().Process(identifier);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public IList<WebStreamingSession> GetStreamingSessions(string filter = null)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebResolution GetStreamSize(WebMediaType type, int? provider, string itemId, int? offset, string profile)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebBoolResult AuthorizeStreaming()
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebBoolResult AuthorizeRemoteHostForStreaming(string host)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebItemSupportStatus GetItemSupportStatus(WebMediaType type, int? provider, string itemId, int? offset)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public WebBoolResult RequestImageResize(WebMediaType mediatype, int? provider, string id, WebFileType imagetype, int offset, int maxWidth, int maxHeight, string borders = null, string format = null)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
