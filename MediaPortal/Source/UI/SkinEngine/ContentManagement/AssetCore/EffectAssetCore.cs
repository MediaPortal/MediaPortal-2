#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Linq;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX.Direct2D1;
using Effect = SharpDX.Direct2D1.Effect;

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public interface IEffectAssetCore<out T> : IEffectAssetCore
  {
    /// <summary>
    /// Gets the effect wrapper instance.
    /// </summary>
    T EffectInstance { get; }
  }

  public interface IEffectAssetCore : IAssetCore
  {
    /// <summary>
    /// Tries to allocat the effect resources.
    /// </summary>
    /// <returns><c>true</c> if successful.</returns>
    bool Allocate();

    /// <summary>
    /// Gets the actual effect for rendering.
    /// </summary>
    Effect Effect { get; }

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="texture"/>.
    /// </summary>
    /// <param name="texture">The texture to be rendered.</param>
    /// <param name="texture2">Optional 2nd texture used for transitions.</param>
    /// <param name="renderContext">Render context.</param>
    void StartRender(Bitmap1 texture, RenderContext renderContext, Bitmap1 texture2 = null);
  }

  /// <summary>
  /// Encapsulates an effect which can render a vertex buffer with a texture.
  /// The effect gets loaded from the skin's shaders directory.
  /// </summary>
  public class EffectAssetCore<T> : TemporaryAssetCoreBase, IEffectAssetCore
    where T : class, ICustomRenderEffect, new()
  {
    public event AssetAllocationHandler AllocationChanged = delegate { };

    protected readonly string _effectName;
    protected volatile Effect _effect;
    protected T _instance;
    private Matrix _lastTransform;

    public EffectAssetCore()
    { }

    /// <summary>
    /// EffectAssetCore constructor.
    /// </summary>
    /// <param name="effectName"> The name of the shader file or a semi-colon seperated list of filenames,
    /// all which will be assumed to be in the directory specified by <see cref="SkinResources.SHADERS_DIRECTORY"/>.
    /// The files will be concatenated in reverse order to create the final effect.</param>
    public EffectAssetCore(string effectName)
    {
      _effectName = effectName;
    }

    public bool Allocate()
    {
      if (string.IsNullOrEmpty(_effectName))
        return false;

      if (_instance == null)
      {
        Guid effectId;
        // We create a temporary effect to retrieve the actual effect ID, which depends on the given _effectName
        using (var tmpEffect = new T())
        {
          tmpEffect.Init(_effectName);
          effectId = tmpEffect.EffectId;
        }
        // We can only register an effect once by its Guid.
        if (!GraphicsDevice11.Instance.Factory2D.RegisteredEffects.Contains(effectId))
        {
          // TODO: is it possible to register same class with different internal IDs?
          GraphicsDevice11.Instance.Factory2D.RegisterEffect<T>(CreateInstance);
        }
      }
      _effect = new Effect<T>(GraphicsDevice11.Instance.Context2D1);
      return true;
    }

    private T CreateInstance()
    {
      _instance = new T();
      _instance.Init(_effectName);
      return _instance;
    }

    #region Public properties

    public Effect Effect
    {
      get
      {
        if (!IsAllocated)
          Allocate();
        return _effect;
      }
    }

    public T EffectInstance
    {
      get
      {
        if (!IsAllocated)
          Allocate();
        return _instance;
      }
    }

    #endregion

    #region IAssetCore implementation

    public bool IsAllocated
    {
      get { return _effect != null; }
    }

    public int AllocationSize
    {
      get { return 0; }
    }

    public void Free()
    {
      if (_effect != null)
      {
        _effect.Dispose();
        _effect = null;
      }
    }

    #endregion

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="texture"/>.
    /// </summary>
    /// <param name="texture">The texture to be rendered.</param>
    /// <param name="texture2">Optional 2nd texture used for transitions.</param>
    /// <param name="renderContext">Render context.</param>
    public void StartRender(Bitmap1 texture, RenderContext renderContext, Bitmap1 texture2 = null)
    {
      if (!IsAllocated)
      {
        Allocate();
        if (!IsAllocated)
          return;
      }

      _effect.SetInput(0, texture, true);
      if (texture2 != null && _effect.InputCount == 2)
        _effect.SetInput(1, texture2, true);

      _lastTransform = renderContext.Transform;

      //var oldTransform = GraphicsDevice11.Instance.Context2D1.Transform;
      //GraphicsDevice11.Instance.Context2D1.Transform = renderContext.Transform;
      //GraphicsDevice11.Instance.Context2D1.DrawImage(_effect);
      GraphicsDevice11.Instance.Context2D1.DrawImage(_effect, renderContext.OccupiedTransformedBounds.TopLeft);
      //GraphicsDevice11.Instance.Context2D1.Transform = oldTransform;
      KeepAlive();
    }
  }
}
