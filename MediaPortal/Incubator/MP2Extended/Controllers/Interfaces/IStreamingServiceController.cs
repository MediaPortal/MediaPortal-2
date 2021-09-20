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

using System.Collections.Generic;
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
    Task<WebStreamServiceDescription> GetServiceDescription();
    Task<IList<WebTranscoderProfile>> GetTranscoderProfiles();
    Task<IList<WebTranscoderProfile>> GetTranscoderProfilesForTarget(string target);
    Task<WebTranscoderProfile> GetTranscoderProfileByName(string name);
    Task<WebMediaInfo> GetMediaInfo(string itemId, WebMediaType type);
    // playerPosition is in seconds
    Task<WebTranscodingInfo> GetTranscodingInfo(string identifier, long? playerPosition);
    Task<WebBoolResult> InitStream(string itemId, string clientDescription, string identifier, WebMediaType type, int? idleTimeout);
    // startPosition is in seconds
    Task<WebStringResult> StartStream(string identifier, string profileName, long startPosition);
    // startPosition is in seconds
    Task<WebStringResult> StartStreamWithStreamSelection(string identifier, string profileName, long startPosition, int audioId, int subtitleId);
    Task<WebBoolResult> StopStream(string identifier);
    Task<WebBoolResult> FinishStream(string identifier);
    Task<IList<WebStreamingSession>> GetStreamingSessions(string filter = null);
    Task<WebResolution> GetStreamSize(WebMediaType type, int? provider, string itemId, int? offset, string profile);
    Task<WebBoolResult> AuthorizeStreaming();
    Task<WebBoolResult> AuthorizeRemoteHostForStreaming(string host);
    Task<WebItemSupportStatus> GetItemSupportStatus(WebMediaType type, int? provider, string itemId, int? offset);
    //Task<WebStreamLogs> GetStreamLogs(string identifier);
    //Task<WebMediaHash> GetItemHash(WebMediaType type, int? provider, string itemId, int? offset, bool smartHash);
    Task<WebBoolResult> RequestImageResize(WebMediaType mediatype, int? provider, string id, WebFileType imagetype, int offset, int maxWidth, int maxHeight, string borders = null, string format = null);
  }
}
