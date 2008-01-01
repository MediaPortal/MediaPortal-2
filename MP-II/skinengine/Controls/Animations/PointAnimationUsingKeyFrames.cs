#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;
using Microsoft.DirectX;

namespace SkinEngine.Controls.Animations
{
  public class PointAnimationUsingKeyFrames : Timeline, IList
  {
    Property _keyFramesProperty;
    Property _targetProperty;
    Property _targetNameProperty;
    Property _property;
    Vector2 _originalValue;

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
      foreach (PointKeyFrame k in a.KeyFrames)
      {
        KeyFrames.Add((PointKeyFrame)k.Clone());
      }
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
      _targetProperty.Attach(new PropertyChangedHandler(OnTargetChanged));
      _targetNameProperty.Attach(new PropertyChangedHandler(OnTargetChanged));
    }
    void OnTargetChanged(Property prop)
    {
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


    /// <summary>
    /// Animates the property.
    /// </summary>
    /// <param name="timepassed">The timepassed.</param>
    protected override void AnimateProperty(uint timepassed)
    {
      if (_property == null) return;
      double time = 0;
      Vector2 start = new Vector2();
      for (int i = 0; i < KeyFrames.Count; ++i)
      {
        PointKeyFrame key = KeyFrames[i];
        if (key.KeyTime.TotalMilliseconds >= timepassed)
        {
          double progress = (timepassed - time);
          progress /= (key.KeyTime.TotalMilliseconds - time);
          Vector2 result = key.Interpolate(start, progress);
          _property.SetValue(result);
          return;
        }
        else
        {
          time = key.KeyTime.TotalMilliseconds;
          start = key.Value;
        }
      }
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
      if (KeyFrames.Count > 0)
      {
        Duration = KeyFrames[KeyFrames.Count - 1].KeyTime;
      }
      //find _property...

      _property = null;
      if (String.IsNullOrEmpty(TargetName) || String.IsNullOrEmpty(TargetProperty)) return;
      _property = GetProperty(TargetName, TargetProperty);
      _originalValue = (Vector2)_property.GetValue();

      _timeStarted = timePassed;
      _state = State.WaitBegin;
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

    #region IList Members

    public int Add(object value)
    {
      KeyFrames.Add((PointKeyFrame)value);
      return KeyFrames.Count;
    }

    public void Clear()
    {
      KeyFrames.Clear();
    }

    public bool Contains(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int IndexOf(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Insert(int index, object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool IsFixedSize
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsReadOnly
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void RemoveAt(int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public object this[int index]
    {
      get
      {
        return KeyFrames[index];
      }
      set
      {
      }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsSynchronized
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion

  }
}
