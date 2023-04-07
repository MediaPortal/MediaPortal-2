///
/// Added by ric@rts.co.at
/// 

using System;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "get_streaming_capabilities", Namespace = "")]
  public class CapabilitiesRequest
  {
  }

  [DataContract(Name = "streaming_caps", Namespace = "")]
  public class StreamingCapabilities
  {
    [Flags]
    public enum SupportedProtocol
    {
      NONE = 0, HTTP = 1, UDP = 2, RTSP = 4,
      ASF = 8, HLS = 16, WEBM = 32, MP4 = 64, ALL = 65535
    }

    [Flags]
    public enum SupportedTranscoder
    {
      NONE = 0, WMV = 1, WMA = 2, H264 = 4,
      AAC = 8, RAW = 16, ALL = 65535
    }

    [DataMember(Name = "protocols", EmitDefaultValue = false, Order = 0)]
    public int Protocols { get; set; }

    [DataMember(Name = "transcoders", EmitDefaultValue = false, Order = 1)]
    public int Transcoders { get; set; }

    [DataMember(Name = "pb_transcoders", EmitDefaultValue = false, Order = 2)]
    public int PbTranscoders { get; set; }

    [DataMember(Name = "pb_protocols", EmitDefaultValue = false, Order = 3)]
    public int PbProtocols { get; set; }

    [DataMember(Name = "addressees", EmitDefaultValue = false, Order = 4)]
    public object Addresses { get; set; }

    [DataMember(Name = "can_record", EmitDefaultValue = false, Order = 5)]
    public bool CanRecord { get; set; }

    [DataMember(Name = "supports_timeshift", EmitDefaultValue = false, Order = 6)]
    public bool SupportsTimeshift { get; set; }

    [DataMember(Name = "timeshift_version", EmitDefaultValue = false, Order = 7)]
    public int TimeshiftVersion { get; set; }

    [DataMember(Name = "device_management", EmitDefaultValue = false, Order = 8)]
    public bool DeviceManagement { get; set; }

    public SupportedProtocol SupProtocols
    {
      get { return (SupportedProtocol)(Protocols % 100); }
    }
    public SupportedProtocol SupPbProtocols
    {
      get { return (SupportedProtocol)(PbProtocols % 100); }
    }
    public SupportedTranscoder SupTranscoders
    {
      get { return (SupportedTranscoder)(Transcoders % 100); }
    }
    public SupportedTranscoder SupPbTranscoders
    {
      get { return (SupportedTranscoder)(PbTranscoders % 100); }
    }

  }
}
