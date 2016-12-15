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

using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using SharpDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  /// <summary>
  /// Represents a font set (of glyphs).
  /// </summary>
  public class FontAsset : AssetWrapper<FontAssetCore>, ITextureAsset
  {
    public delegate void DeallocationHandler();

    /// <summary>
    /// Event for notifying this assets clients that the underlying texture has been deallocated, as that will required text to be re-generated.
    /// </summary>
    public event DeallocationHandler Deallocated;

    public FontAsset(FontAssetCore core) : base(core) 
    {
      _assetCore.AllocationChanged += CoreAllocationHandler;
    }

    /// <summary>
    /// Get the size of this <see cref="FontAsset"/> in pixels.
    /// </summary>
    public float Size
    {
      get { return _assetCore.Size; }
    }

    /// <summary>
    /// Gets the <see cref="FontAsset"/>'s ascender for the given <paramref name="fontSize"/>.
    /// The ascender is the portion of a letter from its base line to the top; the ascender is the size of most upper case letters.
    /// The ascender is smaller than the <see cref="LineHeight"/>; some letters like <c>y</c> or <c>j</c> add descender below the
    /// base line. Together, ascender and descender are the <see cref="LineHeight"/>.
    /// </summary>
    /// <param name="fontSize">The scale size.</param>
    public float Ascender(float fontSize)
    {
      return _assetCore.Ascender(fontSize); 
    }

    /// <summary>
    /// Gets the height of the <see cref="FontAsset"/> if scaled to a different size.
    /// </summary>
    /// <param name="fontSize">The scale size.</param>
    /// <returns>The height of the scaled font.</returns>
    public float LineHeight(float fontSize)
    {
      return _assetCore.LineHeight(fontSize);
    }

    /// <summary>
    /// Gets the width of a string if rendered with this <see cref="FontAsset"/> as a particular size.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <param name="fontSize">The size of font to use for measurement.</param>
    /// <param name="kerning">Whether kerning is used to improve font spacing.</param>
    /// <returns>The width of the passed text.</returns>
    public float TextWidth(string text, float fontSize, bool kerning)
    {
      return _assetCore.TextWidth(text, fontSize, kerning);
    }

    /// <summary>
    /// Gets the width of a sub-string if rendered with this <see cref="FontAsset"/> as a particular size.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <param name="fromIndex">The index of the first character of the sub-string.</param>
    /// <param name="toIndex">The index of the last character of the sub-string to measure.</param>
    /// <param name="fontSize">The size of font to use for measurement.</param>
    /// <param name="kerning">Whether kerning is used to improve font spacing.</param>
    /// <returns>The width of the passed text.</returns>
    public float TextWidth(string text, int fromIndex, int toIndex, float fontSize, bool kerning)
    {
      return _assetCore.TextWidth(text, fromIndex, toIndex, fontSize, kerning);
    }

    /// <summary>
    /// Gets the width of a sub-string if rendered with this <see cref="FontAsset"/> as a particular size, excluding the 
    /// special additional width required for the last char.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <param name="fromIndex">The index of the first character of the sub-string.</param>
    /// <param name="toIndex">The index of the last character of the sub-string to measure.</param>
    /// <param name="fontSize">The size of font to use for measurement.</param>
    /// <param name="kerning">Whether kerning is used to improve font spacing.</param>
    /// <returns>The width of the sub-string text.</returns>
    public float PartialTextWidth(string text, int fromIndex, int toIndex, float fontSize, bool kerning)
    {
      return _assetCore.PartialTextWidth(text, fromIndex, toIndex, fontSize, kerning);
    }

    /// <summary>
    /// In order to accurately determine the length of a string the final character may need to have a small
    /// additional width applied to compensate for the amount that it would normally over-hang the following 
    /// character. This function returns the value of that extension for a given character in the passed string
    /// </summary>
    /// <param name="text">The string containing the character to measure.</param>
    /// <param name="charIndex">The index of the character in the string.</param>
    /// <param name="fontSize">The size of font to use for measurement.</param>
    /// <returns>The additonal width required for the specified character.</returns>
    public float CharWidthExtension(string text, int charIndex, float fontSize)
    {
      return _assetCore.CharWidthExtension(text, charIndex, fontSize);
    }

    /// <summary>
    /// Get the height of a text block containing the specified number of lines. In order to get correct vertical 
    /// centering we add an additonal value to compensate for the space required under the font's base line.
    /// </summary>
    /// <param name="fontSize">The actual font size.</param>
    /// <param name="lineCount">The number of lines.</param>
    /// <returns>The height of the text.</returns>
    public float TextHeight(float fontSize, int lineCount)
    {
      return _assetCore.TextHeight(fontSize, lineCount);
    }

    /// <summary>Adds a new string to the list to render.</summary>
    /// <param name="text">Text to render.</param>
    /// <param name="size">Font size.</param>
    /// <param name="kerning">True to use kerning, false otherwise.</param>
    /// <param name="textSize">Output size of the created text.</param>
    /// <param name="lineIndex">Output indices of the first vertex for of each line of text.</param>
    /// <returns>An array of vertices representing a triangle list.</returns>
    public PositionColoredTextured[] CreateText(string[] text, float size, bool kerning, out SizeF textSize, out int[] lineIndex)
    {
      return _assetCore.CreateText(text, size, kerning, out textSize, out lineIndex);
    }

    public void Allocate()
    {
      _assetCore.Allocate();
    }

    private void CoreAllocationHandler(int allocationDelta)
    {
      if (allocationDelta < 0 && Deallocated != null)
        Deallocated();
    }

    #region ITextureAsset Members

    public Texture Texture
    {
      get { return _assetCore.Texture; }
    }

    #endregion
  }
}