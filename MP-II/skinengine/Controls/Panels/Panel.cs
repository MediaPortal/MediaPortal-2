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
using SkinEngine.Controls.Visuals;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine.DirectX;
using SkinEngine.Controls.Brushes;
using SkinEngine;

namespace SkinEngine.Controls.Panels
{
  public class Panel : FrameworkElement, IAsset, IList
  {
    Property _alignmentXProperty;
    Property _alignmentYProperty;
    Property _childrenProperty;
    Property _backgroundProperty;
    VertexBuffer _vertexBufferBackground;
    DateTime _lastTimeUsed;

    public Panel()
    {
      _childrenProperty = new Property(new UIElementCollection(this));
      _alignmentXProperty = new Property(AlignmentX.Center);
      _alignmentYProperty = new Property(AlignmentY.Top);
      _backgroundProperty = new Property(null);
      ContentManager.Add(this);

      _alignmentXProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      _alignmentYProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      _backgroundProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    protected void OnPropertyInvalidate(Property property)
    {
      Invalidate();
    }
    protected void OnPropertyChanged(Property property)
    {
      Free();
    }

    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property BackgroundProperty
    {
      get
      {
        return _backgroundProperty;
      }
      set
      {
        _backgroundProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public Brush Background
    {
      get
      {
        return _backgroundProperty.GetValue() as Brush;
      }
      set
      {
        _backgroundProperty.SetValue(value);
      }
    }

    public Property ChildrenProperty
    {
      get
      {
        return _childrenProperty;
      }
      set
      {
        _childrenProperty = value;
      }
    }

    public UIElementCollection Children
    {
      get
      {
        return _childrenProperty.GetValue() as UIElementCollection;
      }
      set
      {
        _childrenProperty.SetValue(value);
      }
    }

    public Property AlignmentXProperty
    {
      get
      {
        return _alignmentXProperty;
      }
      set
      {
        _alignmentXProperty = value;
      }
    }

    public AlignmentX AlignmentX
    {
      get
      {
        return (AlignmentX)_alignmentXProperty.GetValue();
      }
      set
      {
        _alignmentXProperty.SetValue(value);
      }
    }

    public Property AlignmentYProperty
    {
      get
      {
        return _alignmentYProperty;
      }
      set
      {
        _alignmentYProperty = value;
      }
    }

    public AlignmentY AlignmentY
    {
      get
      {
        return (AlignmentY)_alignmentYProperty.GetValue();
      }
      set
      {
        _alignmentYProperty.SetValue(value);
      }
    }

    public override void DoRender()
    {

      if (_vertexBufferBackground == null)
      {
        PerformLayout();
      }
      if (Background != null)
      {
        Matrix mrel, mt;
        Background.RelativeTransform.GetTransform(out mrel);
        Background.Transform.GetTransform(out mt);
        GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix * mrel * mt;
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        GraphicsDevice.Device.SetStreamSource(0, _vertexBufferBackground, 0);
        Background.BeginRender();
        GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
        Background.EndRender();
      }
      foreach (UIElement element in Children)
      {
        if (element.IsVisible)
        {
          element.DoRender();
        }
      }
      _lastTimeUsed = SkinContext.Now;
    }

    public void PerformLayout()
    {
      Free();
      if (Background != null)
      {
        _vertexBufferBackground = new VertexBuffer(typeof(PositionColored2Textured), 4, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
        PositionColored2Textured[] verts = (PositionColored2Textured[])_vertexBufferBackground.Lock(0, 0);
        unchecked
        {
          verts[0].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X), (float)(ActualPosition.Y), 1.0f);
          verts[1].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X), (float)(ActualPosition.Y + ActualHeight), 1.0f);
          verts[2].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X + ActualWidth), (float)(ActualPosition.Y + ActualHeight), 1.0f);
          verts[3].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X + ActualWidth), (float)(ActualPosition.Y), 1.0f);

        }
        Background.SetupBrush(this, ref verts);
        _vertexBufferBackground.Unlock();
      }
    }


    public void Free()
    {
      if (_vertexBufferBackground != null)
      {
        _vertexBufferBackground.Dispose();
        _vertexBufferBackground = null;
      }
    }

    #region IAsset Members

    public bool IsAllocated
    {
      get
      {
        return (_vertexBufferBackground != null);
      }
    }

    public bool CanBeDeleted
    {
      get
      {
        if (!IsAllocated)
        {
          return false;
        }
        TimeSpan ts = SkinContext.Now - _lastTimeUsed;
        if (ts.TotalSeconds >= 1)
        {
          return true;
        }

        return false;
      }
    }


    #endregion



    #region IList Members

    public int Add(object value)
    {
      Children.Add((UIElement)value);
      return Children.Count;
    }

    public void Clear()
    {
      Children.Clear();
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
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
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
      get 
      {
        return Children.Count;
      }
    }

    public bool IsSynchronized
    {
      get 
      {
        return true;
      }
    }

    public object SyncRoot
    {
      get 
      { 
        throw new Exception("The method or operation is not implemented.");
      }
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
