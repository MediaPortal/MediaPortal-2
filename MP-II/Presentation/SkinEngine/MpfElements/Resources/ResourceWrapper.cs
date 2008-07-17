#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a value object which cannot directly be used. This may be the case if
  /// the object is resolved by a markup extension, for example. Instances of this class
  /// will be automatically converted to the underlaying <see cref="Resource"/> object.
  /// </summary>
  public class ResourceWrapper : ValueWrapper
  {
    #region Protected fields

    protected bool _freezable = false;

    #endregion

    #region Ctor

    public ResourceWrapper()
    { }

    public ResourceWrapper(object resource): base(resource)
    { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ResourceWrapper rw = (ResourceWrapper) source;
      Freezable = copyManager.GetCopy(rw.Freezable);
    }

    #endregion

    #region Public properties

    public object Resource
    {
      get { return Value; }
      set { Value = value; }
    }

    public bool Freezable
    {
      get { return _freezable; }
      set { _freezable = value; }
    }

    #endregion
  }
}
