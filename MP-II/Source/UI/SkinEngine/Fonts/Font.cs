#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using MediaPortal.UI.SkinEngine.ContentManagement;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using Tao.FreeType;

namespace MediaPortal.UI.SkinEngine.Fonts
{
  public class FontLibrary 
  {
    /// <summary>
    /// Singleton instance variable.
    /// </summary>
    private static FontLibrary _instance;
    private static IntPtr _library;

    private FontLibrary() 
    {
      FT.FT_Init_FreeType(out _library);
    }

    /// <summary>
    /// Returns the Singleton FontLibrary instance.
    /// </summary>
    /// <value>The Singleton FontLibrary instance.</value>
    public static FontLibrary Instance
    {
      get
      {
        if (_instance == null)
          _instance = new FontLibrary();
        return _instance;
      }
    }

    public IntPtr Library
    {
      get { return _library; }
    }
  }

  /// <summary>
  /// Represents a font family.
  /// </summary>
  public class FontFamily : IDisposable
  {
    private string _name;
    private IntPtr _library;
    private IntPtr _face;

    public List<Font> _fontList = new List<Font>();

    public FontFamily(string name, string filePathName)
    {
      _name = name;

      _library = FontLibrary.Instance.Library;
 
      // Load the requested font.
      if (FT.FT_New_Face(_library, filePathName, 0, out _face) != 0)
        throw new ArgumentException("Failed to load face.");
    }

    /// <summary>
    /// Name of font family.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Adds a font set to the font family.
    /// </summary>
    public Font Addfont(int fontSize, uint resolution)
    {
      // Do we already have this font?
      foreach (Font font in _fontList)
        if (font.Size == fontSize)
          return font;
      Font newFont = new Font(_library, _face, fontSize, resolution);
      _fontList.Add(newFont);
      return newFont;
    }

    public void Dispose()
    {
      if (_face != IntPtr.Zero)
        FT.FT_Done_Face(_face);
      foreach (Font font in _fontList)
        font.Free(true);
    }
  }

  /// <summary>
  /// Represents a font set (of glyphs).
  /// </summary>
  public class Font : ITextureAsset
  {
    public enum Align
    {
      Left,
      Center,
      Right
    }

    private const int MAX_WIDTH = 1024;
    private const int MAX_HEIGHT = 1024;
    private const int PAD = 1;

    private IntPtr _face;
    private IntPtr _library;

    private BitmapCharacterSet _charSet;
    private List<FontQuad> _quads;
    private List<StringBlock> _strings;
    private Texture _texture = null;
    public const int MaxVertices = 4096;
    private int _nextChar;

    private uint _resolution;
    private int _currentX = 0;
    private int _rowHeight = 0;
    private int _currentY = 0;

    private float _firstCharWidth = 0;
    EffectAsset _effect;

    /// <summary>Creates a new font set.</summary>
    /// <param name="library">The free type library ptr.</param>
    /// <param name="face">The face ptr.</param>
    /// <param name="size">Size in pixels.</param>
    /// <param name="resolution">Resolution in dpi.</param>
    public Font(IntPtr library, IntPtr face, int size, uint resolution)
    {
      _quads = new List<FontQuad>();
      _strings = new List<StringBlock>();
      _library = library;
      _face = face;

      _resolution = resolution;

      _charSet = new BitmapCharacterSet();
      _charSet.RenderedSize = size;
      _charSet.LineHeight = size;

      _charSet.Width = MAX_WIDTH;
      _charSet.Height = MAX_HEIGHT;

      FT_FaceRec Face = (FT_FaceRec) Marshal.PtrToStructure(_face, typeof(FT_FaceRec));

      _charSet.Base = _charSet.RenderedSize * Face.ascender / Face.height;

      _texture = new Texture(GraphicsDevice.Device, MAX_WIDTH, MAX_HEIGHT, 1, Usage.None, Format.L8, Pool.Managed);

      // Add 'not defined' glyph
      AddGlyph(0);
      _effect = ContentManager.GetEffect("font");
    }

