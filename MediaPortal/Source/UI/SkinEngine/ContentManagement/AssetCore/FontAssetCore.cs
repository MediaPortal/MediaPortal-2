#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MediaPortal.UI.SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D9;
using Tao.FreeType;
using FontFamily = MediaPortal.UI.SkinEngine.Fonts.FontFamily;

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  /// <summary>
  /// Represents a font set (of glyphs).
  /// </summary>
  public class FontAssetCore : TemporaryAssetCoreBase, IAssetCore, ITextureAsset
  {
    public event AssetAllocationHandler AllocationChanged = delegate { };

    protected const int MAX_WIDTH = 1024;
    protected const int MAX_HEIGHT = 1024;
    protected const int PAD = 1;

    protected FontFamily _family;
    private readonly BitmapCharacterSet _charSet;
    protected Texture _texture = null;

    protected uint _resolution;
    protected int _currentX = 0;
    protected int _rowHeight = 0;
    protected int _currentY = 0;

    #region Ctor

    /// <summary>
    /// Creates a new font set.
    /// </summary>
    /// <param name="family">The font family.</param>
    /// <param name="size">Size in pixels.</param>
    /// <param name="resolution">Resolution in dpi.</param>
    public FontAssetCore(FontFamily family, int size, uint resolution)
    {
      _family = family;
      _resolution = resolution;

      FT_FaceRec face = (FT_FaceRec) Marshal.PtrToStructure(_family.Face, typeof(FT_FaceRec));

      _charSet = new BitmapCharacterSet(face.num_glyphs)
        {
          RenderedSize = size,
          Width = MAX_WIDTH,
          Height = MAX_HEIGHT
        };
      _charSet.Base = _charSet.RenderedSize * face.ascender / face.height;
      _charSet.MaxHeight = (int) Math.Ceiling(_charSet.RenderedSize * (face.bbox.yMax + Math.Abs(face.bbox.yMin)) / (double)face.height);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Get the size of this <see cref="FontAssetCore"/> in pixels.
    /// </summary>
    public float Size
    {
      get { return _charSet.RenderedSize; }
    }

    /// <summary>
    /// Gets the <see cref="FontAssetCore"/>'s base for the given font size.
    /// </summary>
    public float Base(float fontSize)
    {
      return _charSet.Base * fontSize / _charSet.RenderedSize;
    }

    /// <summary>
    /// Gets the height of the <see cref="FontAssetCore"/> if scaled to a different size.
    /// </summary>
    /// <param name="fontSize">The scale size.</param>
    /// <returns>The height of the scaled font.</returns>
    public float LineHeight(float fontSize)
    {
      return fontSize;
    }

    /// <summary>
    /// Gets the width of a string if rendered with this <see cref="FontAssetCore"/> as a particular size.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <param name="fontSize">The size of font to use for measurement.</param>
    /// <param name="kerning">Whether kerning is used to improve font spacing.</param>
    /// <returns>The width of the passed text.</returns>
    public float TextWidth(string text, float fontSize, bool kerning)
    {
      return PartialTextWidth(text, 0, text.Length - 1, fontSize, kerning) + CharWidthExtension(text, text.Length - 1, fontSize);
    }

    /// <summary>
    /// Gets the width of a sub-string if rendered with this <see cref="FontAssetCore"/> as a particular size.
    /// </summary>
    /// <param name="text">The string to measure.</param>
    /// <param name="fromIndex">The index of the first character of the sub-string.</param>
    /// <param name="toIndex">The index of the last character of the sub-string to measure.</param>
    /// <param name="fontSize">The size of font to use for measurement.</param>
    /// <param name="kerning">Whether kerning is used to improve font spacing.</param>
    /// <returns>The width of the passed text.</returns>
    public float TextWidth(string text, int fromIndex, int toIndex, float fontSize, bool kerning)
    {
      return PartialTextWidth(text, fromIndex, toIndex, fontSize, kerning) + CharWidthExtension(text, toIndex, fontSize);
    }

    /// <summary>
    /// Gets the width of a sub-string if rendered with this <see cref="FontAssetCore"/> as a particular size, excluding the 
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
      if (!IsAllocated)
        Allocate();

      float width = 0;
      BitmapCharacter lastChar = null;

      for (int i = fromIndex; i <= toIndex; i++)
      {
        BitmapCharacter c = Character(text[i]);

        width += c.XAdvance;
        if (kerning && lastChar != null)
          width += GetKerningAmount(lastChar, text[i]);
        lastChar = c;
      }
      return width * (fontSize / _charSet.RenderedSize);
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
      if (charIndex < 0 || charIndex >= text.Length) return 0.0f;
      BitmapCharacter c = Character(text[charIndex]);
      return Math.Max(c.Width - c.XAdvance + c.XOffset, 0) * (fontSize / _charSet.RenderedSize);
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
      if (lineCount == 1) 
        return _charSet.MaxHeight;
      return LineHeight(fontSize) * (lineCount + 1) - Base(fontSize) - 1.0f;
    }

    /// <summary>
    /// Gets the font texture.
    /// </summary>
    public Texture Texture
    {
      get
      {
        if (!IsAllocated)
          Allocate();
        KeepAlive();
        return _texture;
      }
    }

    #endregion

    #region Font map initialization

    /// <summary>
    /// Creates the font map texture.
    /// </summary>
    public void Allocate()
    {
      lock (_syncObj)
      {
        if (IsAllocated)
          return;
        _texture = new Texture(GraphicsDevice.Device, MAX_WIDTH, MAX_HEIGHT, 1, Usage.Dynamic, Format.L8, Pool.Default);
        // Add 'not defined' glyph
        AddGlyph(0);
      }

      AllocationChanged(AllocationSize);
    }

    /// <summary>
    /// Adds a glyph to the font set by character code.
    /// </summary>
    /// <param name="character">The char to add.</param>
    private bool AddChar(char character)
    {
      return AddGlyph(GlyphIndex(character));
    }

    /// <summary>Adds a glyph to the font set by glyph index.</summary>
    /// <param name="glyphIndex">The index of the glyph to add.</param>
    private bool AddGlyph(uint glyphIndex)
    {
      // FreeType measures font size in terms Of 1/64ths of a point.
      // 1 point = 1/72th of an inch. Resolution is in dots (pixels) per inch.
      // Locking is also required here to avoid accessing to texture when handling glyphs (can lead to AccessViolationException)
      lock (_syncObj)
      {
        float pointSize = 64.0f * _charSet.RenderedSize * 72.0f / _resolution;
        FT.FT_Set_Char_Size(_family.Face, (int) pointSize, 0, _resolution, 0);

        // Font does not contain that glyph, the 'missing' glyph will be shown instead
        if (glyphIndex == 0 && _charSet.GetCharacter(0) != null)
          return false;

        // Load the glyph for the current character.
        if (FT.FT_Load_Glyph(_family.Face, glyphIndex, FT.FT_LOAD_DEFAULT) != 0)
          return false;

        FT_FaceRec face = (FT_FaceRec) Marshal.PtrToStructure(_family.Face, typeof(FT_FaceRec));

        IntPtr glyphPtr;
        // Load the glyph data into our local array.
        if (FT.FT_Get_Glyph(face.glyph, out glyphPtr) != 0)
          return false;

        // Convert the glyph to bitmap form.
        if (FT.FT_Glyph_To_Bitmap(ref glyphPtr, FT_Render_Mode.FT_RENDER_MODE_NORMAL, IntPtr.Zero, 1) != 0)
          return false;

        // Get the structure fron the intPtr
        FT_BitmapGlyph glyph = (FT_BitmapGlyph) Marshal.PtrToStructure(glyphPtr, typeof(FT_BitmapGlyph));

        // Width/height of char
        int cwidth = glyph.bitmap.width;
        int cheight = glyph.bitmap.rows;

        // Width/height of char including padding
        int pwidth = cwidth + 3 * PAD;
        int pheight = cheight + 3 * PAD;

        // Check glyph index is in the character set range.
        if (!_charSet.IsInRange(glyphIndex))
          return false;

        // Check glyph fits in our texture
        if (_currentX + pwidth > MAX_WIDTH)
        {
          _currentX = 0;
          _currentY += _rowHeight;
          _rowHeight = 0;
        }
        if (_currentY + pheight > MAX_HEIGHT)
          return false;

        // Create and store a BitmapCharacter for this glyph
        _charSet.SetCharacter(glyphIndex, CreateCharacter(glyph));

        // Copy the glyph bitmap to our local array
        Byte[] bitmapBuffer = new Byte[cwidth * cheight];
        if (glyph.bitmap.buffer != IntPtr.Zero)
          Marshal.Copy(glyph.bitmap.buffer, bitmapBuffer, 0, cwidth * cheight);

        // Write glyph bitmap to our texture
        WriteGlyphToTexture(glyph, pwidth, pheight, bitmapBuffer);

        _currentX += pwidth;
        _rowHeight = Math.Max(_rowHeight, pheight);

        // Free the glyph
        FT.FT_Done_Glyph(glyphPtr);
      }
      return true;
    }

    private uint GlyphIndex(uint charIndex)
    {
      lock (_syncObj)
        return FT.FT_Get_Char_Index(_family.Face, charIndex);
    }

    private BitmapCharacter CreateCharacter(FT_BitmapGlyph glyph)
    {
      lock (_syncObj)
        return new BitmapCharacter
          {
            Width = glyph.bitmap.width + PAD * 2,
            Height = glyph.bitmap.rows + PAD * 2,
            X = _currentX,
            Y = _currentY,
            XOffset = glyph.left,
            YOffset = _charSet.Base - glyph.top,
            // Convert fixed point 16.16 to float by divison with 2^16
            XAdvance = (int) (glyph.root.advance.x / 65536.0f)
          };
    }

    private void WriteGlyphToTexture(FT_BitmapGlyph glyph, int pwidth, int pheight, Byte[] bitmapBuffer)
    {
      lock (_syncObj)
      {
        // Lock the the area we intend to update
        Rectangle charArea = new Rectangle(_currentX, _currentY, pwidth, pheight);
        DataRectangle rect = _texture.LockRectangle(0, charArea, LockFlags.None);
        using (rect.Data)
        {
          // Copy FreeType glyph bitmap into our font texture.
          Byte[] fontPixels = new Byte[pwidth];
          Byte[] padPixels = new Byte[pwidth];

          int pitch = Math.Abs(glyph.bitmap.pitch);

          // Write the first padding row
          rect.Data.Write(padPixels, 0, pwidth);
          rect.Data.Seek(MAX_WIDTH - pwidth, SeekOrigin.Current);
          // Write the glyph
          for (int y = 0; y < glyph.bitmap.rows; y++)
          {
            for (int x = 0; x < glyph.bitmap.width; x++)
              fontPixels[x + PAD] = bitmapBuffer[y * pitch + x];
            rect.Data.Write(fontPixels, 0, pwidth);
            rect.Data.Seek(MAX_WIDTH - pwidth, SeekOrigin.Current);
          }
          // Write the last padding row
          rect.Data.Write(padPixels, 0, pwidth);
          rect.Data.Seek(MAX_WIDTH - pwidth, SeekOrigin.Current);
          _texture.UnlockRectangle(0);
        }
      }
    }

    #endregion

    #region Text creation

    /// <summary>Adds a new string to the list to render.</summary>
    /// <param name="text">Text to render.</param>
    /// <param name="size">Font size.</param>
    /// <param name="kerning">True to use kerning, false otherwise.</param>
    /// <param name="textSize">Output size of the created text.</param>
    /// <param name="lineIndex">Output indices of the first vertex for of each line of text.</param>
    /// <returns>An array of vertices representing a triangle list.</returns>
    public PositionColoredTextured[] CreateText(string[] text, float size, bool kerning, out SizeF textSize, out int[] lineIndex)
    {
      if (!IsAllocated)
        Allocate();

      List<PositionColoredTextured> verts = new List<PositionColoredTextured>();
      float[] lineWidth = new float[text.Length];
      int liney = _charSet.RenderedSize - _charSet.Base;
      float sizeScale = size / _charSet.RenderedSize;

      lineIndex = new int[text.Length];

      for (int i = 0; i < text.Length; ++i)
      {
        int ix = verts.Count;

        lineWidth[i] = CreateTextLine(text[i], liney, sizeScale, kerning, ref verts);
        lineIndex[i] = ix;
        liney += _charSet.RenderedSize;
      }

      textSize = new SizeF(0.0f, verts[verts.Count - 1].Y);

      // Stores the line widths as the Z coordinate of the verices. This means alignment
      // can be performed by a vertex shader durng rendering
      PositionColoredTextured[] vertArray = verts.ToArray();
      for (int i = 0; i < lineIndex.Length; ++i)
      {
        float width = lineWidth[i];
        int end = (i < lineIndex.Length - 1) ? lineIndex[i + 1] : vertArray.Length;
        for (int j = lineIndex[i]; j < end; ++j)
          vertArray[j].Z = width;
        textSize.Width = Math.Max(lineWidth[i], textSize.Width);
      }

      KeepAlive();
      return vertArray;
    }

    protected float CreateTextLine(string line, float y, float sizeScale, bool kerning, ref List<PositionColoredTextured> verts)
    {
      lock (_syncObj)
      {
        int x = 0;

        BitmapCharacter lastChar = null;
        foreach (char character in line)
        {
          BitmapCharacter c = Character(character);
          // Adjust for kerning
          if (kerning && lastChar != null)
            x += GetKerningAmount(lastChar, character);
          lastChar = c;
          if (!char.IsWhiteSpace(character))
            CreateQuad(c, sizeScale, x, y, ref verts);
          x += c.XAdvance;
        }
        // Make sure there is a t least one character
        if (verts.Count == 0)
        {
          BitmapCharacter c = Character(' ');
          CreateQuad(c, sizeScale, c.XOffset, c.YOffset, ref verts);
        }
        return x*sizeScale;
      }
    }

    protected void CreateQuad(BitmapCharacter c, float sizeScale, float x, float y, ref List<PositionColoredTextured> verts)
    {
      x += c.XOffset;
      y += c.YOffset;
      PositionColoredTextured tl = new PositionColoredTextured(
          x * sizeScale, y * sizeScale, 1.0f,
          (c.X + 0.5f) / _charSet.Width,
          c.Y / (float) _charSet.Height,
          0
          );
      PositionColoredTextured br = new PositionColoredTextured(
          (x + c.Width) * sizeScale,
          (y + c.Height) * sizeScale,
          1.0f,
          (c.X + c.Width) / (float) _charSet.Width,
          (c.Y + c.Height - 0.5f) / _charSet.Height,
          0
          );
      PositionColoredTextured bl = new PositionColoredTextured(tl.X, br.Y, 1.0f, tl.Tu1, br.Tv1, 0);
      PositionColoredTextured tr = new PositionColoredTextured(br.X, tl.Y, 1.0f, br.Tu1, tl.Tv1, 0);

      verts.Add(tl);
      verts.Add(bl);
      verts.Add(tr);

      verts.Add(tr);
      verts.Add(bl);
      verts.Add(br);
    }

    protected BitmapCharacter Character(char character)
    {
      lock (_syncObj)
      {
        uint glyphIndex = GlyphIndex(character);
        BitmapCharacter result = _charSet.GetCharacter(glyphIndex);
        if (glyphIndex == 0)
          result = null;
        if (result == null)
        {
          if (!AddGlyph(glyphIndex))
            return _charSet.GetCharacter(0);
          return _charSet.GetCharacter(glyphIndex);
        }
        return result;
      }
    }

    protected int GetKerningAmount(BitmapCharacter first, char second)
    {
      Kerning result = first.KerningList.Where(node => node.Second == second).FirstOrDefault();
      return result == null ? 0 : result.Amount;
    }
    #endregion

    #region IAssetCore implementation

    public bool IsAllocated
    {
      get { return _texture != null; }
    }

    public void Free()
    {
      if (_texture != null)
      {
        if (AllocationChanged != null)
          AllocationChanged(-AllocationSize);
        _texture.Dispose();
        _texture = null;
        _charSet.Clear();
        _currentX = 0;
        _rowHeight = 0;
        _currentY = 0;
      }
    }

    public int AllocationSize
    {
      get { return IsAllocated ? MAX_WIDTH * MAX_HEIGHT * 1 : 0; }
    }

    #endregion
  }

  /// <summary>
  /// Represents a single bitmap character set.
  /// </summary>
  internal class BitmapCharacterSet
  {
    public int Base;
    public int RenderedSize;
    public int Width;
    public int Height;
    public int MaxHeight;
    private BitmapCharacter[] _characters;

    public BitmapCharacterSet(int numberOfGlyphs)
    {
      _characters = new BitmapCharacter[numberOfGlyphs];
    }

    public BitmapCharacter GetCharacter(uint index)
    {
      return IsInRange(index) ? _characters[index] : null;
    }

    public bool SetCharacter(uint index, BitmapCharacter character)
    {
      if (!IsInRange(index))
        return false;
      _characters[index] = character;
      return true;
    }

    public bool IsInRange(uint index)
    {
      return index < _characters.Length;
    }

    public void Clear()
    {
      _characters = new BitmapCharacter[_characters.Length];
    }
  }

  /// <summary>
  /// Represents a single bitmap character.
  /// </summary>
  public class BitmapCharacter : ICloneable
  {
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public int XOffset;
    public int YOffset;
    public int XAdvance;
    public int Page;
    public List<Kerning> KerningList = new List<Kerning>();

    /// <summary>
    /// Clones the BitmapCharacter.
    /// </summary>
    /// <returns>Cloned BitmapCharacter.</returns>
    public object Clone()
    {
      BitmapCharacter result = new BitmapCharacter
        {
          X = X,
          Y = Y,
          Width = Width,
          Height = Height,
          XOffset = XOffset,
          YOffset = YOffset,
          XAdvance = XAdvance
        };
      result.KerningList.AddRange(KerningList);
      result.Page = Page;
      return result;
    }
  }

  /// <summary>
  /// Represents kerning information for a character.
  /// </summary>
  public class Kerning
  {
    public int Second;
    public int Amount;
  }
}