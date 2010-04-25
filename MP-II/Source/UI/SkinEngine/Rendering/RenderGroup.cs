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
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  public class RenderGroup : IDisposable
  {
    EffectAsset _effect;
    EffectParameters _parameters;
    ITextureAsset _texture;
    VertexBuffer _vertices;
    VertexFormat _vertexFormat;
    int _strideSize;
    PrimitiveType _primitiveType;
    int _primitiveCount;
    readonly List<PrimitiveContext> _primitives = new List<PrimitiveContext>();
    bool _updateVertices;

    #region Properties

    public bool UpdateVertices
    {
      get { return _updateVertices; }
      set { _updateVertices = value; }
    }

    public EffectAsset Effect
    {
      get { return _effect; }
      set { _effect = value; }
    }

    public EffectParameters Parameters
    {
      get { return _parameters; }
      set { _parameters = value; }
    }

    public ITextureAsset Texture
    {
      get { return _texture; }
      set { _texture = value; }
    }

    public VertexBuffer Vertices
    {
      get { return _vertices; }
      set { _vertices = value; }
    }

    public VertexFormat VertexFormat
    {
      get { return _vertexFormat; }
      set { _vertexFormat = value; }
    }

    public int StrideSize
    {
      get { return _strideSize; }
      set { _strideSize = value; }
    }

    public int PrimitiveCount
    {
      get { return _primitiveCount; }
      set { _primitiveCount = value; }
    }

    public PrimitiveType PrimitiveType
    {
      get { return _primitiveType; }
      set { _primitiveType = value; }
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
      primitive.RenderGroup = this;
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
        if (primitive.Vertices != null)
        {
          verticeCount += primitive.Vertices.Length;
          _primitiveCount += primitive.NumVertices;
        }
      }
      if (verticeCount > 0)
      {
        _vertices = new VertexBuffer(GraphicsDevice.Device, PositionColored2Textured.StrideSize * verticeCount, Usage.Dynamic, PositionColored2Textured.Format, Pool.Default);
        using (DataStream stream = _vertices.Lock(0, 0, LockFlags.Discard))
        {
          foreach (PrimitiveContext primitive in _primitives)
            if (primitive.Vertices != null)
              stream.WriteRange(primitive.Vertices);
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
        if (!_texture.IsAllocated)
          _texture.Allocate();

      _parameters.Set();
      _effect.StartRender(_texture == null ? null : _texture.Texture);
      GraphicsDevice.Device.VertexFormat = _vertexFormat;
      GraphicsDevice.Device.SetStreamSource(0, _vertices, 0, _strideSize);
      GraphicsDevice.Device.DrawPrimitives(_primitiveType, 0, _primitiveCount);
      _effect.EndRender();

      if (_texture != null)
        _texture.KeepAlive();
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
