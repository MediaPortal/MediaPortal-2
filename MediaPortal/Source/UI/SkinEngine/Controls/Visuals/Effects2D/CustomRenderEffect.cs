#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D
{
  public interface ICustomRenderEffect : CustomEffect, DrawTransform
  {
    void Init(string effectName);
    Guid EffectId { get; }
  }

  /// <summary>
  /// Effect for rendering video using different transforms and effects.
  /// </summary>
  public abstract class CustomRenderEffect<T> : CustomEffectBase, ICustomRenderEffect where T : struct
  {
    protected static readonly Dictionary<string, Tuple<Guid, Guid>> ShaderGuidCache = new Dictionary<string, Tuple<Guid, Guid>>(StringComparer.InvariantCultureIgnoreCase);
    protected Guid _effectId;
    protected Guid _effectIdVertextShader;
    protected DrawInformation _drawInformation;
    protected T _effectParams;
    protected string _effectName;
    protected bool _fileMissing;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomRenderEffect{T}"/> class.
    /// </summary>
    protected CustomRenderEffect()
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="CustomRenderEffect{T}"/> class.
    /// </summary>
    protected CustomRenderEffect(string effectName)
    {
      Init(effectName);
    }

    public void Init(string effectName)
    {
      _effectName = effectName;
      Tuple<Guid, Guid> ids;
      // We are using the same Effect class for different effects. We keep track of every used effect and give them an unique ID.
      if (!ShaderGuidCache.TryGetValue(effectName, out ids))
      {
        ids = new Tuple<Guid, Guid>(Guid.NewGuid(), Guid.NewGuid());
        ShaderGuidCache[effectName] = ids;
      }
      _effectId = ids.Item1;
      _effectIdVertextShader = ids.Item2;
    }

    public Guid EffectId
    {
      get { return _effectId; }
    }

    public override void Initialize(EffectContext effectContext, TransformGraph transformGraph)
    {
      string[] files = _effectName.Split(';');
      if (files.Length == 0)
        return;

      StringBuilder effectShader = new StringBuilder(8196 * 4);
      // TODO: we uses this shader version for DX9 compatibility. If we can be sure that all used shaders are working with higher versions, than we can use higher supported versions
      // NOTE: the level "4_0_level_9_3" represents DX9.3 (see http://msdn.microsoft.com/en-us/library/windows/desktop/ff476876%28v=vs.85%29.aspx)
      const string shaderVersion = "4_0_level_9_3";

      for (int i = files.Length - 1; i >= 0; --i)
      {
        string effectFilePath = SkinContext.SkinResources.GetResourceFilePath(string.Format(@"{0}\{1}.fx", SkinResources.SHADERS_DIRECTORY, files[i]));
        if (effectFilePath == null || !File.Exists(effectFilePath))
        {
          if (!_fileMissing)
            ServiceRegistration.Get<ILogger>().Error("Effect file {0} does not exist", effectFilePath);
          _fileMissing = true;
          return;
        }
        _fileMissing = false;

        using (StreamReader reader = new StreamReader(effectFilePath))
          effectShader.Append(reader.ReadToEnd());

        // Concatenate
        effectShader.Append(Environment.NewLine);
      }

      effectShader.Replace("vs_2_0", String.Format("vs_{0}", shaderVersion));
      effectShader.Replace("ps_2_0", String.Format("ps_{0}", shaderVersion));

      try
      {
        const ShaderFlags shaderFlags = ShaderFlags.OptimizationLevel3 | ShaderFlags.EnableBackwardsCompatibility;
        var vshader = ShaderBytecode.Compile(effectShader.ToString(), "RenderVertexShader", "vs_" + shaderVersion, shaderFlags);
        var pshader = ShaderBytecode.Compile(effectShader.ToString(), "RenderPixelShader", "ps_" + shaderVersion, shaderFlags);

        effectContext.LoadVertexShader(_effectIdVertextShader, vshader.Bytecode);
        effectContext.LoadPixelShader(_effectId, pshader.Bytecode);
        transformGraph.SetSingleTransformNode(this);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("EffectAsset: Unable to load '{0}'", ex, _effectName);
      }
    }

    public override void PrepareForRender(ChangeType changeType)
    {
      UpdateConstants();
    }

    public override void SetGraph(TransformGraph transformGraph)
    {
      throw new NotImplementedException();
    }

    public virtual void SetDrawInformation(DrawInformation drawInfo)
    {
      _drawInformation = drawInfo;
      _drawInformation.SetPixelShader(_effectId, PixelOptions.None);
      _drawInformation.SetInputDescription(0, new InputDescription(Filter.MinimumMagLinearMipPoint, 1));
      _drawInformation.SetVertexProcessing(null, VertexOptions.None, null, null, _effectIdVertextShader);
    }

    public virtual Rectangle MapInvalidRect(int inputIndex, Rectangle invalidInputRect)
    {
      return invalidInputRect;
    }

    public virtual Rectangle MapInputRectanglesToOutputRectangle(Rectangle[] inputRects, Rectangle[] inputOpaqueSubRects, out Rectangle outputOpaqueSubRect)
    {
      if (inputRects.Length != InputCount)
        throw new ArgumentException("InputRects must be length of " + InputCount, "inputRects");
      outputOpaqueSubRect = default(Rectangle);
      return inputRects[0];
    }

    public virtual void MapOutputRectangleToInputRectangles(Rectangle outputRect, Rectangle[] inputRects)
    {
      int expansion = 0;
      if (inputRects.Length != InputCount)
        throw new ArgumentException("InputRects must be length of " + InputCount, "inputRects");
      for (int idx = 0; idx < inputRects.Length; idx++)
      {
        inputRects[idx].Left = outputRect.Left - expansion;
        inputRects[idx].Top = outputRect.Top - expansion;
        inputRects[idx].Right = outputRect.Right + expansion;
        inputRects[idx].Bottom = outputRect.Bottom + expansion;
      }
    }

    public virtual int InputCount
    {
      get { return 1; }
    }

    public virtual void UpdateConstants()
    {
      if (_drawInformation != null)
      {
        _drawInformation.SetPixelConstantBuffer(ref _effectParams);
        _drawInformation.SetVertexConstantBuffer(ref _effectParams);
      }
    }
  }
}
