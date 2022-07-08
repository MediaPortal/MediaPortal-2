///
/// Copyright(c) 2007-2012 DVBLogic (info@dvblogic.com)    
/// All rights reserved                                    
///

using System.Runtime.Serialization;

namespace TvMosaic.API
{
  [DataContract(Name = "response", Namespace = "")]
  public class Response
  {
    [DataMember(Name = "status_code", EmitDefaultValue = false)]
    public int Status { get; set; }

    [DataMember(Name = "xml_result", EmitDefaultValue = false)]
    public string Result { get; set; }
  }
}
