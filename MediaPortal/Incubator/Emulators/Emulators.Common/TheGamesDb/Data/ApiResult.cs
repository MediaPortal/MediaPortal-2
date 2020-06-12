using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb.Data
{
  #region Response

  /*
   {
  "code": 200,
  "status": "Success",
  "data": {
    ...
  },
  "remaining_monthly_allowance": 998,
  "extra_allowance": 0,
  "allowance_refresh_timer": 2538381
} 
  */

  #endregion

  [DataContract]
  public class ApiResult<T>
  {
    [DataMember(Name = "code")]
    public int Code { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "data")]
    public T Data { get; set; }

    [DataMember(Name = "remaining_monthly_allowance")]
    public int RemainingMonthlyAllowance { get; set; }

    [DataMember(Name = "extra_allowance")]
    public int ExtraAllowance { get; set; }

    [DataMember(Name = "allowance_refresh_timer")]
    public int AllowanceRefreshTimer { get; set; }
  }
}
