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

namespace MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos
{
  /// <summary>
  /// Usage:
  /// If PossibleValues.Length == 0 && IsBool == false ? Value is a simple string
  /// If PossibleValues.Length != 0 ? The value must be one of these options
  /// If IsBool == true ? the value must be true or false
  /// </summary>
  public class WebOnlineVideosSiteSetting
  {
    public string Id { get; set; }
    public string SiteId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Value { get; set; }
    public string[] PossibleValues { get; set; }
    public bool IsBool { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
