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

using System.Diagnostics;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine.Effects;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls
{
  public class VisualBrush : Control
  {
    private Rectangle _sourceRect;
    private VertextBufferAsset _imageVb;
    private TextureAsset _imageTex;
    private string _effect;
    private uint _lastTime = 0;
    private bool _snapShotTaken = false;
    private Property _trigger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public VisualBrush(Control parent)
      : base(parent)
    {
      _trigger = new Property(false);
    }

    public bool Trigger
    {
      get { return (bool)_trigger.GetValue(); }
      set { _trigger.SetValue(value); }
    }

    public Property TriggerProperty
    {
      get { return _trigger; }
      set { _trigger = value; }
    }


    public string Effect
    {
      get { return _effect; }
      set { _effect = value; }
    }

    public Rectangle Source
    {
      get { return _sourceRect; }
      set { _sourceRect = value; }
    }

    private void Free()
    {
      if (_imageVb != null)
      {
        Trace.WriteLine("visualbrush free image");
        ContentManager.Remove(_imageVb);
        _imageVb.Free();
        _imageVb = null;
      }
      if (_imageTex != null)
      {
        ContentManager.Remove(_imageTex);
        _imageTex.Free();
        _imageTex = null;
      }
      _snapShotTaken = false;
    }

    private void TakeSnapShot()
    {
      Free();
      float scaleX = ((float)GraphicsDevice.Width) / ((float)SkinContext.Width);
      float scaleY = ((float)GraphicsDevice.Height) / ((float)SkinContext.Height);
      Rectangle rSource = new Rectangle();
      rSource.X = (int)(scaleX * ((float)_sourceRect.X));
      rSource.Width = (int)(scaleX * ((float)_sourceRect.Width));

      rSource.Y = (int)(scaleY * ((float)_sourceRect.Y));
      rSource.Height = (int)(scaleY * ((float)_sourceRect.Height));
      Rectangle rectDest = new Rectangle(0, 0, (int)(scaleX * Width), (int)(Height * scaleY));


      //      ServiceScope.Get<ILogger>().Debug("visualbrush alloc texture");
      Texture texture =
        new Texture(GraphicsDevice.Device, (int)(scaleX * Width), (int)(scaleY * Height), 1, Usage.RenderTarget,
                    Format.X8R8G8B8, Pool.Default);

      ContentManager.TextureReferences++;
      Surface surfaceDest = texture.GetSurfaceLevel(0);
      //      ServiceScope.Get<ILogger>().Debug("visualbrush alloc surface");
      using (Surface surfaceSource = GraphicsDevice.Device.GetRenderTarget(0))
      {
        GraphicsDevice.Device.StretchRectangle(surfaceSource,
                                               rSource,
                                               surfaceDest,
                                               rectDest,
                                               TextureFilter.None);
      }
      surfaceDest.Dispose();
      Trace.WriteLine("visualbrush alloc image");
      _imageTex = new TextureAsset(texture, 1, 1);
      _imageVb = new VertextBufferAsset(_imageTex);
      ContentManager.Add(_imageVb);
      ContentManager.Add(_imageTex);
      _snapShotTaken = true;
    }

    public override void Render(uint timePassed)
    {
      if (Trigger)
      {
        TakeSnapShot();
      }
      if (!IsVisible)
      {
        if (!IsAnimating)
        {
          Free();
          return;
        }
      }

      uint passed = timePassed - _lastTime;
      if (passed >= 400)
      {
        Free();
      }
      if (_snapShotTaken == false)
      {
        TakeSnapShot();
      }
      if (_imageVb == null || _imageTex == null)
      {
        return;
      }
      _lastTime = timePassed;

      Vector4 alpha = AlphaMask;
      GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
      if (SkinContext.TemporaryTransform != null)
      {
        GraphicsDevice.Device.Transform.World *= SkinContext.TemporaryTransform.Matrix;
        alpha.X *= SkinContext.TemporaryTransform.Alpha.X;
        alpha.W *= SkinContext.TemporaryTransform.Alpha.W;
        alpha.Z *= SkinContext.TemporaryTransform.Alpha.Z;
        alpha.Y *= SkinContext.TemporaryTransform.Alpha.Y;
      }
      if (_effect.Length == 0 || (GraphicsDevice.SupportsShaders == false))
      {
        _imageVb.Draw(Position.X, Position.Y, Position.Z, Width, Height, 0, 0, 1, 1,
                    SkinContext.FinalMatrix.Alpha.X * alpha.X,
                    SkinContext.FinalMatrix.Alpha.W * alpha.W,
                    SkinContext.FinalMatrix.Alpha.Z * alpha.Z,
                    SkinContext.FinalMatrix.Alpha.Y * alpha.Y);
      }
      else
      {
        EffectAsset effect = ContentManager.GetEffect(_effect);
        if (effect != null)
        {
          _imageVb.Draw(Position.X, Position.Y, Position.Z, Width, Height, 0, 0, 1, 1,
                      SkinContext.FinalMatrix.Alpha.X * alpha.X,
                      SkinContext.FinalMatrix.Alpha.W * alpha.W,
                      SkinContext.FinalMatrix.Alpha.Z * alpha.Z,
                      SkinContext.FinalMatrix.Alpha.Y * alpha.Y, effect);
        }
      }
    }
  }
}