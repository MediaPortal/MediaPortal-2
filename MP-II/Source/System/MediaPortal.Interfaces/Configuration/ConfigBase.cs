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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;


namespace MediaPortal.Configuration
{
  public abstract class ConfigBase
  {
    #region Variables

    protected ConfigBaseMetadata _metadata;

    /// <summary>
    /// Text to display for the current ConfigBase.
    /// </summary>
    protected IResourceString _text;
    protected bool _hidden;
    protected bool _disabled;

    #endregion

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

    // TODO: There is no API yet to set this property. Should it be set in subclasses?
    public bool Hidden
    {
      get { return _hidden; }
    }

    // TODO: There is no API yet to set this property. Should it be set in subclasses?
    public bool Disabled
    {
      get { return _disabled; }
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
      _text = LocalizationHelper.CreateLabelProperty(_metadata.Text);
    }
  }
}
