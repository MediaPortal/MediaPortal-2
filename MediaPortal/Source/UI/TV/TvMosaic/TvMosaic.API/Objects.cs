///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///


using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  public enum ObjectType
  {
    OBJECT_UNKNOWN = -1,
    OBJECT_container = 0,
    OBJECT_ITEM = 1
  }

  public enum ItemType
  {
    ITEM_UNKNOWN = -1,
    ITEM_RECORDED_TV = 0,
    ITEM_VIDEO = 1,
    ITEM_AUDIO = 2,
    ITEM_IMAGE = 3
  }

  public enum RecordingType
  {
    RTVS_IN_PROGRESS = 0,
    RTVS_ERROR = 1,
    RTVS_FORCED_TO_COMPLETION = 2,
    RTVS_COMPLETED = 3
  }

  [DataContract(Name = "object_requester", Namespace = "")]
  public class ObjectRequester
  {
    [DataMember(Name = "object_id", Order = 1)]
    public string ObjectID { get; set; }

    [DataMember(Name = "object_type", EmitDefaultValue = false, Order = 2)]
    public int ObjectType { get; set; }

    [DataMember(Name = "item_type", EmitDefaultValue = false, Order = 3)]
    public int ItemType { get; set; }

    [DataMember(Name = "start_position", EmitDefaultValue = false, Order = 4)]
    public int StartPosition { get; set; }

    [DataMember(Name = "requested_count", EmitDefaultValue = false, Order = 6)]
    public int RequestedCount { get; set; }

    [DataMember(Name = "children_request", EmitDefaultValue = false, Order = 7)]
    public bool ChildrenRequest { get; set; }

    [DataMember(Name = "server_address", EmitDefaultValue = false, Order = 8)]
    public string ServerAddress { get; set; }
  }

  [DataContract(Name = "container", Namespace = "")]
  public class Container
  {

    [DataMember(Name = "object_id", EmitDefaultValue = false, Order = 11)]
    public string ObjectID { get; set; }

    [DataMember(Name = "parent_id", EmitDefaultValue = false, Order = 12)]
    public string ParentID { get; set; }

    [DataMember(Name = "name", EmitDefaultValue = false, Order = 13)]
    public string Name { get; set; }

    [DataMember(Name = "description", EmitDefaultValue = false, Order = 14)]
    public string Description { get; set; }

    [DataMember(Name = "logo", EmitDefaultValue = false, Order = 15)]
    public string Logo { get; set; }

    [DataMember(Name = "source_id", EmitDefaultValue = false, Order = 16)]
    public string SourceId { get; set; }

    [DataMember(Name = "container_type", EmitDefaultValue = false, Order = 17)]
    public int ContainerType { get; set; }

    [DataMember(Name = "content_type", EmitDefaultValue = false, Order = 18)]
    public int ContentType { get; set; }

    [DataMember(Name = "total_count", EmitDefaultValue = false, Order = 19)]
    public int TotalCount { get; set; }


  }

  [DataContract(Name = "video_info", Namespace = "")]
  public class VideoInfo
  {
    [DataMember(Name = "name", EmitDefaultValue = false, Order = 50)]
    public string Name { get; set; }

    [DataMember(Name = "short_desc", EmitDefaultValue = false, Order = 51)]
    public string ShortDesc { get; set; }

    [DataMember(Name = "subname", EmitDefaultValue = false, Order = 52)]
    public string SubName { get; set; }

    [DataMember(Name = "categories", EmitDefaultValue = false, Order = 53)]
    public string Categories { get; set; }

    [DataMember(Name = "image", EmitDefaultValue = false, Order = 54)]
    public string image { get; set; }

    [DataMember(Name = "start_time", EmitDefaultValue = false, Order = 55)]
    public long StartTime { get; set; }

    [DataMember(Name = "duration", EmitDefaultValue = false, Order = 56)]
    public long Duration { get; set; }

    [DataMember(Name = "episode_num", EmitDefaultValue = false, Order = 57)]
    public int EpisodeNum { get; set; }

    [DataMember(Name = "season_num", EmitDefaultValue = false, Order = 58)]
    public int SeasonNum { get; set; }

    [DataMember(Name = "cat_movie", EmitDefaultValue = false, Order = 59)]
    public bool CatMovie { get; set; }

    [DataMember(Name = "cat_news", EmitDefaultValue = false, Order = 60)]
    public bool CatNews { get; set; }

    [DataMember(Name = "cat_serial", EmitDefaultValue = false, Order = 61)]
    public bool CatSerial { get; set; }

    [DataMember(Name = "cat_special", EmitDefaultValue = false, Order = 62)]
    public bool CatSpecial { get; set; }

  }

  [DataContract(Name = "recorded_tv", Namespace = "")]
  public class RecordedTV
  {
    [DataMember(Name = "object_id", EmitDefaultValue = false, Order = 30)]
    public string ObjectID { get; set; }

    [DataMember(Name = "parent_id", EmitDefaultValue = false, Order = 31)]
    public string ParentID { get; set; }

    [DataMember(Name = "url", EmitDefaultValue = false, Order = 32)]
    public string Url { get; set; }

    [DataMember(Name = "thumbnail", EmitDefaultValue = false, Order = 33)]
    public string Thumbnail { get; set; }

    [DataMember(Name = "can_be_deleted", EmitDefaultValue = false, Order = 34)]
    public bool CanBeDeleted { get; set; }

    [DataMember(Name = "size", EmitDefaultValue = false, Order = 35)]
    public long Size { get; set; }

    [DataMember(Name = "creation_time", EmitDefaultValue = false, Order = 36)]
    public long CreationTime { get; set; }

    [DataMember(Name = "channel_id", EmitDefaultValue = false, Order = 37)]
    public string ChannelId { get; set; }

    [DataMember(Name = "channel_name", EmitDefaultValue = false, Order = 38)]
    public string ChannelName { get; set; }

    [DataMember(Name = "channel_number", EmitDefaultValue = false, Order = 39)]
    public int ChannelNumber { get; set; }

    [DataMember(Name = "channel_subnumber", EmitDefaultValue = false, Order = 40)]
    public int ChannelSubNumber { get; set; }

    [DataMember(Name = "state", EmitDefaultValue = false, Order = 41)]
    public int State { get; set; }

    [DataMember(Name = "schedule_id", EmitDefaultValue = false, Order = 42)]
    public long ScheduleId { get; set; }

    [DataMember(Name = "schedule_name", EmitDefaultValue = false, Order = 43)]
    public string ScheduleName { get; set; }

    [DataMember(Name = "schedule_series", EmitDefaultValue = false, Order = 44)]
    public bool ScheduleSeries { get; set; }

    [DataMember(Name = "video_info", EmitDefaultValue = false, Order = 45)]
    public VideoInfo VideoInfo { get; set; }
  }

  [CollectionDataContract(Name = "containers", Namespace = "")]
  public class Containers : List<Container>
  {
  }

  [CollectionDataContract(Name = "items", Namespace = "")]
  public class Items : List<RecordedTV>
  {
  }

  [DataContract(Name = "object", Namespace = "")]
  public class ObjectResponse
  {
    [DataMember(Name = "containers", EmitDefaultValue = false, Order = 10)]
    public Containers Containers { get; set; }

    [DataMember(Name = "items", EmitDefaultValue = false, Order = 29)]
    public Items Items { get; set; }

    [DataMember(Name = "actual_count", EmitDefaultValue = false, Order = 98)]
    public int ActualCount { get; set; }

    [DataMember(Name = "total_count", EmitDefaultValue = false, Order = 99)]
    public int TotalCount { get; set; }
  }

  [DataContract(Name = "object_remover", Namespace = "")]
  public class ObjectRemover
  {
    [DataMember(Name = "object_id", EmitDefaultValue = false)]
    public string ObjectID { get; private set; }

    public ObjectRemover(string recording_id)
    {
      ObjectID = recording_id;
    }
  }
}
