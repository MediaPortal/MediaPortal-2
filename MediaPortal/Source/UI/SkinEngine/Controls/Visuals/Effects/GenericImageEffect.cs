#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
  /// <see cref="GenericImageEffect"/> provides a Shader that allows setting the partitial filename (<see cref="PartitialEffectName"/>) of an image shader from XAML. 
  /// </summary>
  public class GenericImageEffect : ImageEffect
  {
    #region Protected fields

    protected AbstractProperty _partialShaderEffectProperty;

    #endregion

    #region Ctor & maintainance

    public GenericImageEffect()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _partialShaderEffectProperty = new SProperty(typeof(string), "effects\\none");
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
      GenericImageEffect el = (GenericImageEffect) source;
      PartitialEffectName = el.PartitialEffectName;
      Attach();
    }

    private void OnPropertyChanged(AbstractProperty property, object oldvalue)
    {
      _partialShaderEffect = "effects\\" + PartitialEffectName;
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
    /// Gets or sets the name of the partitial shader (directory \shaders\effects). Only the file name is required, folder name and .fx is added internally.
    /// </summary>
    public string PartitialEffectName
    {
      get { return (string) _partialShaderEffectProperty.GetValue(); }
      set { _partialShaderEffectProperty.SetValue(value); }
    }

    #endregion
  }
}
