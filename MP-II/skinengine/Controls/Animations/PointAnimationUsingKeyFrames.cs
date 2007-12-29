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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Animations
{
  public class PointAnimationUsingKeyFrames : Timeline, IList
  {
    Property _keyFramesProperty;
    Property _targetProperty;
    Property _targetNameProperty;
    Property _property;

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
