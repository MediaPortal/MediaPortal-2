#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  public enum RelativeSourceMode
  {
    //PreviousData, // Not implemented yet
    TemplatedParent,
    Self,
    FindAncestor
  }

  /// <summary>
  /// Implements the MPF RelativeSource element.
  /// This class acts as a data object. The search for the element referenced by this
  /// instance will be done by class <see cref="BindingMarkupExtension"/>.
  /// </summary>
  public class RelativeSource
  {
    //public static RelativeSource PreviousData = new RelativeSource(RelativeSourceMode.PreviousData);
    public static RelativeSource TemplatedParent = new RelativeSource(RelativeSourceMode.TemplatedParent);
    public static RelativeSource Self = new RelativeSource(RelativeSourceMode.Self);

    #region Protected fields

    protected RelativeSourceMode _mode = RelativeSourceMode.Self;
    protected int _ancestorLevel = 1;
    protected Type _ancestorType = null;

    #endregion

    #region Ctor

    public RelativeSource()
    { }

    public RelativeSource(RelativeSourceMode mode)
    {
      _mode = mode;
    }

    #endregion

    #region Public properties

    public RelativeSourceMode Mode
    {
      get { return _mode; }
      set { _mode = value; }
    }

    public int AncestorLevel
    {
      get { return _ancestorLevel; }
      set
      {
        _ancestorLevel = value;
        _mode = RelativeSourceMode.FindAncestor;
      }
    }

    public Type AncestorType
    {
      get { return _ancestorType; }
      set
      {
        _ancestorType = value;
        _mode = RelativeSourceMode.FindAncestor;
      }
    }

    #endregion

    public override string ToString()
    {
      if (Mode == RelativeSourceMode.FindAncestor)
      { // More information for the complex mode 'FindAncestor'
        IList<string> l = new List<string>();
        if (AncestorType != null)
          l.Add("AncestorType=" + AncestorType.Name);
        if (AncestorLevel != 1)
          l.Add(String.Format("AncestorLevel={0}", AncestorLevel));
        return "{RelativeSource " + StringUtils.Join(",", l)+"}";
      }
      else
        return "RelativeSource: " + _mode;
    }
  }
}
