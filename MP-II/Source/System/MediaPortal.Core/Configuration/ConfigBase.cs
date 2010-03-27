#region Copyright (C) 2007-2010 Team MediaPortal

/*
 *  Copyright (C) 2007-2010 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal 2
 *
 *  MediaPortal 2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal 2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;


namespace MediaPortal.Core.Configuration
{
  public abstract class ConfigBase
  {
    #region Protected fields

    protected ConfigBaseMetadata _metadata = null;

    /// <summary>
    /// Text to display for the current ConfigBase.
    /// </summary>
    protected IResourceString _text = null;
    protected AbstractProperty _visibleProperty;
    protected AbstractProperty _enabledProperty;

    #endregion

    protected ConfigBase()
    {
      _visibleProperty = new WProperty(typeof(bool), true);
      _enabledProperty = new WProperty(typeof(bool), true);
    }

    #region Public properties

    public ConfigBaseMetadata Metadata
    {
      get { return _metadata; }
    }

    /// <summary>
    /// Gets or sets the text to be displayed for this setting registration element.
    /// </summary>
    public IResourceString Text
    {
      get { return _text; }
      set { _text = value; }
    }

    public AbstractProperty VisibleProperty
    {
       get { return _visibleProperty; }
    }

    /// <summary>
    /// Gets or sets the visibility of this config object.
    /// </summary>
    /// <remarks>
    /// Will be set in subclasses.
    /// </remarks>
    public bool Visible
    {
      get { return (bool) _visibleProperty.GetValue(); }
      set { _visibleProperty.SetValue(value); }
    }

    public AbstractProperty EnabledProperty
    {
       get { return _enabledProperty; }
    }

    /// <summary>
    /// Gets or sets if this config object is enabled.
    /// </summary>
    /// <remarks>
    /// Will be set in subclasses.
    /// </remarks>
    public bool Enabled
    {
      get { return (bool) _enabledProperty.GetValue(); }
      set { _enabledProperty.SetValue(value); }
    }

    #endregion

    /// <summary>
    /// Returns all text strings in this configuration object which are searchable.
    /// Those strings can be used by the engine to make this configuration object searchable.
    /// </summary>
    public virtual IEnumerable<string> GetSearchTexts()
    {
      yield return Text.Evaluate();
    }

    public virtual void SetMetadata(ConfigBaseMetadata metadata)
    {
      _metadata = metadata;
      _text = LocalizationHelper.CreateResourceString(_metadata.Text);
    }
  }
}
