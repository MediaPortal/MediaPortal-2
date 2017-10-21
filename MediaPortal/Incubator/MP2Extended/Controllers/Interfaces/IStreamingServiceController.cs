using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.WSS;
using MediaPortal.Plugins.MP2Extended.WSS.General;
using MediaPortal.Plugins.MP2Extended.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.WSS.StreamInfo;

namespace MediaPortal.Plugins.MP2Extended.Controllers.Interfaces
{
  public interface IStreamingServiceController
  {
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebStreamServiceDescription GetServiceDescription();

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebTranscoderProfile> GetTranscoderProfiles();

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebTranscoderProfile> GetTranscoderProfilesForTarget(string target);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebTranscoderProfile GetTranscoderProfileByName(string name);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebMediaInfo GetMediaInfo(string itemId, WebMediaType type);

    // playerPosition is in seconds
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebTranscodingInfo GetTranscodingInfo(string identifier, long? playerPosition);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult InitStream(string itemId, string clientDescription, string identifier, WebMediaType type, int? idleTimeout);

    // startPosition is in seconds
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebStringResult StartStream(string identifier, string profileName, long startPosition);

    // startPosition is in seconds
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebStringResult StartStreamWithStreamSelection(string identifier, string profileName, long startPosition, int audioId, int subtitleId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult StopStream(string identifier);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult FinishStream(string identifier);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebStreamingSession> GetStreamingSessions(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebResolution GetStreamSize(WebMediaType type, int? provider, string itemId, int? offset, string profile);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult AuthorizeStreaming();

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult AuthorizeRemoteHostForStreaming(string host);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebItemSupportStatus GetItemSupportStatus(WebMediaType type, int? provider, string itemId, int? offset);

    /*[OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebStreamLogs GetStreamLogs(string identifier);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebMediaHash GetItemHash(WebMediaType type, int? provider, string itemId, int? offset, bool smartHash);*/

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult RequestImageResize(WebMediaType mediatype, int? provider, string id, WebFileType imagetype, int offset, int maxWidth, int maxHeight, string borders = null, string format = null);
  }
}
