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
using System.Text;

using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Configuration
{
  public class SettingBase
  {
    #region variables
    protected string _id;
    protected bool _hidden = false;
    protected bool _enabled = true;
    protected StringId _text;
    protected StringId _help;
    protected SettingType _type;
    protected string _iconSmall;
    protected string _iconLarge;
    #endregion

    #region properties
    public string Id
    {
      get { return _id; }
      set { _id = value; }
    }

    public bool Hidden
    {
      get { return _hidden; }
    }

    public bool Enabled
    {
      get { return _enabled; }
    }

    public StringId Text
    {
      get { return _text; }
      set { _text = value; }
    }

    public StringId Help
    {
      get { return _help; }
      set { _help = value; }
    }

    public SettingType Type
    {
      get { return _type; }
      set { _type = value; }
    }

    public string IconSmall
    {
      get { return _iconSmall; }
      set { _iconSmall = value; }
    }

    public string IconLarge
    {
      get { return _iconLarge; }
      set { _iconLarge = value; }
    }
    #endregion

    #region methods
    public virtual void Save() 
    {
    }

    public virtual void Apply()
    {
    }
    #endregion
  }
}
