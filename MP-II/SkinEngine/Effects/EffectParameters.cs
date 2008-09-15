using System;
using System.Collections.Generic;
using System.Text;
using SlimDX;
namespace MediaPortal.SkinEngine.Effects
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
      protected Color4 _color;
      public ParamContextColor(EffectHandleAsset handle, Color4 color)
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
    class ParamContextFloat2 : ParamContext
    {
      protected EffectHandleAsset _handle;
      protected float[] _values = new float[2];
      public ParamContextFloat2(EffectHandleAsset handle, float[] values)
      {
        _handle = handle;
        _values[0] = values[0];
        _values[1] = values[1];
      }
      public void Set()
      {
        _handle.SetParameter(_values);
      }
      public override bool Equals(object obj)
      {
        ParamContextFloat2 c = obj as ParamContextFloat2;
        if (c == null) return false;
        if (_handle != c._handle || _values[0] != c._values[0] || _values[1] != c._values[1]) return false;
        return true;
      }

      public override int GetHashCode()
      {
        return _handle.GetHashCode() ^ _values[0].GetHashCode() ^ _values[1].GetHashCode();
      }
    };
    class ParamContextFloat : ParamContext
    {
      protected EffectHandleAsset _handle;
      protected float _value;
      public ParamContextFloat(EffectHandleAsset handle, float value)
      {
        _handle = handle;
        _value = value;
      }
      public void Set()
      {
        _handle.SetParameter(_value);
      }
      public override bool Equals(object obj)
      {
        ParamContextFloat c = obj as ParamContextFloat;
        if (c == null) return false;
        if (_handle != c._handle || _value != c._value) return false;
        return true;
      }

      public override int GetHashCode()
      {
        return _handle.GetHashCode() ^ _value.GetHashCode();
      }
    };
    class ParamContextMatrix : ParamContext
    {
      protected EffectHandleAsset _handle;
      protected SlimDX.Matrix _matrix;
      public ParamContextMatrix(EffectHandleAsset handle, SlimDX.Matrix m)
      {
        _handle = handle;
        _matrix = m;
      }
      public void Set()
      {
        _handle.SetParameter(_matrix);
      }
      public override bool Equals(object obj)
      {
        ParamContextMatrix c = obj as ParamContextMatrix;
        if (c == null) return false;
        if (_handle != c._handle || _matrix != c._matrix) return false;
        return true;
      }

      public override int GetHashCode()
      {
        return _handle.GetHashCode() ^ _matrix.GetHashCode();
      }
    };

    List<ParamContext> _params = new List<ParamContext>();


    public void Add(EffectHandleAsset handle, Color4 color)
    {
      _params.Add(new ParamContextColor(handle, color));
    }

    public void Add(EffectHandleAsset handle, float[] v)
    {
      _params.Add(new ParamContextFloat2(handle, v));
    }

    public void Add(EffectHandleAsset handle, float v)
    {
      _params.Add(new ParamContextFloat(handle, v));
    }
    public void Add(EffectHandleAsset handle, SlimDX.Matrix m)
    {
      _params.Add(new ParamContextMatrix(handle, m));
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
