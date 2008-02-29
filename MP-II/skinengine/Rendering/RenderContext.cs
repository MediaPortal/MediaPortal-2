using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Effects;
using SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;

namespace SkinEngine.Rendering
{
  public class RenderContext : IDisposable
  {
    EffectAsset _effect;
    EffectParameters _parameters;
    ITextureAsset _texture;
    VertexBuffer _vertices;
    int _primitiveCount;
    List<PrimitiveContext> _primitives = new List<PrimitiveContext>();
    bool _updateVertices;

    #region properties
    public bool UpdateVertices
    {
      get
      {
        return _updateVertices;
      }
      set
      {
        _updateVertices = value;
      }
    }
    public EffectAsset Effect
    {
      get
      {
        return _effect;
      }
      set
      {
        _effect = value;
      }
    }

    public EffectParameters Parameters
    {
      get
      {
        return _parameters;
      }
      set
      {
        _parameters = value;
      }
    }

    public ITextureAsset Texture
    {
      get
      {
        return _texture;
      }
      set
      {
        _texture = value;
      }
    }

    public VertexBuffer Vertices
    {
      get
      {
        return _vertices;
      }
      set
      {
        _vertices = value;
      }
    }

    public int PrimitiveCount
    {
      get
      {
        return _primitiveCount;
      }
      set
      {
        _primitiveCount = value;
      }
    }
    #endregion

    public void Clear()
    {
      _primitives.Clear();
      _updateVertices = true;
      Dispose();
    }

    public void Remove(PrimitiveContext primitive)
    {
      _primitives.Remove(primitive);
      _updateVertices = true;
    }
    public void Add(PrimitiveContext primitive)
    {
      primitive.RenderContext = this;
      _primitives.Add(primitive);
      _updateVertices = true;
    }

    void UpdateVertexbuffer()
    {
      if (_vertices != null)
      {
        _vertices.Dispose();
        _vertices = null;
      }
      int verticeCount = 0;
      _primitiveCount = 0;
      foreach (PrimitiveContext primitive in _primitives)
      {
        verticeCount += primitive.Vertices.Length;
        _primitiveCount += primitive.PrimitiveCount;
      }
      if (verticeCount > 0)
      {
        _vertices = new VertexBuffer(GraphicsDevice.Device, PositionColoredTextured.StrideSize * verticeCount, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
        using (DataStream stream = _vertices.Lock(0, 0, LockFlags.Discard))
        {
          foreach (PrimitiveContext primitive in _primitives)
          {
            stream.WriteRange(primitive.Vertices);
          }
        }
        _vertices.Unlock();
      }
      _updateVertices = false;
    }


    public bool Render()
    {
      if (_updateVertices)
        UpdateVertexbuffer();
      if (_primitiveCount == 0) return false;

      if (_texture != null)
      {
        if (!_texture.IsAllocated)
          _texture.Allocate();
        _parameters.Set();
        _effect.StartRender(_texture.Texture);
        GraphicsDevice.Device.SetStreamSource(0, _vertices, 0, PositionColored2Textured.StrideSize);
        GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _primitiveCount);
        _effect.EndRender();
      }
      else
      {
        _parameters.Set();
        _effect.StartRender(null);
        GraphicsDevice.Device.SetStreamSource(0, _vertices, 0, PositionColored2Textured.StrideSize);
        GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _primitiveCount);
        _effect.EndRender();
      }
      return true;
    }

    #region IDisposable Members

    public void Dispose()
    {
      if (_vertices != null)
      {
        _vertices.Dispose();
        _vertices = null;
      }
      _updateVertices = true;
    }

    #endregion
  }
}
