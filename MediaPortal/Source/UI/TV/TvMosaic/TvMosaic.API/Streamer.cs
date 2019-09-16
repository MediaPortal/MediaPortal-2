///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "stream", Namespace = "")]
  public class RequestStream
  {
    public static readonly string ANDROID_TYPE = "rtp";
    public static readonly string IPHONE_TYPE = "hls";
    public static readonly string WINPHONE_TYPE = "asf";
    public static readonly string RAW_HTTP_TYPE = "raw_http";
    public static readonly string RAW_UDP_TYPE = "raw_udp";

    [DataMember(Name = "channel_dvblink_id", EmitDefaultValue = false)]
    public string DvbLinkId { get; private set; }

    [DataMember(Name = "physical_channel_id", EmitDefaultValue = false)]
    public string PhysicalChannelId { get; private set; }

    [DataMember(Name = "source_id", EmitDefaultValue = false)]
    public string SourceName { get; private set; }

    [DataMember(Name = "client_id", EmitDefaultValue = false)]
    public string ClientId { get; private set; }

    [DataMember(Name = "stream_type", EmitDefaultValue = false)]
    public string StreamType { get; private set; }

    [DataMember(Name = "transcoder", EmitDefaultValue = false)]
    public Transcoder Transcoder { get; private set; }

    [DataMember(Name = "server_address", EmitDefaultValue = false)]
    public string ServerAddress { get; private set; }

    [DataMember(Name = "client_address", EmitDefaultValue = false)]
    public string ClientAddress { get; set; }

    [DataMember(Name = "streaming_port", EmitDefaultValue = false)]
    public ushort StreamingPort { get; set; }

    [DataMember(Name = "duration", EmitDefaultValue = false)]
    public int Duration { get; set; }

    //public RequestStream(string server_address, string channel_id, string client_id, string stream_type)
    //    : this(server_address, channel_id, client_id, stream_type, null) 
    //{ 
    //}

    public RequestStream(string server_address, string physical_channel_id, string source_name, string client_id, string stream_type)
        : this(server_address, physical_channel_id, source_name, client_id, stream_type, null)
    {
    }

    public RequestStream(string server_address, string channel_id, string client_id, string stream_type, Transcoder transcoder)
    {
      ServerAddress = server_address;
      DvbLinkId = channel_id;
      ClientId = client_id;
      StreamType = stream_type;
      Transcoder = transcoder;
    }

    public RequestStream(string server_address, string physical_channel_id, string source_name, string client_id, string stream_type, Transcoder transcoder)
    {
      ServerAddress = server_address;
      PhysicalChannelId = physical_channel_id;
      SourceName = source_name;
      ClientId = client_id;
      StreamType = stream_type;
      Transcoder = transcoder;
    }
  }

  [DataContract(Name = "stream", Namespace = "")]
  public class Streamer
  {
    [DataMember(Name = "url", EmitDefaultValue = false, Order = 0)]
    public string Url { get; set; }

    [DataMember(Name = "channel_handle", EmitDefaultValue = false, Order = 1)]
    public long ChannelHandle { get; set; }
  }

  [DataContract(Name = "stop_stream", Namespace = "")]
  public class StopStream
  {
    [DataMember(Name = "channel_handle", EmitDefaultValue = false)]
    public long ChannelHandle { get; private set; }

    [DataMember(Name = "client_id", EmitDefaultValue = false)]
    public string ClientId { get; private set; }

    public StopStream(long channel_handle)
    {
      ChannelHandle = channel_handle;
    }

    public StopStream(string client_id)
    {
      ClientId = client_id;
    }
  }
}
