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
  /// Model that allows the configuration and creation of a schedule rule.
  /// </summary>
  public class SlimTvScheduleRuleModel : SlimTvScheduleRuleModelBase
  {
    #region Constants

    public const string MODEL_ID_STR = "CDA8DC76-94DB-4823-9199-C07FA1AF75F5";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string STATE_MANUAL_SCHEDULE_RULE_STR = "FEB92DFE-3D76-41A1-AE89-4B692AE95D3F";
    public static readonly Guid STATE_MANUAL_SCHEDULE_RULE = new Guid(STATE_MANUAL_SCHEDULE_RULE_STR);

    #endregion

    public SlimTvScheduleRuleModel()
    {
      _mediaMode = MediaMode.Tv;
    }

    #region GUI properties/methods

    public async Task CreateSchedule()
    {
      await CreateSchedule(STATE_MANUAL_SCHEDULE_RULE);
    }

    public static void Show()
    {
      Show(STATE_MANUAL_SCHEDULE_RULE);
    }

    public static void Show(IProgram program)
    {
      Show(program, STATE_MANUAL_SCHEDULE_RULE);
    }

    public static void Show(IScheduleRule scheduleRule)
    {
      Show(scheduleRule, STATE_MANUAL_SCHEDULE_RULE);
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
