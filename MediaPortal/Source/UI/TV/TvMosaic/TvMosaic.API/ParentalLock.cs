///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "parental_lock", Namespace = "")]
  public class ParentalLock
  {
    [DataMember(Name = "client_id", EmitDefaultValue = false)]
    public string ClientId { get; private set; }

    [DataMember(Name = "code", EmitDefaultValue = false)]
    public string LockCode { get; set; }

    [DataMember(Name = "is_enable", EmitDefaultValue = false)]
    public bool IsEnable { get; set; }

    public ParentalLock(string client_id)
    {
      IsEnable = true;
    }
  }

  [DataContract(Name = "parental_status", Namespace = "")]
  public class ParentalStatus
  {
    [DataMember(Name = "is_enabled", EmitDefaultValue = false)]
    public bool IsEnabled { get; set; }
  }
}
