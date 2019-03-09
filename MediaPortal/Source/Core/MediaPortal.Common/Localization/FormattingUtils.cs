#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System;
using MediaPortal.Common.Services.Localization;
using MediaPortal.Common.Settings;

namespace MediaPortal.Common.Localization
{
  /// <summary>
  /// Utility class for formatting several things.
  /// </summary>
  public class FormattingUtils
  {
    /// <summary>
    /// Formats a media duration timespan (i.e. the duration of a media item, the current play time, ...) to a string.
    /// </summary>
    /// <param name="duration">Duration to format.</param>
    /// <returns>String containing the given <paramref name="duration"/>. The returned value will be a string similar to
    /// <c>5:41</c> or <c>5 min, 41 sec</c>, depending on the system setting
    /// <see cref="LocalizationSettings.MediaDurationFormat"/>.</returns>
    public static string FormatMediaDuration(TimeSpan duration)
    {
      LocalizationSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LocalizationSettings>();
      if (duration.Hours > 0)
        return string.Format(settings.MediaDurationHourFormat, (int) duration.TotalHours, duration.Minutes, duration.Seconds);
      return string.Format(settings.MediaDurationFormat, (int) duration.TotalMinutes, duration.Seconds);
    }
  }
}