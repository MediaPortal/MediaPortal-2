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
using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;
using Presentation.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.Xaml.Interfaces;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class DataTemplate : FrameworkTemplate, IImplicitKey
  {
    #region Private fields

    Property _triggerProperty;
    Property _dataTypeProperty;

    #endregion

    #region Ctor

    public DataTemplate()
    {
      Init();
    }

    void Init()
    {
      _triggerProperty = new Property(typeof(IList<TriggerBase>), new List<TriggerBase>());
      _dataTypeProperty = new Property(typeof(Type), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      DataTemplate dt = source as DataTemplate;
      foreach (TriggerBase t in dt.Triggers)
        Triggers.Add(copyManager.GetCopy(t));
      DataType = copyManager.GetCopy(dt.DataType);
    }

    #endregion

    #region Public properties

    public Type DataType
    {
      get { return (Type) _dataTypeProperty.GetValue(); }
      set { _dataTypeProperty.SetValue(value); }
    }

    public Property TriggersProperty
    {
      get { return _triggerProperty; }
    }

    public IList<TriggerBase> Triggers
    {
      get { return (IList<TriggerBase>)_triggerProperty.GetValue(); }
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
