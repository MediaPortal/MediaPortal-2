#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.UI.Presentation.Players.ResumeState
{
  /// <summary>
  /// Holds the required information to resume playback based on binary data.
  /// </summary>
  public class BinaryResumeState : ResumeStateBase
  {
    public byte[] ResumeData { get; set; }

    public override void InitFromString(string value)
    {
      ResumeData = Convert.FromBase64String(value);
    }
    public override string ToString()
    {
      return Convert.ToBase64String(ResumeData);
    }
  }
}
