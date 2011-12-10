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
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.SkinEngine.ContentManagement;
using SlimDX;
using SlimDX.Direct3D9;
using DirectShowLib;
using System.Drawing.Imaging;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  /// <summary>
  /// Structure used in communication with subtitle filter.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct NativeSubtitle
  {
    // start of bitmap fields
    public Int32 bmType;
    public Int32 bmWidth;
    public Int32 bmHeight;
    public Int32 bmWidthBytes;
    public UInt16 bmPlanes;
    public UInt16 bmBitsPixel;
    public IntPtr bmBits;
    //end of bitmap fields

    // subtitle timestmap
    public UInt64 timeStamp;

    // how long to display subtitle
    public UInt64 timeOut; // in seconds
    public Int32 firstScanLine;
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
    public int encoding;
    public string language;

    public int page;
    public string text; // subtitle lines seperated by newline characters
    public LineContent[] lc;
    public UInt64 timeStamp;
    public UInt64 timeOut; // in seconds
  }

  public enum TeletextCharTable
  {
    English = 1,
    Swedish = 2,
    Third = 3,
    Fourth = 4,
    Fifth = 5,
    Sixth = 6//,
  }

  public class TeletextPageEntry
  {

    public TeletextPageEntry() { }

    public TeletextPageEntry(TeletextPageEntry e)
    {
      page = e.page;
      encoding = e.encoding;
      language = String.Copy(e.language);
    }

    public int page = -1; // indicates not valid
    public TeletextCharTable encoding;
    public string language;
  }

  public class Subtitle
  {
    public static int idCount = 0;
    public Subtitle()
    {
      id = idCount++;
    }
    public Bitmap subBitmap;
    public uint width;
    public uint height;
    public double presentTime;  // NOTE: in seconds
    public double timeOut;      // NOTE: in seconds
    public int firstScanLine;
    public long id = 0;

    public override string ToString()
    {
      return "Subtitle " + id + " meta data: Timeout=" + timeOut + " timestamp=" + presentTime;
    }
  }

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
  public delegate int SubtitleCallback(ref NativeSubtitle sub);
  public delegate int ResetCallback();
  public delegate int UpdateTimeoutCallback(ref Int64 timeOut);

  public class SubtitleRenderer
  {
    bool _reinitialzing = true;
    private readonly DeviceEx _device;
    private bool _useBitmap = true; // if false use teletext
    private int _activeSubPage; // if use teletext, what page
    private static SubtitleRenderer _instance = null;
    private IDVBSubtitleSource _subFilter = null;
    private long _subCounter = 0;
    private const int MAX_SUBTITLES_IN_QUEUE = 20;
    /// <summary>
    /// The coordinates of current vertex buffer
    /// </summary>
    private int _wx0, _wy0, _wwidth0, _wheight0 = 0;

    /// <summary>
    /// Primitive buffer for rendering subtitles.
    /// </summary>
    private readonly PrimitiveBuffer _primitiveContext = new PrimitiveBuffer();

    // important, these delegates must NOT be garbage collected
    // or horrible things will happen when the native code tries to call those!
    private readonly SubtitleCallback _callBack;
    // private TextSubtitleCallback textCallBack;
    private readonly ResetCallback _resetCallBack;
    private readonly UpdateTimeoutCallback _updateTimeoutCallBack;

    private double _posOnLastRender; //file position on last render

    /// <summary>
    /// Texture storing the current/last subtitle
    /// </summary>
    private Texture _subTexture;

    /// <summary>
    /// Reference to the DirectShow DVBSub filter, which 
    /// is the source of our subtitle bitmaps
    /// </summary>
    private IBaseFilter _filter = null;

    // timestampt offset in MILLISECONDS
    private double _startPos = 0;

    private Subtitle _currentSubtitle = null;
    private IMediaPlaybackControl _player = null;
    private readonly LinkedList<Subtitle> _subtitles;
    private readonly object _alert = new object();

    private bool _clearOnNextRender = false;
    private bool _renderSubtitles = true;

    private Matrix _finalTransform;

    public bool RenderSubtitles
    {
      get
      {
        return _renderSubtitles;
      }
      set
      {
        _renderSubtitles = value;
        if (value == false)
        {
          _activeSubPage = -1;
          _useBitmap = false;
          _clearOnNextRender = true;
        }
      }
    }

    public Matrix FinalTransform
    {
      get { return _finalTransform; }
      set { _finalTransform = value; }
    }

    public SubtitleRenderer()
    {
      _subtitles = new LinkedList<Subtitle>();
      _callBack = new SubtitleCallback(OnSubtitle);
      //instance.textCallBack = new TextSubtitleCallback(instance.OnTextSubtitle);
      _resetCallBack = new ResetCallback(Reset);
      _updateTimeoutCallBack = new UpdateTimeoutCallback(UpdateTimeout);
      _device = SkinContext.Device;
    }

    public void SetPlayer(IMediaPlaybackControl p)
    {
      lock (_subtitles)
      {
        _subtitles.Clear();
      }
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
        _activeSubPage = option.entry.page;
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
      {
        _subtitles.Clear();
      }
      // Fixed seeking, currently TsPlayer & TsReader is not reseting the base time when seeking
      //this.startPos = startPos;
      _clearOnNextRender = true;
      //posOnLastTextSub = -1;
      ServiceRegistration.Get<ILogger>().Debug("New StartPos is " + startPos);
      return 0;
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
    /// Callback from subtitle filter. Updates the latest subtitle timeout.
    /// </summary>
    /// <returns>The return value is always <c>0</c>.</returns>
    public int UpdateTimeout(ref Int64 timeOut)
    {
      ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: UpdateTimeout");
      Subtitle latest = _subtitles.Count > 0 ? _subtitles.Last.Value : _currentSubtitle;

      if (latest != null)
      {
        latest.timeOut = (double) timeOut / 1000.0f;
        ServiceRegistration.Get<ILogger>().Debug("  new timeOut = {0}", latest.timeOut);
      }
      return 0;
    }


    /// <summary>
    /// Callback from subtitle filter, alerting us that a new subtitle is available.
    /// It receives the neew subtitle as the argument sub, which data is only valid for the duration of OnSubtitle.
    /// </summary>
    /// <returns>The return value is always <c>0</c>.</returns>
    public int OnSubtitle(ref NativeSubtitle sub)
    {
      if (!_useBitmap) return 0; // TODO: Might be good to let this cache and then check in Render method because bitmap subs arrive a while before display
      ServiceRegistration.Get<ILogger>().Debug("OnSubtitle - stream position " + _player.CurrentTime);
      lock (_alert)
      {
        try
        {
          ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer:  Bitmap: bpp=" + sub.bmBitsPixel + " planes " + sub.bmPlanes + " dim = " + sub.bmWidth + " x " + sub.bmHeight + " stride : " + sub.bmWidthBytes);
          ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: to = " + sub.timeOut + " ts=" + sub.timeStamp + " fsl=" + sub.firstScanLine + " (startPos = " + _startPos + ")");

          Subtitle subtitle = new Subtitle();
          subtitle.subBitmap = new Bitmap(sub.bmWidth, sub.bmHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
          subtitle.timeOut = sub.timeOut;
          subtitle.presentTime = ((double)sub.timeStamp / 1000.0f) + _startPos; // compute present time in SECONDS
          subtitle.height = (uint)sub.bmHeight;
          subtitle.width = (uint)sub.bmWidth;
          subtitle.firstScanLine = sub.firstScanLine;
          subtitle.id = _subCounter++;
          //ServiceRegistration.Get<ILogger>().Debug("Received Subtitle : " + subtitle.ToString());

          // get bits of allocated image
          BitmapData bmData = subtitle.subBitmap.LockBits(new Rectangle(0, 0, sub.bmWidth, sub.bmHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
          int newSize = bmData.Stride * sub.bmHeight;
          int size = sub.bmWidthBytes * sub.bmHeight;

          if (newSize != size)
          {
            ServiceRegistration.Get<ILogger>().Error("SubtitleRenderer: newSize != size : {0} != {1}", newSize, size);
          }
          // Copy to new bitmap
          //Marshal.Copy(sub.bmBits,bmData.Scan0, 0, newSize);
          byte[] srcData = new byte[size];

          // could be done in one copy, but no IntPtr -> IntPtr Marshal.Copy method exists?
          Marshal.Copy(sub.bmBits, srcData, 0, size);
          Marshal.Copy(srcData, 0, bmData.Scan0, newSize);

          subtitle.subBitmap.UnlockBits(bmData);

          // subtitle.subBitmap.Save("C:\\users\\petert\\sub" + subtitle.id + ".bmp"); // debug

          lock (_subtitles)
          {
            while (_subtitles.Count >= MAX_SUBTITLES_IN_QUEUE)
            {
              ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Subtitle queue too big, discarding first element");
              _subtitles.RemoveFirst();
            }
            _subtitles.AddLast(subtitle);
            ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Subtitle added, now have " + _subtitles.Count + " subtitles in cache");
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error(e);
        }
      }
      return 0;
    }

    /* private double posOnLastTextSub = -1;
     private bool lastTextSubBlank = false;
     private bool useMinSeperation = false;*/

    public void OnTextSubtitle(ref TextSubtitle sub)
    {
      //bool blank = false;
      ServiceRegistration.Get<ILogger>().Debug("On TextSubtitle called");

      try
      {
        if (sub.page == _activeSubPage)
        {
          ServiceRegistration.Get<ILogger>().Debug("Page: " + sub.page);
          ServiceRegistration.Get<ILogger>().Debug("Character table: " + sub.encoding);
          ServiceRegistration.Get<ILogger>().Debug("Timeout: " + sub.timeOut);
          ServiceRegistration.Get<ILogger>().Debug("Timestamp" + sub.timeStamp);
          ServiceRegistration.Get<ILogger>().Debug("Language: " + sub.language);

          String content = sub.text;
          if (content == null)
          {
            ServiceRegistration.Get<ILogger>().Error("OnTextSubtitle: sub.txt == null!");
            return;
          }
          // FIXME: Remove this
          //ServiceRegistration.Get<ILogger>().Debug("Content: ");
          //if (content.Trim().Length > 0) // debug log subtitles
          //{
          //  StringTokenizer st = new StringTokenizer(content, new char[] { '\n' });
          //  while (st.HasMore)
          //  {
          //    ServiceRegistration.Get<ILogger>().Debug(st.NextToken());
          //  }
          //}
          //else
          //{
          //  //blank = true;
          //  ServiceRegistration.Get<ILogger>().Debug("<BLANK PAGE>");
          //}
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
        if (!_renderSubtitles || _useBitmap || (_activeSubPage != sub.page))
        {
          ServiceRegistration.Get<ILogger>().Debug("Text subtitle (page {0}) discarded: useBitmap is {1} and activeSubPage is {2}", sub.page, _useBitmap, _activeSubPage);
          return;
        }
        ServiceRegistration.Get<ILogger>().Debug("Text subtitle (page {0}) ACCEPTED: useBitmap is {1} and activeSubPage is {2}", sub.page, _useBitmap, _activeSubPage);

        Subtitle subtitle = new Subtitle();
        subtitle.subBitmap = RenderText(sub.lc);
        subtitle.timeOut = sub.timeOut;
        subtitle.presentTime = sub.timeStamp / 90000.0f + _startPos;

        subtitle.height = (uint) SkinContext.SkinResources.SkinHeight;
        subtitle.width = (uint) SkinContext.SkinResources.SkinWidth;
        subtitle.firstScanLine = 0;

        lock (_subtitles)
        {
          while (_subtitles.Count >= MAX_SUBTITLES_IN_QUEUE)
          {
            ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Subtitle queue too big, discarding first element");
            _subtitles.RemoveFirst();
          }
          _subtitles.AddLast(subtitle);

          ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Text subtitle added, now have " + _subtitles.Count + " subtitles in cache " + subtitle + " pos on last render was " + _posOnLastRender);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Problem processing text subtitle");
        ServiceRegistration.Get<ILogger>().Error(e);
      }

      return;
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
            int horzOffset = (int)((w - size.Width) / 2); // center based on actual text width
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


    /// <summary>
    /// Update the subtitle texture from a Bitmap.
    /// </summary>
    /// <param name="subtitle"></param>
    private void SetSubtitle(Subtitle subtitle)
    {
      ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: SetSubtitle : " + subtitle);
      Texture texture;
      try
      {
        Bitmap bitmap = subtitle.subBitmap;
        if (bitmap != null)
        {
          // allocate new texture
          texture = new Texture(_device, bitmap.Width, bitmap.Height, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);

          DataRectangle rect = texture.LockRectangle(0, LockFlags.None);

          BitmapData bd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

          // Quick copy of content
          unsafe
          {
            byte* to = (byte*)rect.Data.DataPointer.ToPointer();
            byte* from = (byte*)bd.Scan0.ToPointer();
            for (int y = 0; y < bd.Height; ++y)
            {
              for (int x = 0; x < bd.Width * 4; ++x)
              {
                to[rect.Pitch * y + x] = from[y * bd.Stride + x];
              }
            }
          }

          texture.UnlockRectangle(0);
          bitmap.UnlockBits(bd);
          bitmap.Dispose();
          bitmap = null;
          rect.Data.Dispose();
        }
        else
        {
          texture = new Texture(_device, 100, 100, 1, Usage.Dynamic, Format.A8R8G8B8, Pool.Default);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: Failed to create subtitle surface!!!");
        ServiceRegistration.Get<ILogger>().Error(e);
        return;
      }

      // dispose of old subtitle
      if (_subTexture != null)
      {
        _subTexture.Dispose();
        _subTexture = null;
      }

      // set new subtitle
      _subTexture = texture;
      _currentSubtitle = subtitle;
      _currentSubtitle.subBitmap.Dispose();
      _currentSubtitle.subBitmap = null;
    }

    /// <summary>
    /// Adds the subtitle filter to the graph.
    /// </summary>
    /// <param name="graphBuilder"></param>
    /// <returns></returns>
    public IBaseFilter AddSubtitleFilter(IGraphBuilder graphBuilder)
    {
      try
      {
        _filter = FilterGraphTools.AddFilterByName(graphBuilder, FilterCategory.LegacyAmFilterCategory, "MediaPortal DVBSub2");
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

    public void Render()
    {
      if (_player == null)
        return;
      //ServiceRegistration.Get<ILogger>().Debug("\n\n***** SubtitleRenderer: Subtitle render *********");
      // ServiceRegistration.Get<ILogger>().Debug(" Stream pos: "+player.StreamPosition); 
      //if (!GUIGraphicsContext.IsFullScreenVideo) return;

      if (_clearOnNextRender)
      {
        //ServiceRegistration.Get<ILogger>().Debug("SubtitleRenderer: clearOnNextRender");
        _clearOnNextRender = false;
        if (_subTexture != null) _subTexture.Dispose();
        _subTexture = null;
        _currentSubtitle = null;
      }

      if (_renderSubtitles == false)
        return;

      // ugly temp!
      bool timeForNext = false;
      lock (_subtitles)
      {
        if (_subtitles.Count > 0)
        {
          Subtitle next = _subtitles.First.Value;
          if (next.presentTime <= _player.CurrentTime.TotalSeconds) timeForNext = true;
          else
          {
            //ServiceRegistration.Get<ILogger>().Debug("-NEXT subtitle is in the future");
          }
        }
      }

      _posOnLastRender = _player.CurrentTime.TotalSeconds;

      // Check for subtitle if we dont have one currently or if the current one is beyond its timeout
      if (_currentSubtitle == null || _currentSubtitle.presentTime + _currentSubtitle.timeOut <= _player.CurrentTime.TotalSeconds || timeForNext)
      {
        //ServiceRegistration.Get<ILogger>().Debug("-Current position: ");
        if (_currentSubtitle != null && !timeForNext)
        {
          //ServiceRegistration.Get<ILogger>().Debug("-Current subtitle : " + currentSubtitle.ToString() + " time out expired");
          _currentSubtitle = null;
        }
        if (timeForNext)
        {
          //if (currentSubtitle != null) ServiceRegistration.Get<ILogger>().Debug("-Current subtitle : " + currentSubtitle.ToString() + " TIME FOR NEXT!");
        }

        Subtitle next;
        lock (_subtitles)
        {
          while (_subtitles.Count > 0)
          {
            next = _subtitles.First.Value;

            //ServiceRegistration.Get<ILogger>().Debug("-next from queue: " + next.ToString());
            // if the next should be displayed now or previously
            if (next.presentTime <= _player.CurrentTime.TotalSeconds)
            {
              // remove from queue
              _subtitles.RemoveFirst();

              // if it is not too late for this sub to be displayed, break
              // otherwise continue
              if (next.presentTime + next.timeOut >= _player.CurrentTime.TotalSeconds)
              {
                _currentSubtitle = next;
                break;
              }
            }
            // next wants to be displayed in the future so break
            else
            {

              //ServiceRegistration.Get<ILogger>().Debug("-next is in the future");
              break;
            }
          }
        }
        // if currentSubtitle is non-null we have a new subtitle
        if (_currentSubtitle != null)
          SetSubtitle(_currentSubtitle);
        else return;
      }
      bool alphaTest = false;
      bool alphaBlend = false;
      try
      {
        // store current settings so they can be restored when we are done
        alphaTest = true;// (GraphicsDevice.Device.GetRenderState(RenderState.AlphaTestEnable) != 0);
        alphaBlend = true;// (GraphicsDevice.Device.GetRenderState(RenderState.AlphaBlendEnable) != 0);

        int wwidth = 0, wheight = 0;
        float rationW = 1, rationH = 1;

        // FIXME: which "MovieRectangle" to use here?
        Rectangle movieRect = Rectangle.Empty;// player.MovieRectangle;
        rationH = movieRect.Height / (float) SkinContext.SkinResources.SkinHeight;
        rationW = movieRect.Width / (float) SkinContext.SkinResources.SkinWidth;

        int wx = (movieRect.Right) - (movieRect.Width / 2) - (int)((_currentSubtitle.width * rationW) / 2);
        int wy = movieRect.Top + (int)(rationH * _currentSubtitle.firstScanLine);


        wwidth = (int)(_currentSubtitle.width * rationW);
        wheight = (int)(_currentSubtitle.height * rationH);

        // make sure the vertex buffer is ready and correct for the coordinates
        CreateVertexBuffer(wx, wy, wwidth, wheight);

        // ServiceRegistration.Get<ILogger>().Debug("Subtitle render target: wx = {0} wy = {1} ww = {2} wh = {3}", wx, wy, wwidth, wheight);

        // enable alpha testing so that the subtitle is rendered with transparent background
        _device.SetRenderState(RenderState.AlphaBlendEnable, true);
        _device.SetRenderState(RenderState.AlphaTestEnable, false);

        EffectAsset effect = ContentManager.Instance.GetEffect("normal");
        effect.StartRender(_subTexture, FinalTransform);
        _primitiveContext.Render(0);
        effect.EndRender();
      }
      catch (Exception e)
      {

        ServiceRegistration.Get<ILogger>().Error(e);
      }
      try
      {
        // Restore device settings
        _device.SetTexture(0, null);
        _device.SetRenderState(RenderState.AlphaBlendEnable, alphaBlend);
        _device.SetRenderState(RenderState.AlphaTestEnable, alphaTest);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error(e);
      }
    }

    /// <summary>
    /// Creates a vertex buffer for a transformed textured quad matching
    /// the given rectangle and stores it in vertexBuffer
    /// </summary>
    /// <param name="wx"></param>
    /// <param name="wy"></param>
    /// <param name="wwidth"></param>
    /// <param name="wheight"></param>
    private void CreateVertexBuffer(int wx, int wy, int wwidth, int wheight)
    {
      if (_wx0 != wx || _wy0 != wy || _wwidth0 != wwidth || _wheight0 != wheight)
      {
        ServiceRegistration.Get<ILogger>().Debug("Subtitle: Setting vertices");
        PositionColoredTextured[] verts = new PositionColoredTextured[4];
        int color;
        unchecked
        {
          color = (int) 0xffffffff;
        }

        // Upper left
        verts[0].X = wx;
        verts[0].Y = wy;
        verts[0].Z = 1.0f;
        verts[0].Tu1 = 0;
        verts[0].Tv1 = 0;
        verts[0].Color = color;

        // Upper right
        verts[1].X = wx + wwidth;
        verts[1].Y = wy;
        verts[1].Z = 1.0f;
        verts[1].Tu1 = 1;
        verts[1].Tv1 = 0;
        verts[1].Color = color;

        // Lower left
        verts[2].X = wx;
        verts[2].Y = wy + wheight;
        verts[2].Z = 1.0f;
        verts[2].Tu1 = 0;
        verts[2].Tv1 = 1;
        verts[2].Color = color;

        // Lower right
        verts[3].X = wx + wwidth;
        verts[3].Y = wy + wheight;
        verts[3].Z = 1.0f;
        verts[3].Tu1 = 1;
        verts[3].Tv1 = 1;
        verts[3].Color = color;


        // Remember what the vertexBuffer is set to
        _wy0 = wy;
        _wx0 = wx;
        _wheight0 = wheight;
        _wwidth0 = wwidth;
        _primitiveContext.Set(ref verts, PrimitiveType.TriangleStrip);
      }
    }

    /// <summary>
    /// Cleans up resources
    /// </summary>
    public void Clear()
    {
      _startPos = 0;
      lock (_subtitles)
      {
        _subtitles.Clear();
      }
      // swap
      if (_subTexture != null)
      {
        _subTexture.Dispose();
        _subTexture = null;
        lock (_alert)
        {
          _subFilter = null;
        }
      }
    }

    public void ReleaseResources()
    {
      _reinitialzing = true;
      if (_subTexture != null)
      {
        _subTexture.Dispose();
        _subTexture = null;
      }
      _primitiveContext.Dispose();
    }

    public void ReallocResources()
    {
      _reinitialzing = false;
    }
  }
}
