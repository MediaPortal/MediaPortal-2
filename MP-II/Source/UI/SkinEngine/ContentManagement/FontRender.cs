#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Effects;
using MediaPortal.UI.SkinEngine.SkinManagement;
using Font=MediaPortal.UI.SkinEngine.Fonts.Font;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public class FontRender
  {
    #region variables

    private string _previousText;
    private RectangleF _previousTextBox;
    private Font.Align _previousAlignment;
    private float _previousZorder;
    private float _previousSize;
    private Color4 _previousColor;
    private bool _previousGradientUsed = false;
    private Font _font;
    private int _primitivecount;
    private bool _textFits = true;
    private float _xPosition = 0;
    private float _previousTotalWidth;
    private int _characterIndex = 0;
    Matrix _previousMatrix;
    PrimitiveContext _context;
    bool _isAdded = false;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="FontBufferAsset"/> class.
    /// </summary>
    /// <param name="font">The font.</param>
    public FontRender(Font font)
    {
      _font = font;
      _previousText = "";
      _previousTextBox = new RectangleF();
      _previousSize = 0;
      _previousColor = new Color4();
      _previousGradientUsed = false;
      _context = new PrimitiveContext();
      _context.Effect = ContentManager.GetEffect("font");
      _context.Parameters = new EffectParameters();
      _context.Texture = _font;
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
    /// Determines whether the specified rendering attributes are changed
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="textBox">The text box.</param>
    /// <param name="alignment">The alignment.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="color">The color.</param>
    /// <returns>
    /// 	<c>true</c> if the specified text is changed; otherwise, <c>false</c>.
    /// </returns>
    private bool IsChanged(string text, RectangleF textBox, float zOrder, Font.Align alignment, float fontSize, Color4 color)
    {
      return text != _previousText || textBox != _previousTextBox || alignment != _previousAlignment ||
//          SkinContext.GradientInUse != _previousGradientUsed ||
          fontSize != _previousSize || color.ToArgb() != _previousColor.ToArgb() ||
          _previousMatrix != SkinContext.FinalTransform.Matrix;
    }

    /// <summary>
    /// Draws the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="textBox">The text box.</param>
    /// <param name="alignment">The alignment.</param>
    /// <param name="fontSize">The size.</param>
    /// <param name="color">The color.</param>
    /// <param name="scroll">if set to <c>true</c> then scrolling is allowed.</param>
    public void Draw(string text, RectangleF textBox, float zOrder, Font.Align alignment, float fontSize, Color4 color, bool scroll, out float totalWidth)
    {
      totalWidth = 0;
      if (_font == null || String.IsNullOrEmpty(text))
        return;
      Alloc();
      if (scroll)
      {
        if (false == _textFits)
        {
          if (IsChanged(text, textBox, zOrder, alignment, fontSize, color))
          {
            _previousText = text;
            _previousTextBox = textBox;
            _previousAlignment = alignment;
            _previousSize = fontSize;
            _previousColor = color;
            //_previousGradientUsed = SkinContext.GradientInUse;
            _xPosition = 0.0f;
            _characterIndex = 0;
            _previousMatrix = SkinContext.FinalTransform.Matrix;
            _previousZorder = zOrder;
          }

          //need to scroll
          text += " ";
          string textDraw = text.Substring(_characterIndex) + text;

          float x1 = textBox.X;
          float y1 = textBox.Y;
          float x2 = textBox.Width;
          float y2 = textBox.Height;

          uint enabled = GraphicsDevice.Device.GetRenderState<uint>(RenderState.ScissorTestEnable);
          Rectangle rectOld = GraphicsDevice.Device.ScissorRect;
          GraphicsDevice.Device.ScissorRect = new Rectangle((int)x1, (int)y1, (int)x2, (int)y2);
          GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, true);

          textBox.X -= _xPosition;

          _font.AddString(textDraw, textBox, zOrder, alignment, fontSize, color, true, true, out _textFits, out totalWidth);

          PositionColored2Textured[] verts = _font.Vertices;
          _context.OnVerticesChanged(_font.PrimitiveCount, ref verts);
          //_font.Render(GraphicsDevice.Device, _vertexBuffer, out _primitivecount);
          _font.ClearStrings();

          GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, (enabled != 0));
          GraphicsDevice.Device.ScissorRect = rectOld;
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
      if (IsChanged(text, textBox, zOrder, alignment, fontSize, color))
      {
        _previousText = text;
        _previousTextBox = textBox;
        _previousAlignment = alignment;
        _previousSize = fontSize;
        _previousColor = color;
        //_previousGradientUsed = SkinContext.GradientInUse;
        _previousMatrix = SkinContext.FinalTransform.Matrix;
        _previousZorder = zOrder;

        _font.AddString(text, textBox, zOrder, alignment, fontSize, color, true, false, out _textFits, out totalWidth);
        //_font.Render(GraphicsDevice.Device, _vertexBuffer, out _primitivecount);
        PositionColored2Textured[] verts = _font.Vertices;
        _context.OnVerticesChanged(_font.PrimitiveCount, ref verts);
        _font.ClearStrings();
        _previousTotalWidth = totalWidth;
      }
      else
      {
        //GraphicsDevice.Device.SetStreamSource(0, _vertexBuffer, 0, PositionColored2Textured.StrideSize);
        //_font.Render(GraphicsDevice.Device, _primitivecount);
        totalWidth = _previousTotalWidth;
      }
    }

    public override string ToString()
    {
      return _previousText;
    }

    public void Free()
    {
      if (_isAdded)
      {
        RenderPipeline.Instance.Remove(_context);
        _isAdded = false;
      }
    }

    public void Alloc()
    {
      if (_isAdded == false)
      {
        RenderPipeline.Instance.Add(_context);
        _isAdded = true;
      }
    }
  }
}