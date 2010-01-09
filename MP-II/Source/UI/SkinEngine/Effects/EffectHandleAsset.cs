#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Effects
{
  public class EffectHandleAsset
  {
    EffectHandle _handle;
    string _name;
    EffectAsset _asset;

    public EffectHandleAsset(string name, EffectAsset asset)
    {
      _handle = null;
      _name = name;
      _asset = asset;
    }

    public EffectHandle Handle
    {
      get
      {
        return _handle;
      }
      set
      {
        _handle = value;
      }
    }

    public void SetParameter(Color4 color)
    {
      if (_asset.Effect == null)
      {
        _asset.Allocate();
      }
      if (_handle == null)
      {
        _handle = _asset.Effect.GetParameter(null, _name);
      }
      _asset.Effect.SetValue(_handle, color);
    }

    public void SetParameter(float[] floatArray)
    {
      if (_asset.Effect == null)
      {
        _asset.Allocate();
      }
      if (_handle == null)
      {
        _handle = _asset.Effect.GetParameter(null, _name);
      }
      _asset.Effect.SetValue(_handle, floatArray);
    }

    public void SetParameter(float floatValue)
    {
      if (_asset.Effect == null)
      {
        _asset.Allocate();
      }
      if (_handle == null)
      {
        _handle = _asset.Effect.GetParameter(null, _name);
      }
      _asset.Effect.SetValue(_handle, floatValue);
    }

    public void SetParameter(Matrix matrix)
    {
      if (_asset.Effect == null)
      {
        _asset.Allocate();
      }
      if (_handle == null)
      {
        _handle = _asset.Effect.GetParameter(null, _name);
      }
      _asset.Effect.SetValue(_handle, matrix);
    }
    public void SetParameter(Texture tex)
    {
      if (_asset.Effect == null)
      {
        _asset.Allocate();
      }
      if (_handle == null)
      {
        _handle = _asset.Effect.GetParameter(null, _name);
      }
      _asset.Effect.SetTexture(_handle, tex);
    }
  }
}
