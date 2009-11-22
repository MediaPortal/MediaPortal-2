#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.DirectX;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  public class PrimitiveContext
  {
    EffectAsset _effect;
    EffectParameters _parameters;
    ITextureAsset _texture;
    PositionColored2Textured[] _vertices;
    int _primitiveCount;
    RenderContext _renderContext;

    public PrimitiveContext()
    {
    }

    public PrimitiveContext( int primitiveCount, ref PositionColored2Textured[] vertices)
    {
      _primitiveCount = primitiveCount;
      _vertices = vertices;
    }

    #region Properties

    public RenderContext RenderContext
    {
      get { return _renderContext; }
      set { _renderContext = value; }
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

    public PositionColored2Textured[] Vertices
    {
      get { return _vertices; }
      set { _vertices = value; }
    }

    public int PrimitiveCount
    {
      get { return _primitiveCount; }
      set { _primitiveCount = value; }
    }

    #endregion

    public void OnVerticesChanged(int primitiveCount, ref PositionColored2Textured[] vertices)
    {
      _primitiveCount = primitiveCount;
      _vertices = vertices;
      if (_renderContext != null)
        _renderContext.UpdateVertices = true;
    }
  }
}
