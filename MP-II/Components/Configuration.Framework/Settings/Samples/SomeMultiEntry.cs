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

namespace MediaPortal.Configuration.Settings
{
  class SomeMultiEntry : MultipleEntryList
  {

    #region Constructors

    public SomeMultiEntry()
    {
      base.SetSettingsObject(new SampleSettings());
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      SampleSettings settings = (SampleSettings)settingsObject;
      List<string> lines = new List<string>(settings.MultiEntry.Length);
      lines.AddRange(settings.MultiEntry);
      base._lines = lines;
    }

    public override void Save(object settingsObject)
    {
      SampleSettings settings = (SampleSettings)settingsObject;
      settings.MultiEntry = new string[base._lines.Count];
      for (int i = 0; i < settings.MultiEntry.Length; i++)
        settings.MultiEntry[i] = base._lines[i];
    }

    #endregion

  }
}
