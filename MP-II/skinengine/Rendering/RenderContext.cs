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
    TextureAsset _texture;
    VertexBuffer _vertices;
    int _primitiveCount;
    int _verticeCount;
    List<PrimitiveContext> _primitives = new List<PrimitiveContext>();

    #region properties
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

    public TextureAsset Texture
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
    public int VerticeCount
    {
      get
      {
        return _verticeCount;
      }
      set
      {
        _verticeCount = value;
      }
    }
    #endregion

    public void Clear()
    {
      _primitives.Clear();
      _primitiveCount = 0;
    }

    public void Add(PrimitiveContext context)
    {
      _primitives.Add(context);
      _primitiveCount += context.PrimitiveCount;
      _verticeCount += context.Vertices.Length;
    }

    public void Finish()
    {
      _vertices = new VertexBuffer(GraphicsDevice.Device, PositionColoredTextured.StrideSize * _verticeCount, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
      using (DataStream stream = _vertices.Lock(0, 0, LockFlags.Discard))
      {
        foreach (PrimitiveContext primitive in _primitives)
        {
          stream.WriteRange(primitive.Vertices);
        }
      }
      _vertices.Unlock();
    }


    public void Render()
    {
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
    }

    #region IDisposable Members

    public void Dispose()
    {
      if (_vertices != null)
      {
        _vertices.Dispose();
        _vertices = null;
      }
    }

    #endregion
  }
}
