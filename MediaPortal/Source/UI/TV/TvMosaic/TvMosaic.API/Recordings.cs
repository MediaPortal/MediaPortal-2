using System.Collections.Generic;
using System.Runtime.Serialization;
///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///


namespace TvMosaic.API
{
  [DataContract(Name = "recordings", Namespace = "")]
  public class RecordingsRequest
  {
  }

  [DataContract(Name = "remove_recording", Namespace = "")]
  public class RecordingRemover
  {
    [DataMember(Name = "recording_id", EmitDefaultValue = false)]
    public string RecordingID { get; private set; }

    public RecordingRemover(string recording_id)
    {
      RecordingID = recording_id;
    }
  }

  [DataContract(Name = "recording", Namespace = "")]
  public class Recording
  {
    public Recording()
    {
      Program = new Program();
    }

    [DataMember(Name = "recording_id", EmitDefaultValue = false, Order = 0)]
    public string RecordingId { get; set; }

    [DataMember(Name = "schedule_id", EmitDefaultValue = false, Order = 1)]
    public string ScheduleId { get; set; }

    [DataMember(Name = "channel_id", EmitDefaultValue = false, Order = 2)]
    public string ChannelId { get; set; }

    [DataMember(Name = "is_conflict", EmitDefaultValue = false, Order = 3)]
    public bool IsConflict { get; set; }

    [DataMember(Name = "program", EmitDefaultValue = false, Order = 4)]
    public Program Program { get; set; }
  }

  [CollectionDataContract(Name = "recordings", Namespace = "")]
  public class Recordings : List<Recording>
  {
  }
}
