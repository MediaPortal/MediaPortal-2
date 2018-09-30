#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Client.Notifications
{
  /// <summary>
  /// Base class for a TV notification that contains a channel and schedule.
  /// </summary>
  public abstract class SlimTvRecordingNotificationBase : ISlimTvNotification
  {
    protected ISchedule _schedule;
    protected IChannel _channel;

    public SlimTvRecordingNotificationBase(ISchedule schedule, IChannel channel)
    {
      _schedule = schedule;
      _channel = channel;
    }

    public ISchedule Schedule
    {
      get { return _schedule; }
    }

    public IChannel Channel
    {
      get { return _channel; }
    }

    public abstract string SuperLayerScreenName
    {
      get;
    }
  }

  /// <summary>
  /// Tv notification for a recording started notification. 
  /// </summary>
  public class SlimTvRecordingStartedNotification : SlimTvRecordingNotificationBase
  {
    public const string SUPER_LAYER_SCREEN = "RecordingStartedNotification";

    public SlimTvRecordingStartedNotification(ISchedule schedule, IChannel channel)
      : base(schedule, channel)
    {
    }

    public override string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }

  /// <summary>
  /// Tv notification for a recording ended notification. 
  /// </summary>
  public class SlimTvRecordingEndedNotification : SlimTvRecordingNotificationBase
  {
    public const string SUPER_LAYER_SCREEN = "RecordingEndedNotification";

    public SlimTvRecordingEndedNotification(ISchedule schedule, IChannel channel)
      : base(schedule, channel)
    {
    }

    public override string SuperLayerScreenName
    {
      get { return SUPER_LAYER_SCREEN; }
    }
  }
}
