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

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store a thumb for a series and corresponding information
  /// </summary>
  public class SeriesThumbStub
  {
    #region Enums / Classes

    public enum ThumbAspect
    {
      Banner,
      Poster,
      Fanart
    }

    public enum ThumbType
    {
      Series,
      Season
    }

    public class Color
    {
      public Color(byte r, byte g, byte b) { R = r; G = g; B = b; }
      public byte R { get; set; }
      public byte G { get; set; }
      public byte B { get; set; }
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Thumb as byte array
    /// </summary>
    public byte[] Thumb { get; set; }

    /// <summary>
    /// Aspect of the Thumb
    /// </summary>
    public ThumbAspect? Aspect { get; set; }

    /// <summary>
    /// Light color to paint on the Thumb
    /// </summary>
    public Color LightAccentColor { get; set; }

    /// <summary>
    /// Dark color to paint on the Thumb
    /// </summary>
    public Color DarkAccentColor { get; set; }

    /// <summary>
    /// Midtone color to paint on the Thumb
    /// </summary>
    public Color NeutralMidtoneColor { get; set; }


    /// <summary>
    /// Number of the season this Thumb relates to; if <c>null</c>, the Thumb relates to the whole series
    /// </summary>
    public int? Season { get; set; }

    /// <summary>
    /// Indicates whether the Thumb relates to a single season or the whole series
    /// </summary>
    public ThumbType Type
    {
      get { return Season == null ? ThumbType.Series : ThumbType.Season; }
    }

    #endregion
  }
}
