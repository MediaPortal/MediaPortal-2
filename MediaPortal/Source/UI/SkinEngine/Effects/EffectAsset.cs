#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Effects
{
  /// <summary>
  /// Encapsulates an effect which can render a vertex buffer with a texture.
  /// The effect gets loaded from the skin's shaders directory.
  /// </summary>
  public class EffectAsset : IAsset
  {
    private readonly string _effectName;
    readonly Dictionary<string, object> _effectParameters;
    readonly Dictionary<string, EffectHandleAsset> _parameters;
    private Effect _effect;
    private DateTime _lastUsed = DateTime.MinValue;
    EffectHandle _handleWorldProjection;
    EffectHandle _handleTexture;
    EffectHandle _handleTechnique;

    public EffectAsset(string effectName)
    {
      _parameters = new Dictionary<string, EffectHandleAsset>();
      _effectParameters = new Dictionary<string, object>();
      _effectName = effectName;
      Allocate();
    }

    public void Allocate()
    {
      string effectFilePath = SkinContext.SkinResources.GetResourceFilePath(
          string.Format(@"{0}\{1}.fx", SkinResources.SHADERS_DIRECTORY ,_effectName));
      if (effectFilePath != null && File.Exists(effectFilePath))
      {
        string effectShader;
        using (StreamReader reader = new StreamReader(effectFilePath))
          effectShader = reader.ReadToEnd();
        Version vertexShaderVersion = GraphicsDevice.Device.Capabilities.VertexShaderVersion;
        Version pixelShaderVersion = GraphicsDevice.Device.Capabilities.PixelShaderVersion;

        const ShaderFlags shaderFlags = ShaderFlags.OptimizationLevel3 | ShaderFlags.EnableBackwardsCompatibility; //| ShaderFlags.NoPreshader;
        //ShaderFlags shaderFlags = ShaderFlags.NoPreshader;
        effectShader = effectShader.Replace("vs_2_0", String.Format("vs_{0}_{1}", vertexShaderVersion.Major, vertexShaderVersion.Minor));
        effectShader = effectShader.Replace("ps_2_0", String.Format("ps_{0}_{1}", pixelShaderVersion.Major, pixelShaderVersion.Minor));

        string errors = string.Empty;
        try
        {
          _effect = Effect.FromString(GraphicsDevice.Device, effectShader, null, null, null, shaderFlags, null, out errors);
          _lastUsed = SkinContext.FrameRenderingStartTime;
          _handleWorldProjection = _effect.GetParameter(null, "worldViewProj");
          _handleTexture = _effect.GetParameter(null, "g_texture");
          _handleTechnique = _effect.GetTechnique(0);
        }
        catch
        { 
          ServiceScope.Get<ILogger>().Error("EffectAsset: Unable to load '{0}'", effectFilePath);
          ServiceScope.Get<ILogger>().Error("EffectAsset: Errors: {0}", errors);
        }
      }
    }

    public Dictionary<string, object> Parameters
    {
      get { return _effectParameters; }
    }

    #region IAsset Members

    public bool IsAllocated
    {
      get { return (_effect != null); }
    }

    public bool CanBeDeleted
    {
      get
      {
        TimeSpan ts = SkinContext.FrameRenderingStartTime - _lastUsed;
        return (ts.TotalSeconds >= 5);
      }
    }

    public void Free(bool force)
    {
      if (_handleTechnique != null)
        _handleTechnique.Dispose();
      if (_handleTexture != null)
        _handleTexture.Dispose();
      if (_handleWorldProjection != null)
        _handleWorldProjection.Dispose();
      Dictionary<string, EffectHandleAsset>.Enumerator enumer = _parameters.GetEnumerator();
      while (enumer.MoveNext())
      {
        if (enumer.Current.Value.Handle != null)
          enumer.Current.Value.Handle.Dispose();
        enumer.Current.Value.Handle = null;
      }
      _handleTechnique = null;
      _handleWorldProjection = null;
      _handleTexture = null;

      if (_effect != null)
      {
        _effect.Dispose();
        _effect = null;
      }
    }

    #endregion

    public void Render(TextureAsset tex, int stream, Matrix finalTransform)
    {
      if (!IsAllocated)
        Allocate();

      if (!IsAllocated)
      {
        // Render without effect
        tex.Draw(stream);
        return;
      }
      if (!tex.IsAllocated)
      {
        tex.Allocate();
        if (!tex.IsAllocated)
          return;
      }
      _effect.SetValue(_handleWorldProjection, finalTransform * GraphicsDevice.FinalTransform);
      _effect.SetTexture(_handleTexture, tex.Texture);
      _effect.Technique = _handleTechnique;
      SetEffectParameters();
      _effect.Begin(0);
      _effect.BeginPass(0);
      tex.Draw(stream);
      _effect.EndPass();
      _effect.End();
      _lastUsed = SkinContext.FrameRenderingStartTime;
    }

    public void StartRender(Matrix finalTransform)
    {
      StartRender(null, 0, finalTransform);
    }

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="tex"/> in the stream of number <code>0</code>.
    /// </summary>
    /// <param name="tex">The texture to be rendered.</param>
    /// <param name="finalTransform">Final render transformation to apply.</param>
    public void StartRender(Texture tex, Matrix finalTransform)
    {
      StartRender(tex, 0, finalTransform);
    }

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="tex"/> in the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="tex">The texture to be rendered.</param>
    /// <param name="stream">Number of the stream to render.</param>
    /// <param name="finalTransform">Final render transformation to apply.</param>
    public void StartRender(Texture tex, int stream, Matrix finalTransform)
    {
      if (!IsAllocated)
        Allocate();
      if (!IsAllocated)
      {
        // Render without effect
        GraphicsDevice.Device.SetTexture(stream, tex);
        return;
      }
      _effect.SetValue(_handleWorldProjection, finalTransform * GraphicsDevice.FinalTransform);
      _effect.SetTexture(_handleTexture, tex);
      _effect.Technique = _handleTechnique;
      SetEffectParameters();
      _effect.Begin(0);
      _effect.BeginPass(0);

      GraphicsDevice.Device.SetTexture(stream, tex);
    }

    /// <summary>
    /// Ends the rendering of the stream of number <code>0</code>.
    /// </summary>
    public void EndRender()
    {
      EndRender(0);
    }

    /// <summary>
    /// Ends the rendering of the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of the stream to end the rendering.</param>
    public void EndRender(int stream)
    {
      if (_effect != null)
      {
        _effect.EndPass();
        _effect.End();
        _lastUsed = SkinContext.FrameRenderingStartTime;
      }
      GraphicsDevice.Device.SetTexture(stream, null);
    }

    public void Render(Texture tex, Matrix finalTransform)
    {
      StartRender(tex, finalTransform);
      GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
      EndRender();
    }

    void SetEffectParameters()
    {
      if (_effectParameters.Count == 0) return;
      foreach (KeyValuePair<string, object> kvp in _effectParameters)
      {
        Type type = kvp.Value.GetType();
        if (type == typeof(Texture))
          _effect.SetTexture(kvp.Key, (Texture) kvp.Value);
        if (type == typeof(Color4))
          _effect.SetValue(kvp.Key, (Color4) kvp.Value);
        else if (type == typeof(Color4[]))
          _effect.SetValue<Color4>(kvp.Key, (Color4[]) kvp.Value);
        else if (type == typeof(float))
          _effect.SetValue(kvp.Key, (float) kvp.Value);
        else if (type == typeof(float[]))
          _effect.SetValue<float>(kvp.Key, (float[]) kvp.Value);
        else if (type == typeof(Matrix))
          _effect.SetValue(kvp.Key, (Matrix) kvp.Value);
        else if (type == typeof(Vector4))
          _effect.SetValue(kvp.Key, (Vector4) kvp.Value);
        else if (type == typeof(bool))
          _effect.SetValue(kvp.Key, (bool) kvp.Value);
        else if (type == typeof(float))
          _effect.SetValue(kvp.Key, (float) kvp.Value);
        else if (type == typeof(int))
          _effect.SetValue(kvp.Key, (int) kvp.Value);
      }
    }

    public EffectHandleAsset GetParameterHandle(string name)
    {
      if (_parameters.ContainsKey(name))
        return _parameters[name];
      EffectHandleAsset asset = new EffectHandleAsset(name, this);
      _parameters[name] = asset;
      return asset;
    }

    public Effect Effect
    {
      get { return _effect; }
    }
  }
}
