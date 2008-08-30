#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Collections.Generic;

using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Configuration.Settings
{
  public class SomeMultiSelection : MultipleSelectionList
  {

    #region Constructors

    public SomeMultiSelection()
    {
      base.SetSettingsObject(new SampleSettings());
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      base._items = new List<StringId>(5);
      base._items.Add(new StringId("First item"));
      base._items.Add(new StringId("Second item"));
      base._items.Add(new StringId("Thirth item"));
      base._items.Add(new StringId("Fourth item"));
      base._items.Add(new StringId("Fifth item"));
      base._selected = new List<int>(((SampleSettings)settingsObject).MultiSelection);
    }

    public override void Save(object settingsObject)
    {
      ((SampleSettings)settingsObject).MultiSelection = base._selected.ToArray();
    }

    #endregion

  }
}
