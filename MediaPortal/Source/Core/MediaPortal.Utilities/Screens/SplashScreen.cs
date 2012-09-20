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

// This file was taken from DGDev from CodeProject (http://www.codeproject.com/KB/dialog/DGDevSplashScreen.aspx).
// Some refactorings and minimal reworks have been made.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Utilities.Graphics;
using Timer = System.Windows.Forms.Timer;

namespace MediaPortal.Utilities.Screens
{
  public class SplashScreen : Form
  {
    /// <summary>
    /// Fade timer interval in MS.
    /// </summary>
    protected const int FADE_TIMER_INTERVAL = 50;

    public const int DEFAULT_FADE_IN_MS = 1000;
    public const int DEFAULT_FADE_OUT_MS = 500;

    private delegate void UpdateLabel();
    private delegate void CloseSplash();

    private static TimeSpan _fadeInDuration = TimeSpan.Zero;
    private static TimeSpan _fadeOutDuration = TimeSpan.Zero;

    private double _opacityDecrement;
    private double _opacityIncrement;
    private Boolean _fadeMode;
    private String _infoText;
    private String _statusText;
    private Thread _splashScreenThread;
    private Color _transparentColor;
    private Label _programInfoLabel;
    private Label _statusLabel;
    private Timer _splashTimer;
    private IContainer _components;

    #region Public Properties & Methods

    /// <summary>
    /// Sets the content of the info label.
    /// </summary>
    public String InfoText
    {
      get { return _infoText; }
      set
      {
        _infoText = value;
        if (_programInfoLabel.InvokeRequired)
        {
          var InfoUpdate = new UpdateLabel(UpdateInfo);
          Invoke(InfoUpdate);
        }
        else
          UpdateInfo();
      }
    }

    /// <summary>
    /// Sets the content of the status label.
    /// </summary>
    public String StatusText
    {
      get { return _statusText; }
      set
      {
        _statusText = value;
        if (_statusLabel.InvokeRequired)
        {
          var StatusUpdate = new UpdateLabel(UpdateStatus);
          Invoke(StatusUpdate);
        }
        else
          UpdateStatus();
      }
    }

    /// <summary>
    /// Gets or Sets a flag if the <see cref="SplashBackgroundImage"/> should be scaled to fullscreen.
    /// To enable resizing, set this property to true before you set the image.
    /// </summary>
    public bool ScaleToFullscreen { get; set; }

    /// <summary>
    /// Sets the background image and automatically sets the size to match the image size.
    /// </summary>
    public Image SplashBackgroundImage
    {
      get { return BackgroundImage; }
      set { ResizeImageFullscreen(value); }
    }

    /// <summary>
    /// Sets the color of the background image which will be presented transparent.
    /// </summary>
    public Color TransparentColor
    {
      get { return _transparentColor; }
      set
      {
        _transparentColor = value;
        if (value != Color.Empty)
          TransparencyKey = TransparentColor;
      }
    }

    /// <summary>
    /// Sets the duration how long the form will fade in. If set to <see cref="TimeSpan.Zero"/>, the form will be
    /// shown at once.
    /// </summary>
    public TimeSpan FadeInDuration
    {
      get { return _fadeInDuration; }
      set
      {
        _fadeInDuration = value;
        if (_fadeInDuration == TimeSpan.Zero)
        {
          _opacityIncrement = 0;
          Opacity = 1;
        }
        else
        {
          _opacityIncrement = FADE_TIMER_INTERVAL / _fadeInDuration.TotalMilliseconds;
          Opacity = 0;
        }
      }
    }

    /// <summary>
    /// Sets the duration how long the form will fade out. If set to <see cref="TimeSpan.Zero"/>, the form will be
    /// hidden at once.
    /// </summary>
    public TimeSpan FadeOutDuration
    {
      get { return _fadeOutDuration; }
      set
      {
        _fadeOutDuration = value;
        if (_fadeOutDuration == TimeSpan.Zero)
          _opacityDecrement = 0;
        else
          _opacityDecrement = FADE_TIMER_INTERVAL / _fadeOutDuration.TotalMilliseconds;
      }
    }

    /// <summary>
    /// Sets default values for the <see cref="FadeInDuration"/> and <see cref="FadeOutDuration"/>.
    /// </summary>
    /// <param name="value">If set to <c>true</c>, the form will fade in in <see cref="DEFAULT_FADE_IN_MS"/> and fade out
    /// int <see cref="DEFAULT_FADE_OUT_MS"/>, else, the form won't fade in and out.</param>
    public void SetFade(bool value)
    {
      if (value)
      {
        FadeInDuration = TimeSpan.FromMilliseconds(DEFAULT_FADE_IN_MS);
        FadeOutDuration = TimeSpan.FromMilliseconds(DEFAULT_FADE_OUT_MS);
      }
      else
      {
        FadeInDuration = TimeSpan.Zero;
        FadeOutDuration = TimeSpan.Zero;
      }
    }

