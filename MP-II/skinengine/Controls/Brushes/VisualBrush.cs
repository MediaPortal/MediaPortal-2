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
using MediaPortal.Presentation.Properties;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Bindings;
using SkinEngine.Effects;
using SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;

namespace SkinEngine.Controls.Brushes
{
  public class VisualBrush : TileBrush, IBindingCollection
  {
    BindingCollection _bindings;
    Property _visualProperty;
    EffectAsset _effect;
    Texture _textureOpacity;
    /// <summary>
    /// Initializes a new instance of the <see cref="VisualBrush"/> class.
    /// </summary>
    public VisualBrush()
    {
      Init();
    }

    public VisualBrush(VisualBrush b)
      : base(b)
    {
      Init();
      foreach (Binding binding in b._bindings)
      {
        _bindings.Add((Binding)binding.Clone());
      }
      Visual = b.Visual;
    }
    void Init()
    {
      _visualProperty = new Property(null);
      _bindings = new BindingCollection();
      _effect = ContentManager.GetEffect("normal");
    }
    public override object Clone()
    {
      return new VisualBrush(this);
    }

    /// <summary>
    /// Gets or sets the visual property.
    /// </summary>
    /// <value>The visual property.</value>
    public Property VisualProperty
    {
      get
      {
        return _visualProperty;
      }
      set
      {
        _visualProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the visual.
    /// </summary>
    /// <value>The visual.</value>
    public FrameworkElement Visual
    {
      get
      {
        return (FrameworkElement)_visualProperty.GetValue();
      }
      set
      {
        _visualProperty.SetValue(value);
      }
    }
    public override void SetupBrush(FrameworkElement element, ref SkinEngine.DirectX.PositionColored2Textured[] verts)
    {
      UpdateBounds(element, ref verts);
      base.SetupBrush(element, ref verts);
      InitializeBindings();
      _textureOpacity = new Texture(GraphicsDevice.Device, (int)_bounds.Width, (int)_bounds.Height, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
    }
    public override bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      if (this.Visual == null) return false;
      List<ExtendedMatrix> originalTransforms = SkinContext.Transforms;
      SkinContext.Transforms = new List<ExtendedMatrix>();


      GraphicsDevice.Device.EndScene();

      //get the current backbuffer
      using (Surface backBuffer = GraphicsDevice.Device.GetRenderTarget(0))
      {
        //get the surface of our opacity texture
        using (Surface textureOpacitySurface = _textureOpacity.GetSurfaceLevel(0))
        {
          SurfaceDescription desc = backBuffer.Description;

          ExtendedMatrix matrix = new ExtendedMatrix();
          Vector3 pos = new Vector3((float)this.Visual.ActualPosition.X, (float)this.Visual.ActualPosition.Y, this.Visual.ActualPosition.Z);
          float width = (float)this.Visual.ActualWidth;
          float height = (float)this.Visual.ActualHeight;
          float w = (float)(_bounds.Width / this.Visual.Width);
          float h = (float)(_bounds.Height / this.Visual.Height);

          //m.Matrix *= SkinContext.FinalMatrix.Matrix;
          //matrix.Matrix *= Matrix.Scaling(w, h, 1);


          if (desc.Width == GraphicsDevice.Width && desc.Height == GraphicsDevice.Height)
          {
            float cx = 1.0f;// ((float)desc.Width) / ((float)GraphicsDevice.Width);
            float cy = 1.0f;//((float)desc.Height) / ((float)GraphicsDevice.Height);

            //copy the correct rectangle from the backbuffer in the opacitytexture
            GraphicsDevice.Device.StretchRect(backBuffer,
                                                   new System.Drawing.Rectangle((int)(_orginalPosition.X * cx), (int)(_orginalPosition.Y * cy), (int)(_bounds.Width * cx), (int)(_bounds.Height * cy)),
                                                   textureOpacitySurface,
                                                   new System.Drawing.Rectangle((int)0, (int)0, (int)(_bounds.Width), (int)(_bounds.Height)),
                                                   TextureFilter.None);
            matrix.Matrix *= Matrix.Translation(new Vector3(-pos.X, -pos.Y, 0));
            matrix.Matrix *= Matrix.Scaling((float)(((float)GraphicsDevice.Width) / width), (float)(((float)GraphicsDevice.Height) / height), 1);
          }
          else
          {
            GraphicsDevice.Device.StretchRect(backBuffer,
                                                   new System.Drawing.Rectangle(0, 0, desc.Width, desc.Height),
                                                   textureOpacitySurface,
                                                   new System.Drawing.Rectangle((int)0, (int)0, (int)(_bounds.Width), (int)(_bounds.Height)),
                                                   TextureFilter.None);
            
            matrix.Matrix *= Matrix.Translation(new Vector3(-pos.X, -pos.Y, 0));
            matrix.Matrix *= Matrix.Scaling((float)(((float)GraphicsDevice.Width) / width), (float)(((float)GraphicsDevice.Height) / height), 1);
          }


          SkinContext.AddTransform(matrix);

          //change the rendertarget to the opacitytexture
          GraphicsDevice.Device.SetRenderTarget(0, textureOpacitySurface);

          //render the control (will be rendered into the opacitytexture)
          GraphicsDevice.Device.BeginScene();
          //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          Visual.DoRender();
          GraphicsDevice.Device.EndScene();
          SkinContext.RemoveTransform();

          //restore the backbuffer
          GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
        }
        //Texture.ToFile(_textureOpacity, @"c:\1\test.png", ImageFileFormat.Png);
        //TextureLoader.Save(@"C:\erwin\trunk\MP-II\MediaPortal\bin\x86\Debug\text.png", ImageFileFormat.Png, _textureOpacity);
      }
      SkinContext.Transforms = originalTransforms;
      if (Transform != null)
      {
        ExtendedMatrix mTrans;
        Transform.GetTransform(out mTrans);
        SkinContext.AddTransform(mTrans);
      }
      //now render the opacitytexture with the opacitymask brush
      GraphicsDevice.Device.BeginScene();
      //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
      _effect.StartRender(_textureOpacity);

      return true;
    }

    public override void EndRender()
    {
      if (this.Visual != null)
      {
        _effect.EndRender();
        if (Transform != null)
        {
          SkinContext.RemoveTransform();
        }
      }
    }

    #region IBindingCollection Members


    public void Add(Binding binding)
    {
      _bindings.Add(binding);
    }


    public virtual void InitializeBindings()
    {
      if (_bindings.Count == 0) return;
      foreach (Binding binding in _bindings)
      {
        binding.Initialize(this);
      }
    }
    #endregion
  }
}
