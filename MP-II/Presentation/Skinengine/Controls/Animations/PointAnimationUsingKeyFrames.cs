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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals;
using SlimDX;
using SlimDX.Direct3D9;
using MyXaml.Core;
namespace Presentation.SkinEngine.Controls.Animations
{
  public class PointAnimationUsingKeyFrames : Timeline, IAddChild
  {
    Property _keyFramesProperty;
    Property _targetProperty;
    Property _targetNameProperty;

    #region ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="PointAnimation"/> class.
    /// </summary>
    public PointAnimationUsingKeyFrames()
    {
      Init();
    }

    public PointAnimationUsingKeyFrames(PointAnimationUsingKeyFrames a)
      : base(a)
    {
      Init();
      TargetProperty = a.TargetProperty;
      TargetName = a.TargetName;
      //foreach (PointKeyFrame k in a.KeyFrames)
      //{
      //  KeyFrames.Add((PointKeyFrame)k.Clone());
      //}
      _keyFramesProperty.SetValue(a.KeyFrames);
    }

    public override object Clone()
    {
      return new PointAnimationUsingKeyFrames(this);
    }

    void Init()
    {
      _targetProperty = new Property("");
      _targetNameProperty = new Property("");
      _keyFramesProperty = new Property(new PointKeyFrameCollection());
    }
    #endregion

    #region properties
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

    /// <summary>
    /// Gets or sets the target name property.
    /// </summary>
    /// <value>The target name property.</value>
    public Property KeyFramesProperty
    {
      get
      {
        return _keyFramesProperty;
      }
      set
      {
        _keyFramesProperty = value;
      }
    }
    /// <summary>
    /// Gets or sets the name of the target.
    /// </summary>
    /// <value>The name of the target.</value>
    public PointKeyFrameCollection KeyFrames
    {
      get
      {
        return _keyFramesProperty.GetValue() as PointKeyFrameCollection;
      }
    }
    #endregion

    #region animation methods
    /// <summary>
    /// Animates the property.
    /// </summary>
    /// <param name="timepassed">The timepassed.</param>
    protected override void AnimateProperty(AnimationContext context, uint timepassed)
    {
      if (context.Property == null) return;
      double time = 0;
      Vector2 start = new Vector2();
      for (int i = 0; i < KeyFrames.Count; ++i)
      {
        PointKeyFrame key = KeyFrames[i];
        if (key.KeyTime.TotalMilliseconds >= timepassed)
        {
          double progress = (timepassed - time);
          if (progress == 0)
          {
            context.Property.SetValue(key.Value);
          }
          else
          {
            progress /= (key.KeyTime.TotalMilliseconds - time);
            Vector2 result = key.Interpolate(start, progress);
            context.Property.SetValue(result);
          }
          return;
        }
        else
        {
          time = key.KeyTime.TotalMilliseconds;
          start = key.Value;
        }
      }
    }

    public override void Ended(AnimationContext context)
    {
      if (IsStopped(context)) return;
      if (context.Property != null)
      {
        if (FillBehaviour != FillBehaviour.HoldEnd)
        {
          context.Property.SetValue(OriginalValue);
        }
      }
    }

    public override void Start(AnimationContext context, uint timePassed)
    {
      if (!IsStopped(context))
        Stop(context);

      context.State = State.Starting;
      if (KeyFrames.Count > 0)
      {
        Duration = KeyFrames[KeyFrames.Count - 1].KeyTime;
      }
      //find context.Property...


      context.TimeStarted = timePassed;
      context.State = State.WaitBegin;
    }
    public override void Setup(AnimationContext context)
    {
      context.Property = null;
      if (String.IsNullOrEmpty(TargetName) || String.IsNullOrEmpty(TargetProperty)) return;
      context.Property = GetProperty(context.VisualParent, TargetName, TargetProperty);
    }

    public override void Initialize(UIElement element)
    {
      if (String.IsNullOrEmpty(TargetName) || String.IsNullOrEmpty(TargetProperty)) return;
      Property prop = GetProperty(element, TargetName, TargetProperty);
      OriginalValue = prop.GetValue();
    }
    public override void Stop(AnimationContext context)
    {
      if (IsStopped(context)) return;
      context.State = State.Idle;
      if (context.Property != null)
      {
        context.Property.SetValue(OriginalValue);
      }
    }
    #endregion



    #region IAddChild Members

    public void AddChild(object o)
    {
      KeyFrames.Add((PointKeyFrame)o);
    }

    #endregion
  }
}
