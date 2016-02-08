using System;
using System.IO;
using Microsoft.AspNet.Mvc;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Control;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images;

namespace MediaPortal.Plugins.MP2Extended.Controllers.stream
{
  [Route("[Controller]/stream/[Action]")]
  public class StreamingServiceController : Controller
  {
    #region General

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public void GetMediaItem(Guid itemId, long? startPosition)
    {
      new GetMediaItem().Process(HttpContext, itemId);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public void GetHTMLResource(string path)
    {
      new GetHtmlResource().Process(path, HttpContext);
    }

    #endregion

    #region Control

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public void RetrieveStream(string identifier, string file, string hls)
    {
      new RetrieveStream().Process(HttpContext, identifier, file, hls);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public Stream DoStream(WebMediaType type, int? provider, string itemId, string clientDescription, string profileName, long startPosition, int? idleTimeout)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public Stream CustomTranscoderData(string identifier, string action, string parameters)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Images

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public FileResult ExtractImage(WebMediaType type, string itemId)
    {
      return File(new ExtractImage().Process(type, itemId), "image/*");
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public FileResult ExtractImageResized(WebMediaType type, string itemId, int maxWidth, int maxHeight, string borders = null)
    {
      return File(new ExtractImageResized().Process(type, itemId, maxWidth, maxHeight, borders), "image/*");
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public void GetImage(WebMediaType type, string id)
    {
      new GetImage().Process(HttpContext, type, id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public void GetImageResized(WebMediaType type, string id, int maxWidth, int maxHeight, string borders = null)
    {
      new GetImageResized().Process(HttpContext, type, id, maxWidth, maxHeight, borders);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public FileResult GetArtwork(WebMediaType mediatype, string id, WebFileType artworktype, int offset)
    {
      return File(new GetArtwork().Process(mediatype, id, artworktype, offset), "image/*");
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public FileResult GetArtworkResized(WebMediaType mediatype, string id, WebFileType artworktype, int offset, int maxWidth, int maxHeight, string borders = null)
    {
      return File(new GetArtworkResized().Process(mediatype, id, artworktype, offset, maxWidth, maxHeight, borders), "image/*");
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public FileResult GetOnlineVideosArtwork(WebOnlineVideosMediaType mediatype, string id)
    {
      return File(new GetOnlineVideosArtwork().Process(mediatype, id), "image/*");
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "StreamingService")]
    public FileResult GetOnlineVideosArtworkResized(WebOnlineVideosMediaType mediatype, string id, int maxWidth, int maxHeight, string borders = null)
    {
      return File(new GetOnlineVideosArtworkResized().Process(mediatype, id, maxWidth, maxHeight, borders), "image/*");
    }

    #endregion
  }
}
