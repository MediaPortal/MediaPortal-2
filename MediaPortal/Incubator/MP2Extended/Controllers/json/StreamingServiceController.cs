#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
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
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [RoutePrefix("MPExtended/StreamingService/json")]
  [Route("{action}")]
  [Authorize]
  public class StreamingServiceController : ApiController, IStreamingServiceController
  {
    #region General

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public async Task<WebStreamServiceDescription> GetServiceDescription()
    {
      return await new GetServiceDescription().ProcessAsync(Request.GetOwinContext());
    }

    #endregion

    #region Profiles

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTranscoderProfile>> GetTranscoderProfiles()
    {
      return await new GetTranscoderProfiles().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTranscoderProfile>> GetTranscoderProfilesForTarget(string target)
    {
      return await new GetTranscoderProfilesForTarget().ProcessAsync(Request.GetOwinContext(), target);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTranscoderProfile> GetTranscoderProfileByName(string name)
    {
      return await new GetTranscoderProfileByName().ProcessAsync(Request.GetOwinContext(), name);
    }

    #endregion

    #region StreamInfo

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMediaInfo> GetMediaInfo(string itemId, WebMediaType type)
    {
      return await new GetMediaInfo().ProcessAsync(Request.GetOwinContext(), itemId, type);
    }

    #endregion

    #region Control

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTranscodingInfo> GetTranscodingInfo(string identifier, long? playerPosition)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> InitStream(string itemId, string clientDescription, string identifier, WebMediaType type, int? idleTimeout)
    {
      return await new InitStream().ProcessAsync(Request.GetOwinContext(), itemId, clientDescription, identifier, type, idleTimeout);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebStringResult> StartStream(string identifier, string profileName, long startPosition)
    {
      return await new StartStream().ProcessAsync(Request.GetOwinContext(), identifier, profileName, startPosition);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebStringResult> StartStreamWithStreamSelection(string identifier, string profileName, long startPosition, int audioId, int subtitleId)
    {
      return await new StartStreamWithStreamSelection().ProcessAsync(Request.GetOwinContext(), identifier, profileName, startPosition, audioId, subtitleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> StopStream(string identifier)
    {
      return await new StopStream().ProcessAsync(Request.GetOwinContext(), identifier);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> FinishStream(string identifier)
    {
      return await new FinishStream().ProcessAsync(Request.GetOwinContext(), identifier);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebStreamingSession>> GetStreamingSessions(string filter = null)
    {
      return await new GetStreamingSessions().ProcessAsync(Request.GetOwinContext(), filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebResolution> GetStreamSize(WebMediaType type, int? provider, string itemId, int? offset, string profile)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> AuthorizeStreaming()
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> AuthorizeRemoteHostForStreaming(string host)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebItemSupportStatus> GetItemSupportStatus(WebMediaType type, int? provider, string itemId, int? offset)
    {
      return await new GetItemSupportStatus().ProcessAsync(Request.GetOwinContext(), type, provider, itemId, offset);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> RequestImageResize(WebMediaType mediatype, int? provider, string id, WebFileType imagetype, int offset, int maxWidth, int maxHeight, string borders = null, string format = null)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