    /// <summary>Adds a glyph to the font set.</summary>
    /// <param name="charIndex">The char to add.</param>
    private bool AddGlyph(uint charIndex)
    {
      // FreeType measures font size in terms Of 1/64ths of a point.
      // 1 point = 1/72th of an inch. Resolution is in dots (pixels) per inch.

      float point_size = 64.0f*_charSet.RenderedSize * 72.0f / _resolution;
      FT.FT_Set_Char_Size(_face, (int) point_size, 0, _resolution, 0);
      uint glyphIndex = FT.FT_Get_Char_Index(_face, charIndex);
      
      // Font does not contain glyph
      if (glyphIndex == 0 && charIndex != 0)
      {
        // Copy 'not defined' glyph
        _charSet.SetCharacter(charIndex,_charSet.GetCharacter(0));
        return true;
      }

      // Load the glyph for the current character.
      if (FT.FT_Load_Glyph(_face, glyphIndex, FT.FT_LOAD_DEFAULT) != 0)
        return false;
        
      FT_FaceRec face = (FT_FaceRec) Marshal.PtrToStructure(_face, typeof(FT_FaceRec));

      IntPtr glyph;
      // Load the glyph data into our local array.
      if (FT.FT_Get_Glyph(face.glyph, out glyph) != 0)
        return false;

      // Convert the glyph to bitmap form.
      if (FT.FT_Glyph_To_Bitmap(ref glyph, FT_Render_Mode.FT_RENDER_MODE_NORMAL, IntPtr.Zero, 1) != 0)
        return false;

      // get the structure fron the intPtr
      FT_BitmapGlyph Glyph = (FT_BitmapGlyph) Marshal.PtrToStructure(glyph, typeof(FT_BitmapGlyph));

      // Width/height of char
      int cwidth = Glyph.bitmap.width;
      int cheight = Glyph.bitmap.rows;

      // Width/height of char including padding
      int pwidth = cwidth + 3 * PAD;
      int pheight = cheight + 3 * PAD;
  
      if (_currentX + pwidth > MAX_WIDTH)
      {
        _currentX = 0;
        _currentY += _rowHeight;
        _rowHeight = 0; 
      }

      if (_currentY  + pheight > MAX_HEIGHT)
        return false;

      BitmapCharacter Character = new BitmapCharacter(); 
  
      Character.Width = cwidth + PAD;
      Character.Height = cheight + PAD;

      Character.X = _currentX;
      Character.Y = _currentY;
      Character.XOffset = Glyph.left;
      Character.YOffset = _charSet.Base - Glyph.top;

      // Convert fixed point 16.16 to float by divison with 2^16
      Character.XAdvance = (int)(Glyph.root.advance.x / 65536.0f);

      _charSet.SetCharacter(charIndex, Character);
      // Copy the glyph bitmap to our local array
      Byte[] BitmapBuffer = new Byte[cwidth * cheight];

      if (Glyph.bitmap.buffer != IntPtr.Zero)
        Marshal.Copy(Glyph.bitmap.buffer, BitmapBuffer, 0, cwidth * cheight);

      // Lock the the area we intend to update
      Rectangle charArea = new Rectangle(_currentX, _currentY, pwidth, pheight);

      DataRectangle rect = _texture.LockRectangle(0, charArea, LockFlags.None);

      // Copy FreeType glyph bitmap into our font texture.
      Byte[] FontPixels = new Byte[pwidth];
      Byte[] PadPixels = new Byte[pwidth];

      int Pitch = Math.Abs(Glyph.bitmap.pitch);

      // Write the first padding row
      rect.Data.Write(PadPixels, 0, pwidth);
      rect.Data.Seek(MAX_WIDTH - pwidth, SeekOrigin.Current);

      // Write the glyph
      for (int y = 0; y < Glyph.bitmap.rows; y++)
      {
        for (int x = 0; x < Glyph.bitmap.width; x++)
          if (Glyph.bitmap.buffer == IntPtr.Zero || x >= Glyph.bitmap.width || y >= Glyph.bitmap.rows)
            // If we're outside the bounds of the FreeType bitmap
            // then fill with black.
            FontPixels[x + PAD] = 0;
          else
            // Otherwise copy the FreeType bits.
            FontPixels[x + PAD] = BitmapBuffer[y * Pitch + x]; 
        rect.Data.Write(FontPixels, 0, pwidth);
        rect.Data.Seek(MAX_WIDTH - pwidth, SeekOrigin.Current);
      }

      // Write the last padding row
      rect.Data.Write(PadPixels, 0, pwidth);
      rect.Data.Seek(MAX_WIDTH - pwidth, SeekOrigin.Current);

      _texture.UnlockRectangle(0);

      rect.Data.Dispose();

      _currentX += pwidth;
      _rowHeight = Math.Max(_rowHeight, pheight);

      // Free the glyph
      FT.FT_Done_Glyph(glyph);
      return true;
    }

