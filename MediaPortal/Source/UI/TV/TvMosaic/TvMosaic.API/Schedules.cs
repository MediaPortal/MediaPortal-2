///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "schedules", Namespace = "")]
  public class SchedulesRequest
  {
  }

  [DataContract(Name = "remove_schedule", Namespace = "")]
  public class ScheduleRemover
  {
    [DataMember(Name = "schedule_id", EmitDefaultValue = false)]
    public string ScheduleID { get; private set; }

    public ScheduleRemover(string schedule_id)
    {
      ScheduleID = schedule_id;
    }
  }

  [DataContract(Name = "schedule", Namespace = "")]
  public class Schedule
  {
    [DataMember(Name = "schedule_id", EmitDefaultValue = false, Order = 0)]
    public string ScheduleID { get; set; }

    [DataMember(Name = "user_param", EmitDefaultValue = false, Order = 1)]
    public string UserParam { get; set; }

    [DataMember(Name = "force_add", EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public bool IsForceAdd { get; set; }

    [DataMember(Name = "by_epg", EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public ByEpgSchedule ByEpg { get; set; }

    [DataMember(Name = "manual", EmitDefaultValue = false, IsRequired = false, Order = 4)]
    public ManualSchedule Manual { get; set; }

    public Schedule()
    {
      //ByEpg = new ByEpgSchedule(null, null);
      //Manual = new ManualSchedule(null, null, 0, 0, 0);
    }
    public Schedule(ByEpgSchedule by_epg)
        : this(by_epg, null, false)
    {
    }

    public Schedule(ByEpgSchedule by_epg, string user_param)
        : this(by_epg, user_param, false)
    {
    }

    public Schedule(ByEpgSchedule by_epg, string user_param, bool is_force_add)
    {
      ByEpg = by_epg;
      UserParam = user_param;
      IsForceAdd = is_force_add;
    }

    public Schedule(ManualSchedule manual)
        : this(manual, null, false)
    {
    }

    public Schedule(ManualSchedule manual, string user_param)
        : this(manual, user_param, false)
    {
    }

    public Schedule(ManualSchedule manual, string user_param, bool is_force_add)
    {
      Manual = manual;
      UserParam = user_param;
      IsForceAdd = is_force_add;
    }
  }

  [CollectionDataContract(Name = "schedules", Namespace = "")]
  public class Schedules : List<Schedule>
  {
  }


  [DataContract(IsReference = false, Name = "by_epg", Namespace = "")]
  public class ByEpgSchedule
  {
    [DataMember(Name = "channel_id", EmitDefaultValue = false, Order = 0)]
    public string ChannelId { get; set; }

    [DataMember(Name = "program_id", EmitDefaultValue = false, Order = 1)]
    public string ProgramId { get; set; }

    [DataMember(Name = "repeat", EmitDefaultValue = false, IsRequired = false, Order = 2)]
    public bool IsRepeat { get; set; }

    [DataMember(Name = "recordings_to_keep", EmitDefaultValue = false, IsRequired = false, Order = 3)]
    public int RecordingsToKeep { get; set; }

    public ByEpgSchedule()
        : this(null, null)
    {
    }
    public ByEpgSchedule(string channel_id, string program_id)
        : this(channel_id, program_id, false)
    {
    }

    public ByEpgSchedule(string channel_id, string program_id, bool is_repeat)
    {
      ChannelId = channel_id;
      ProgramId = program_id;
      IsRepeat = is_repeat;
    }
  }

  [DataContract(IsReference = false, Name = "manual", Namespace = "")]
  public class ManualSchedule
  {
    public static readonly int DAY_MASK_ONCE = 0;
    public static readonly int DAY_MASK_SUN = 1;
    public static readonly int DAY_MASK_MON = 2;
    public static readonly int DAY_MASK_TUE = 4;
    public static readonly int DAY_MASK_WED = 8;
    public static readonly int DAY_MASK_THU = 16;
    public static readonly int DAY_MASK_FRI = 32;
    public static readonly int DAY_MASK_SAT = 64;
    public static readonly int DAY_MASK_DAILY = 255;

    [DataMember(Name = "channel_id", EmitDefaultValue = false, Order = 0)]
    public string ChannelId { get; set; }

    [DataMember(Name = "title", EmitDefaultValue = false, Order = 1)]
    public string Title { get; set; }

    [DataMember(Name = "start_time", EmitDefaultValue = false, Order = 2)]
    public long StartTime { get; set; }

    [DataMember(Name = "duration", EmitDefaultValue = false, Order = 3)]
    public int Duration { get; set; }

    [DataMember(Name = "day_mask", EmitDefaultValue = false, Order = 4)]
    public int DayMask { get; set; }

    [DataMember(Name = "recordings_to_keep", EmitDefaultValue = false, IsRequired = false, Order = 5)]
    public int RecordingsToKeep { get; set; }

    public ManualSchedule()
    {
    }

    public ManualSchedule(string channel_id, string title, long start_time, int duration, int day_mask)
    {
      ChannelId = channel_id;
      Title = title;
      StartTime = start_time;
      Duration = duration;
      DayMask = day_mask;
    }
  }
}
