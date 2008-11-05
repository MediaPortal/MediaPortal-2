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

using System.Collections.Generic;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Rendering
{
  /// <summary>
  /// TODO:
  ///  - sort by texture, effect
  ///  - render front->back (improves zbuffer performance)
  ///  - find solution for changing world transformations
  ///  - use 1 big texture
  /// 
  /// left todo:
  ///  - image control
  ///  - path control fill
  /// 
  /// </summary>
  public class RenderPipeline : IAsset
  {
    static RenderPipeline _instance = new RenderPipeline();
    List<PrimitiveContext> _primitives = new List<PrimitiveContext>();
    List<PrimitiveContext> _newPrimitives = new List<PrimitiveContext>();
    List<RenderContext> _renderList = new List<RenderContext>();
    bool _sort = false;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
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
      {
        return;
      }

      lock (_primitives)
      {
        _newPrimitives.Add(context);
      }
    }

    public void Remove(PrimitiveContext primitive)
    {
      if (SkinContext.UseBatching == false) return;
      if (primitive == null) return;
      
      lock (_primitives)
      {
        if (primitive.RenderContext != null)
        {
          RenderContext renderContext = primitive.RenderContext;
          renderContext.Remove(primitive);
          primitive.RenderContext = null;
        }
        bool res1 = _primitives.Remove(primitive);
        bool res2 = _newPrimitives.Remove(primitive);
      }
    }

    public void Clear()
    {
      for (int i = 0; i < _renderList.Count; ++i)
      {
        _renderList[i].Dispose();
      }
      _renderList.Clear();
    }

    void CreateBatches()
    {
      _sort = false;
      Clear();
      foreach (PrimitiveContext primitive in _primitives)
      {
        bool processed = false;
        foreach (RenderContext rendercontext in _renderList)
        {
          if (rendercontext.Effect.Equals(primitive.Effect) &&
              rendercontext.Parameters.Equals(primitive.Parameters) &&
              rendercontext.Texture == primitive.Texture)
          {
            rendercontext.Add(primitive);
            processed = true;
            break;
          }
        }
        if (!processed)
        {
          RenderContext rendercontext = new RenderContext();
          rendercontext.Effect = primitive.Effect;
          rendercontext.Parameters = primitive.Parameters;
          rendercontext.Texture = primitive.Texture;
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
        foreach (RenderContext rendercontext in _renderList)
        {
          if (rendercontext.Effect.Equals(primitive.Effect) &&
              rendercontext.Parameters.Equals(primitive.Parameters) &&
              rendercontext.Texture == primitive.Texture)
          {
            rendercontext.Add(primitive);
            processed = true;
            _primitives.Add(primitive);
            break;
          }
        }
        if (!processed)
        {
          RenderContext rendercontext = new RenderContext();
          rendercontext.Effect = primitive.Effect;
          rendercontext.Parameters = primitive.Parameters;
          rendercontext.Texture = primitive.Texture;
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
        {
          CreateBatches();
        }
        if (_newPrimitives.Count > 0)
        {
          PlaceNewPrimitivesInBatches();
        }
        List<RenderContext> old = new List<RenderContext>();
        foreach (RenderContext context in _renderList)
        {
          if (!context.Render())
          {
            old.Add(context);
          }
        }

        foreach (RenderContext context in old)
        {
          _renderList.Remove(context);
        }
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
      foreach (RenderContext context in _renderList)
      {
        context.Dispose();
      }
      return false;
    }

    #endregion
  }
}
