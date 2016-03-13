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
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers.Tv
{
  /// <summary>
  /// AspNet MVC Controller for Tv Program Information
  /// </summary>
  [Route("v1/Tv/[Controller]")]
  public class ProgramInfoController : Controller
  {
    #region Const

    const int CACHE_EXPIRATION_PERIOD = 5; // in Minutes

    #endregion

    #region Private fields

    private readonly ILogger _logger;
    private readonly IMemoryCache _cache;
    private static int _cacheVersion = 0; // TODO: once there is the possibility to clear the cache, remove this
    private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(relative: TimeSpan.FromMinutes(CACHE_EXPIRATION_PERIOD));

    #endregion

    #region Constructor

    public ProgramInfoController(ILoggerFactory loggerFactory, IMemoryCache cache)
    {
      _logger = loggerFactory.CreateLogger<ProgramInfoController>();
      _cache = cache;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/NowNextProgramByChannel
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <returns>Tries to get the current and next program for the given <paramref name="channelId"/></returns>
    [HttpGet("NowNextProgramByChannel/{channelId}")]
    public IDictionary<int, List<IProgram>> NowNextProgramByChannel(int channelId)
    {
      TvHelper.TvAvailable();

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IChannel channel = GetChannel(channelId);

      IProgram programNow;
      IProgram programNext;
      if (programInfo == null || !programInfo.GetNowNextProgram(channel, out programNow, out programNext))
        throw new HttpException(HttpStatusCode.NotFound, "No Programs found");

      IDictionary<int, List<IProgram>> programList = new Dictionary<int, List<IProgram>>
      {
        {channelId, new List<IProgram>{programNow, programNext}}
      };

      return programList;
    }

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/NowNextProgramByChannelGroup
    /// </summary>
    /// <param name="channelGroupId">Channel Group ID</param>
    /// <returns>Tries to get the current and next program for all channels of the the given <paramref name="channelGroupId"/></returns>
    [HttpGet("NowNextProgramByChannelGroup/{channelGroupId}")]
    public IDictionary<int, List<IProgram>> NowNextProgramByChannelGroup(int channelGroupId)
    {
      TvHelper.TvAvailable();

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IChannelGroup group = GetGroup(channelGroupId);

      IDictionary<int, IProgram[]> programNowNext;
      if (programInfo == null || !programInfo.GetNowAndNextForChannelGroup(group, out programNowNext))
        throw new HttpException(HttpStatusCode.NotFound, "No Programs found");

      IDictionary<int, List<IProgram>> output = programNowNext.ToDictionary(item => item.Key, item => item.Value.ToList());

      return output;
    }

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/ProgramsByChannel
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// /// <param name="from">Time from</param>
    /// /// <param name="to">Time to</param>
    /// <returns>Tries to get a list of programs for the given <paramref name="channelId"/> and time range.</returns>
    [HttpGet("ProgramsByChannel/{channelId}/{from}/{to}")]
    public IDictionary<int, List<IProgram>> ProgramsByChannel(int channelId, DateTime from, DateTime to)
    {
      TvHelper.TvAvailable();
      
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IChannel channel = GetChannel(channelId);

      IList<IProgram> programList;
      if (programInfo == null || !programInfo.GetPrograms(channel, from, to, out programList))
        throw new HttpException(HttpStatusCode.NotFound, "No Programs found");

      IDictionary<int, List<IProgram>> output = ProgramListToDictionary(programList);

      return output;
    }

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/ProgramsByTitle
    /// </summary>
    /// <param name="title">Program title</param>
    /// /// <param name="from">Time from</param>
    /// /// <param name="to">Time to</param>
    /// <returns>Tries to get a list of programs for the given <paramref name="title"/> and time range.</returns>
    [HttpGet("ProgramsByTitle/{channelId}/{from}/{to}")]
    public IDictionary<int, List<IProgram>> ProgramsByTitle(string title, DateTime from, DateTime to)
    {
      TvHelper.TvAvailable();

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IList<IProgram> programList;
      if (programInfo == null || !programInfo.GetPrograms(title, from, to, out programList))
        throw new HttpException(HttpStatusCode.NotFound, "No Programs found");

      IDictionary<int, List<IProgram>> output = ProgramListToDictionary(programList);

      return output;
    }

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/ProgramsByGroup
    /// </summary>
    /// <param name="channelGroupId">Channel Group ID</param>
    /// /// <param name="from">Time from</param>
    /// /// <param name="to">Time to</param>
    /// <returns>Tries to get a list of programs for all channels of the given <paramref name="channelGroupId"/> and time range.</returns>
    [HttpGet("ProgramsByGroup/{channelGroupId}/{from}/{to}")]
    public IDictionary<int, List<IProgram>> ProgramsByGroup(int channelGroupId, DateTime from, DateTime to)
    {
      TvHelper.TvAvailable();

      IDictionary<int, List<IProgram>> output;
      if (_cache.TryGetValue($"ProgramsByGroup_{channelGroupId}_{from}_{to}_{_cacheVersion}", out output))
      {
        _logger.LogInformation("Returned from Cache");
        return output;
      }

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IChannelGroup group = GetGroup(channelGroupId);

      IList<IProgram> programList;
      if (programInfo == null || !programInfo.GetProgramsGroup(group, from, to, out programList))
        throw new HttpException(HttpStatusCode.NotFound, "No Programs found");

      output = ProgramListToDictionary(programList);

      _cache.Set($"ProgramsByGroup_{channelGroupId}_{from}_{to}_{_cacheVersion}", output, _memoryCacheEntryOptions);

      return output;
    }

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/ScheduledProgramsByChannel
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <returns>Tries to get a list of programs for the given <paramref name="channelId"/></returns>
    [HttpGet("ScheduledProgramsByChannel/{channelId}")]
    public IDictionary<int, List<IProgram>> ScheduledProgramsByChannel(int channelId)
    {
      TvHelper.TvAvailable();

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IChannel channel = GetChannel(channelId);

      IList<IProgram> programList;
      if (programInfo == null || !programInfo.GetScheduledPrograms(channel, out programList))
        throw new HttpException(HttpStatusCode.NotFound, "No Programs found");

      IDictionary<int, List<IProgram>> output = ProgramListToDictionary(programList);

      return output;
    }

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/ChannelByProgram
    /// </summary>
    /// <param name="programId">Program ID</param>
    /// <returns>Tries to get the Channel for the given <paramref name="programId"/></returns>
    [HttpGet("ChannelByProgram/{programId}")]
    public IList<IChannel> ChannelByProgram(int programId)
    {
      TvHelper.TvAvailable();

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IProgram program;
      if (programInfo == null || !programInfo.GetProgram(programId, out program))
        throw new HttpException(HttpStatusCode.NotFound, "No Program found");

      IChannel channel;
      if (!programInfo.GetChannel(program, out channel))
        throw new HttpException(HttpStatusCode.NotFound, "No Channel found");

      return new List<IChannel> { channel };
    }

    /// <summary>
    /// GET /api/v1/Tv/ProgramInfo/Program
    /// </summary>
    /// <param name="programId">Program ID</param>
    /// <returns>Tries to get the Program for the given <paramref name="programId"/></returns>
    [HttpGet("Program/{programId}")]
    public IList<IProgram> Program(int programId)
    {
      TvHelper.TvAvailable();

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IProgram program;
      if (programInfo == null || !programInfo.GetProgram(programId, out program))
        throw new HttpException(HttpStatusCode.NotFound, "No Program found");

      return new List<IProgram> { program };
    }

    #endregion

    #region Public Cache Methods

    /// <summary>
    /// Get called from other Controllers to clear the cache e.g. if a new Schedule was added
    /// </summary>
    public static void ClearCache()
    {
      _cacheVersion++;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Converts the Program List to a Dictionary with the channel Id as Key and the programs for that Channel as Value
    /// </summary>
    /// <param name="programList"></param>
    /// <returns>Dictionary with Channel Id as Key and Programs as Value</returns>
    private IDictionary<int, List<IProgram>> ProgramListToDictionary(IList<IProgram> programList)
    {
      IDictionary<int, List<IProgram>> output = new Dictionary<int, List<IProgram>>();
      foreach (var program in programList)
      {
        if (output.ContainsKey(program.ChannelId))
          output[program.ChannelId].Add(program);
        else
          output.Add(program.ChannelId, new List<IProgram>
          {
            program
          });
      }

      return output;
    }

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

    /// <summary>
    /// Tries to get IChannel object for a given channel Id
    /// </summary>
    /// <param name="channelId"></param>
    /// <returns>IChannel object</returns>
    private IChannel GetChannel(int channelId)
    {
      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      IChannel channel;
      if (channelAndGroupInfo == null || !channelAndGroupInfo.GetChannel(channelId, out channel))
        throw new HttpException(HttpStatusCode.NotFound, "No Channel found");

      return channel;
    }

    #endregion
  }
}