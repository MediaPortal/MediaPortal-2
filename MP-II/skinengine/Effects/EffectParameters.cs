using System;
using System.Collections.Generic;
using System.Text;
using SlimDX;
namespace SkinEngine.Effects
{
  public class EffectParameters
  {
    interface ParamContext
    {
      void Set();
    };

    class ParamContextColor : ParamContext
    {
      protected EffectHandleAsset _handle;
      protected ColorValue _color;
      public ParamContextColor(EffectHandleAsset handle, ColorValue color)
      {
        _handle = handle;
        _color = color;
      }
      public void Set()
      {
        _handle.SetParameter(_color);
      }
      public override bool Equals(object obj)
      {
        ParamContextColor c = obj as ParamContextColor;
        if (c == null) return false;
        if (_handle != c._handle || _color != c._color) return false;
        return true;
      }

      public override int GetHashCode()
      {
        return _handle.GetHashCode() ^ _color.GetHashCode();
      }
    };

    List<ParamContext> _params = new List<ParamContext>();


    public void Add(EffectHandleAsset handle, ColorValue color)
    {
      _params.Add(new ParamContextColor(handle, color));
    }

    public void Set()
    {
      foreach (ParamContext context in _params)
      {
        context.Set();
      }
    }

    public override bool Equals(object obj)
    {
      EffectParameters p = obj as EffectParameters;
      if (p == null) return false;
      if (p._params.Count != this._params.Count) return false;
      for (int i = 0; i < _params.Count; ++i)
      {
        if (_params[i].Equals(p._params[i]) == false) return false;
      }
      return true;
    }

    public override int GetHashCode()
    {
      return _params.GetHashCode();
    }

  }
}
