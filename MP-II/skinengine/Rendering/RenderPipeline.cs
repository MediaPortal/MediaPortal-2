using System;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Rendering
{
  public class RenderPipeline
  {
    static RenderPipeline _instance = new RenderPipeline();
    List<PrimitiveContext> _primitives = new List<PrimitiveContext>();
    List<RenderContext> _renderList = new List<RenderContext>();
    bool _sort = false;

    public static RenderPipeline Instance
    {
      get
      {
        return _instance;
      }
    }
    public void Add(PrimitiveContext context)
    {
      _primitives.Add(context);
      _sort = true;
    }

    public void Remove(PrimitiveContext context)
    {
      _primitives.Remove(context);
      _sort = true;
    }

    void Clear()
    {
      foreach (RenderContext context in _renderList)
      {
        context.Dispose();
      }
      _renderList.Clear();
    }

    void Sort()
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
      foreach (RenderContext context in _renderList)
      {
        context.Finish();
      }
    }

    public void Render()
    {
      if (_sort)
      {
        Sort();
      }
      foreach (RenderContext context in _renderList)
      {
        context.Render();
      }
    }
  }
}
