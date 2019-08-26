///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "epg_searcher", Namespace = "")]
  public class EpgSearcher
  {
    public const long EPG_INVALID_TIME = -1;

    [DataMember(Name = "channels_ids", EmitDefaultValue = false)]
    public ChannelIDList ChannelsIDs { get; private set; }

    [DataMember(Name = "program_id", EmitDefaultValue = false)]
    public string ProgramID { get; private set; }

    [DataMember(Name = "keywords", EmitDefaultValue = false)]
    public string Keyword { get; private set; }

    [DataMember(Name = "start_time", EmitDefaultValue = false)]
    public long StartTime { get; private set; }

    [DataMember(Name = "end_time", EmitDefaultValue = false)]
    public long EndTime { get; private set; }

    [DataMember(Name = "epg_short", EmitDefaultValue = false)]
    public bool IsEpgShort { get; private set; }

    public EpgSearcher(bool is_epg_short = false, long start_time = EPG_INVALID_TIME, long end_time = EPG_INVALID_TIME)
    {
      IsEpgShort = is_epg_short;
      StartTime = start_time;
      EndTime = end_time;
    }

    public EpgSearcher(string keywords, bool is_epg_short = false, long start_time = EPG_INVALID_TIME, long end_time = EPG_INVALID_TIME)
    {
      Keyword = keywords;
      IsEpgShort = is_epg_short;
      StartTime = start_time;
      EndTime = end_time;
    }

    public EpgSearcher(ChannelIDList channels_ids, bool is_epg_short = false, long start_time = EPG_INVALID_TIME, long end_time = EPG_INVALID_TIME)
    {
      ChannelsIDs = channels_ids;
      IsEpgShort = is_epg_short;
      StartTime = start_time;
      EndTime = end_time;
    }

    public EpgSearcher(ChannelIDList channels_ids, string keywords, bool is_epg_short = false, long start_time = EPG_INVALID_TIME, long end_time = EPG_INVALID_TIME)
    {
      ChannelsIDs = channels_ids;
      Keyword = keywords;
      IsEpgShort = is_epg_short;
      StartTime = start_time;
      EndTime = end_time;
    }

    public EpgSearcher(string channel_id, string program_id)
    {
      ChannelsIDs = new ChannelIDList(channel_id);
      ProgramID = program_id;
    }
  }

  [CollectionDataContract(Name = "channels_ids", ItemName = "channel_id", Namespace = "")]
  public class ChannelIDList : List<string>
  {
    public ChannelIDList() { }

    public ChannelIDList(string id)
    {
      this.Add(id);
    }

    public ChannelIDList(List<string> ids)
    {
      this.AddRange(ids);
    }
  }

  [DataContract(Name = "channel_epg", Namespace = "")]
  public class ChannelIdWithPrograms
  {
    [DataMember(Name = "channel_id")]
    public string ChannelId { get; set; }

    [DataMember(Name = "dvblink_epg")]
    public Programs Programs { get; set; }
  }

  [CollectionDataContract(Name = "epg_searcher", Namespace = "")]
  public class ChannelsIdWithPrograms : List<ChannelIdWithPrograms>
  {
  }
}
