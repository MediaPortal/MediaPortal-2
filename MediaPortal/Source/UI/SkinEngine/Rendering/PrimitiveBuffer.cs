#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.DirectX;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  public class PrimitiveBuffer : IDisposable
  {
    #region Protected fields

    protected VertexBuffer _vertexBuffer = null;
    protected PrimitiveType _primitiveType = PrimitiveType.TriangleList;
    protected int _primitiveCount = 0;
    protected static int _allocationCount = 0;

    #endregion

    #region Ctor & maintainance

    public PrimitiveBuffer()
    {
    }

    /// <summary>
    /// Contructor to intialise with an allocated but empty buffer.
    /// </summary>
    /// <param name="numVertices">The number of vertices to allocate.</param>
    public PrimitiveBuffer(int numVertices)
    {
      Create(numVertices);
    }

    /// <summary>
    /// Constructor to fully initialise the buffer with the passed vertices.
    /// </summary>
    /// <param name="vertices">The vertex data to copy.</param>
    /// <param name="primitiveType">The primitive type to be used when rendering.</param>
    public PrimitiveBuffer(ref PositionColoredTextured[] vertices, PrimitiveType primitiveType)
    {
      Set(ref vertices, primitiveType);
    }

    public void Dispose()
    {
      Clear();
    }

    #endregion

    #region Buffer creation & disposal

    /// <summary>
    /// Allocates an empty vertex buffer.
    /// </summary>
    /// <param name="numVertices">The number of vertices to allocate.</param>
    public void Create(int numVertices)
    {
      if (_vertexBuffer != null)
        Clear();
      _vertexBuffer = new VertexBuffer(GraphicsDevice.Device, PositionColoredTextured.StrideSize * numVertices, Usage.WriteOnly, PositionColoredTextured.Format, Pool.Default);
      ++_allocationCount;
    }

    /// <summary>
    /// Copies the given vertex data into a vertex buffer, re-allocating it if necessary.
    /// </summary>
    /// <param name="vertices">The vertex data to copy.</param>
    /// <param name="primitiveType">The primitive type to be used when rendering.</param>
    public void Set(ref PositionColoredTextured[] vertices,  PrimitiveType primitiveType)
    {
      if (vertices == null)
      {
        Clear();
        return;
      }
      int numVertices = vertices.Length;
      if (_vertexBuffer == null || numVertices > VertexCount)
        Create(numVertices);

      switch (primitiveType)
      {
        // case PrimitiveType.PointList:
        case PrimitiveType.LineList:
          if (numVertices % 2 != 0 || numVertices < 2)
            ThrowInvalidVertexCount();
          _primitiveCount = numVertices / 2;
          break;
        case PrimitiveType.LineStrip:
          if (numVertices < 2)
            ThrowInvalidVertexCount();
          _primitiveCount = numVertices - 1;
          break;
        case PrimitiveType.TriangleList:
          if (numVertices < 3 || numVertices % 3 != 0)
            ThrowInvalidVertexCount();
          _primitiveCount = numVertices / 3;
          break;
        case PrimitiveType.TriangleStrip:
        case PrimitiveType.TriangleFan:
          if (numVertices < 3)
            ThrowInvalidVertexCount();
          _primitiveCount = numVertices - 2;
          break;
        default:
          throw new NotImplementedException("Unknown PrimitiveType");
      }
      _primitiveType = primitiveType;

      // Update vertices to compensate texel/pixel coordinate origins (top left of pixel vs. center of texel)
      // See https://msdn.microsoft.com/en-us/library/bb219690(VS.85).aspx
      for (int i = 0; i < vertices.Length; i++)
      {
        vertices[i].X -= 0.5f;
        vertices[i].Y -= 0.5f;
      }
      using (DataStream stream = _vertexBuffer.Lock(0, 0, LockFlags.None))
        stream.WriteRange(vertices);
      _vertexBuffer.Unlock();
    }

    protected void Clear()
    {
      if (_vertexBuffer != null)
      {
        _vertexBuffer.Dispose();
        --_allocationCount;
        _vertexBuffer = null;
      }
      _primitiveCount = 0;
    }

    public static void SetPrimitiveBuffer(ref PrimitiveBuffer _buffer, ref PositionColoredTextured[] verts, PrimitiveType type)
    {
      if (_buffer == null)
        _buffer = new PrimitiveBuffer();
      _buffer.Set(ref verts, type);
    }

    public static void DisposePrimitiveBuffer(ref PrimitiveBuffer _buffer)
    {
      if (_buffer != null)
        _buffer.Dispose();
      _buffer = null;
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Renders the resource.
    /// </summary>
    /// <param name="stream"></param>
    public void Render(int stream)
    {
      if (_vertexBuffer == null)
        return;
      GraphicsDevice.Device.VertexFormat = VertexFormat;

      GraphicsDevice.Device.SetRenderState(RenderState.MultisampleAntialias, true);

      GraphicsDevice.Device.SetStreamSource(stream, _vertexBuffer, 0, StrideSize);
      GraphicsDevice.Device.DrawPrimitives(_primitiveType, 0, _primitiveCount);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets a value indicating the resource is allocated.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset is allocated; otherwise, <c>false</c>.
    /// </value>
    public bool IsAllocated
    {
      get { return _vertexBuffer != null && _primitiveCount > 0; }
    }
    /// <summary>
    /// Gets the internal <see cref="VertexBuffer"/> object.
    /// </summary>
    public VertexBuffer VertexBuffer
    {
      get { return _vertexBuffer; }
    }

    /// <summary>
    /// Gets the <see cref="PrimitiveType"/> used for rendering this buffer.
    /// </summary>
    public PrimitiveType PrimitiveType
    {
      get { return _primitiveType; }
    }

    /// <summary>
    /// Gets the <see cref="VertexFormat"/> of this buffer.
    /// </summary>
    public VertexFormat VertexFormat
    {
      get { return PositionColoredTextured.Format; }
    }

    /// <summary>
    /// Gets the stride between vertices used for this buffer.
    /// </summary>
    public int StrideSize
    {
      get { return PositionColoredTextured.StrideSize; }
    }

    /// <summary>
    /// Gets the number of primitives stored in this buffer.
    /// </summary>
    public int PrimitiveCount
    {
      get { return _primitiveCount; }
    }

    /// <summary>
    /// Returns the total number of allocated <see cref="PrimitiveBuffer"/>s.
    /// </summary>
    static public int GlobalAllocationCount
    {
      get { return _allocationCount; }
    }

    /// <summary>
    /// Calculates the number of vertices stored in this buffer.
    /// </summary>
    /// <returns></returns>
    public int VertexCount
    {
      get { return (_vertexBuffer == null) ? 0 : (_vertexBuffer.Description.SizeInBytes / StrideSize); }
    }

    #endregion

    #region Private members

    private static void ThrowInvalidVertexCount()
    {
      throw new ArgumentException("Vertex count does not match primitive type");
    }

    #endregion
  }
}
