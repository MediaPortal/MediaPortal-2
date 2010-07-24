#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using Font=MediaPortal.UI.SkinEngine.Fonts.Font;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public class FontBufferAsset : IAsset
  {
    #region variables

    private string _previousText;
    private RectangleF _previousTextBox;
    private Font.Align _previousAlignment;
    private float _previousSize;
    private Color4 _previousColor;
    private bool _previousGradientUsed = false;
    private readonly Font _font;
    private VertexBuffer _vertexBuffer;
    private int _primitivecount;
    private DateTime _lastTimeUsed = DateTime.MinValue;
    private bool _textFits = true;
    private float _xPosition = 0;
    private float _previousTotalWidth;
    private int _characterIndex = 0;
    Matrix _previousMatrix;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="FontBufferAsset"/> class.
    /// </summary>
    /// <param name="font">The font.</param>
    public FontBufferAsset(Font font)
    {
      _font = font;
      Allocate();
    }

    /// <summary>
    /// Gets the font.
    /// </summary>
    /// <value>The font.</value>
    public Font Font
    {
      get { return _font; }
    }

    /// <summary>
    /// Allocates the vertex buffer
    /// </summary>
    public void Allocate()
    {
      if (_vertexBuffer != null)
      {
        Free(true);
      }

      //ServiceRegistration.Get<ILogger>().Debug("FONTASSET alloc vertextbuffer");
      _vertexBuffer = PositionColored2Textured.Create(Font.MaxVertices);//dynamic|writeonly?
      ContentManager.VertexReferences++;
      _previousText = "";
      _previousTextBox = new RectangleF();
      _previousSize = 0;
      _previousColor = new Color4();
      _previousGradientUsed = false;
    }

    /// <summary>
    /// Determines whether the specified rendering attributes are changed
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="textBox">The text box.</param>
    /// <param name="alignment">The alignment.</param>
    /// <param name="size">The size.</param>
    /// <param name="color">The color.</param>
    /// <param name="finalTransform">Final combined layout-/render-transform.</param>
    /// <returns>
    /// 	<c>true</c> if the specified text is changed; otherwise, <c>false</c>.
    /// </returns>
    private bool IsChanged(string text, RectangleF textBox, Font.Align alignment, float size, Color4 color, Matrix finalTransform)
    {
      if (text != _previousText)
        return true;
      if (textBox != _previousTextBox)
        return true;
      if (alignment != _previousAlignment)
        return true;
      if (size != _previousSize)
        return true;
      //if (SkinContext.GradientInUse != _previousGradientUsed)
      //  return true;
      if (color.ToArgb() != _previousColor.ToArgb())
        return true;
      if (_previousMatrix != finalTransform)
        return true;
      return false;
    }

    /// <summary>
    /// Draws the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="textBox">The text box.</param>
    /// <param name="alignment">The alignment.</param>
    /// <param name="size">The size.</param>
    /// <param name="color">The color.</param>
    /// <param name="scroll">if set to <c>true</c> then scrolling is allowed.</param>
    /// <param name="finalTransform">The final combined layout-/render-transform.</param>
    public void Draw(string text, RectangleF textBox, Font.Align alignment, float size, Color4 color, bool scroll,
        out float totalWidth, Matrix finalTransform)
    {
      finalTransform = finalTransform.Clone();
      totalWidth = 0;
      if (_font == null || String.IsNullOrEmpty(text))
      {
        return;
      }
      if (!IsAllocated)
      {
        Allocate();
      }
      if (!IsAllocated)
      {
        return;
      }
      if (scroll)
      {
        if (false == _textFits)
        {
          if (IsChanged(text, textBox, alignment, size, color, finalTransform))
          {
            _previousText = text;
            _previousTextBox = textBox;
            _previousAlignment = alignment;
            _previousSize = size;
            _previousColor = color;
            //_previousGradientUsed = SkinContext.GradientInUse;
            _xPosition = 0.0f;
            _characterIndex = 0;
            _previousMatrix = finalTransform;
          }

          //need to scroll
          text += " ";
          string textDraw = text.Substring(_characterIndex) + text;

          textBox.X -= _xPosition;

          _font.AddString(textDraw, textBox, 0.0f, alignment, size, color, true, true, finalTransform, out _textFits, out totalWidth);
          _font.Render(GraphicsDevice.Device, _vertexBuffer, finalTransform, out _primitivecount);
          _font.ClearStrings();

          if (_xPosition >= _font.FirstCharWidth)
          {
            _characterIndex++;
            if (_characterIndex >= text.Length)
              _characterIndex = 0;
            _xPosition = 0.0f;
          }
          else
            _xPosition += 0.5f;
          _previousTotalWidth = totalWidth;
          _lastTimeUsed = SkinContext.FrameRenderingStartTime;
          return;
        }
      }
      else
      {
        if (_xPosition != 0.0)
          _previousText = "";
        _characterIndex = 0;
        _xPosition = 0;
      }
      if (IsChanged(text, textBox, alignment, size, color, finalTransform))
      {
        _previousText = text;
        _previousTextBox = textBox;
        _previousAlignment = alignment;
        _previousSize = size;
        _previousColor = color;
        //_previousGradientUsed = SkinContext.GradientInUse;
        _previousMatrix = finalTransform;

        _font.AddString(text, textBox, 0.0f, alignment, size, color, true, false, finalTransform, out _textFits, out totalWidth);
        _font.Render(GraphicsDevice.Device, _vertexBuffer, finalTransform, out _primitivecount);
        _font.ClearStrings();
        _previousTotalWidth = totalWidth;
      }
      else
      {
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        GraphicsDevice.Device.SetStreamSource(0, _vertexBuffer, 0, PositionColored2Textured.StrideSize);
        _font.Render(GraphicsDevice.Device, _primitivecount, finalTransform);
        totalWidth = _previousTotalWidth;
      }
      _lastTimeUsed = SkinContext.FrameRenderingStartTime;
    }

    #region IAsset Members

    /// <summary>
    /// Gets a value indicating the asset is allocated
    /// </summary>
    /// <value><c>true</c> if this asset is allocated; otherwise, <c>false</c>.</value>
    public bool IsAllocated
    {
      get { return (_vertexBuffer != null); }
    }

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    public bool CanBeDeleted
    {
      get
      {
        if (!IsAllocated)
        {
          return false;
        }
        TimeSpan ts = SkinContext.FrameRenderingStartTime - _lastTimeUsed;
        if (ts.TotalSeconds >= 5)
        {
          return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Frees this asset.
    /// </summary>
    public void Free(bool force)
    {
      if (_vertexBuffer != null)
      {
        //ServiceRegistration.Get<ILogger>().Debug("FONTASSET dispose vertextbuffer");
        _vertexBuffer.Dispose();
        _vertexBuffer = null;
        ContentManager.VertexReferences--;
      }
    }

    public override string ToString()
    {
      return _previousText;
    }
    #endregion
  }
}
