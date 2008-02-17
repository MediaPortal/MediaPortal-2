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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine.Effects;
using SkinEngine.DirectX;

namespace SkinEngine.Fonts
{
  public class Font
  {
    public enum Align
    {
      Left,
      Center,
      Right
    } ;

    private BitmapCharacterSet _charSet;
    private List<FontQuad> _quads;
    private List<StringBlock> _strings;
    private readonly string _fntFile;
    private string _textureFile;
    private Texture _texture = null;
    //private VertexBuffer _vb = null;
    public const int MaxVertices = 4096;
    private int _nextChar;
    private readonly float _defaultSize = 20;
    private readonly Dictionary<int, string> _pages;
    private float _firstCharWidth = 0;
    EffectAsset _effect;
    /// <summary>Creates a new bitmap font.</summary>
    /// <param name="fntFile">Font file name.</param>
    /// <param name="size"></param>
    public Font(string fntFile, float size)
    {
      _pages = new Dictionary<int, string>();
      _quads = new List<FontQuad>();
      _strings = new List<StringBlock>();
      _fntFile = fntFile;
      _charSet = new BitmapCharacterSet();
      _defaultSize = size;
      ParseFNTFile();
      _effect = ContentManager.GetEffect("normal");
    }

    public float FirstCharWidth
    {
      get { return _firstCharWidth; }
    }

    public float Size
    {
      get { return _defaultSize; }
    }

    public List<FontQuad> Quads
    {
      get { return _quads; }
      set { _quads = value; }
    }

    public void Reload()
    {
      _quads = new List<FontQuad>();
      _strings = new List<StringBlock>();
      _charSet = new BitmapCharacterSet();
      ParseFNTFile();
    }

    public float Base
    {
      get { return _charSet.Base; }
    }

    public float LineHeight
    {
      get { return _charSet.LineHeight; }
    }
    public float AverageWidth
    {
      get { return _charSet.AverageWidth; }
    }
    public float Height
    {
      get { return _charSet.Height; }
    }

    /// <summary>Parses the FNT file.</summary>
    private void ParseFNTFile()
    {
      string fntFile = String.Format(@"skin\{0}\fonts\{1}", SkinContext.SkinName, _fntFile);

      StreamReader stream = new StreamReader(fntFile);
      string line;
      char[] separators = new char[] { ' ', '=' };
      while ((line = stream.ReadLine()) != null)
      {
        string[] tokens = line.Split(separators);
        if (tokens[0] == "info")
        {
          // Get rendered size
          for (int i = 1; i < tokens.Length; i++)
          {
            if (tokens[i] == "size")
            {
              _charSet.RenderedSize = int.Parse(tokens[i + 1]);
            }
          }
        }
        else if (tokens[0] == "common")
        {
          // Fill out BitmapCharacterSet fields
          for (int i = 1; i < tokens.Length; i++)
          {
            if (tokens[i] == "lineHeight")
            {
              _charSet.LineHeight = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "base")
            {
              _charSet.Base = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "scaleW")
            {
              _charSet.Width = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "scaleH")
            {
              _charSet.Height = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "pages")
            {
              //
            }
          }
        }
        else if (tokens[0] == "page")
        {
          int pageId = 0;
          for (int i = 1; i < tokens.Length; i++)
          {
            if (tokens[i] == "id")
            {
              pageId = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "file")
            {
              string name = tokens[i + 1];
              name = name.Substring(1, name.Length - 2);
              _pages[pageId] = name;
              _textureFile = name;
            }
          }
        }
        else if (tokens[0] == "chars")
        {
          for (int i = 1; i < tokens.Length; i++)
          {
            if (tokens[i] == "count")
            {
              int charCount = int.Parse(tokens[i + 1]);
              _charSet.Allocate(charCount);
            }
          }
        }

        else if (tokens[0] == "char")
        {
          // New BitmapCharacter
          int index = 0;
          for (int i = 1; i < tokens.Length; i++)
          {
            if (tokens[i] == "id")
            {
              index = int.Parse(tokens[i + 1]);
              ;
              if (index > _charSet.MaxCharacters)
              {
                break;
              }
            }
            else if (tokens[i] == "x")
            {
              _charSet.Characters[index].X = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "y")
            {
              _charSet.Characters[index].Y = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "width")
            {
              _charSet.Characters[index].Width = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "height")
            {
              _charSet.Characters[index].Height = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "xoffset")
            {
              _charSet.Characters[index].XOffset = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "yoffset")
            {
              _charSet.Characters[index].YOffset = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "xadvance")
            {
              _charSet.Characters[index].XAdvance = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "page")
            {
              _charSet.Characters[index].Page = int.Parse(tokens[i + 1]);
            }
          }
        }
        else if (tokens[0] == "kernings")
        {
          //skip kernings count...
        }
        else if (tokens[0] == "kerning")
        {
          // Build kerning list
          int index = 0;
          Kerning k = new Kerning();
          for (int i = 1; i < tokens.Length; i++)
          {
            if (tokens[i] == "first")
            {
              index = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "second")
            {
              k.Second = int.Parse(tokens[i + 1]);
            }
            else if (tokens[i] == "amount")
            {
              k.Amount = int.Parse(tokens[i + 1]);
            }
          }
          if (index >= 0 && index < _charSet.MaxCharacters)
          {
            _charSet.Characters[index].KerningList.Add(k);
          }
        }
      }
      stream.Close();
    }

