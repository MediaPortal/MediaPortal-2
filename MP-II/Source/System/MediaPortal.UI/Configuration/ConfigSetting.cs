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
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;

namespace MediaPortal.Configuration
{
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
      get { return (ConfigSettingMetadata)Metadata; }
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
    public event ConfigChangedEventHandler OnChangeEvent;

    #endregion

    #region Protected Methods

    /// <summary>
    /// Notifies all registered items that the current setting is changed.
    /// </summary>
    protected void NotifyChange()
    {
      if (OnChangeEvent != null)
        OnChangeEvent(this, _metadata.Location);
    }

    /// <summary>
    /// Override this to handle changes in other instances of ConfigBase.
    /// </summary>
    /// <param name="sender">Sender of the change notification.</param>
    /// <param name="senderLocation">Location of the sender in the configurationtree.</param>
    protected virtual void ConfigChangedHandler(ConfigBase sender, string senderLocation)
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
    /// Saves the setting to the underlaying setting. The setting can be loaded from the
    /// <see cref="ISettingsManager"/> service, changed, and saved again at the
    /// <see cref="ISettingsManager"/> service.
    /// </summary>
    /// <remarks>
    /// This method should not apply the setting to the system, as the <see cref="Apply"/> method will
    /// also be called. The <see cref="Apply"/> method will be called after this method was called.
    /// </remarks>
    public virtual void Save() { }

    /// <summary>
    /// Applies the setting in the application.
    /// </summary>
    /// <remarks>
    /// Before this method is called, method <see cref="Save"/> will be called.
    /// </remarks>
    public virtual void Apply() { }

    /// <summary>
    /// Registers this instance to the <see cref="OnChangeEvent"/> of the <paramref name="other"/>
    /// setting.
    /// This object will be notified by the other object on a change.
    /// </summary>
    /// <param name="other">Other setting which should notify this setting when it changes.</param>
    public void ListenTo(ConfigSetting other)
    {
      other.OnChangeEvent += ConfigChangedHandler;
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
