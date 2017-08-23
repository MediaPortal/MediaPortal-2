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

using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  /// <summary>
  /// <see cref="SimpleImageEffect"/> provides a shader that allows setting the partitial filename
  /// (<see cref="PartitialEffectName"/>) of an image shader from XAML.
  /// </summary>
  public class SimpleImageEffect : ImageEffect
  {
    #region Protected fields

    protected AbstractProperty _partialShaderEffectProperty;

    #endregion

    #region Ctor & maintainance

    public SimpleImageEffect()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _partialShaderEffectProperty = new SProperty(typeof(string), "none");
    }

    void Attach()
    {
      _partialShaderEffectProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _partialShaderEffectProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      SimpleImageEffect el = (SimpleImageEffect) source;
      PartitialEffectName = el.PartitialEffectName;
      Attach();
    }

    private void OnPropertyChanged(AbstractProperty property, object oldvalue)
    {
      _partialShaderEffect = PartitialEffectName;
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    #endregion

    #region Properties

    public AbstractProperty PartialShaderEffectProperty
    {
      get { return _partialShaderEffectProperty; }
    }

    /// <summary>
    /// Gets or sets the name of the partitial shader to use. A corresponding shader file must be present in the skin's shader effects
    /// directory (directory <c>shaders\effects</c>). Only the file name without extension is required, folder name and <c>.fx</c> extension
    /// are added internally.
    /// </summary>
    public string PartitialEffectName
    {
      get { return (string) _partialShaderEffectProperty.GetValue(); }
      set { _partialShaderEffectProperty.SetValue(value); }
    }

    #endregion
  }
}