    /// <summary>Call when the device is created.</summary>
    /// <param name="device">D3D GraphicsDevice.Device.</param>
    public void OnCreateDevice(Device device)
    {
      if (device == null)
      {
        return;
      }
      string fileName = String.Format(@"skin\{0}\fonts\{1}", SkinContext.SkinName, _textureFile);
      if (File.Exists(fileName))
      {
        if (_texture != null)
        {
          _texture.Dispose();
          ContentManager.TextureReferences--;
        }

        _texture = Texture.FromFile(device, fileName,
                                          _charSet.Width, _charSet.Height, 1, Usage.None, Format.Dxt3, Pool.Default,
                                          Filter.Linear, Filter.Linear, 0);
        ContentManager.TextureReferences++;
      }
    }

    /// <summary>Call when the device is destroyed.</summary>
    public void OnDestroyDevice()
    {
      if (_texture != null)
      {
        _texture.Dispose();
        _texture = null;
        ContentManager.TextureReferences--;
      }
    }

    /// <summary>Call when the device is reset.</summary>
    /// <param name="device">D3D GraphicsDevice.Device.</param>
    public void OnResetDevice(Device device)
    {
      //_vb = new VertexBuffer(GraphicsDevice.Device, MaxVertices * SkinEngine.DirectX.PositionColored2Textured.StrideSize,
      //    Usage.Dynamic | Usage.WriteOnly, SkinEngine.DirectX.PositionColored2Textured.Format,
      //    Pool.Default);
    }

    /// <summary>Call when the device is lost.</summary>
    public void OnLostDevice()
    {
      //if (_vb != null)
      //{
      //  _vb.Dispose();
      //  _vb = null;
      //}
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
    public int AddString(string text, RectangleF textBox, Align alignment, float size,
                         ColorValue color, bool kerning, bool scroll, out bool textFits, out float totalWidth)
    {
      StringBlock b = new StringBlock(text, textBox, alignment, size, color, kerning);
      _strings.Add(b);
      int index = _strings.Count - 1;
      _quads.AddRange(GetProcessedQuads(index, scroll, out textFits));
      if (_quads.Count > 0)
        totalWidth = _quads[_quads.Count - 1].TopRight.X - textBox.X;
      else
        totalWidth = 0;
      return index;
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
      GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
      GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * count);
      _effect.EndRender();
    }

    public void Render(Device device, VertexBuffer buffer, out int count)
    {
      count = _quads.Count;
      if (buffer == null || _texture == null)
      {
        return;
      }
      if (count <= 0)
      {
        return;
      }

      // Add vertices to the buffer
      //GraphicsBuffer<SkinEngine.DirectX.PositionColored2Textured> gb =
      //GraphicsStream gb = buffer.Lock(0, 6 * count * PositionColored2Textured.StrideSize, LockFlags.Discard);

      using (DataStream stream = buffer.Lock(0, 0, LockFlags.None))
      {
        foreach (FontQuad q in _quads)
        {
          stream.WriteRange(q.Vertices);
        }
      }

      buffer.Unlock();

      _effect.StartRender(_texture);
      GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;

      GraphicsDevice.Device.SetTexture(0, _texture);
      GraphicsDevice.Device.SetStreamSource(0, buffer, 0, PositionColored2Textured.StrideSize);
      GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2 * count);
      _effect.EndRender();
    }

