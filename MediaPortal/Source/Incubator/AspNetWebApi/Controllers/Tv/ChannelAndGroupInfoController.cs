#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Net;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers.Tv
{
  /// <summary>
  /// AspNet MVC Controller for Tv Program Information
  /// </summary>
  [Route("v1/Tv/[Controller]")]
  public class ChannelAndGroupInfoController : Controller
  {
    #region Const

    const int CACHE_EXPIRATION_PERIOD = 5; // in Minutes

    #endregion

    #region Private fields

    private readonly ILogger _logger;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(relative: TimeSpan.FromMinutes(CACHE_EXPIRATION_PERIOD));

    /// <summary>
    /// Default mime type used as Content-Type header when returning an image
    /// </summary>
    /// <remarks>
    /// We currently don't store the mime type when storing images in the MediaLibrary; we only store the raw image data as byte[].
    /// But when returning an <see cref="FileContentResult"/>, it requires us to provide a <see cref="MediaTypeHeaderValue"/>.
    /// Although it is not recommended in productive systems, "image/*" would be correct, but has the disadvantage that both,
    /// Firefox and IE do not render the image, but offer to download and save it. When using "image/gif", both browsers correctly
    /// render the image - even if the image is not a gif, but e.g. a jpg.
    /// </remarks>
    private const string DEFAULT_IMAGE_CONTENT_TYPE_STRING = "image/gif";

    private const int DEFAULT_IMAGE_MAX_WIDTH = 3840;
    private const int DEFAULT_IMAGE_MAX_HEIGHT = 2160;

    #endregion

    #region Constructor

    public ChannelAndGroupInfoController(ILoggerFactory loggerFactory, IMemoryCache cache)
    {
      _logger = loggerFactory.CreateLogger<ChannelAndGroupInfoController>();
      _cache = cache;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/v1/Tv/ChannelAndGroupInfo/Groups
    /// </summary>
    /// <returns>Gets the list of available channel groups.</returns>
    [HttpGet("Groups")]
    public IList<IChannelGroup> Groups()
    {
      TvHelper.TvAvailable();

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      IList<IChannelGroup> groups;
      if (channelAndGroupInfo == null || !channelAndGroupInfo.GetChannelGroups(out groups))
        throw new HttpException(HttpStatusCode.NotFound, "No Groups found");

      return groups;
    }

    /// <summary>
    /// GET /api/v1/Tv/ChannelAndGroupInfo/ChannelsByGroup
    /// </summary>
    /// <param name="channelGroupId">Channel group id</param>
    /// <returns>Gets the list of channels in a channel group.</returns>
    [HttpGet("ChannelsByGroup/{channelGroupId}")]
    public IList<IChannel> ChannelsByGroup(int channelGroupId)
    {
      TvHelper.TvAvailable();

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      IChannelGroup group = GetGroup(channelGroupId);

      IList<IChannel> channels;
      if (channelAndGroupInfo == null || !channelAndGroupInfo.GetChannels(group, out channels))
        throw new HttpException(HttpStatusCode.NotFound, "No Channels found");

      return channels;
    }

    /// <summary>
    /// GET /api/v1/Tv/ChannelAndGroupInfo/Channel/[id]
    /// /// GET /api/v1/Tv/ChannelAndGroupInfo/Channels?chIds=[id]&chIds=[id]&...
    /// </summary>
    /// <param name="chIds">Channel id</param>
    /// <returns>Gets a List of channels by given <paramref name="chIds"/>.</returns>
    /// <remarks>Arrays are not supported in this 'channel/[id]' style: https://github.com/aspnet/Mvc/issues/1738 </remarks>
    [HttpGet("Channel/{chIds}")]
    [HttpGet("Channels")]
    public IList<IChannel> Channels(int[] chIds)
    {
      TvHelper.TvAvailable();

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      // remove dublicates
      int[] channelIds = chIds.Distinct().ToArray();
      List<IChannel> output = new List<IChannel>();
      foreach (var channelId in channelIds)
      {
        IChannel channel;
        if (channelAndGroupInfo != null && channelAndGroupInfo.GetChannel(channelId, out channel))
          output.Add(channel);
      }

      return output;
    }

    /// <summary>
    /// GET /api/v1/Tv/ChannelAndGroupInfo/[channelName]/Logo
    /// </summary>
    /// <param name="channelName">Name of the Channel</param>
    /// <param name="radio">Is the channel radio or Tv</param>
    /// <param name="maxWidth">Maximum width of the image returned; if it is larger, it will be downscaled</param>
    /// <param name="maxHeight">Maximum height of the image returned; if it is larger, it will be downscaled</param>
    /// <returns>The requested Logo</returns>
    [HttpGet("{channelName}/Logo")]
    public IActionResult Get(string channelName, bool radio = false, int maxWidth = DEFAULT_IMAGE_MAX_WIDTH, int maxHeight = DEFAULT_IMAGE_MAX_HEIGHT)
    {
      var images = ServiceRegistration.Get<IFanArtService>().GetFanArt(radio ? FanArtMediaTypes.ChannelRadio : FanArtMediaTypes.ChannelTv, FanArtTypes.Undefined, channelName, maxWidth, maxHeight, false);
      if (images == null || !images.Any())
        throw new HttpException(HttpStatusCode.NotFound, "No Logo found");
      var bytes = images[0].BinaryData;
      return new FileContentResult(bytes, new MediaTypeHeaderValue(DEFAULT_IMAGE_CONTENT_TYPE_STRING));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Tries to get IChannelGroup object for a given group Id
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns>IChannelGroup object</returns>
    private IChannelGroup GetGroup(int groupId)
    {
      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IList<IChannelGroup> groupList;
      if (channelAndGroupInfo == null || !channelAndGroupInfo.GetChannelGroups(out groupList))
        throw new HttpException(HttpStatusCode.NotFound, "No Groups found");

      IChannelGroup group = groupList.Single(x => x.ChannelGroupId == groupId);

      return group;
    }

    #endregion
  }
}