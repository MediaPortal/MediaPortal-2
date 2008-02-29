using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Effects;
using SkinEngine.DirectX;
using SkinEngine.Controls.Brushes;
namespace SkinEngine.Rendering
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

    #region properties

    public RenderContext RenderContext
    {
      get
      {
        return _renderContext;
      }
      set
      {
        _renderContext = value;
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

    public PositionColored2Textured[] Vertices
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


    public void OnVerticesChanged(int primitiveCount, ref PositionColored2Textured[] vertices)
    {
      _primitiveCount = primitiveCount;
      _vertices = vertices;
      if (_renderContext != null)
      {
        _renderContext.UpdateVertices = true;
      }
    }
  }
}
