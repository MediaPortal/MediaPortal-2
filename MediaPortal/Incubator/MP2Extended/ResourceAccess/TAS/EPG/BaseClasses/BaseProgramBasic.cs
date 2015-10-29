using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses
{
  class BaseProgramBasic
  {
    internal WebProgramBasic ProgramBasic(IProgram program)
    {
      if (program == null)
        return new WebProgramBasic();

      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;

      WebProgramBasic webProgramBasic = new WebProgramBasic
      {
        Description = program.Description,
        ChannelId = program.ChannelId,
        StartTime = program.StartTime,
        EndTime = program.EndTime,
        Title = program.Title,
        Id = program.ProgramId,
        DurationInMinutes = Convert.ToInt32(program.EndTime.Subtract(program.StartTime).TotalMinutes),
        IsScheduled = recordingStatus.IsScheduled,
      };

      return webProgramBasic;
    }
  }
}