    public float FirstCharWidth
    {
      get { return _firstCharWidth; }
    }

    public float Size
    {
      get { return _charSet.RenderedSize; }
    }

    public float Base
    {
      get { return _charSet.Base; }
    }

    public float LineHeight(float fontSize)
    {
      return fontSize / _charSet.RenderedSize * _charSet.LineHeight;
    }

    public float Width(string text, float fontSize)
    {
      float width = 0;
      float sizeScale = fontSize / _charSet.RenderedSize;
      
      for (int i = 0; i < text.Length; i++)
      {
        char chk = text[i];

        BitmapCharacter c = Character(chk);

        width += c.XAdvance * sizeScale;
        if (i != text.Length - 1)
        {
          _nextChar = text[i+1];
          Kerning kern = c.KerningList.Find(FindKerningNode);
          if (kern != null)
            width += kern.Amount*sizeScale;
        }
      }
      return width;
    }

    /// <summary>
    /// Calculates the maximum substring of the specified <paramref name="text"/>
    /// starting at index <paramref name="startIndex"/> which fits into the specified
    /// <paramref name="maxWidth"/>.
    /// </summary>
    /// <param name="text">Text string whose substring should be calculated.</param>
    /// <param name="fontSize">Size of the font to be used for the calculation.</param>
    /// <param name="startIndex">First index in string which denotes the character from which
    /// the width calculation starts.</param>
    /// <param name="maxWidth">Maximum width the substring is allowed to take.</param>
    /// <returns>Index of the first character in <paramref name="text"/> which doesn't
    /// fit any more into the specified <paramref name="maxWidth"/>.</returns>
    public int CalculateMaxSubstring(string text, float fontSize, int startIndex, float maxWidth)
    {
      float sizeScale = fontSize / _charSet.RenderedSize;
      
      for (int i = startIndex; i < text.Length; i++)
      {
        char chk = text[i];

        BitmapCharacter c = Character(chk);

        float charWidth = c.XAdvance * sizeScale;
        if (i != text.Length - 1)
        {
          _nextChar = text[i+1];
          Kerning kern = c.KerningList.Find(FindKerningNode);
          if (kern != null)
            charWidth += kern.Amount*sizeScale;
        }
        if (maxWidth >= charWidth)
          maxWidth -= charWidth;
        else
          return i+1;
      }
      return text.Length;
    }

    public float Height
    {
      get { return _charSet.Height; }
    }

    public List<FontQuad> Quads
    {
      get { return _quads; }
      set { _quads = value; }
    }

