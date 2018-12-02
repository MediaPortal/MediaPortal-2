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

using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Groups
{
  public class HomeMenuGroup
  {
    public HomeMenuGroup()
    {
      Actions = new List<HomeMenuAction>();
    }

    public HomeMenuGroup(string displayName, Guid id)
      : this()
    {
      Id = id;
      DisplayName = LocalizationHelper.CreateResourceString(displayName).Evaluate();
    }

    public string DisplayName { get; set; }
    public Guid Id { get; set; }
    public List<HomeMenuAction> Actions { get; set; }
  }
}
