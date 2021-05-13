#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using System;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// Model that allows the configuration and creation of a manual schedule.
  /// </summary>
  public class SlimTvRadioManualScheduleModel : SlimTvManualScheduleModelBase
  {
    #region Constants

    public const string MODEL_ID_STR = "005C8C85-C0A6-48A8-83AC-E826AF2DF99D";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string STATE_MANUAL_SCHEDULE_STR = "DF5741CC-3B1A-485C-AFA4-15ADA4FFACFF";
    public static readonly Guid STATE_MANUAL_SCHEDULE = new Guid(STATE_MANUAL_SCHEDULE_STR);

    #endregion

    public SlimTvRadioManualScheduleModel()
    {
      _mediaMode = MediaMode.Radio;
    }

    #region GUI properties/methods

    public async Task CreateSchedule()
    {
      await CreateSchedule(STATE_MANUAL_SCHEDULE);
    }

    public static void Show()
    {
      Show(null, STATE_MANUAL_SCHEDULE);
    }

    public static void Show(IProgram program)
    {
      Show(program, STATE_MANUAL_SCHEDULE);
    }

    #endregion

    #region IWorkflow

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    #endregion
  }
}
