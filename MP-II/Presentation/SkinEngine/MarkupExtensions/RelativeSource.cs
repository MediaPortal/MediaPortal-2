#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using Presentation.SkinEngine.Xaml.Interfaces;

namespace Presentation.SkinEngine.MarkupExtensions
{
  public enum RelativeSourceMode
  {
    //PreviousData, // Not implemented yet
    TemplatedParent,
    Self,
    FindAncestor
  }

  /// <summary>
  /// Implements the RelativeSource element.
  /// This class mainly acts as a data object.
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
      return "RelativeSource: " + _mode.ToString();
    }
  }
}