    FontQuad createQuad(BitmapCharacter c, Color4 Color, float x, float y, float z, float xOffset, float yOffset, float width, float height)
    {
      //float u2, v2;
      //u2 = v2 = 1;
      Vector3 uvPos = new Vector3(x + xOffset, y + yOffset, z);
      Vector3 finalScale = new Vector3(SkinContext.FinalTransform.Matrix.M11, SkinContext.FinalTransform.Matrix.M22, SkinContext.FinalTransform.Matrix.M33);
      Vector3 finalTranslation = new Vector3(SkinContext.FinalTransform.Matrix.M41, SkinContext.FinalTransform.Matrix.M42, SkinContext.FinalTransform.Matrix.M43);

      uvPos.X *= finalScale.X;
      uvPos.Y *= finalScale.Y;
      uvPos.Z *= finalScale.Z;
      uvPos.X += finalTranslation.X;
      uvPos.Y += finalTranslation.Y;
      uvPos.Z += finalTranslation.Z;
      //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);
      // Create the vertices
      PositionColored2Textured topLeft = new PositionColored2Textured(
        x + xOffset, 
        y + yOffset, 
        z,
        c.X / (float)_charSet.Width,
        c.Y / (float)_charSet.Height,
        Color.ToArgb());

      uvPos = new Vector3(topLeft.X + width, y + yOffset, z);
      uvPos.X *= finalScale.X;
      uvPos.Y *= finalScale.Y;
      uvPos.Z *= finalScale.Z;
      uvPos.X += finalTranslation.X;
      uvPos.Y += finalTranslation.Y;
      uvPos.Z += finalTranslation.Z;
      //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);

      PositionColored2Textured topRight = new PositionColored2Textured(
        topLeft.X + width,
        y + yOffset, 
        z ,
        (c.X + c.Width) / (float)_charSet.Width,
        c.Y / (float)_charSet.Height,
        Color.ToArgb());

      uvPos = new Vector3(topLeft.X + width, topLeft.Y + height, z);
      uvPos.X *= finalScale.X;
      uvPos.Y *= finalScale.Y;
      uvPos.Z *= finalScale.Z;
      uvPos.X += finalTranslation.X;
      uvPos.Y += finalTranslation.Y;
      uvPos.Z += finalTranslation.Z;
      //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);
      PositionColored2Textured bottomRight = new PositionColored2Textured(
        topLeft.X + width, 
        topLeft.Y + height, 
        z,
        (c.X + c.Width) / (float)_charSet.Width,
        (c.Y + c.Height) / (float)_charSet.Height,
        Color.ToArgb());

      uvPos = new Vector3(x + xOffset, topLeft.Y + height, z);
      uvPos.X *= finalScale.X;
      uvPos.Y *= finalScale.Y;
      uvPos.Z *= finalScale.Z;
      uvPos.X += finalTranslation.X;
      uvPos.Y += finalTranslation.Y;
      uvPos.Z += finalTranslation.Z;
      //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);
      PositionColored2Textured bottomLeft = new PositionColored2Textured(
        x + xOffset, 
        topLeft.Y + height, 
        z,
        c.X / (float)_charSet.Width,
        (c.Y + c.Height) / (float)_charSet.Height,
        Color.ToArgb());

      return new FontQuad(topLeft, topRight, bottomLeft, bottomRight);
    }

    /// <summary>Adds a new string to the list to render.</summary>
    /// <param name="text">Text to render</param>
    /// <param name="textBox">Rectangle to constrain text</param>
    /// <param name="alignment">Font alignment</param>
    /// <param name="size">Font size</param>
    /// <param name="color">Color</param>
    /// <param name="kerning">true to use kerning, false otherwise.</param>
    /// <returns>The index of the added StringBlock</returns>
    /// <param name="scroll"></param>
    /// <param name="textFits"></param>
    public int AddString(string text, RectangleF textBox, float zOrder, Align alignment, float size,
        Color4 color, bool kerning, bool scroll, out bool textFits, out float totalWidth)
    {
      StringBlock b = new StringBlock(text, textBox, zOrder, alignment, size, color, kerning);
      _strings.Add(b);
      _quads.AddRange(GetProcessedQuads(b, scroll, out textFits));
      if (_quads.Count > 0)
        totalWidth = _quads[_quads.Count - 1].TopRight.X - textBox.X;
      else
        totalWidth = 0;
      return _strings.Count-1;
    }

    /// <summary>Removes a string from the list of strings.</summary>
    /// <param name="i">Index to remove</param>
    public void ClearString(int i)
    {
      _strings.RemoveAt(i);
    }

    /// <summary>Clears the list of strings</summary>
    public void ClearStrings()
    {
      _strings.Clear();
      _quads.Clear();
    }

    public void Render(Device device, int count)
    {
      // Render
      _effect.StartRender(_texture);
      //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
      GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * count);
      _effect.EndRender();
    }

    public PositionColored2Textured[] Vertices
    {
      get
      {
        PositionColored2Textured[] verts = new PositionColored2Textured[6 * _quads.Count];
        int x = 0;
        foreach (FontQuad q in _quads)
        {
          verts[x++] = q.Vertices[0];
          verts[x++] = q.Vertices[1];
          verts[x++] = q.Vertices[2];
          verts[x++] = q.Vertices[3];
          verts[x++] = q.Vertices[4];
          verts[x++] = q.Vertices[5];
        }
        return verts;
      }
    }

