///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System;
using System.Runtime.Serialization;

namespace TvMosaic.API
{

  [DataContract(Name = "timeshift_get_stats", Namespace = "")]
  public class TimeshiftGetStats
  {
    [DataMember(Name = "channel_handle", EmitDefaultValue = false, Order = 1)]
    public long ChannelHandle { get; set; }
  }

  [DataContract(Name = "timeshift_status", Namespace = "")]
  public class TimeshiftStatus
  {
    /// <summary>
    /// Max buffer length in Bytes
    /// </summary>
    [DataMember(Name = "max_buffer_length", EmitDefaultValue = false, Order = 1)]
    public UInt64 MaxBufferLength { get; set; }
    /// <summary>
    /// Current buffer length in Bytes
    /// </summary>
    [DataMember(Name = "buffer_length", EmitDefaultValue = false, Order = 2)]
    public UInt64 BufferLength { get; set; }
    /// <summary>
    /// Current buffer position in Bytes
    /// </summary>
    [DataMember(Name = "cur_pos_bytes", EmitDefaultValue = false, Order = 3)]
    public UInt64 CurrentPositionBytes { get; set; }
    /// <summary>
    /// Current buffer duration in seconds
    /// </summary>
    [DataMember(Name = "buffer_duration", EmitDefaultValue = false, Order = 4)]
    public UInt64 BufferDuration { get; set; }
    /// <summary>
    /// Current buffer position in seconds
    /// </summary>
    [DataMember(Name = "cur_pos_sec", EmitDefaultValue = false, Order = 5)]
    public UInt64 CurrentPositionSeconds { get; set; }
  }

  //  <timeshift_seek>
  //   <channel_handle/> - long mandatory, channel handle
  //   <type/> - long mandatory, type of seek operation: 0 – by bytes, 1 – by time
  //   <offset/> - int64 mandatory, offset in bytes (for seek by bytes) or in seconds(for seek by time). Offset may be negative value and is calculated from a position, given by whence parameter
  //   <whence/> - long mandatory: 0 – offset is calculated from the beginning of the timeshift buffer, 1 – offset is calculated from the current playback position, 2 – offset is calculated from the end of the timeshift buffer
  //  </timeshift_seek>

  [DataContract(Name = "timeshift_seek", Namespace = "")]
  public class TimeshiftSeek
  {
    [DataMember(Name = "channel_handle", EmitDefaultValue = false, Order = 1)]
    public long ChannelHandle { get; set; }

    [DataMember(Name = "type", EmitDefaultValue = false, Order = 2)]
    public long Type { get; set; }

    [DataMember(Name = "offset", EmitDefaultValue = false, Order = 3)]
    public Int64 Offset { get; set; }

    [DataMember(Name = "whence", EmitDefaultValue = false, Order = 4)]
    public long SeekOrigin { get; set; }

  }
}
