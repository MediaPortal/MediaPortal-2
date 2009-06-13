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
using MediaPortal.Core.Settings;
using MediaPortal.Core.Localization;

namespace MediaPortal.Core.Configuration
{
  public delegate void ConfigChangedEventHandler(ConfigSetting sender);

  /// <summary>
  /// Base class for all configuration settings classes.
  /// </summary>
  public abstract class ConfigSetting : ConfigBase
  {
    #region Variables

    protected IResourceString _help = null;

    #endregion

    #region Properties

    public IResourceString Help
    {
      get { return _help; }
      set { _help = value; }
    }

    public ConfigSettingMetadata SettingMetadata
    {
      get { return (ConfigSettingMetadata) Metadata; }
    }

    /// <summary>
    /// Convenience property to quickly access the <see cref="ISettingsManager"/> service.
    /// </summary>
    public ISettingsManager SettingsManager
    {
      get { return ServiceScope.Get<ISettingsManager>(); }
    }

    #endregion

    #region Public events

    /// <summary>
    /// Gets called if this setting changes.
    /// </summary>
    public event ConfigChangedEventHandler Changed;

    #endregion

    #region Protected Methods

    /// <summary>
    /// Notifies all registered listeners about a change in this setting.
    /// </summary>
    protected void NotifyChange()
    {
      if (Changed != null)
        Changed(this);
    }

    /// <summary>
    /// Override this to handle changes in other instances of ConfigBase.
    /// </summary>
    /// <param name="sender">Sender of the change notification.</param>
    protected virtual void ConfigChangedHandler(ConfigSetting sender)
    {
      // Needs to be overriden by the inheriting class.
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the setting from the underlaying setting. The setting can be loaded from the
    /// <see cref="ISettingsManager"/> service, for example.
    /// </summary>
    public virtual void Load() { }

    /// <summary>
    /// Applies and saves the setting in the application.
    /// </summary>
    public virtual void Save() { }

    /// <summary>
    /// Registers this instance to the <see cref="Changed"/> of the <paramref name="other"/>
    /// setting.
    /// This object will be notified by the other object on a change.
    /// </summary>
    /// <param name="other">Other setting which should notify this setting when it changes.</param>
    public void ListenTo(ConfigSetting other)
    {
      other.Changed += ConfigChangedHandler;
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
      _help = LocalizationHelper.CreateResourceString(SettingMetadata.HelpText);
    }

    #endregion
  }
}
