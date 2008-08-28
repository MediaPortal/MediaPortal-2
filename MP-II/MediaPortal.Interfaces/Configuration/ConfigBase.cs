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

namespace MediaPortal.Configuration
{

  /// <summary>
  /// Base class for all configurable settings.
  /// </summary>
  public class ConfigBase
  {

    #region Protected Variables

    /// <summary>
    /// The ID of the setting.
    /// </summary>
    protected string _id;
    /// <summary>
    /// Hide the setting?
    /// </summary>
    protected bool _hidden = false;
    /// <summary>
    /// Enable the setting?
    /// </summary>
    protected bool _enabled = true;
    /// <summary>
    /// Text describing the setting.
    /// </summary>
    protected StringId _text;
    /// <summary>
    /// Helptext for the setting.
    /// </summary>
    protected StringId _help;
    /// <summary>
    /// Type of the setting.
    /// </summary>
    protected SettingType _type;
    /// <summary>
    /// Small icon to display with the setting.
    /// </summary>
    protected string _iconSmall;
    /// <summary>
    /// Large icon to display with the setting.
    /// </summary>
    protected string _iconLarge;
    /// <summary>
    /// Preferred width of the setting, as a number of characters.
    /// </summary>
    protected int _width;
    /// <summary>
    /// Preferred height of the setting, as a number of lines.
    /// </summary>
    protected int _height;

    #endregion

    #region Private Variables

    /// <summary>
    /// The type of object which can be used to extract settings from.
    /// </summary>
    private object _settingsObject;

    /// <summary>
    /// The location where the current ConfigBase is registered.
    /// </summary>
    private string _registrationLocation;

    /// <summary>
    /// The items to be notified from on a change.
    /// </summary>
    private ICollection<string> _listenItems;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public string Id
    {
      get { return this._id; }
      set { this._id = value; }
    }

    /// <summary>
    /// Gets if this setting must be hidden.
    /// </summary>
    public bool Hidden
    {
      get { return this._hidden; }
    }

    /// <summary>
    /// Gets if this settings is enabled.
    /// </summary>
    public bool Enabled
    {
      get { return this._enabled; }
    }

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public StringId Text
    {
      get { return this._text; }
      set { this._text = value; }
    }

    /// <summary>
    /// Gets or sets the text to display as help.
    /// </summary>
    public StringId Help
    {
      get { return this._help; }
      set { this._help = value; }
    }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public SettingType Type
    {
      get { return this._type; }
      set { this._type = value; }
    }

    /// <summary>
    /// Gets or sets the small icon to display.
    /// </summary>
    public string IconSmall
    {
      get { return this._iconSmall; }
      set { this._iconSmall = value; }
    }

    /// <summary>
    /// Gets or sets the large icon to display.
    /// </summary>
    public string IconLarge
    {
      get { return this._iconLarge; }
      set { this._iconLarge = value; }
    }

    /// <summary>
    /// Gets or sets the preferred width (number of characters)
    /// </summary>
    public int Width
    {
      get { return this._width; }
      set { this._width = value; }
    }

    /// <summary>
    /// Gets or sets the preferred height (number of lines)
    /// </summary>
    public int Height
    {
      get { return this._height; }
      set { this._height = value; }
    }

    /// <summary>
    /// Gets the type of object which can be used to extract settings from.
    /// </summary>
    public object SettingsObject
    {
      get { return _settingsObject; }
    }

    /// <summary>
    /// Gets or sets the items to be notified from on a change.
    /// </summary>
    public ICollection<string> ListenItems
    {
      get { return _listenItems; }
      set { _listenItems = value; }
    }

    #endregion

    #region Public Events

    /// <summary>
    /// Gets called when the UI must redraw the setting.
    /// </summary>
    public event ConfigChangedEventHandler RedrawSettingEvent;

    #endregion

    #region Private Events

    /// <summary>
    /// Gets called if the configuration gets changed.
    /// </summary>
    private event ConfigChangedEventHandler OnChangeEvent;

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the setting from the specified object.
    /// </summary>
    /// <param name="settingsObject">Object to extract setting from.</param>
    public virtual void Load(object settingsObject) { }

    /// <summary>
    /// Saves the setting.
    /// </summary>
    /// <param name="settingsObject">Object to save setting to.</param>
    public virtual void Save(object settingsObject) { }

    /// <summary>
    /// Applies the setting.
    /// </summary>
    public virtual void Apply() { }

    /// <summary>
    /// Sets the location in the configurationtree where the current ConfigBase is registered.
    /// </summary>
    /// <param name="location"></param>
    public void SetRegistrationLocation(string location)
    {
      _registrationLocation = location;
    }

    /// <summary>
    /// Registers an other instance of ConfigBase.
    /// The current object will notify the registered object on a change.
    /// </summary>
    /// <param name="other"></param>
    public void Register(ConfigBase other)
    {
      OnChangeEvent += new ConfigChangedEventHandler(other.ConfigChangedMainHandler);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Sets the type of object which can be used to extract settings from.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected void SetSettingsObject(object obj)
    {
      _settingsObject = obj;
    }

    /// <summary>
    /// Notifies all registered items that the current setting is changed.
    /// </summary>
    protected void NotifyChange()
    {
      if (OnChangeEvent != null)
        OnChangeEvent(this, _registrationLocation);
    }

    /// <summary>
    /// Override this to handle changes in other instances of ConfigBase.
    /// </summary>
    /// <param name="sender">Sender of the change notification.</param>
    /// <param name="senderLocation">Location of the sender in the configurationtree.</param>
    protected virtual void ConfigChangedHandler(ConfigBase sender, string senderLocation)
    {

    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handler for a configuration change of another item.
    /// </summary>
    /// <param name="sender"></param>
    private void ConfigChangedMainHandler(ConfigBase sender, string senderLocation)
    {
      ConfigChangedHandler(sender, senderLocation);
      if (RedrawSettingEvent!= null)
        RedrawSettingEvent(this, _registrationLocation);
    }

    #endregion

  }
}
