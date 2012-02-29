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
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using DirectShowLib;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  #region Structs and helper classes

  /// <summary>
  /// Structure used in communication with subtitle filter.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct NativeSubtitle
  {
    // Start of bitmap fields
    public Int32 Type;
    public Int32 Width;
    public Int32 Height;
    public Int32 WidthBytes;
    public UInt16 Planes;
    public UInt16 BitsPixel;
    public IntPtr Bits;
    // End of bitmap fields

    // Start of screen size definition
    public Int32 ScreenWidth;
    public Int32 ScreenHeight;

    // Subtitle timestmap
    public UInt64 TimeStamp;

    // How long to display subtitle
    public UInt64 TimeOut; // in seconds
    public Int32 FirstScanLine;
  }

  /*
   * int character_table;
  LPCSTR language;
  int page;
  LPCSTR text;
  int firstLine;  // can be 0 to (totalLines - 1)
  int totalLines; // for teletext this is 25 lines

  unsigned    __int64 timestamp;
  unsigned    __int64 timeOut;

  */
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct TextSubtitle
  {
    public int Encoding;
    public string Language;

    public int Page;
    public string Text; // subtitle lines seperated by newline characters
    public LineContent[] LineContents;
    public UInt64 TimeStamp;
    public UInt64 TimeOut; // in seconds
  }

  public enum TeletextCharTable
  {
    English = 1,
    Swedish = 2,
    Third = 3,
    Fourth = 4,
    Fifth = 5,
    Sixth = 6
  }

  public class TeletextPageEntry
  {
    public TeletextPageEntry() { }

    public TeletextPageEntry(TeletextPageEntry e)
    {
      Page = e.Page;
      Encoding = e.Encoding;
      Language = String.Copy(e.Language);
    }

    public int Page = -1; // indicates not valid
    public TeletextCharTable Encoding;
    public string Language;
  }

  public class Subtitle
  {
    public static int IdCount = 0;
    public Subtitle()
    {
      Id = IdCount++;
    }
    public Bitmap SubBitmap;
    public uint Width;
    public uint Height;
    public double PresentTime;  // NOTE: in seconds
    public double TimeOut;      // NOTE: in seconds
    public int FirstScanLine;
    public long Id = 0;
    public bool ShouldDraw;
    public Int32 ScreenWidth; // Required for aspect ratio correction
    public Int32 HorizontalPosition;

    public override string ToString()
    {
      return "Subtitle " + Id + " meta data: Timeout=" + TimeOut + " timestamp=" + PresentTime;
    }
  }

  #endregion

  #region DVBSub2(3) interfaces

  /// <summary>
  /// Interface to the subtitle filter, which allows us to get notified of subtitles and retrieve them.
  /// </summary>
  [Guid("4A4fAE7C-6095-11DC-8314-0800200C9A66"),
  InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IDVBSubtitleSource
  {
    void SetBitmapCallback(IntPtr callBack);
    void SetResetCallback(IntPtr callBack);
    void SetUpdateTimeoutCallback(IntPtr callBack);
    void StatusTest(int status);
  }

  [UnmanagedFunctionPointer(CallingConvention.StdCall)]
  public delegate int SubtitleCallback(IntPtr sub);
  public delegate int ResetCallback();
  public delegate int UpdateTimeoutCallback(ref Int64 timeOut);

  #endregion

  /// <summary>
  /// SubtitleRenderer uses the DVBSub2 direct show filter in the video graph to retrieve subtitles.
  /// The subtitles are handled by drawing bitmap to the video frame (<see cref="DrawOverlay"/>).
  /// </summary>
  public class SubtitleRenderer : IDisposable
  {
    #region Constants

    private const int MAX_SUBTITLES_IN_QUEUE = 20;

    #endregion

    #region Fields

    // DirectX DeviceEx to handle graphic operations
    protected DeviceEx _device;
    protected IDVBSubtitleSource _subFilter = null;

    /// <summary>
    /// Reference to the DirectShow DVBSub filter, which 
    /// is the source of our subtitle bitmaps
    /// </summary>
    protected IBaseFilter _filter = null;

    // The current player associated with this instance
    protected IMediaPlaybackControl _player = null;

    // important, these delegates must NOT be garbage collected
    // or horrible things will happen when the native code tries to call those!
    protected SubtitleCallback _callBack;
    protected readonly ResetCallback _resetCallBack;
    protected readonly UpdateTimeoutCallback _updateTimeoutCallBack;

    /// <summary>
    /// Texture storing the current/last subtitle
    /// </summary>
    protected Texture _subTexture;

    // Timestamp offset in MILLISECONDS
    protected double _startPos = 0;

    protected Subtitle _currentSubtitle = null;
    protected readonly LinkedList<Subtitle> _subtitles;
    protected readonly object _syncObj = new object();

    protected double _currentTime; // File position on last render
    protected bool _useBitmap = true; // If false use teletext
    protected long _subCounter = 0;
    protected bool _clearOnNextRender = false;
    protected bool _renderSubtitles = true;
    protected int _activeSubPage; // If use teletext, what page
    protected int _drawCount = 0;
    
    protected readonly Action _onTextureInvalidated;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or Sets a flag if subtitles should be rendered.
    /// </summary>
    public bool RenderSubtitles
    {
      get
      {
        return _renderSubtitles;
      }
      set
      {
        _renderSubtitles = value;
        if (value)
          EnableSubtitleHandling();
        else
          DisableSubtitleHandling();
      }
    }

    /// <summary>
    /// Gets the name of the required DirectShow filter.
    /// </summary>
    protected virtual string FilterName
    {
      get { return "MediaPortal DVBSub2"; }
    }

    #endregion

    #region Constructor and initialization

    /// <summary>
    /// Constructs a <see cref="SubtitleRenderer"/> instance.
    /// </summary>
    public SubtitleRenderer(Action onTextureInvalidated)
    {
      _onTextureInvalidated = onTextureInvalidated;
      _subtitles = new LinkedList<Subtitle>();
      //instance.textCallBack = new TextSubtitleCallback(instance.OnTextSubtitle);
      _resetCallBack = new ResetCallback(Reset);
      _updateTimeoutCallBack = new UpdateTimeoutCallback(UpdateTimeout);
      _device = SkinContext.Device;
    }

    public void SetPlayer(IMediaPlaybackControl p)
    {
      lock (_subtitles)
        _subtitles.Clear();

      _clearOnNextRender = true;
      _player = p;
    }

    public void SetSubtitleOption(SubtitleOption option)
    {
      if (option.type == SubtitleType.None)
      {
        _useBitmap = false;
        _activeSubPage = 0;
      }
      else if (option.type == SubtitleType.Teletext)
      {
        _useBitmap = false;
        _activeSubPage = option.entry.Page;
        ServiceRegistration.Get<ILogger>().Debug("SubtitleRender: Now rendering {0} teletext subtitle page {1}", option.language, _activeSubPage);
      }
      else if (option.type == SubtitleType.Bitmap)
      {
        _useBitmap = true;
        ServiceRegistration.Get<ILogger>().Debug("SubtitleRender: Now rendering bitmap subtitles in language {0}", option.language);
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Error("Unknown subtitle option " + option);
      }
    }

    #endregion

    #region Callback and event handling

    /// <summary>
    /// Alerts the subtitle render that a seek has just been performed.
    /// Stops displaying the current subtitle and removes any cached subtitles.
    /// Furthermore updates the time that playback starts after the seek.
    /// </summary>
    /// <returns></returns>
    public int OnSeek(double startPos)
    {
      ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: OnSeek - clear subtitles");
      // Remove all previously received subtitles
      lock (_subtitles)
        _subtitles.Clear();

      // Fixed seeking, currently TsPlayer & TsReader is not reseting the base time when seeking
      //this.startPos = startPos;
      _clearOnNextRender = true;
      //posOnLastTextSub = -1;
      ServiceRegistration.Get<ILogger>().Debug("New StartPos is " + startPos);
      return 0;
    }

    /// <summary>
    /// Callback from subtitle filter. Updates the latest subtitle timeout.
    /// </summary>
    /// <returns>The return value is always <c>0</c>.</returns>
    public int UpdateTimeout(ref Int64 timeOut)
    {
      ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: UpdateTimeout");
      Subtitle latest = _subtitles.Count > 0 ? _subtitles.Last.Value : _currentSubtitle;

      if (latest != null)
      {
        latest.TimeOut = (double) timeOut / 1000.0f;
        ServiceRegistration.Get<ILogger>().Debug("  new timeOut = {0}", latest.TimeOut);
      }
      return 0;
    }

    /// <summary>
    /// Callback from subtitle filter, alerting us that a new subtitle is available.
    /// It receives the neew subtitle as the argument sub, which data is only valid for the duration of OnSubtitleV2.
    /// </summary>
    /// <returns>The return value is always <c>0</c>.</returns>
    public int OnSubtitle(IntPtr sub)
    {
      if (!_useBitmap)
        return 0; // TODO: Might be good to let this cache and then check in Render method because bitmap subs arrive a while before display

      ServiceRegistration.Get<ILogger>().Debug("OnSubtitle - stream position " + _player.CurrentTime);
      lock (_syncObj)
      {
        try
        {
          Subtitle subtitle = ToSubtitle(sub);
          //subtitle.SubBitmap.Save(string.Format("C:\\temp\\sub{0}.png", subtitle.Id), ImageFormat.Png); // debug
          ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: to = " + subtitle.TimeOut + " ts=" + subtitle.PresentTime + " fsl=" + subtitle.FirstScanLine + " (startPos = " + _startPos + ")");

          lock (_subtitles)
          {
            while (_subtitles.Count >= MAX_SUBTITLES_IN_QUEUE)
            {
              ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Subtitle queue too big, discarding first element");
              _subtitles.RemoveFirst();
            }
            _subtitles.AddLast(subtitle);
            ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Subtitle added, now have {0} subtitles in cache", _subtitles.Count);
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error(e);
        }
      }
      return 0;
    }

    public void OnTextSubtitle(ref TextSubtitle sub)
    {
      ServiceRegistration.Get<ILogger>().Debug("On TextSubtitle called");
      try
      {
        if (sub.Page == _activeSubPage)
        {
          ServiceRegistration.Get<ILogger>().Debug("Page: " + sub.Page);
          ServiceRegistration.Get<ILogger>().Debug("Character table: " + sub.Encoding);
          ServiceRegistration.Get<ILogger>().Debug("Timeout: " + sub.TimeOut);
          ServiceRegistration.Get<ILogger>().Debug("Timestamp" + sub.TimeStamp);
          ServiceRegistration.Get<ILogger>().Debug("Language: " + sub.Language);

          String content = sub.Text;
          if (content == null)
          {
            ServiceRegistration.Get<ILogger>().Error("OnTextSubtitle: sub.txt == null!");
            return;
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Problem with TEXT_SUBTITLE");
        ServiceRegistration.Get<ILogger>().Error(e);
      }

      try
      {
        // if we dont need the subtitle
        if (!_renderSubtitles || _useBitmap || (_activeSubPage != sub.Page))
        {
          ServiceRegistration.Get<ILogger>().Debug("Text subtitle (page {0}) discarded: useBitmap is {1} and activeSubPage is {2}", sub.Page, _useBitmap, _activeSubPage);
          return;
        }
        ServiceRegistration.Get<ILogger>().Debug("Text subtitle (page {0}) ACCEPTED: useBitmap is {1} and activeSubPage is {2}", sub.Page, _useBitmap, _activeSubPage);

        Subtitle subtitle = new Subtitle
                              {
                                SubBitmap = RenderText(sub.LineContents),
                                TimeOut = sub.TimeOut,
                                PresentTime = sub.TimeStamp / 90000.0f + _startPos,
                                Height = (uint) SkinContext.SkinResources.SkinHeight,
                                Width = (uint) SkinContext.SkinResources.SkinWidth,
                                FirstScanLine = 0
                              };

        lock (_subtitles)
        {
          while (_subtitles.Count >= MAX_SUBTITLES_IN_QUEUE)
          {
            ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Subtitle queue too big, discarding first element");
            _subtitles.RemoveFirst();
          }
          _subtitles.AddLast(subtitle);

          ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Text subtitle added, now have {0} subtitles in cache {1} pos on last render was {2}", _subtitles.Count, subtitle, _currentTime);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Problem processing text subtitle");
        ServiceRegistration.Get<ILogger>().Error(e);
      }

      return;
    }

    #endregion

    #region Filter handling

    /// <summary>
    /// Adds the subtitle filter to the graph. The caller need to call <see cref="Marshal.ReleaseComObject"/> on the
    /// returned instance when done.
    /// </summary>
    /// <param name="graphBuilder">The IGraphBuilder</param>
    /// <returns>DvbSub2(3) filter instance</returns>
    public IBaseFilter AddSubtitleFilter(IGraphBuilder graphBuilder)
    {
      try
      {
        _filter = FilterGraphTools.AddFilterByName(graphBuilder, FilterCategory.LegacyAmFilterCategory, FilterName);
        _subFilter = _filter as IDVBSubtitleSource;
        ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: CreateFilter success: " + (_filter != null) + " & " + (_subFilter != null));
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error(e);
      }
      if (_subFilter != null)
      {
        _subFilter.StatusTest(111);
        _callBack = new SubtitleCallback(OnSubtitle);

        IntPtr pCallback = Marshal.GetFunctionPointerForDelegate(_callBack);
        _subFilter.SetBitmapCallback(pCallback);

        _subFilter.StatusTest(222);

        IntPtr pResetCallBack = Marshal.GetFunctionPointerForDelegate(_resetCallBack);
        _subFilter.SetResetCallback(pResetCallBack);

        IntPtr pUpdateTimeoutCallBack = Marshal.GetFunctionPointerForDelegate(_updateTimeoutCallBack);
        _subFilter.SetUpdateTimeoutCallback(pUpdateTimeoutCallBack);
      }
      return _filter;
    }

    protected virtual void EnableSubtitleHandling()
    {
      _useBitmap = true;
    }

    protected virtual void DisableSubtitleHandling()
    {
      _activeSubPage = -1;
      _useBitmap = false;
      _clearOnNextRender = true;
    }

    protected virtual Subtitle ToSubtitle(IntPtr nativeSubPtr)
    {
      NativeSubtitle nativeSub = (NativeSubtitle)Marshal.PtrToStructure(nativeSubPtr, typeof(NativeSubtitle));
      Subtitle subtitle = new Subtitle
      {
        SubBitmap = new Bitmap(nativeSub.Width, nativeSub.Height, PixelFormat.Format32bppArgb),
        TimeOut = nativeSub.TimeOut,
        PresentTime = ((double)nativeSub.TimeStamp / 1000.0f) + _startPos,
        Height = (uint)nativeSub.Height,
        Width = (uint)nativeSub.Width,
        ScreenWidth = nativeSub.ScreenWidth,
        FirstScanLine = nativeSub.FirstScanLine,
        Id = _subCounter++
      };
      CopyBits(nativeSub.Bits, ref subtitle.SubBitmap, nativeSub.Width, nativeSub.Height, nativeSub.WidthBytes);
      return subtitle;
    }

    protected static void CopyBits(IntPtr srcBits, ref Bitmap destBitmap, int width, int height, int widthBytes)
    {
      // get bits of allocated image
      BitmapData bmData = destBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
      int newSize = bmData.Stride * height;
      int size = widthBytes * height;

      if (newSize != size)
      {
        ServiceRegistration.Get<ILogger>().Error("SubtitleRenderer: newSize != size : {0} != {1}", newSize, size);
      }
      // Copy to new bitmap
      //Marshal.Copy(sub.Bits,bmData.Scan0, 0, newSize);
      byte[] srcData = new byte[size];


      // could be done in one copy, but no IntPtr -> IntPtr Marshal.Copy method exists?
      Marshal.Copy(srcBits, srcData, 0, size);
      Marshal.Copy(srcData, 0, bmData.Scan0, newSize);

      destBitmap.UnlockBits(bmData);
    }

    #endregion

    #region Subtitle rendering

    public void DrawOverlay(Surface targetSurface)
    {
      SetMatchingSubTitle();
      if (targetSurface == null || targetSurface.Disposed || _subTexture == null)
      {
        if (_drawCount > 0)
          ServiceRegistration.Get<ILogger>().Debug("Draw count for last sub: {0}", _drawCount);
        _drawCount = 0;
        return;
      }

      // Available sub is not due
      if (!_currentSubtitle.ShouldDraw)
        return;

      _drawCount++;

      try
      {
        if (!_subTexture.Disposed)
        {
          // TemporaryRenderTarget changes RenderTarget to texture and restores settings when done (Dispose)
          using (new TemporaryRenderTarget(targetSurface))
          using (TemporaryRenderState temporaryRenderState = new TemporaryRenderState())
          using (Sprite sprite = new Sprite(_device))
          {
            sprite.Begin(SpriteFlags.AlphaBlend);
            // No alpha test here, allow all values
            temporaryRenderState.SetTemporaryRenderState(RenderState.AlphaTestEnable, 0);

            // Use the SourceAlpha channel and InverseSourceAlpha for destination
            temporaryRenderState.SetTemporaryRenderState(RenderState.BlendOperation, (int) BlendOperation.Add);
            temporaryRenderState.SetTemporaryRenderState(RenderState.SourceBlend, (int) Blend.SourceAlpha);
            temporaryRenderState.SetTemporaryRenderState(RenderState.DestinationBlend, (int) Blend.InverseSourceAlpha);

            // Check the target texture dimensions and adjust scaling and translation
            SurfaceDescription desc = targetSurface.Description;
            Matrix transform = Matrix.Identity;
            transform *= Matrix.Translation(_currentSubtitle.HorizontalPosition, _currentSubtitle.FirstScanLine, 0);

            // Subtitle could be smaller for 16:9 anamorphic video (subtitle width: 720, video texture: 1024)
            // then we need to scale the subtitle width also.
            if (_currentSubtitle.ScreenWidth != desc.Width)
              transform *= Matrix.Scaling((float) desc.Width / _currentSubtitle.ScreenWidth, 1, 1);

            sprite.Transform = transform;
            sprite.Draw(_subTexture, Color.White);
            sprite.End();
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("Error in DrawOverlay", ex);
      }

      if (_onTextureInvalidated != null)
        _onTextureInvalidated();
    }

    public static Bitmap RenderText(LineContent[] lc)
    {

      int w = SkinContext.SkinResources.SkinWidth;
      int h = SkinContext.SkinResources.SkinHeight;

      Bitmap bmp = new Bitmap(w, h);

      using (Graphics gBmp = Graphics.FromImage(bmp))
      using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
      using (SolidBrush blackBrush = new SolidBrush(Color.FromArgb(0, 0, 0)))
      {
        gBmp.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        for (int i = 0; i < lc.Length; i++)
        {
          using (System.Drawing.Font fnt = new System.Drawing.Font("Courier", (lc[i].doubleHeight ? 22 : 15), FontStyle.Bold)) // fixed width font!
          {
            int vertOffset = (h / lc.Length) * i;

            SizeF size = gBmp.MeasureString(lc[i].line, fnt);
            //gBmp.FillRectangle(new SolidBrush(Color.Pink), new Rectangle(0, 0, w, h));
            int horzOffset = (int) ((w - size.Width) / 2); // center based on actual text width
            gBmp.DrawString(lc[i].line, fnt, blackBrush, new PointF(horzOffset + 1, vertOffset + 0));
            gBmp.DrawString(lc[i].line, fnt, blackBrush, new PointF(horzOffset + 0, vertOffset + 1));
            gBmp.DrawString(lc[i].line, fnt, blackBrush, new PointF(horzOffset - 1, vertOffset + 0));
            gBmp.DrawString(lc[i].line, fnt, blackBrush, new PointF(horzOffset + 0, vertOffset - 1));
            gBmp.DrawString(lc[i].line, fnt, brush, new PointF(horzOffset, vertOffset));

          }
        }
      }
      return bmp;
    }

    #endregion

    #region Subtitle queue handling

    private void SetMatchingSubTitle()
    {
      if (_player == null)
        return;

      if (_clearOnNextRender)
      {
        _clearOnNextRender = false;
        _currentSubtitle = null;
        FilterGraphTools.TryDispose(ref _subTexture);
      }

      if (_renderSubtitles == false)
        return;

      _currentTime = _player.CurrentTime.TotalSeconds;

      // If we currently have a subtitle an it is in current time, no need to look for another one yet
      if (_currentSubtitle != null && _currentSubtitle.PresentTime <= _currentTime && _currentTime <= _currentSubtitle.PresentTime + _currentSubtitle.TimeOut)
      {
        _currentSubtitle.ShouldDraw = true;
        return;
      }

      // If we have already a sub in queue, wait until it's time to show
      if (_currentSubtitle != null && _currentSubtitle.PresentTime > _currentTime)
      {
        _currentSubtitle.ShouldDraw = false;
        return;
      }

      // Check for waiting subs
      Subtitle next;
      lock (_subtitles)
      {
        while (_subtitles.Count > 0)
        {
          next = _subtitles.First.Value;

          // if the next should be displayed now or previously
          if (next.PresentTime <= _currentTime)
          {
            // remove from queue
            _subtitles.RemoveFirst();

            // if it is not too late for this sub to be displayed, break
            if (next.PresentTime + next.TimeOut >= _currentTime)
            {
              next.ShouldDraw = true;
              SetSubtitle(next);
              return;
            }
          }
          else
            break;
        }
      }
      if (_currentSubtitle != null)
        SetSubtitle(null);
      return;
    }

    /// <summary>
    /// Alerts the subtitle render that a reset has just been performed.
    /// Stops displaying the current subtitle and removes any cached subtitles.
    /// </summary>
    /// <returns></returns>
    public int Reset()
    {
      ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: RESET");
      // Remove all previously received subtitles
      lock (_subtitles)
        _subtitles.Clear();

      _clearOnNextRender = true;
      return 0;
    }

    /// <summary>
    /// Update the subtitle texture from a Bitmap.
    /// </summary>
    /// <param name="subtitle"></param>
    private void SetSubtitle(Subtitle subtitle)
    {
      Texture texture = null;
      ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: SetSubtitle : " + subtitle);
      if (subtitle != null)
      {
        try
        {
          Bitmap bitmap = subtitle.SubBitmap;
          if (bitmap != null)
          {
            using (MemoryStream stream = new MemoryStream())
            {
              ImageInformation imageInformation;
              bitmap.Save(stream, ImageFormat.Bmp);
              stream.Position = 0;
              texture = Texture.FromStream(_device, stream, (int) stream.Length, (int) subtitle.Width,
                                           (int) subtitle.Height, 1,
                                           Usage.Dynamic, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0,
                                           out imageInformation);
            }
            // Free bitmap
            FilterGraphTools.TryDispose(ref subtitle.SubBitmap);
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("SubtitleRenderer: Failed to create subtitle texture!!!", e);
          return;
        }
      }
      // Dispose of old subtitle
      FilterGraphTools.TryDispose(ref _subTexture);

      // Set new subtitle
      _subTexture = texture;
      _currentSubtitle = subtitle;
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
      lock (_subtitles)
        _subtitles.Clear();

      FilterGraphTools.TryDispose(ref _subTexture);
    }

    #endregion
  }
}
