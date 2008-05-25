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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Specifies the visual structure and behavioral aspects of a Control that can be shared across multiple instances of the control.
  /// </summary>
  public class DataTemplate : FrameworkTemplate
  {
    #region Private fields

    Property _triggerProperty;

    #endregion

    #region Ctor

    public DataTemplate()
    {
      Init();
    }

    void Init()
    {
      _triggerProperty = new Property(typeof(IList<Trigger>), new List<Trigger>());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      DataTemplate dt = source as DataTemplate;
      foreach (Trigger t in dt.Triggers)
        Triggers.Add(copyManager.GetCopy(t));
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the type of the target (not used here, but required for real xaml)
    /// </summary>
    /// <value>The type of the target.</value>
    public Type DataType
    {
      get
      {
        return null;
      }
      set
      {
      }
    }

    public Property TriggersProperty
    {
      get { return _triggerProperty; }
    }

    public IList<Trigger> Triggers
    {
      get { return (IList<Trigger>)_triggerProperty.GetValue(); }
    }

    #endregion
  }
}
