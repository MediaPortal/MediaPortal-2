using System;
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
  public class Panel : FrameworkElement, IAsset
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


  }
}
