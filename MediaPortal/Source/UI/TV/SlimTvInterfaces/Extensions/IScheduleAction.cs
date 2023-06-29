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

namespace MediaPortal.Plugins.SlimTv.Interfaces.Extensions
{
  public delegate bool ScheduleActionDelegate(ISchedule schedule, MediaMode mediaMode);

  /// <summary>
  /// Extension interface to add actions for <see cref="IProgram"/>s. Plugins can implement this interface and register the class in
  /// <c>plugin.xml</c> <see cref="SlimTvExtensionBuilder.SLIMTVEXTENSIONPATH"/> path.
  /// </summary>
  public interface IScheduleAction
  {
    /// <summary>
    /// Checks if this action is available for the given <paramref name="schedule"/>.
    /// </summary>
    /// <param name="schedule">Schedule</param>
    /// <param name="mediaMode">Media mode</param>
    /// <returns><c>true</c> if available</returns>
    bool IsAvailable(ISchedule schedule, MediaMode mediaMode);

    /// <summary>
    /// Gets the action to be executed for the selected schedule (<seealso cref="ScheduleActionDelegate"/>).
    /// </summary>
    ScheduleActionDelegate ScheduleAction { get; }
  }
}
