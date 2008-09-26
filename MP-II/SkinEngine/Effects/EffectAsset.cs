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
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.SkinEngine.ContentManagement;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Effects
{
  public class EffectAsset : IAsset
  {
    private string _effectName;
    private Effect _effect;
    private DateTime _lastUsed = DateTime.MinValue;
    Dictionary<string, object> _effectParameters;
    EffectHandle _handleWorldProjection;
    EffectHandle _handleTexture;
    EffectHandle _handleTechnique;
    Dictionary<string, EffectHandleAsset> _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectAsset"/> class.
    /// </summary>
    /// <param name="effectName">Name of the effect.</param>
    public EffectAsset(string effectName)
    {
      _parameters = new Dictionary<string, EffectHandleAsset>();
      _effectParameters = new Dictionary<string, object>();
      _effectName = effectName;
      Allocate();
    }

    /// <summary>
    /// Allocates this instance.
    /// </summary>
    public void Allocate()
    {
      string effectFilePath = SkinContext.SkinResources.GetResourceFilePath(
          string.Format(@"{0}\{1}.fx", Skin.SHADERS_DIRECTORY ,_effectName));
      if (effectFilePath != null && File.Exists(effectFilePath))
      {
        string effectShader;
        using (StreamReader reader = new StreamReader(effectFilePath))
          effectShader = reader.ReadToEnd();
        Version vertexShaderVersion = GraphicsDevice.Device.Capabilities.VertexShaderVersion;
        Version pixelShaderVersion = GraphicsDevice.Device.Capabilities.PixelShaderVersion;

        ShaderFlags shaderFlags = ShaderFlags.OptimizationLevel3 | ShaderFlags.EnableBackwardsCompatibility; //| ShaderFlags.NoPreshader;
        //ShaderFlags shaderFlags =  ShaderFlags.NoPreshader;
        effectShader = effectShader.Replace("vs_2_0", String.Format("vs_{0}_{1}", vertexShaderVersion.Major, vertexShaderVersion.Minor));
        effectShader = effectShader.Replace("ps_2_0", String.Format("ps_{0}_{1}", pixelShaderVersion.Major, pixelShaderVersion.Minor));

        string errors = "";
        try
        {
          _effect = Effect.FromString(GraphicsDevice.Device, effectShader, null, null, null, shaderFlags, null, out errors);
        }
        catch
        { 
          ServiceScope.Get<ILogger>().Error("EffectAsset: Unable to load '{0}'", effectFilePath);
          ServiceScope.Get<ILogger>().Error("EffectAsset: Errors: {0}", errors);
        }

        _lastUsed = SkinContext.Now;
        _handleWorldProjection = _effect.GetParameter(null, "worldViewProj");
        _handleTexture = _effect.GetParameter(null, "g_texture");
        _handleTechnique = _effect.GetTechnique(0);

      }
    }

    public Dictionary<string, object> Parameters
    {
      get
      {
        return _effectParameters;
      }
    }

    #region IAsset Members

    /// <summary>
    /// Gets a value indicating the asset is allocated
    /// </summary>
    /// <value><c>true</c> if this asset is allocated; otherwise, <c>false</c>.</value>
    public bool IsAllocated
    {
      get { return (_effect != null); }
    }

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    public bool CanBeDeleted
    {
      get
      {
        TimeSpan ts = SkinContext.Now - _lastUsed;
        return (ts.TotalSeconds >= 5);
      }
    }

    /// <summary>
    /// Frees this asset.
    /// </summary>
    public bool Free(bool force)
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
      return false;
    }

    #endregion

    /// <summary>
    /// Renders the effect
    /// </summary>
    /// <param name="tex">The texture.</param>
    public void Render(TextureAsset tex, int stream)
    {
      if (!IsAllocated)
      {
        Allocate();
      }

      if (!IsAllocated)
      {
        //render without effect
        tex.Draw(stream);
        return;
      }
      if (!tex.IsAllocated)
      {
        tex.Allocate();
        if (!tex.IsAllocated)
        {
          return;
        }
      }
      _effect.SetValue(_handleWorldProjection, SkinContext.FinalMatrix.Matrix * GraphicsDevice.FinalTransform);
      _effect.SetTexture(_handleTexture, tex.Texture);
      _effect.Technique = _handleTechnique;
      SetEffectParameters();
      _effect.Begin(0);
      _effect.BeginPass(0);
      tex.Draw(stream);
      _effect.EndPass();
      _effect.End();
      _lastUsed = SkinContext.Now;
    }

    public void StartRender(Texture tex)
    {
      if (!IsAllocated)
      {
        Allocate();
      }
      if (!IsAllocated)
      {
        //render without effect
        GraphicsDevice.Device.SetTexture(0, tex);
        return;
      }
      _effect.SetValue(_handleWorldProjection, SkinContext.FinalMatrix.Matrix * GraphicsDevice.FinalTransform);
      _effect.SetTexture(_handleTexture, tex);
      _effect.Technique = _handleTechnique;
      SetEffectParameters();
      _effect.Begin(0);
      _effect.BeginPass(0);

      GraphicsDevice.Device.SetTexture(0, tex);
    }

    public void EndRender()
    {
      if (_effect != null)
      {
        _effect.EndPass();
        _effect.End();
        _lastUsed = SkinContext.Now;
      }
    }

    public void StartRender(Texture tex, int stream)
    {
      if (!IsAllocated)
      {
        Allocate();
      }
      if (!IsAllocated)
      {
        //render without effect
        GraphicsDevice.Device.SetTexture(stream, tex);
        return;
      }
      _effect.SetValue(_handleWorldProjection, SkinContext.FinalMatrix.Matrix * GraphicsDevice.FinalTransform);
      _effect.SetTexture(_handleTexture, tex);
      _effect.Technique = _handleTechnique;
      SetEffectParameters();
      _effect.Begin(0);
      _effect.BeginPass(0);

      GraphicsDevice.Device.SetTexture(stream, tex);
    }
    public void EndRender(int stream)
    {
      if (_effect != null)
      {
        _effect.EndPass();
        _effect.End();
        _lastUsed = SkinContext.Now;
      }
      GraphicsDevice.Device.SetTexture(stream, null);
    }
    public void Render(Texture tex)
    {
      StartRender(tex);
      GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
      EndRender();
    }

    void SetEffectParameters()
    {
      if (_effectParameters.Count == 0) return;
      Dictionary<string, object>.Enumerator enumer = _effectParameters.GetEnumerator();
      while (enumer.MoveNext())
      {
        object v = enumer.Current.Value;
        Type type = v.GetType();
        if (type == typeof(Texture))
          _effect.SetTexture(enumer.Current.Key, (Texture)v);
        if (type == typeof(Color4))
          _effect.SetValue(enumer.Current.Key, (Color4)v);

        else if (type == typeof(Color4[]))
          _effect.SetValue(enumer.Current.Key, (Color4[])v);

        else if (type == typeof(float))
          _effect.SetValue(enumer.Current.Key, (float)v);

        else if (type == typeof(float[]))
          _effect.SetValue(enumer.Current.Key, (float[])v);


        else if (type == typeof(Matrix))
          _effect.SetValue(enumer.Current.Key, (Matrix)v);

        else if (type == typeof(Vector4))
          _effect.SetValue(enumer.Current.Key, (Vector4)v);

        else if (type == typeof(bool))
          _effect.SetValue(enumer.Current.Key, (bool)v);

        else if (type == typeof(float))
          _effect.SetValue(enumer.Current.Key, (float)v);

        else if (type == typeof(int))
          _effect.SetValue(enumer.Current.Key, (int)v);

      }
    }

    public EffectHandleAsset GetParameterHandle(string name)
    {
      if (_parameters.ContainsKey(name))
      {
        return _parameters[name];
      }
      EffectHandleAsset asset = new EffectHandleAsset(name, this);
      _parameters[name] = asset;
      return asset;
    }

    public Effect Effect
    {
      get
      {
        return _effect;
      }
    }
  }
}
