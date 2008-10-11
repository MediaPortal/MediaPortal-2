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

using System;
using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Configuration
{
  public abstract class ConfigSetting : ConfigBase
  {
    #region Variables

    protected int _columns = 0;
    protected int _rows = 0;
    protected IStringBuilder _help = null;

    #endregion

    #region Properties

    public int Columns
    {
      get { return _columns; }
      set { _columns = value; }
    }

    public int Rows
    {
      get { return _rows; }
      set { _rows = value; }
    }

    public IStringBuilder Help
    {
      get { return _help; }
      set { _help = value; }
    }

    public ConfigSettingMetadata SettingMetadata
    {
      get { return (ConfigSettingMetadata)Metadata; }
    }

    public abstract Type SettingsObjectType { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the setting from the specified object.
    /// </summary>
    /// <param name="settingsObject">Object to extract setting from.</param>
    public virtual void Load(object settingsObject) { }

    /// <summary>
    /// Saves the setting to the specified <paramref name="settingsObject"/>.
    /// </summary>
    /// <param name="settingsObject">Object to save setting to.</param>
    public virtual void Save(object settingsObject) { }

    /// <summary>
    /// Applies the setting in the application.
    /// </summary>
    public virtual void Apply() { }

    /// <summary>
    /// Registers an other instance of ConfigBase.
    /// The current object will notify the registered object on a change.
    /// </summary>
    /// <param name="other"></param>
    public void Register(ConfigSetting other)
    {
      OnChangeEvent += other.ConfigChangedMainHandler;
    }

    public override IEnumerable<string> GetSearchTexts()
    {
      List<string> result = new List<string>();
      result.AddRange(base.GetSearchTexts());
      result.Add(Help.Evaluate());
      return result;
    }

    public override void SetMetadata(ConfigBaseMetadata metadata)
    {
      base.SetMetadata(metadata);
      _metadata = metadata;
      _help = LocalizationHelper.CreateLabelProperty(SettingMetadata.Text);
    }

    #endregion
  }
}
