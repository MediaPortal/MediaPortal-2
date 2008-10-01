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

using MediaPortal.Presentation.Localisation;


namespace MediaPortal.Configuration
{
  public class ConfigBase
  {

    #region Variables

    /// <summary>
    /// Registration location of the current ConfigBase.
    /// </summary>
    protected string _location;
    /// <summary>
    /// Text to display for the current ConfigBase.
    /// </summary>
    protected StringId _text;
    /// <summary>
    /// Indicates whether the current ConfigBase is disabled.
    /// </summary>
    protected bool _disabled;
    /// <summary>
    /// Indicates whether the current ConfigBase is hidden.
    /// </summary>
    protected bool _hidden;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the location path of this setting registration. The location contains
    /// the parent location as well as the Id part of this setting registration.
    /// </summary>
    public string Location
    {
      get { return _location; }
      set { _location = value; }
    }

    /// <summary>
    /// Gets the location part of the parent setting registration element.
    /// </summary>
    public string ParentLocation
    {
      get
      {
        int i = _location.LastIndexOf('/');
        return i == -1 ? string.Empty : _location.Substring(0, i);
      }
    }

    /// <summary>
    /// Gets the Id part of the location path.
    /// </summary>
    public string Id
    {
      get
      {
        int i = _location.LastIndexOf('/');
        return i == -1 ? _location : _location.Substring(i + 1);
      }
    }

    /// <summary>
    /// Gets or sets the text to be displayed for this setting registration element.
    /// </summary>
    public StringId Text
    {
      get { return _text; }
      set { _text = value; }
    }

    /// <summary>
    /// Gets or sets whether the current ConfigBase is disabled.
    /// </summary>
    public bool Disabled
    {
      get { return _disabled; }
      set { _disabled = value; }
    }

    /// <summary>
    /// Gets or sets whether the current ConfigBase is hidden.
    /// </summary>
    public bool Hidden
    {
      get { return _hidden; }
      set { _hidden = value; }
    }

    #endregion

    #region Public Events

    /// <summary>
    /// Gets called when the UI must redraw the setting.
    /// </summary>
    public event ConfigChangedEventHandler RedrawSettingEvent;

    #endregion

    #region Protected Events

    /// <summary>
    /// Gets called if the configuration gets changed.
    /// </summary>
    protected event ConfigChangedEventHandler OnChangeEvent;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new (invalid) instance of ConfigBase.
    /// </summary>
    protected ConfigBase()
    {
      
    }

    /// <summary>
    /// Initializes a new <see cref="ConfigBase"/> instance.
    /// </summary>
    /// <param name="location">The location of the new instance. This must contain the parent location
    /// if there is a parent, and the Id of this setting registration as last location path element.</param>
    /// <param name="text">The text to be displayed for this setting registration.</param>
    public ConfigBase(string location, StringId text)
    {
      _location = location;
      _text = text;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Notifies all registered items that the current setting is changed.
    /// </summary>
    protected void NotifyChange()
    {
      if (OnChangeEvent != null)
        OnChangeEvent(this, _location);
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

    /// <summary>
    /// Handler for a configuration change of another item.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="senderLocation"></param>
    protected void ConfigChangedMainHandler(ConfigBase sender, string senderLocation)
    {
      ConfigChangedHandler(sender, senderLocation);
      if (RedrawSettingEvent != null)
        RedrawSettingEvent(this, _location);
    }

    #endregion

  }
}