    /// <summary>Gets the list of Quads from a StringBlock all ready to render.</summary>
    /// <param name="index">Index into StringBlock List</param>
    /// <returns>List of Quads</returns>
    /// <param name="scroll"></param>
    /// <param name="textFits"></param>
    public List<FontQuad> GetProcessedQuads(int index, bool scroll, out bool textFits)
    {
      textFits = true;
      if (index >= _strings.Count || index < 0)
      {
        throw new Exception("String block index out of range.");
      }

      List<FontQuad> quads = new List<FontQuad>();
      StringBlock b = _strings[index];
      string text = b.Text;
      float x = b.TextBox.X;
      float y = b.TextBox.Y;
      float maxWidth = b.TextBox.Width;
      Align alignment = b.Alignment;
      float lineWidth = 0f;
      float sizeScale = b.Size / _charSet.RenderedSize;
      char lastChar = new char();
      int lineNumber = 1;
      int wordNumber = 1;
      float wordWidth = 0f;
      bool firstCharOfLine = true;
      float z = 0f;
      bool fadeOut = false;
      if (text == null)
      {
        return quads;
      }
      for (int i = 0; i < text.Length; i++)
      {
        char chk = text[i];
        if (chk >= _charSet.Characters.Length) continue;
        BitmapCharacter c = _charSet.Characters[text[i]];
        float xOffset = c.XOffset * sizeScale;
        float yOffset = c.YOffset * sizeScale;
        float xAdvance = c.XAdvance * sizeScale;
        float width = c.Width * sizeScale;
        float height = c.Height * sizeScale;

        // Check vertical bounds
        if (y + yOffset + height > b.TextBox.Bottom)
        {
          break;
        }

        // Newline
        if (text[i] == '\n' || text[i] == '\r' || (lineWidth + xAdvance >= maxWidth))
        {
          if (y + yOffset + height + _charSet.LineHeight * sizeScale > b.TextBox.Bottom)
          {
            textFits = false;
            fadeOut = true;

            if (scroll == false)
            {
              //line does not fit...
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
                  {
                    break;
                  }
                }
                if (i - 3 >= 1 & i - 3 < text.Length)
                {
                  text = text.Substring(0, i - 3) + "...";
                  i -= 4;
                }
              }
              continue;
            }
            else
            {
              text = text.Substring(0, i);
              break;
            }
          }
          if (alignment == Align.Left)
          {
            // Start at left
            x = b.TextBox.X;
          }
          if (alignment == Align.Center)
          {
            // Start in center
            x = b.TextBox.X + (maxWidth / 2f);
          }
          else if (alignment == Align.Right)
          {
            // Start at right
            x = b.TextBox.Right;
          }

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
                  quads[j].X = x + (quads[j].BitmapCharacter.XOffset * sizeScale);
                  quads[j].Y = y + (quads[j].BitmapCharacter.YOffset * sizeScale);
                  x += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  lineWidth += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  if (b.Kerning)
                  {
                    _nextChar = quads[j].Character;
                    Kerning kern = _charSet.Characters[newLineLastChar].KerningList.Find(FindKerningNode);
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
                  quads[j].X = x + (quads[j].BitmapCharacter.XOffset * sizeScale);
                  quads[j].Y = y + (quads[j].BitmapCharacter.YOffset * sizeScale);
                  x += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  lineWidth += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  offset += quads[j].BitmapCharacter.XAdvance * sizeScale / 2f;
                  if (b.Kerning)
                  {
                    _nextChar = quads[j].Character;
                    Kerning kern = _charSet.Characters[newLineLastChar].KerningList.Find(FindKerningNode);
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
                  quads[j].X = x + (quads[j].BitmapCharacter.XOffset * sizeScale);
                  quads[j].Y = y + (quads[j].BitmapCharacter.YOffset * sizeScale);
                  lineWidth += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  x += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  offset += quads[j].BitmapCharacter.XAdvance * sizeScale;
                  if (b.Kerning)
                  {
                    _nextChar = quads[j].Character;
                    Kerning kern = _charSet.Characters[newLineLastChar].KerningList.Find(FindKerningNode);
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
              {
                if (quads[k].LineNumber == lineNumber + 1)
                {
                  quads[k].X -= offset;
                }
              }
              x -= offset;

              // Rejustify the line it was moved from
              for (int k = 0; k < quads.Count; k++)
              {
                if (quads[k].LineNumber == lineNumber)
                {
                  quads[k].X += offset;
                }
              }
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
        {
          continue;
        }

        // Set starting cursor for alignment
        if (firstCharOfLine)
        {
          if (alignment == Align.Left)
          {
            // Start at left
            x = b.TextBox.Left;
          }
          if (alignment == Align.Center)
          {
            // Start in center
            x = b.TextBox.Left + (maxWidth / 2f);
          }
          else if (alignment == Align.Right)
          {
            // Start at right
            x = b.TextBox.Right;
          }
        }

        // Adjust for kerning
        float kernAmount = 0f;
        if (b.Kerning && !firstCharOfLine)
        {
          _nextChar = text[i];
          Kerning kern = _charSet.Characters[lastChar].KerningList.Find(FindKerningNode);
          if (kern != null)
          {
            kernAmount = kern.Amount * sizeScale;
            x += kernAmount;
            lineWidth += kernAmount;
            wordWidth += kernAmount;
          }
        }

        firstCharOfLine = false;

        float u2, v2;
        u2 = v2 = 1;
        Vector3 uvPos = new Vector3(x + xOffset, y + yOffset, z);
        Vector3 finalScale = new Vector3(SkinContext.FinalMatrix.Matrix.M11, SkinContext.FinalMatrix.Matrix.M22, SkinContext.FinalMatrix.Matrix.M33);
        Vector3 finalTranslation = new Vector3(SkinContext.FinalMatrix.Matrix.M41, SkinContext.FinalMatrix.Matrix.M42, SkinContext.FinalMatrix.Matrix.M43);

        uvPos.X *= finalScale.X;
        uvPos.Y *= finalScale.Y;
        uvPos.Z *= finalScale.Z;
        uvPos.X += finalTranslation.X;
        uvPos.Y += finalTranslation.Y;
        uvPos.Z += finalTranslation.Z;
        //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);
        // Create the vertices
        PositionColored2Textured topLeft = new PositionColored2Textured(
          x + xOffset, y + yOffset, z,
          c.X / (float)_charSet.Width,
          c.Y / (float)_charSet.Height,
           b.Color.ToArgb());


        uvPos = new Vector3(topLeft.X + width, y + yOffset, z);
        uvPos.X *= finalScale.X;
        uvPos.Y *= finalScale.Y;
        uvPos.Z *= finalScale.Z;
        uvPos.X += finalTranslation.X;
        uvPos.Y += finalTranslation.Y;
        uvPos.Z += finalTranslation.Z;
        //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);
        PositionColored2Textured topRight = new PositionColored2Textured(
          topLeft.X + width, y + yOffset, z,
          (c.X + c.Width) / (float)_charSet.Width,
          c.Y / (float)_charSet.Height,
           b.Color.ToArgb());


        uvPos = new Vector3(topLeft.X + width, topLeft.Y + height, z);
        uvPos.X *= finalScale.X;
        uvPos.Y *= finalScale.Y;
        uvPos.Z *= finalScale.Z;
        uvPos.X += finalTranslation.X;
        uvPos.Y += finalTranslation.Y;
        uvPos.Z += finalTranslation.Z;
        //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);
        PositionColored2Textured bottomRight = new PositionColored2Textured(
          topLeft.X + width, topLeft.Y + height, z,
          (c.X + c.Width) / (float)_charSet.Width,
          (c.Y + c.Height) / (float)_charSet.Height,
           b.Color.ToArgb());


        uvPos = new Vector3(x + xOffset, topLeft.Y + height, z);
        uvPos.X *= finalScale.X;
        uvPos.Y *= finalScale.Y;
        uvPos.Z *= finalScale.Z;
        uvPos.X += finalTranslation.X;
        uvPos.Y += finalTranslation.Y;
        uvPos.Z += finalTranslation.Z;
        //SkinContext.GetAlphaGradientUV(uvPos, out u2, out v2);
        PositionColored2Textured bottomLeft = new PositionColored2Textured(
          x + xOffset, topLeft.Y + height, z,
          c.X / (float)_charSet.Width,
          (c.Y + c.Height) / (float)_charSet.Height,
           b.Color.ToArgb());

        // Create the quad
        FontQuad q = new FontQuad(topLeft, topRight, bottomLeft, bottomRight);
        q.LineNumber = lineNumber;
        if (text[i] == ' ' && alignment == Align.Right)
        {
          wordNumber++;
          wordWidth = 0f;
        }
        q.WordNumber = wordNumber;
        wordWidth += xAdvance;
        q.WordWidth = wordWidth;
        q.BitmapCharacter = c;
        q.SizeScale = sizeScale;
        q.Character = text[i];
        q.CharacterIndex = i;
        q.XAdvance = xAdvance;
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
        {
          _firstCharWidth = lineWidth;
        }
        // Rejustify text
        if (alignment == Align.Center)
        {
          // We have to recenter all Quads since we addded a 
          // new character
          float offset = xAdvance / 2f;
          if (b.Kerning)
          {
            offset += kernAmount / 2f;
          }
          for (int j = 0; j < quads.Count; j++)
          {
            if (quads[j].LineNumber == lineNumber)
            {
              quads[j].X -= offset;
            }
          }
          x -= offset;
        }
        else if (alignment == Align.Right)
        {
          // We have to rejustify all Quads since we addded a 
          // new character
          float offset = 0f;
          if (b.Kerning)
          {
            offset += kernAmount;
          }
          for (int j = 0; j < quads.Count; j++)
          {
            if (quads[j].LineNumber == lineNumber)
            {
              offset = xAdvance;
              quads[j].X -= xAdvance;
            }
          }
          x -= offset;
        }
      }
      if (fadeOut)
      {
        float alpha = 1.0f;
        int startIndex = (int)(text.Length * 0.5f);
        float step = 0.9f / ((float)text.Length - startIndex);
        if (scroll)
        {
          step = 1.0f / ((float)text.Length - startIndex);
        }
        for (int i = 0; i < quads.Count; ++i)
        {
          if (quads[i].CharacterIndex < startIndex)
          {
            continue;
          }
          float charIndex = quads[i].CharacterIndex - startIndex;
          float charAlphaStart = alpha - (step * charIndex);
          float charAlphaEnd = alpha - (step * (1 + charIndex));
          for (int v = 0; v < quads[i].Vertices.Length; v++)
          {
            float newAlpha = charAlphaStart;
            if (v == 1 || v == 4 || v == 5)
            {
              newAlpha = charAlphaEnd;
            }
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
      return quads;
    }

    /// <summary>Gets the line height of a StringBlock.</summary>
    public float GetLineHeight(int index)
    {
      if (index < 0 || index > _strings.Count)
      {
        throw new ArgumentException("StringBlock index out of range.");
      }
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
  } ;

  /// <summary>Represents a single bitmap character set.</summary>
  internal class BitmapCharacterSet
  {
    public int MaxCharacters = 256;
    public int LineHeight;
    public int Base;
    public int RenderedSize;
    public int Width;
    public int Height;
    public float _averageWidth;
    public BitmapCharacter[] Characters;

    /// <summary>Creates a new BitmapCharacterSet</summary>
    public BitmapCharacterSet()
    {
      Characters = new BitmapCharacter[MaxCharacters];
      for (int i = 0; i < MaxCharacters; i++)
      {
        Characters[i] = new BitmapCharacter();
      }
    }

    public void Allocate(int charCount)
    {
      if (charCount > MaxCharacters)
      {
        MaxCharacters = charCount + 1;
        Characters = new BitmapCharacter[MaxCharacters];
        for (int i = 0; i < MaxCharacters; i++)
        {
          Characters[i] = new BitmapCharacter();
        }
      }
    }
    public float AverageWidth
    {
      get
      {
        if (_averageWidth > 0) return _averageWidth;

        float w = 0;
        float count = 0;
        for (int i = 0; i < MaxCharacters; i++)
        {
          if (Characters[i].Width > 0)
          {
            w += Characters[i].Width;
            count++;
          }
        }
        _averageWidth = (w / count)*1.5f;
        return _averageWidth;
      }
    }
  } ;

  /// <summary>Represents a single bitmap character.</summary>
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

    /// <summary>Clones the BitmapCharacter</summary>
    /// <returns>Cloned BitmapCharacter</returns>
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
  } ;

  /// <summary>Represents kerning information for a character.</summary>
  public class Kerning
  {
    public int Second;
    public int Amount;
  } ;

  /// <summary>Individual string to load into vertex buffer.</summary>
  internal struct StringBlock
  {
    public string Text;
    public RectangleF TextBox;
    public Font.Align Alignment;
    public float Size;
    public ColorValue Color;
    public bool Kerning;

    /// <summary>Creates a new StringBlock</summary>
    /// <param name="text">Text to render</param>
    /// <param name="textBox">Text box to constrain text</param>
    /// <param name="alignment">Font alignment</param>
    /// <param name="size">Font size</param>
    /// <param name="color">Color</param>
    /// <param name="kerning">true to use kerning, false otherwise.</param>
    public StringBlock(string text, RectangleF textBox, Font.Align alignment,
                     float size, ColorValue color, bool kerning)
    {
      Text = text;
      TextBox = textBox;
      Alignment = alignment;
      Size = size;
      Color = color;
      Kerning = kerning;
    }
  } ;
}
