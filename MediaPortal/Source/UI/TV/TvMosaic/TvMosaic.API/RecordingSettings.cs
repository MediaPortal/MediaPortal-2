using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///


namespace TvMosaic.API
{
  [DataContract(Name = "get_recording_settings", Namespace = "")]
  public class RecordingSettingsRequest
  {
  }

  [DataContract(Name = "recording_settings", Namespace = "")]
  public class RecordingSettings
  {
    [DataMember(Name = "recording_path", EmitDefaultValue = false, Order = 0)]
    public string RecordingPath { get; set; }

    [DataMember(Name = "before_margin", EmitDefaultValue = false, Order = 1)]
    public int BeforeMargin { get; set; }

    [DataMember(Name = "after_margin", EmitDefaultValue = false, Order = 2)]
    public int AfterMargin { get; set; }

    [DataMember(Name = "total_space", EmitDefaultValue = false, Order = 3)]
    public int TotalSpace { get; set; }

    [DataMember(Name = "avail_space", EmitDefaultValue = false, Order = 4)]
    public int AvailSpace { get; set; }

    [DataMember(Name = "check_deleted", EmitDefaultValue = false, Order = 5)]
    public bool CheckDeleted { get; set; }

    [DataMember(Name = "ds_auto_mode", EmitDefaultValue = false, Order = 6)]
    public bool DsAutoMode { get; set; }

    [DataMember(Name = "ds_man_value", EmitDefaultValue = false, Order = 7)]
    public int DsManValue { get; set; }

    [DataMember(Name = "auto_delete", EmitDefaultValue = false, Order = 8)]
    public bool AutoDelete { get; set; }

    [DataMember(Name = "new_only_algo_type", EmitDefaultValue = false, Order = 9)]
    public bool NewOnlyAlgoType { get; set; }

    [DataMember(Name = "new_only_default_value", EmitDefaultValue = false, Order = 10)]
    public bool NewOnlyDefaultValue { get; set; }

    [DataMember(Name = "filename_pattern", EmitDefaultValue = false, Order = 11)]
    public string FilenamePattern { get; set; }
  }
}
