using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  public class TvServerState
  {
    public static readonly Guid STATE_ID = new Guid("2A58935C-3363-4FA1-B48D-1EF0E81F830D");

    public bool IsRecording { get; set; }
    public bool IsTimeshifting { get; set; }
  }
}
