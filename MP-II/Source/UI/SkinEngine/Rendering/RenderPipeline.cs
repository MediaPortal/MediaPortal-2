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

using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  /// <summary>
  /// Holds all primitives to be rendered, grouped by <see cref="RenderGroup"/>.
  /// </summary>
  /// <remarks>
  /// Primitives are pieces of information to be rendered, each containing a buffer of
  /// vertices, a texture and an effect plus parameters.
  /// A <see cref="RenderGroup"/> optimizes the calculations in the shader; it groups
  /// all objects with the same texture/effect combination together and contains the union
  /// set of all vertice buffers of all primitives. So we only need to invoke the shaders
  /// for each render group, instead of for each primitive.
  /// </remarks>

  // TODO:
  //  - make primitive type in render group configurable
  //  - sort by texture, effect
  //  - render front->back (improves zbuffer performance)
  //  - find solution for changing world transformations
  //  - use 1 big texture
  // 
  // left todo:
  //  - image control
  //  - path control fill
  // 
  public class RenderPipeline : IAsset
  {
    static readonly RenderPipeline _instance = new RenderPipeline();
    readonly List<PrimitiveContext> _primitives = new List<PrimitiveContext>();
    readonly List<PrimitiveContext> _newPrimitives = new List<PrimitiveContext>();
    readonly List<RenderGroup> _renderList = new List<RenderGroup>();
    bool _sort = false;

    public static RenderPipeline Instance
    {
      get { return _instance; }
    }

    public RenderPipeline()
    {
      ContentManager.Add(this);
    }

    public void Add(PrimitiveContext context)
    {
      if (SkinContext.UseBatching == false) return;
      if (context == null) return;
      if (context.Effect == null || context.Parameters == null)
        return;

      lock (_primitives)
        _newPrimitives.Add(context);
    }

    public void Remove(PrimitiveContext primitive)
    {
      if (SkinContext.UseBatching == false) return;
      if (primitive == null) return;
      
      lock (_primitives)
      {
        if (primitive.RenderGroup != null)
        {
          RenderGroup renderGroup = primitive.RenderGroup;
          renderGroup.Remove(primitive);
          primitive.RenderGroup = null;
        }
        _primitives.Remove(primitive);
        _newPrimitives.Remove(primitive);
      }
    }

    public void Clear()
    {
      for (int i = 0; i < _renderList.Count; ++i)
        _renderList[i].Dispose();
      _renderList.Clear();
    }

    void CreateBatches()
    {
      _sort = false;
      Clear();
      foreach (PrimitiveContext primitive in _primitives)
      {
        bool processed = false;
        foreach (RenderGroup rendercontext in _renderList)
        {
          if (rendercontext.Effect.Equals(primitive.Effect) &&
              rendercontext.Parameters.Equals(primitive.Parameters) &&
              rendercontext.Texture == primitive.Texture &&
              rendercontext.PrimitiveType == primitive.PrimitiveType &&
              rendercontext.VertexFormat == primitive.VertexFormat &&
              rendercontext.StrideSize == primitive.StrideSize)
          {
            rendercontext.Add(primitive);
            processed = true;
            break;
          }
        }
        if (!processed)
        {
          RenderGroup rendercontext = new RenderGroup
            {
                Effect = primitive.Effect,
                Parameters = primitive.Parameters,
                Texture = primitive.Texture,
                PrimitiveType = primitive.PrimitiveType,
                VertexFormat = primitive.VertexFormat,
                StrideSize = primitive.StrideSize
            };
          rendercontext.Add(primitive);
          _renderList.Add(rendercontext);
        }
      }
    }

    void PlaceNewPrimitivesInBatches()
    {
      foreach (PrimitiveContext primitive in _newPrimitives)
      {
        bool processed = false;
        foreach (RenderGroup rendercontext in _renderList)
        {
          if (rendercontext.Effect.Equals(primitive.Effect) &&
              rendercontext.Parameters.Equals(primitive.Parameters) &&
              rendercontext.Texture == primitive.Texture &&
              rendercontext.PrimitiveType == primitive.PrimitiveType &&
              rendercontext.VertexFormat == primitive.VertexFormat &&
              rendercontext.StrideSize == primitive.StrideSize)
          {
            rendercontext.Add(primitive);
            processed = true;
            _primitives.Add(primitive);
            break;
          }
        }
        if (!processed)
        {
          RenderGroup rendercontext = new RenderGroup
            {
                Effect = primitive.Effect,
                Parameters = primitive.Parameters,
                Texture = primitive.Texture,
                PrimitiveType = primitive.PrimitiveType,
                VertexFormat = primitive.VertexFormat,
                StrideSize = primitive.StrideSize
            };
          rendercontext.Add(primitive);
          _renderList.Add(rendercontext);
          _primitives.Add(primitive);
        }
      }
      _newPrimitives.Clear();
    }

    public void Render()
    {
      lock (_primitives)
      {
        if (_sort)
          CreateBatches();
        if (_newPrimitives.Count > 0)
          PlaceNewPrimitivesInBatches();
        List<RenderGroup> old = new List<RenderGroup>();
        foreach (RenderGroup context in _renderList)
          if (!context.Render())
            old.Add(context);

        foreach (RenderGroup context in old)
          _renderList.Remove(context);
      }
    }

    #region IAsset Members

    public bool IsAllocated
    {
      get { return true; }
    }

    public bool CanBeDeleted
    {
      get { return false; }
    }

    public bool Free(bool force)
    {
      if (!force) return false;
      foreach (RenderGroup context in _renderList)
        context.Dispose();
      return false;
    }

    #endregion
  }
}
