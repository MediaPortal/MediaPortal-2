using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine;
namespace SkinEngine.Rendering
{
  public class RenderPipeline : IAsset
  {
    static RenderPipeline _instance = new RenderPipeline();
    List<PrimitiveContext> _primitives = new List<PrimitiveContext>();
    List<PrimitiveContext> _newPrimitives = new List<PrimitiveContext>();
    List<RenderContext> _renderList = new List<RenderContext>();
    bool _sort = false;

    public static RenderPipeline Instance
    {
      get
      {
        return _instance;
      }
    }

    public RenderPipeline()
    {
      ContentManager.Add(this);
    }

    public void Add(PrimitiveContext context)
    {
      _newPrimitives.Add(context);
    }

    public void Remove(PrimitiveContext primitive)
    {
      if (primitive.RenderContext != null)
      {
        RenderContext renderContext = primitive.RenderContext;
        renderContext.Remove(primitive);

      }
      _primitives.Remove(primitive);
    }

    void Clear()
    {
      foreach (RenderContext context in _renderList)
      {
        context.Dispose();
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
