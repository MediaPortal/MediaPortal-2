#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects2D
{
  /// <summary>
  /// Provides a base class for all bitmap effects.
  /// </summary>
  public abstract class Effect : DependencyObject
  {
    #region Fields

    protected Bitmap1 _input;
    protected AbstractProperty _cacheProperty;

    #endregion

    #region (De-)Allocation

    protected Effect()
    {
      _cacheProperty = new SProperty(typeof(bool), false);
      Attach();
    }

    void Attach()
    {
      _cacheProperty.Attach(EffectChanged);
    }

    void Detach()
    {
      _cacheProperty.Detach(EffectChanged);
    }

    public bool IsAllocated
    {
      get { return Output != null; }
    }

    public virtual bool Allocate()
    {
      if (_input == null)
      {
        Deallocate();
        return false;
      }
      Attach();
      return true;
    }

    public virtual void Deallocate()
    {
      Detach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Deallocate();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Indicates if the output of effect can be cached. This is only possible, if the content control (<see cref="Input"/>) doesn't change.
    /// </summary>
    public bool Cache
    {
      get { return (bool)_cacheProperty.GetValue(); }
      set { _cacheProperty.SetValue(value); }
    }

    public AbstractProperty CacheProperty
    {
      get { return _cacheProperty; }
    }

    #endregion

    #region Processing properties

    public Bitmap1 Input
    {
      get { return _input; }
      set
      {
        bool changed = _input != value;
        _input = value;
        if (changed)
          Allocate();
      }
    }

    public abstract SharpDX.Direct2D1.Effect Output { get; }

    #endregion

    #region Protected methods

    protected abstract void EffectChanged(AbstractProperty property, object oldvalue);

    #endregion
  }
}
