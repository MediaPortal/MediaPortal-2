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
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using System;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{

  public interface IProgramInfoAsync
  {
    /// <summary>
    /// Tries to get the current and next program for the given <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">Channel</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if programs could be found.
    /// <see cref="AsyncResult{T}.Result"/> an array of now/next programs.
    /// </returns>
    Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel);

    /// <summary>
    /// Tries to get the current and next program for all channels of the the given <paramref name="channelGroup"/>.
    /// </summary>
    /// <param name="channelGroup">Channel group</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if programs could be found.
    /// <see cref="AsyncResult{T}.Result"/> programs.
    /// </returns>
    Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup);

    /// <summary>
    /// Tries to get a list of programs for the given <paramref name="channel"/> and time range.
    /// </summary>
    /// <param name="channel">Channel</param>
    /// <param name="from">Time from</param>
    /// <param name="to">Time to</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one program could be found.
    /// <see cref="AsyncResult{T}.Result"/> programs.
    /// </returns>
    Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime from, DateTime to);

    /// <summary>
    /// Tries to get a list of programs for the given <paramref name="title"/> and time range.
    /// </summary>
    /// <param name="title">Title</param>
    /// <param name="from">Time from</param>
    /// <param name="to">Time to</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one program could be found.
    /// <see cref="AsyncResult{T}.Result"/> programs.
    /// </returns>
    Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime from, DateTime to);

    /// <summary>
    /// Tries to get a list of programs for the given <paramref name="channelGroup"/> and time range.
    /// </summary>
    /// <param name="channelGroup">Channel group</param>
    /// <param name="from">Time from</param>
    /// <param name="to">Time to</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one program could be found.
    /// <see cref="AsyncResult{T}.Result"/> programs.
    /// </returns>
    Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to);

    /// <summary>
    /// Gets a channel from an IProgram.
    /// </summary>
    /// <param name="program">Program.</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one program could be found.
    /// <see cref="AsyncResult{T}.Result"/> Channel.
    /// </returns>
    Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program);

    /// <summary>
    /// Gets a program by its <see cref="IProgram.ProgramId"/>.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if the program could be found.
    /// <see cref="AsyncResult{T}.Result"/> Program.
    /// </returns>
    Task<AsyncResult<IProgram>> GetProgramAsync(int programId);
  }
}
