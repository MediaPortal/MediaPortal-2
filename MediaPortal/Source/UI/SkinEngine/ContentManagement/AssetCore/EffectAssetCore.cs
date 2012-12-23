#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  /// <summary>
  /// Encapsulates an effect which can render a vertex buffer with a texture.
  /// The effect gets loaded from the skin's shaders directory.
  /// </summary>
  public class EffectAssetCore : TemporaryAssetCoreBase, IAssetCore
  {
    public event AssetAllocationHandler AllocationChanged = delegate { };

    #region Consts

    protected const string PARAM_WORLDVIEWPROJ = "worldViewProj";
    protected const string PARAM_TEXTURE = "g_texture";

    #endregion

    protected readonly string _effectName;
    protected readonly IDictionary<string, object> _parameterValues;
    protected volatile Effect _effect;
    protected EffectHandle _handleWorldProjection;
    protected EffectHandle _handleTexture;
    protected EffectHandle _handleTechnique;
    protected bool _fileMissing = false;

    /// <summary>
    /// EffectAssetCore constructor.
    /// </summary>
    /// <param name="effectName"> The name of the shader file or a semi-colon seperated list of filenames,
    /// all which will be assumed to be in the directory specified by <see cref="SkinResources.SHADERS_DIRECTORY"/>.
    /// The files will be concatenated in reverse order to create the final effect.</param>
    public EffectAssetCore(string effectName)
    {
      _parameterValues = new Dictionary<string, object>();
       _effectName = effectName;
    }

    public bool Allocate()
    {
      string[] files = _effectName.Split(';');
      if (files.Length == 0) 
        return false;

      StringBuilder effectShader = new StringBuilder(8196);
      Version vertexShaderVersion = GraphicsDevice.Device.Capabilities.VertexShaderVersion;
      Version pixelShaderVersion = GraphicsDevice.Device.Capabilities.PixelShaderVersion;
      for (int i = files.Length-1; i >= 0; --i)
      {
        string effectFilePath = SkinContext.SkinResources.GetResourceFilePath(string.Format(@"{0}\{1}.fx", SkinResources.SHADERS_DIRECTORY, files[i]));
        if (effectFilePath == null || !File.Exists(effectFilePath))
        {
          if (!_fileMissing)
            ServiceRegistration.Get<ILogger>().Error("Effect file {0} does not exist", effectFilePath);
          _fileMissing = true;
          return false;
        }
        _fileMissing = false;

        using (StreamReader reader = new StreamReader(effectFilePath))
          effectShader.Append(reader.ReadToEnd());

        // Concatenate
        effectShader.Append(Environment.NewLine);
      }

      effectShader.Replace("vs_2_0", String.Format("vs_{0}_{1}", vertexShaderVersion.Major, vertexShaderVersion.Minor));
      effectShader.Replace("ps_2_0", String.Format("ps_{0}_{1}", pixelShaderVersion.Major, pixelShaderVersion.Minor));

      // We place the lock here to comply to the MP2 multithreading guideline - we are not allowed to request the
      // effect resources when holding our lock
      lock (_syncObj)
      {
        if (_effect != null)
          return true;

        string errors = string.Empty;
        try
        {
          const ShaderFlags shaderFlags = ShaderFlags.OptimizationLevel3 | ShaderFlags.EnableBackwardsCompatibility; //| ShaderFlags.NoPreshader;
          _effect = Effect.FromString(GraphicsDevice.Device, effectShader.ToString(), null, null, null, shaderFlags, null, out errors);
          _handleWorldProjection = _effect.GetParameter(null, PARAM_WORLDVIEWPROJ);
          _handleTexture = _effect.GetParameter(null, PARAM_TEXTURE);
          _handleTechnique = _effect.GetTechnique(0);
          return true;
        }
        catch
        {
          ServiceRegistration.Get<ILogger>().Error("EffectAsset: Unable to load '{0}'", _effectName);
          ServiceRegistration.Get<ILogger>().Error("EffectAsset: Errors: {0}", errors);
          return false;
        }
      }
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

    public IDictionary<string, object> Parameters
    {
      get { return _parameterValues; }
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
      if (_handleTechnique != null)
        _handleTechnique.Dispose();
      if (_handleTexture != null)
        _handleTexture.Dispose();
      if (_handleWorldProjection != null)
        _handleWorldProjection.Dispose();
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

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="texture"/> in the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="texture">The texture to be rendered.</param>
    /// <param name="stream">Number of the stream to render.</param>
    /// <param name="finalTransform">Final render transformation to apply.</param>
    public void StartRender(Texture texture, int stream, Matrix finalTransform)
    {
      if (!IsAllocated)
      {
        Allocate();
        if (!IsAllocated)
          return;
      }

      _effect.SetValue(_handleWorldProjection, finalTransform * GraphicsDevice.FinalTransform);
      _effect.SetTexture(_handleTexture, texture);
      _effect.Technique = _handleTechnique;
      SetEffectParameters();
      _effect.Begin(0);
      _effect.BeginPass(0);

      KeepAlive();
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
      }
    }

    public void SetEffectParameters()
    {
      if (_parameterValues.Count == 0 | !IsAllocated) 
        return;
      foreach (KeyValuePair<string, object> kvp in _parameterValues)
      {
        Type type = kvp.Value.GetType();
        if (type == typeof(Texture))
          _effect.SetTexture(kvp.Key, (Texture) kvp.Value);
        else if (type == typeof(Color4))
          _effect.SetValue(kvp.Key, (Color4) kvp.Value);
        else if (type == typeof(Color4[]))
          _effect.SetValue(kvp.Key, (Color4[]) kvp.Value);
        else if (type == typeof(float))
          _effect.SetValue(kvp.Key, (float) kvp.Value);
        else if (type == typeof(float[]))
          _effect.SetValue(kvp.Key, (float[]) kvp.Value);
        else if (type == typeof(Matrix))
          _effect.SetValue(kvp.Key, (Matrix) kvp.Value);
        else if (type == typeof(Vector3))
          _effect.SetValue(kvp.Key, (Vector3) kvp.Value);
        else if (type == typeof(Vector4))
          _effect.SetValue(kvp.Key, (Vector4) kvp.Value);
        else if (type == typeof(bool))
          _effect.SetValue(kvp.Key, (bool) kvp.Value);
        else if (type == typeof(int))
          _effect.SetValue(kvp.Key, (int) kvp.Value);
      }
    }
  }
}