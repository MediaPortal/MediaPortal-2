#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Linq;
using System.Collections.Generic;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Providers.Helpers
{
  public class ProgramCacheKey
  {
    public int ChannelId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
  }

  public class ProgramNowNextValue
  {
    public IProgram ProgramNow { get; set; }
    public IProgram ProgramNext { get; set; }
  }

  public class ProgramCache : Dictionary<ProgramCacheKey, ProgramNowNextValue>
  {
    private readonly object _syncObj = new object();

    public bool TryAdd(IChannel channel, IProgram programNow, IProgram programNext)
    {
      if (channel == null || programNow == null)
        return false;

      ProgramCacheKey key = new ProgramCacheKey { ChannelId = channel.ChannelId, StartTime = programNow.StartTime, EndTime = programNow.EndTime };
      lock (_syncObj)
      {
        if (ContainsKey(key))
          return false;

        ProgramNowNextValue programs = new ProgramNowNextValue { ProgramNow = programNow, ProgramNext = programNext };
        Add(key, programs);
      }
      return true;
    }

    public bool TryGetPrograms(IChannel channel, out ProgramNowNextValue programs)
    {
      return TryGetProgramsByTime(channel,  DateTime.Now, out programs);
    }

    public bool TryGetProgramsByTime(IChannel channel, DateTime time, out ProgramNowNextValue programs)
    {
      lock (_syncObj)
      {
        ProgramCacheKey key = Keys.FirstOrDefault(k => k.ChannelId == channel.ChannelId && k.StartTime <= time && k.EndTime > time);
        if (key != null)
        {
          programs = this[key];
          return true;
        }
      }
      programs = null;
      return false;
    }

    public void ClearCache(IChannel channel)
    {
      DateTime now = DateTime.Now;
      lock (_syncObj)
      {
        IEnumerable<ProgramCacheKey> keysToRemove = Keys.Where(k => k.ChannelId == channel.ChannelId && (k.StartTime >= now && k.EndTime <= now));
        foreach (ProgramCacheKey key in keysToRemove)
          Remove(key);
      }
    }
  }
}
