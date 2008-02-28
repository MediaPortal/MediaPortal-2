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
    TextureAsset _texture;
    PositionColored2Textured[] _vertices;
    int _primitiveCount;

    public PrimitiveContext()
    {
    }
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

  }
}
