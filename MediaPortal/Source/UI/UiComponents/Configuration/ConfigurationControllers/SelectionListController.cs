#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Configuration;
using MediaPortal.UI.Presentation.DataObjects;

namespace UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for all decendants of <see cref="ConfigItemList"/>.
  /// </summary>
  public abstract class SelectionListController : DialogConfigurationController
  {
    #region Protected fields

    protected ItemsList _items;

    #endregion

    protected SelectionListController()
    {
      _items = new ItemsList();
    }

    protected abstract void UpdateItemsList();

    protected override void SettingChanged()
    {
      base.SettingChanged();
      UpdateItemsList();
    }

    public ItemsList Items
    {
      get { return _items; }
    }
  }
}
