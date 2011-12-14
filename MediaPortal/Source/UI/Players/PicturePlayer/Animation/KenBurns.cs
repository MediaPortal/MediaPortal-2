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

using System;
using System.Drawing;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.Players.Picture.Animation
{
  /// <summary>
  /// Ken Burns effect uses different pan and zoom operations to animate a picture.
  /// </summary>
  public class KenBurns: IPictureAnimator
  {
    private enum EffectType
    {
      None,
      Zoom,
      Pan
    }

    private const float KENBURNS_ZOOM_FACTOR = 1.30f; // Zoom factor for pictures that have a black border on the sides
    private const float KENBURNS_ZOOM_FACTOR_FS = 1.20f; // Zoom factor for pictures that are filling the whole screen
    private const float KENBURNS_MAXZOOM = 1.30f;
    private const int KENBURNS_TRANSISTION_SPEED = 50;
    private const int MAX_ZOOM_FACTOR = 10;

    private float _bestZoomFactorCurrent = 1.0f;

    private EffectType _currentEffectType = EffectType.None;
    private readonly Random _randomizer = new Random(DateTime.Now.Millisecond);
    private Size _imageSize;
    private RectangleF _zoomRect;

    private float _currentZoomFactor = 1.0f;
    private float _currentZoomLeft;
    private float _currentZoomTop;
    private int _currentZoomType;

    private int _startPoint;
    private int _endPoint;
    private float _endZoomFactor = 1.0f;
    private float _startZoomFactor = 1.0f;

    private bool _reset = false;
    private bool _fullScreen;
    private bool _landScape = false;
    private int _frameNumber = 0;

    private int _kenBurnsState;
    private float _panXChange;
    private float _panYChange;

    private float _zoomChange;
    private float _zoomWidth;
    private float _zoomHeight;

    public RectangleF ZoomRect
    {
      get { return _zoomRect; }
    }

    public void Initialize(Size imageSize)
    {
      _imageSize = imageSize;
      _zoomRect = new RectangleF(PointF.Empty, new SizeF(1, 1));

      _landScape = _imageSize.Width > _imageSize.Height;

      _currentZoomFactor = CalculateBestZoom(_imageSize.Width, _imageSize.Height);
      _currentZoomLeft = 0;
      _currentZoomTop = 0;
      _currentZoomLeft = 0;
      _currentZoomTop = 0;

      _bestZoomFactorCurrent = _currentZoomFactor;
      _currentEffectType = (EffectType) 1 + _randomizer.Next(2);

      _reset = true;
    }

    public void Animate()
    {
      Animate(_reset);
      Zoom(_currentZoomFactor);
      _reset = false;
    }

    /// <summary>
    /// Ken Burn effects
    /// </summary>
    /// <returns></returns>
    private void Animate(bool bReset)
    {
      const int iNrOfFramesPerEffect = KENBURNS_TRANSISTION_SPEED * 30;

      // Init methode
      if (bReset)
      {
        // Set first state parameters: start and end zoom factor
        _frameNumber = 0;
        _kenBurnsState = 0;
      }

      // Check single effect end
      if (_frameNumber == iNrOfFramesPerEffect)
      {
        _frameNumber = 0;
        _kenBurnsState++;
      }

      // Select effect
      switch (_currentEffectType)
      {
        case EffectType.None:
          // No effects, just wait for next picture
          break;

        case EffectType.Zoom:
          KenBurnsRandomZoom(_frameNumber, iNrOfFramesPerEffect, bReset);
          break;

        case EffectType.Pan:
          KenBurnsRandomPan(_frameNumber, iNrOfFramesPerEffect, bReset);
          break;
      }

      // Check new rectangle
      if (_currentZoomTop > (_imageSize.Height - _zoomHeight))
        _currentZoomTop = (_imageSize.Height - _zoomHeight);

      if (_currentZoomLeft > (_imageSize.Width - _zoomWidth))
        _currentZoomLeft = (_imageSize.Width - _zoomWidth);

      if (_currentZoomTop < 0)
        _currentZoomTop = 0;

      if (_currentZoomLeft < 0)
        _currentZoomLeft = 0;

      if (_currentEffectType != EffectType.None && !bReset)
        _frameNumber++;
    }

    /* Zoom types:
       * 0: // centered, centered
       * 1: // Width centered, Top unchanged
       * 2: // Heigth centered, Left unchanged
       * 3: // Widht centered, Bottom unchanged
       * 4: // Height centered, Right unchanged
       * 5: // Top Left unchanged
       * 6: // Top Right unchanged
       * 7: // Bottom Left unchanged
       * 8: // Bottom Right unchanged
       * */

    /* Zoom points arround the rectangle
     * Selected zoom type will hold the selected point at the same place while zooming the rectangle
     *
     *     1---------2---------3
     *     |                   |
     *     8         0         4
     *     |                   |
     *     7---------6---------5
     *
     */

    private void KenBurnsRandomZoom(int iFrameNr, int iNrOfFramesPerEffect, bool bReset)
    {
      if (bReset)
      {
        int iRandom = _randomizer.Next(3);
        switch (iRandom)
        {
          case 0:
            _currentZoomType = _landScape ? 8 : 2;
            break;

          case 1:
            _currentZoomType = _landScape ? 4 : 6;
            break;

          default:
            _currentZoomType = 0; // centered
            break;
        }

        // Init zoom
        _endZoomFactor = _fullScreen ? _bestZoomFactorCurrent * KENBURNS_ZOOM_FACTOR_FS : _bestZoomFactorCurrent * KENBURNS_ZOOM_FACTOR;

        _startZoomFactor = _bestZoomFactorCurrent;
        _zoomChange = (_endZoomFactor - _startZoomFactor) / iNrOfFramesPerEffect;
        _currentZoomFactor = _startZoomFactor;
      }
      else
      {
        float zoomFactor = _startZoomFactor + _zoomChange * iFrameNr;
        Zoom(zoomFactor);
      }
    }

    private void SetOutputRect(float fZoomLevel)
    {
      // Current image size
      float iSourceWidth = _imageSize.Width;
      float iSourceHeight = _imageSize.Height;

      // Calculate aspect ratio correction factor
      float iScreenWidth = SkinContext.WindowSize.Width;
      float iScreenHeight = SkinContext.WindowSize.Height;

      float fSourceFrameAR = iSourceWidth / iSourceHeight;

      float width = iSourceWidth * fZoomLevel;
      float height = iSourceHeight * fZoomLevel;

      _zoomWidth = iSourceWidth;
      _zoomHeight = iSourceHeight;

      // Check org rectangle
      if (width > iScreenWidth)
      {
        width = iScreenWidth;
        _zoomWidth = width / fZoomLevel;
      }

      if (height > iScreenHeight)
      {
        height = iScreenHeight;
        _zoomHeight = height / fZoomLevel;
      }

      if (_zoomHeight > iSourceHeight)
      {
        _zoomHeight = iSourceHeight;
        _zoomWidth = _zoomHeight * fSourceFrameAR;
      }

      if (_zoomWidth > iSourceWidth)
      {
        _zoomWidth = iSourceWidth;
        _zoomHeight = _zoomWidth / fSourceFrameAR;
      }

      float x = (iScreenWidth - width) / 2;
      float y = (iScreenHeight - height) / 2;

      SetOutputRect(x, y, width, height);
    }

    private void SetOutputRect(float x, float y, float width, float height)
    {
      float viewX = x < 0 ? 0 : x / _imageSize.Width;
      float viewY = y < 0 ? 0 : y / _imageSize.Height;
      float viewWidth = width / _imageSize.Width;
      float viewHeight = height / _imageSize.Height;
      _zoomRect = new RectangleF(viewX, viewY, viewWidth, viewHeight);
    }

    private void KenBurnsRandomPan(int iFrameNr, int iNrOfFramesPerEffect, bool bReset)
    {
      var landScapePoints = new Point[]
          {
            new Point(1, 4), new Point(1, 5), new Point(8, 3), new Point(8, 4),
            new Point(8, 5), new Point(7, 4), new Point(7, 3), new Point(5, 8),
            new Point(5, 1), new Point(4, 7), new Point(4, 8), new Point(4, 1),
            new Point(3, 7), new Point(3, 8)
          };
      var portraitPoints = new Point[]
          {
            new Point(1, 6), new Point(1, 5), new Point(2, 7), new Point(2, 6),
            new Point(2, 5), new Point(3, 7), new Point(3, 6), new Point(5, 2),
            new Point(5, 1), new Point(6, 3), new Point(6, 2), new Point(6, 1),
            new Point(7, 3), new Point(7, 2)
          };

      // For Landscape picutres zoomstart BestWidth than Pan
      if (bReset)
      {
        // Find start and end points (8 possible points around the rectangle)
        int iRandom = _randomizer.Next(14);
        Point p = _landScape ? landScapePoints[iRandom] : portraitPoints[iRandom];
        _startPoint = p.X;
        _endPoint = p.Y;

        // Init 120% top center fixed
        _currentZoomFactor = _bestZoomFactorCurrent * KENBURNS_ZOOM_FACTOR;
        _currentZoomType = _startPoint;
      }
      else
      {
        // - Pan start point to end point
        if (iFrameNr == 0)
        {
          // Init single effect
          float iDestY = 0;
          float iDestX = 0;
          switch (_endPoint)
          {
            case 8:
              iDestY = (float) _imageSize.Height / 2;
              iDestX = _zoomWidth / 2;
              break;
            case 4:
              iDestY = (float) _imageSize.Height / 2;
              iDestX = _imageSize.Width - _zoomWidth / 2;
              break;
            case 2:
              iDestY = _zoomHeight / 2;
              iDestX = (float) _imageSize.Width / 2;
              break;
            case 6:
              iDestY = _imageSize.Height - _zoomHeight / 2;
              iDestX = (float) _imageSize.Width / 2;
              break;
            case 1:
              iDestY = _zoomHeight / 2;
              iDestX = _zoomWidth / 2;
              break;
            case 3:
              iDestY = _zoomHeight / 2;
              iDestX = _imageSize.Width - _zoomWidth / 2;
              break;
            case 7:
              iDestY = _imageSize.Height - _zoomHeight / 2;
              iDestX = _zoomWidth / 2;
              break;
            case 5:
              iDestY = _imageSize.Height - _zoomHeight / 2;
              iDestX = _imageSize.Width - _zoomWidth / 2;
              break;
          }

          _panYChange = (iDestY - (_currentZoomTop + _zoomHeight / 2)) / iNrOfFramesPerEffect; // Travel Y;
          _panXChange = (iDestX - (_currentZoomLeft + _zoomWidth / 2)) / iNrOfFramesPerEffect; // Travel X;
        }

        Pan(_panXChange, _panYChange);
      }
    }

    private float CalculateBestZoom(float fWidth, float fHeight)
    {
      // Default picutes is zoom best fit (max width or max height)
      float zoomFactorX = SkinContext.WindowSize.Width / fWidth;
      float zoomFactorY = SkinContext.WindowSize.Height / fHeight;

      // Get minimal zoom level (1.0==100%)
      float fZoom = zoomFactorX;
      _landScape = true;
      if (zoomFactorY < zoomFactorX)
      {
        fZoom = zoomFactorY; //-ZoomFactorX+1.0f;
        _landScape = false;
      }

      _fullScreen = false;
      if ((zoomFactorY < KENBURNS_ZOOM_FACTOR_FS) && (zoomFactorX < KENBURNS_ZOOM_FACTOR_FS))
      {
        _fullScreen = true;
      }

      // Fit to screen default zoom factor

      // Zoom 100%..150%
      if (fZoom < 1.00f)
        fZoom = 1.00f;

      if (fZoom > KENBURNS_MAXZOOM)
        fZoom = KENBURNS_MAXZOOM;

      return fZoom;
    }

    private void Zoom(float fZoom)
    {
      if (fZoom > MAX_ZOOM_FACTOR || fZoom < 0.0f)
        return;

      // Start and End point positions along the picture rectangle
      // Point zoom in/out only works if the selected Point is at the border
      // example:  "m_dwWidthBackGround == m_iZoomLeft + _zoomWidth"  and zooming to the left (iZoomType=4)
      float middlex = (float) _imageSize.Width / 2;
      float middley = (float) _imageSize.Height / 2;
      float xend = _imageSize.Width;
      float yend = _imageSize.Height;

      _currentZoomFactor = fZoom;

      SetOutputRect(_currentZoomFactor);

      if (_currentZoomTop + _zoomHeight > _imageSize.Height)
        _zoomHeight = _imageSize.Height - _currentZoomTop;

      if (_currentZoomLeft + _zoomWidth > _imageSize.Width)
        _zoomWidth = _imageSize.Width - _currentZoomLeft;

      switch (_currentZoomType)
      {
        /* 0: // centered, centered
         * 1: // Top Left unchanged
         * 2: // Width centered, Top unchanged
         * 3: // Top Right unchanged
         * 4: // Height centered, Right unchanged
         * 5: // Bottom Right unchanged
         * 6: // Widht centered, Bottom unchanged
         * 7: // Bottom Left unchanged
         * 8: // Heigth centered, Left unchanged
         * */
        case 0: // centered, centered
          _currentZoomLeft = middlex - _zoomWidth * 0.5f;
          _currentZoomTop = middley - _zoomHeight * 0.5f;
          break;
        case 2: // Width centered, Top unchanged
          _currentZoomLeft = middlex - _zoomWidth * 0.5f;
          break;
        case 8: // Heigth centered, Left unchanged
          _currentZoomTop = middley - _zoomHeight * 0.5f;
          break;
        case 6: // Widht centered, Bottom unchanged
          _currentZoomLeft = middlex - _zoomWidth * 0.5f;
          _currentZoomTop = yend - _zoomHeight;
          break;
        case 4: // Height centered, Right unchanged
          _currentZoomTop = middley - _zoomHeight * 0.5f;
          _currentZoomLeft = xend - _zoomWidth;
          break;
        case 1: // Top Left unchanged
          break;
        case 3: // Top Right unchanged
          _currentZoomLeft = xend - _zoomWidth;
          break;
        case 7: // Bottom Left unchanged
          _currentZoomTop = yend - _zoomHeight;
          break;
        case 5: // Bottom Right unchanged
          _currentZoomTop = yend - _zoomHeight;
          _currentZoomLeft = xend - _zoomWidth;
          break;
      }
      if (_currentZoomLeft > _imageSize.Width - _zoomWidth)
        _currentZoomLeft = (_imageSize.Width - _zoomWidth);

      if (_currentZoomTop > _imageSize.Height - _zoomHeight)
        _currentZoomTop = (_imageSize.Height - _zoomHeight);

      if (_currentZoomLeft < 0)
        _currentZoomLeft = 0;

      if (_currentZoomTop < 0)
        _currentZoomTop = 0;

      SetOutputRect(_currentZoomLeft, _currentZoomTop, _zoomWidth, _zoomHeight);
    }

    /// <summary>
    /// Pan picture rectangle
    /// </summary>
    /// <param name="fPanX"></param>
    /// <param name="fPanY"></param>
    private void Pan(float fPanX, float fPanY)
    {
      if (fPanX == 0.0f && fPanY == 0.0f)
        return;

      _currentZoomLeft += fPanX;
      _currentZoomTop += fPanY;

      SetOutputRect(_currentZoomLeft, _currentZoomTop, _zoomWidth, _zoomHeight);
    }
  }
}