    public void PositionStatusLabel(Point location, int width, int height)
    {
      if (location != Point.Empty)
        _statusLabel.Location = location;
      if (width == 0 && height == 0)
        _statusLabel.AutoSize = true;
      else
      {
        if (width > 0)
          _statusLabel.Width = width;
        if (height > 0)
          _statusLabel.Height = height;
      }
    }

    public void PositionInfoLabel(Point location, int width, int height)
    {
      if (location != Point.Empty)
        _programInfoLabel.Location = location;
      if (width == 0 && height == 0)
        _programInfoLabel.AutoSize = true;
      else
      {
        if (width > 0)
          _programInfoLabel.Width = width;
        if (height > 0)
          _programInfoLabel.Height = height;
      }
    }

    public void ShowSplashScreen()
    {
      _splashScreenThread = new Thread(ShowForm) { IsBackground = true, Name = "SplashScr" };
      _splashScreenThread.Start();
    }

    public void CloseSplashScreen()
    {
      if (InvokeRequired)
      {
        CloseSplash dlgt = HideSplash;
        Invoke(dlgt);
      }
      else
        HideSplash();
    }

    #endregion

    public SplashScreen()
    {
      InitializeComponent();
    }

    private void ResizeImageFullscreen(Image backgroundImage)
    {
      if (backgroundImage == null)
        return;
      Size screen = Screen.PrimaryScreen.Bounds.Size;
      if (ScaleToFullscreen && (screen.Width != backgroundImage.Width || screen.Height != backgroundImage.Height))
        backgroundImage = ImageUtilities.ResizeImageExact(backgroundImage, screen.Width, screen.Height);

      BackgroundImage = backgroundImage;
      ClientSize = backgroundImage.Size;
    }

    private void ShowForm()
    {
      Application.Run(this);
    }

    private void UpdateStatus()
    {
      _statusLabel.Text = _statusText;
    }

    private void UpdateInfo()
    {
      _programInfoLabel.Text = _infoText;
    }

    #region InitComponents

    private void InitializeComponent()
    {
      _components = new Container();
      _programInfoLabel = new Label();
      _statusLabel = new Label();
      _splashTimer = new Timer(_components);
      SuspendLayout();
      // 
      // _programInfoLabel
      // 
      _programInfoLabel.BackColor = Color.Transparent;
      _programInfoLabel.Location = new Point(56, 52);
      _programInfoLabel.Name = "_programInfoLabel";
      _programInfoLabel.Size = new Size(100, 23);
      _programInfoLabel.TabIndex = 0;
      // 
      // _statusLabel
      // 
      _statusLabel.BackColor = Color.Transparent;
      _statusLabel.Location = new Point(59, 135);
      _statusLabel.Name = "_statusLabel";
      _statusLabel.Size = new Size(100, 23);
      _statusLabel.TabIndex = 1;
      // 
      // SplashScreen
      // 
      ClientSize = new Size(292, 273);
      Controls.Add(_statusLabel);
      Controls.Add(_programInfoLabel);
      FormBorderStyle = FormBorderStyle.None;
      Name = "SplashScreen";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterScreen;
      ResumeLayout(false);
      Load += SplashScreen_Load;
      _splashTimer.Tick += SplashTimer_Tick;

    }

    #endregion

    private void SplashTimer_Tick(object sender, EventArgs e)
    {
      if (_fadeMode) // Form is opening (Increment)
      {
        if (Opacity < 0.99)
          Opacity += _opacityIncrement;
        else
          _splashTimer.Stop();
      }
      else // Form is closing (Decrement)
      {
        if (Opacity > .01)
          Opacity -= _opacityDecrement;
        else
          Dispose();
      }
    }

    private void SplashScreen_Load(object sender, EventArgs e)
    {
      if (_fadeInDuration != TimeSpan.Zero)
      {
        _fadeMode = true;
        _splashTimer.Interval = FADE_TIMER_INTERVAL;
        _splashTimer.Start();
      }
    }

    private void HideSplash()
    {
      if (_fadeOutDuration != TimeSpan.Zero)
      {
        _fadeMode = false;
        _splashTimer.Start();
      }
      else
        Dispose();
    }
  }
}