    public int PrimitiveCount
    {
      get { return (_quads.Count * 2); }
    }

    public void Render(Device device, VertexBuffer buffer, out int count)
    {
      count = _quads.Count;
      if (buffer == null || _texture == null || count <= 0)
        return;
       
      // Add vertices to the buffer
      //GraphicsBuffer<SkinEngine.DirectX.PositionColored2Textured> gb =
      //GraphicsStream gb = buffer.Lock(0, 6 * count * PositionColored2Textured.StrideSize, LockFlags.Discard);

      using (DataStream stream = buffer.Lock(0, 0, LockFlags.None))
      {
        foreach (FontQuad q in _quads)
          stream.WriteRange(q.Vertices);
      }

      buffer.Unlock();

      _effect.StartRender(_texture);
      //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;

      GraphicsDevice.Device.SetTexture(0, _texture);
      GraphicsDevice.Device.SetStreamSource(0, buffer, 0, PositionColored2Textured.StrideSize);
      GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * count);

      _effect.EndRender();
    }

    private void doFadeOut(ref List<FontQuad> quads, bool scroll, string text)
    {
      float alpha = 1.0f;
      int startIndex = (int)(text.Length * 0.5f);
      float step = 0.9f / ((float)text.Length - startIndex);
      if (scroll)
        step = 1.0f / ((float)text.Length - startIndex);
      for (int i = 0; i < quads.Count; ++i)
      {
        if (quads[i].CharacterIndex < startIndex)
          continue;
        float charIndex = quads[i].CharacterIndex - startIndex;
        float charAlphaStart = alpha - (step * charIndex);
        float charAlphaEnd = alpha - (step * (1 + charIndex));
        for (int v = 0; v < quads[i].Vertices.Length; v++)
        {
          float newAlpha = charAlphaStart;
          if (v == 1 || v == 4 || v == 5)
            newAlpha = charAlphaEnd;
          uint color = (uint)quads[i].Vertices[v].Color;
          float colorA = color >> 24;
          colorA /= 255.0f;

          colorA *= newAlpha;
          uint alphaHex = (uint)((colorA * 255.0f));
          unchecked
          {
            alphaHex <<= 24;
            color = color & 0xffffff;
            color |= alphaHex;
          }

          quads[i].Vertices[v].Color = (int)color;
        }
      }
    }

    private BitmapCharacter Character(char c)
    {
      if (_charSet.GetCharacter(c) == null)
        AddGlyph(c);
      return _charSet.GetCharacter(c);
    }

    /// <summary>Gets the list of Quads from a StringBlock all ready to render.</summary>
    /// <param name="b">The string block</param>
    /// <param name="scroll">true if text should scroll</param>
    /// <param name="textFits"></param>
    /// <returns>List of Quads</returns>
    private List<FontQuad> GetProcessedQuads(StringBlock b, bool scroll, out bool textFits)
    {
      textFits = true;

      List<FontQuad> quads = new List<FontQuad>();

      string text = b.Text;
      double x = b.TextBox.X;
      double y = b.TextBox.Y;
      float maxWidth = b.TextBox.Width / SkinContext.Zoom.Width;
      float maxHeight = b.TextBox.Bottom / SkinContext.Zoom.Height;
      Align alignment = b.Alignment;
      double lineWidth = 0f;
      float sizeScale = b.Size / _charSet.RenderedSize;
      char lastChar = new char();
      int lineNumber = 1;
      int wordNumber = 1;
      double wordWidth = 0.0;
      bool firstCharOfLine = true;
      float z = b.ZOrder;
      bool fadeOut = false;
      if (text == null)
        return quads;

      for (int i = 0; i < text.Length; i++)
      {
        char chk = text[i];

        BitmapCharacter c = Character(chk);

        double xOffset = c.XOffset * sizeScale;
        double yOffset = c.YOffset * sizeScale;
        double xAdvance = c.XAdvance * sizeScale;
        double width = c.Width * sizeScale;
        double height = c.Height * sizeScale;

        // TODO: We need to rework this complicated Font code.

        // Albert: The following check makes texts disappear under certain circumstances, see Mantis #1676.
        // Check vertical bounds
        //if (y + yOffset + height > b.TextBox.Bottom)
        //  break;

        // Newline
        if (text[i] == '\n' || text[i] == '\r' || (lineWidth + xAdvance  > Math.Ceiling(maxWidth)))
        {
          //Trace.WriteLine("lineWidth xAdvance maxWidth " + lineWidth + " " + xAdvance + " " + maxWidth);
          if (y + yOffset + height + _charSet.LineHeight * sizeScale > Math.Ceiling(maxHeight))
          {
            textFits = false;
            fadeOut = true;

            if (scroll)
            {
              text = text.Substring(0, i);
              break;
            }
            else
            { // Line does not fit...
              // change last 3 chars to ...
              if (text.Length > 4)
              {
                while (true)
                {
                  int off = quads.Count - 1;
                  if (off >= 0 && quads[off].CharacterIndex >= i - 3)
                  {
                    x -= quads[off].XAdvance;
                    lineWidth -= quads[off].XAdvance;
                    quads.RemoveAt(off);
                  }
                  else
                    break;
                }
                if (i - 3 >= 1 & i - 3 < text.Length)
                {
                  text = text.Substring(0, i - 3) + "...";
                  i -= 4;
                }
              }
              continue;
            }
          }
          if (alignment == Align.Left)
            // Start at left
            x = b.TextBox.X;
          if (alignment == Align.Center)
            // Start in center
            x = b.TextBox.X + (maxWidth / 2f);
          else if (alignment == Align.Right)
            // Start at right
            x = b.TextBox.Right;

          y += _charSet.LineHeight * sizeScale;
          float offset = 0f;

          if ((lineWidth + xAdvance >= maxWidth) && (wordNumber != 1))
          {
            // Next character extends past text box width
            // We have to move the last word down one line
            char newLineLastChar = new char();
            lineWidth = 0f;
            for (int j = 0; j < quads.Count; j++)
            {
              if (alignment == Align.Left)
              {
                // Move current word to the left side of the text box
                if ((quads[j].LineNumber == lineNumber) &&
                    (quads[j].WordNumber == wordNumber))
                {
                  quads[j].LineNumber++;
                  quads[j].WordNumber = 1;
                  quads[j].X = (float)x + (quads[j].BitmapCharacter.XOffset * sizeScale);
                  quads[j].Y = (float)y + (quads[j].BitmapCharacter.YOffset * sizeScale);
                  x += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  lineWidth += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  if (b.Kerning)
                  {
                    _nextChar = quads[j].Character;
                    Kerning kern = _charSet.GetCharacter(newLineLastChar).KerningList.Find(FindKerningNode);
                    if (kern != null)
                    {
                      x += kern.Amount * sizeScale;
                      lineWidth += kern.Amount * sizeScale;
                    }
                  }
                }
              }
              else if (alignment == Align.Center)
              {
                if ((quads[j].LineNumber == lineNumber) &&
                    (quads[j].WordNumber == wordNumber))
                {
                  // First move word down to next line
                  quads[j].LineNumber++;
                  quads[j].WordNumber = 1;
                  quads[j].X = (float)x + (quads[j].BitmapCharacter.XOffset * sizeScale);
                  quads[j].Y = (float)y + (quads[j].BitmapCharacter.YOffset * sizeScale);
                  x += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  lineWidth += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  offset += quads[j].BitmapCharacter.XAdvance * sizeScale / 2f;
                  if (b.Kerning)
                  {
                    _nextChar = quads[j].Character;
                    Kerning kern = _charSet.GetCharacter(newLineLastChar).KerningList.Find(FindKerningNode);
                    if (kern != null)
                    {
                      float kerning = kern.Amount * sizeScale;
                      x += kerning;
                      lineWidth += kerning;
                      offset += kerning / 2f;
                    }
                  }
                }
              }
              else if (alignment == Align.Right)
              {
                if ((quads[j].LineNumber == lineNumber) &&
                    (quads[j].WordNumber == wordNumber))
                {
                  // Move character down to next line
                  quads[j].LineNumber++;
                  quads[j].WordNumber = 1;
                  quads[j].X = (float)x + (quads[j].BitmapCharacter.XOffset * sizeScale);
                  quads[j].Y = (float)y + (quads[j].BitmapCharacter.YOffset * sizeScale);
                  lineWidth += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  x += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  offset += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  if (b.Kerning)
                  {
                    _nextChar = quads[j].Character;
                    Kerning kern = _charSet.GetCharacter(newLineLastChar).KerningList.Find(FindKerningNode);
                    if (kern != null)
                    {
                      float kerning = kern.Amount * sizeScale;
                      x += kerning;
                      lineWidth += kerning;
                      offset += kerning;
                    }
                  }
                }
              }
              newLineLastChar = quads[j].Character;
            }

            // Make post-newline justifications
            if (alignment == Align.Center || alignment == Align.Right)
            {
              // Justify the new line
              for (int k = 0; k < quads.Count; k++)
                if (quads[k].LineNumber == lineNumber + 1)
                  quads[k].X -= offset;
              x -= offset;

              // Rejustify the line it was moved from
              for (int k = 0; k < quads.Count; k++)
                if (quads[k].LineNumber == lineNumber)
                  quads[k].X += offset;
            }
          }
          else
          {
            // New line without any "carry-down" word
            firstCharOfLine = true;
            lineWidth = 0f;
          }

          wordNumber = 1;
          lineNumber++;
        } // End new line check

        // Don't print these
        if (text[i] == '\n' || text[i] == '\r' || text[i] == '\t')
          continue;

        // Set starting cursor for alignment
        if (firstCharOfLine)
        {
          if (alignment == Align.Left)
            // Start at left
            x = b.TextBox.Left;
          if (alignment == Align.Center)
            // Start in center
            x = b.TextBox.Left + (maxWidth / 2f);
          else if (alignment == Align.Right)
            // Start at right
            x = b.TextBox.Right;
        }

        // Adjust for kerning
        float kernAmount = 0f;
        if (b.Kerning && !firstCharOfLine)
        {
          _nextChar = text[i];
          Kerning kern = _charSet.GetCharacter(lastChar).KerningList.Find(FindKerningNode);
          if (kern != null)
          {
            kernAmount = kern.Amount * sizeScale;
            x += kernAmount;
            lineWidth += kernAmount;
            wordWidth += kernAmount;
          }
        }

        firstCharOfLine = false;
        FontQuad q = createQuad(c, b.Color, (float) x, (float) y, z, (float) xOffset, (float) yOffset,
            (float) width, (float) height);
   
        q.LineNumber = lineNumber;
        if (text[i] == ' ' && alignment == Align.Right)
        {
          wordNumber++;
          wordWidth = 0f;
        }
        q.WordNumber = wordNumber;
        wordWidth += xAdvance;
        q.WordWidth = (float)wordWidth;
        q.BitmapCharacter = c;
        q.SizeScale = sizeScale;
        q.Character = text[i];
        q.CharacterIndex = i;
        q.XAdvance = (float)xAdvance;
        quads.Add(q);

        if (text[i] == ' ' && alignment == Align.Left)
        {
          wordNumber++;
          wordWidth = 0f;
        }

        x += xAdvance;
        lineWidth += xAdvance;
        lastChar = text[i];

        if (quads.Count == 1)
          _firstCharWidth = (float) lineWidth;
        // Rejustify text
        if (alignment == Align.Center)
        {
          // We have to recenter all Quads since we addded a 
          // new character
          float offset = (float)xAdvance / 2f;
          if (b.Kerning)
            offset += kernAmount / 2f;
          for (int j = 0; j < quads.Count; j++)
            if (quads[j].LineNumber == lineNumber)
              quads[j].X -= offset;
          x -= offset;
        }
        else if (alignment == Align.Right)
        {
          // We have to rejustify all Quads since we addded a 
          // new character
          float offset = 0f;
          if (b.Kerning)
            offset += kernAmount;
          for (int j = 0; j < quads.Count; j++)
            if (quads[j].LineNumber == lineNumber)
            {
              offset = (float)xAdvance;
              quads[j].X -= (float)xAdvance;
            }
          x -= offset;
        }
      }
      if (fadeOut)
        doFadeOut(ref quads, scroll, text);
      return quads;
    }

    /// <summary>Gets the line height of a StringBlock.</summary>
    public float GetLineHeight(int index)
    {
      if (index < 0 || index > _strings.Count)
        throw new ArgumentException("StringBlock index out of range.");
      return _charSet.LineHeight * (_strings[index].Size / _charSet.RenderedSize);
    }

    /// <summary>Search predicate used to find nodes in _kerningList</summary>
    /// <param name="node">Current node.</param>
    /// <returns>true if the node's name matches the desired node name, false otherwise.</returns>
    private bool FindKerningNode(Kerning node)
    {
      return (node.Second == _nextChar);
    }

    /// <summary>Gets the font texture.</summary>
    public Texture Texture
    {
      get { return _texture; }
    }

    #region ITextureAsset Members


    public void Allocate()
    {
    }

    public void KeepAlive()
    {
    }

    #endregion

    #region IAsset Members

    public bool IsAllocated
    {
      get { return _texture != null; }
    }

    public bool CanBeDeleted
    {
      get { return false; }
    }

    public bool Free(bool force)
    {
      if (_texture != null)
      {
        _texture.Dispose();
        _texture = null;
      }
      _effect.Free(true);
      return false;
    }

    #endregion
  }

  internal class BitmapCharacterRow
  {
    public const int MAX_CHARS = 256;

    public BitmapCharacter[] Characters;

    /// <summary>Creates a new BitmapCharacterSet</summary>
    public BitmapCharacterRow()
    {
      Characters = new BitmapCharacter[MAX_CHARS];
    }
  }

  /// <summary>Represents a single bitmap character set.</summary>
  internal class BitmapCharacterSet
  {
    public const int MAX_ROWS = 256;
    public int LineHeight;
    public int Base;
    public int RenderedSize;
    public int Width;
    public int Height;
    private readonly BitmapCharacterRow[] Rows;

    /// <summary>Creates a new BitmapCharacterSet</summary>
    public BitmapCharacterSet()
    {
      Rows = new BitmapCharacterRow[MAX_ROWS];
    }

    public BitmapCharacter GetCharacter(uint index)
    {
      uint row = index / BitmapCharacterRow.MAX_CHARS;
      if (Rows[row] == null)
        Rows[row] = new BitmapCharacterRow();
      return Rows[row].Characters[index % BitmapCharacterRow.MAX_CHARS];
    }

    public void SetCharacter(uint index, BitmapCharacter character)
    {
      uint row = index / BitmapCharacterRow.MAX_CHARS;
      if (Rows[row] == null)
        Rows[row] = new BitmapCharacterRow();
      Rows[row].Characters[index % BitmapCharacterRow.MAX_CHARS] = character;  
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
      BitmapCharacter result = new BitmapCharacter();
      result.X = X;
      result.Y = Y;
      result.Width = Width;
      result.Height = Height;
      result.XOffset = XOffset;
      result.YOffset = YOffset;
      result.XAdvance = XAdvance;
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

  internal class StringWord
  {
    string _text;
    float _width;

    public StringWord(string text, float width)
    {
      _text = text;
      _width = width;
    }
  } 

  /// <summary>Individual string to load into vertex buffer.</summary>
  internal struct StringBlock
  {
    public string Text;
    public RectangleF TextBox;
    public Font.Align Alignment;
    public float Size;
    public float ZOrder;
    public Color4 Color;
    public bool Kerning;

    /// <summary>Creates a new StringBlock</summary>
    /// <param name="text">Text to render</param>
    /// <param name="textBox">Text box to constrain text</param>
    /// <param name="alignment">Font alignment</param>
    /// <param name="size">Font size</param>
    /// <param name="color">Color</param>
    /// <param name="kerning">true to use kerning, false otherwise.</param>
    public StringBlock(string text, RectangleF textBox, float zOrder, Font.Align alignment,
        float size, Color4 color, bool kerning)
    {
      Text = text;
      TextBox = textBox;
      Alignment = alignment;
      Size = size;
      Color = color;
      Kerning = kerning;
      ZOrder = zOrder;
    }
  }
}
