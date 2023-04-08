#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Controllers.Contexts;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using System;
using System.Net.Http;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Http;
using System.Web.Http.Description;
#endif

namespace MediaPortal.Plugins.MP2Extended.Controllers.stream
{
#if NET5_0_OR_GREATER
  [ApiController]
  [Route("MPExtended/StreamingService/stream/[action]")]
  [MediaPortalAuthorize]
  public class StreamingServiceStreamController : Controller
  {
    protected RequestContext Context => ControllerContext.HttpContext.ToRequestContext();
#else
  [RoutePrefix("MPExtended/StreamingService/stream")]
  [Route("{action}")]
  [MediaPortalAuthorize]
  public class StreamingServiceStreamController : ApiController
  {
    protected RequestContext Context => Request.GetOwinContext().ToRequestContext();
#endif
    #region General

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetMediaItem(Guid itemId, long? startPosition)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.General.GetMediaItem.ProcessAsync(Context, itemId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetHTMLResource(string path)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.General.GetHtmlResource.ProcessAsync(Context, path);
    }

    #endregion

    #region Control

    [HttpGet]
    [ApiExplorerSettings]
    public async Task RetrieveStream(string identifier, string file = null, string hls = null)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Control.RetrieveStream.ProcessAsync(Context, identifier, file, hls);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task DoStream(WebMediaType type, int? provider, string itemId, string clientDescription, string profileName, long startPosition, int? idleTimeout)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      string identifier = Guid.NewGuid().ToString();
      var result = await ResourceAccess.WSS.json.Control.InitStream.ProcessAsync(Context, itemId, clientDescription, identifier, type, idleTimeout);
      if (result.Result)
        await ResourceAccess.WSS.json.Control.StartStream.ProcessAsync(Context, identifier, profileName, startPosition);
    }

    //[HttpGet]
    //[ApiExplorerSettings]
    //public Task CustomTranscoderData(string identifier, string action, string parameters)
    //{
    //  Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());

    //}

    #endregion

    #region Images

    [HttpGet]
    [ApiExplorerSettings]
    public async Task ExtractImage(WebMediaType type, string itemId)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.ExtractImage.ProcessAsync(Context, type, itemId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task ExtractImageResized(WebMediaType type, string itemId, int maxWidth, int maxHeight, string borders = null)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.ExtractImageResized.ProcessAsync(Context, type, itemId, maxWidth, maxHeight, borders);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetImage(WebMediaType type, string id)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.GetImage.ProcessAsync(Context, type, id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetImageResized(WebMediaType type, string id, int maxWidth, int maxHeight, string borders = null)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.GetImageResized.ProcessAsync(Context, type, id, maxWidth, maxHeight, borders);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetArtwork(WebMediaType mediatype, string id, WebFileType artworktype, int offset = 0)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.GetArtwork.ProcessAsync(Context, mediatype, id, artworktype, offset);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetArtworkResized(WebMediaType mediatype, string id, WebFileType artworktype, int maxWidth, int maxHeight, int offset = 0, string borders = null)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.GetArtworkResized.ProcessAsync(Context, mediatype, id, artworktype, offset, maxWidth, maxHeight, borders);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetOnlineVideosArtwork(WebOnlineVideosMediaType mediatype, string id)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.GetOnlineVideosArtwork.ProcessAsync(Context, mediatype, id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task GetOnlineVideosArtworkResized(WebOnlineVideosMediaType mediatype, string id, int maxWidth, int maxHeight, string borders = null)
    {
      Logger.Debug("WSS Request: {0}", Request.GetDisplayUrl());
      await ResourceAccess.WSS.stream.Images.GetOnlineVideosArtworkResized.ProcessAsync(Context, mediatype, id, maxWidth, maxHeight, borders);
    }

    #endregion

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
