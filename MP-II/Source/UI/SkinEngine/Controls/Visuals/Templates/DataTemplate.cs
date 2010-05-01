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
using MediaPortal.Core.General;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Templates
{
  public class DataTemplate : TemplateWithTriggers, IImplicitKey
  {
    #region Protected fields

    protected AbstractProperty _dataTypeProperty;
    protected AbstractProperty _dataStringProperty;

    #endregion

    #region Ctor

    public DataTemplate()
    {
      Init();
    }

    void Init()
    {
      _dataTypeProperty = new SProperty(typeof(Type), null);
      _dataStringProperty = new SProperty(typeof(string), string.Empty);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      DataTemplate dt = (DataTemplate) source;
      DataType = dt.DataType;
      DataString = dt.DataString;
    }

    #endregion

    #region Public properties

    public Type DataType
    {
      get { return (Type) _dataTypeProperty.GetValue(); }
      set { _dataTypeProperty.SetValue(value); }
    }

    public AbstractProperty DataStringProperty
    {
      get { return _dataStringProperty; }
    }

    /// <summary>
    /// Returns a string representation for the data formatted by this template. This is used by
    /// the scrolling engine to find the appropriate element when the user starts to type the first
    /// letters to move the focus to a child entry.
    /// </summary>
    public string DataString
    {
      get { return (string) _dataStringProperty.GetValue(); }
      set { _dataStringProperty.SetValue(value); }
    }

    #endregion

    #region IImplicitKey implementation

    public object GetImplicitKey()
    {
      return DataType;
    }

    #endregion
  }
}
