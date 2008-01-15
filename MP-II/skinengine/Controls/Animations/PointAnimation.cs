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
using System.Text;
using System.Reflection;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;
using SlimDX;
using SlimDX.Direct3D9;

namespace SkinEngine.Controls.Animations
{
  public class PointAnimation : Timeline
  {
    Property _fromProperty;
    Property _toProperty;
    Property _byProperty;
    Property _targetProperty;
    Property _targetNameProperty;
    Property _property;
    Vector2 _originalValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="PointAnimation"/> class.
    /// </summary>
    public PointAnimation()
    {
      Init();
    }
    public PointAnimation(PointAnimation a)
      : base(a)
    {
      Init();
      From = a.From;
      To = a.To;
      By = a.By;
      TargetProperty = a.TargetProperty;
      TargetName = a.TargetName;
    }
    public override object Clone()
    {
      return new PointAnimation(this);
    }
    void Init()
    {
      _targetNameProperty = new Property("");
      _targetProperty = new Property("");
      _fromProperty = new Property(new Vector2(0, 0));
      _toProperty = new Property(new Vector2(0, 0));
      _byProperty = new Property(new Vector2(0, 0));

      _targetProperty.Attach(new PropertyChangedHandler(OnTargetChanged));
      _targetNameProperty.Attach(new PropertyChangedHandler(OnTargetChanged));
    }
    void OnTargetChanged(Property prop)
    {
    }

    /// <summary>
    /// Gets or sets from property.
    /// </summary>
    /// <value>From property.</value>
    public Property FromProperty
    {
      get
      {
        return _fromProperty;
      }
      set
      {
        _fromProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets from.
    /// </summary>
    /// <value>From.</value>
    public Vector2 From
    {
      get
      {
        return (Vector2)_fromProperty.GetValue();
      }
      set
      {
        _fromProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets to property.
    /// </summary>
    /// <value>To property.</value>
    public Property ToProperty
    {
      get
      {
        return _toProperty;
      }
      set
      {
        _toProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets to.
    /// </summary>
    /// <value>To.</value>
    public Vector2 To
    {
      get
      {
        return (Vector2)_toProperty.GetValue();
      }
      set
      {
        _toProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the by property.
    /// </summary>
    /// <value>The by property.</value>
    public Property ByProperty
    {
      get
      {
        return _byProperty;
      }
      set
      {
        _byProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the by.
    /// </summary>
    /// <value>The by.</value>
    public Vector2 By
    {
      get
      {
        return (Vector2)_byProperty.GetValue();
      }
      set
      {
        _byProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the target property.
    /// </summary>
    /// <value>The target property.</value>
    public Property TargetPropertyProperty
    {
      get
      {
        return _targetProperty;
      }
      set
      {
        _targetProperty = value;
      }
    }
    /// <summary>
    /// Gets or sets the target property.
    /// </summary>
    /// <value>The target property.</value>
    public string TargetProperty
    {
      get
      {
        return _targetProperty.GetValue() as string;
      }
      set
      {
        _targetProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the target name property.
    /// </summary>
    /// <value>The target name property.</value>
    public Property TargetNameProperty
    {
      get
      {
        return _targetNameProperty;
      }
      set
      {
        _targetNameProperty = value;
      }
    }
    /// <summary>
    /// Gets or sets the name of the target.
    /// </summary>
    /// <value>The name of the target.</value>
    public string TargetName
    {
      get
      {
        return _targetNameProperty.GetValue() as string;
      }
      set
      {
        _targetNameProperty.SetValue(value);
      }
    }
    protected override void AnimateProperty(uint timepassed)
    {
      if (_property == null) return;
      double distx = (To.X - From.X) / Duration.TotalMilliseconds;
      distx *= timepassed;
      distx += From.X;

      double disty = (To.X - From.Y) / Duration.TotalMilliseconds;
      disty *= timepassed;
      disty += From.Y;

      _property.SetValue(new Vector2((float)distx, (float)disty));
    }

    public override void Ended()
    {
      if (IsStopped) return;
      if (_property != null)
      {
        if (FillBehaviour != FillBehaviour.HoldEnd)
        {
          _property.SetValue(_originalValue);
        }
      }
    }

    public override void Start(uint timePassed)
    {
      if (!IsStopped)
        Stop();

      _state = State.Starting;


      _timeStarted = timePassed;
      _state = State.WaitBegin;
    }
    public override void Setup(UIElement element)
    {
      VisualParent = element;
      _property = null;
      if (String.IsNullOrEmpty(TargetName) || String.IsNullOrEmpty(TargetProperty)) return;
      _property = GetProperty(TargetName, TargetProperty);
      _originalValue = (Vector2)_property.GetValue();
    }

    public override void Stop()
    {
      if (IsStopped) return;
      _state = State.Idle;
      if (_property != null)
      {
        _property.SetValue(_originalValue);
      }
    }
  }
}
