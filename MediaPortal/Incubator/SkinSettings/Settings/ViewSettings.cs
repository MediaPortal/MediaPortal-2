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
using System.Collections.Generic;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.Xml;

namespace SkinSettings.Settings
{
  /// <summary>
  /// <see cref="ViewModeDictionary{TKey,TValue}"/> implements a local type that is valid for XML serialization. It is used only for providing
  /// a type that is defined inside the same assembly as the <see cref="ViewSettings"/> class.
  /// It is a workaround for serialization issues, if the serializing type (MediaPortal.Utilities dictionary) has no reference to the used type
  /// (Media library settings).
  /// </summary>
  public class ViewModeDictionary<TKey, TValue> : SerializableDictionary<TKey, TValue>
  { }

  public class ViewSettings
  {
    private ViewModeDictionary<string, ViewModeDictionary<Guid, LayoutType>> _workflowLayouts;

    [Setting(SettingScope.User)]
    public ViewModeDictionary<string, ViewModeDictionary<Guid, LayoutType>> WorkflowLayouts
    {
      get { return _workflowLayouts ?? (_workflowLayouts = new ViewModeDictionary<string, ViewModeDictionary<Guid, LayoutType>>()); }
      set { _workflowLayouts = value; }
    }
  }
}
