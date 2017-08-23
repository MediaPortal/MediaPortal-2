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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.SkinEngine.Players
{
  public class PlayerRotationTranslator
  {
    public static RightAngledRotation TranslateToRightAngledRotation(ImageRotation imageRotation)
    {
      switch (imageRotation)
      {
        case ImageRotation.Rot_0:
          return RightAngledRotation.Zero;
        case ImageRotation.Rot_90:
          return RightAngledRotation.HalfPi;
        case ImageRotation.Rot_180:
          return RightAngledRotation.Pi;
        case ImageRotation.Rot_270:
          return RightAngledRotation.ThreeHalfPi;
      }
      return RightAngledRotation.Zero;
    }
  }
}