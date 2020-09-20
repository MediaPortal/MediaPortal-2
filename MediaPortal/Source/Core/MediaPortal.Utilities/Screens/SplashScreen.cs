#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

    private TimeSpan _fadeInDuration = TimeSpan.Zero;
    private TimeSpan _fadeOutDuration = TimeSpan.Zero;

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
    private IContainer components;
    private PictureBox picture;
    private Image _backgroundImage;

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
          var infoUpdate = new UpdateLabel(UpdateInfo);
          Invoke(infoUpdate);
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
          var statusUpdate = new UpdateLabel(UpdateStatus);
          Invoke(statusUpdate);
        }
        else
          UpdateStatus();
      }
    }

    /// <summary>
    /// Sets the screen where the SplashScreen should be shown.
    /// </summary>
    public int? StartupScreen { get; set; }

    /// <summary>
    /// Gets or sets a flag if the <see cref="SplashBackgroundImage"/> should be scaled to fullscreen.
    /// To enable resizing, set this property to <c>true</c>.
    /// </summary>
    public bool ScaleToFullscreen { get; set; }

    /// <summary>
    /// Sets the background image.
    /// </summary>
    public Image SplashBackgroundImage
    {
      get { return BackgroundImage; }
      set { _backgroundImage = value; }
    }

    /// <summary>
    /// Allows to use a PictureBox control which also supports animations for GIF.
    /// </summary>
    public bool UsePictureBox { get; set; }

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
    /// Sets the duration how long the form will fade in. If set to <see cref="TimeSpan.Zero"/>, the form will be shown at once.
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
    /// Sets the duration how long the form will fade out. If set to <see cref="TimeSpan.Zero"/>, the form will be hidden at once.
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
      Load += SplashScreenLoad;
      _splashTimer.Tick += SplashTimerTick;
    }

    private void SetFormLocationAndBackground(Image backgroundImage)
    {
      if (backgroundImage == null)
        return;

      // Default screen for splashscreen is the one from where MP2 was started.
      Screen preferredScreen = Screen.FromControl(this);

      // Force the splashscreen to be displayed on a specific screen.
      if (StartupScreen.HasValue && StartupScreen.Value >= 0 && StartupScreen.Value < Screen.AllScreens.Length)
        preferredScreen = Screen.AllScreens[StartupScreen.Value];

      Rectangle screenBounds = preferredScreen.Bounds;
      Size screen = screenBounds.Size;

      if (UsePictureBox)
      {
        // Overlay over picture box doesn't support transparency properly
        _statusLabel.Visible = !string.IsNullOrEmpty(_statusLabel.Text);
        _statusLabel.BackColor = Color.Black;
        _programInfoLabel.Visible = !string.IsNullOrEmpty(_programInfoLabel.Text);
        _programInfoLabel.BackColor = Color.Black;
        Bounds = preferredScreen.Bounds;
        if (ScaleToFullscreen)
        {
          backgroundImage = ImageUtilities.ResizeImageUniformToFill(backgroundImage, screen.Width, screen.Height);
        }
        picture.Visible = true;
        picture.Image = backgroundImage;
        picture.SizeMode = PictureBoxSizeMode.CenterImage;
      }
      else
      {
        if (ScaleToFullscreen && (screen.Width != backgroundImage.Width || screen.Height != backgroundImage.Height))
          backgroundImage = ImageUtilities.ResizeImageExact(backgroundImage, screen.Width, screen.Height);

        BackgroundImage = backgroundImage;
        Location = screenBounds.Location;
        ClientSize = backgroundImage.Size;
      }
    }

    private void ShowForm()
    {
      Application.Run(this);
    }

    private void UpdateStatus()
    {
      _statusLabel.Text = _statusText;
      _statusLabel.Visible = !string.IsNullOrEmpty(_statusLabel.Text);
    }

    private void UpdateInfo()
    {
      _programInfoLabel.Text = _infoText;
      _programInfoLabel.Visible = !string.IsNullOrEmpty(_programInfoLabel.Text);
    }

    #region InitComponents

    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this._programInfoLabel = new System.Windows.Forms.Label();
      this._statusLabel = new System.Windows.Forms.Label();
      this._splashTimer = new System.Windows.Forms.Timer(this.components);
      this.picture = new System.Windows.Forms.PictureBox();
      ((System.ComponentModel.ISupportInitialize)(this.picture)).BeginInit();
      this.SuspendLayout();
      // 
      // _programInfoLabel
      // 
      this._programInfoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this._programInfoLabel.BackColor = System.Drawing.Color.Black;
      this._programInfoLabel.Font = new System.Drawing.Font("Segoe UI", 19.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._programInfoLabel.ForeColor = System.Drawing.Color.Silver;
      this._programInfoLabel.Location = new System.Drawing.Point(1420, 1022);
      this._programInfoLabel.Name = "_programInfoLabel";
      this._programInfoLabel.Size = new System.Drawing.Size(490, 50);
      this._programInfoLabel.TabIndex = 0;
      this._programInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
      // 
      // _statusLabel
      // 
      this._statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this._statusLabel.BackColor = System.Drawing.Color.Black;
      this._statusLabel.Font = new System.Drawing.Font("Segoe UI", 19.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._statusLabel.ForeColor = System.Drawing.Color.Silver;
      this._statusLabel.Location = new System.Drawing.Point(12, 1022);
      this._statusLabel.Name = "_statusLabel";
      this._statusLabel.Size = new System.Drawing.Size(1400, 50);
      this._statusLabel.TabIndex = 1;
      // 
      // picture
      // 
      this.picture.BackColor = System.Drawing.Color.Black;
      this.picture.Dock = System.Windows.Forms.DockStyle.Fill;
      this.picture.Location = new System.Drawing.Point(0, 0);
      this.picture.Name = "picture";
      this.picture.Size = new System.Drawing.Size(1920, 1080);
      this.picture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
      this.picture.TabIndex = 2;
      this.picture.TabStop = false;
      this.picture.Visible = false;
      // 
      // SplashScreen
      // 
      this.ClientSize = new System.Drawing.Size(1920, 1080);
      this.Controls.Add(this._statusLabel);
      this.Controls.Add(this._programInfoLabel);
      this.Controls.Add(this.picture);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
      this.Name = "SplashScreen";
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      ((System.ComponentModel.ISupportInitialize)(this.picture)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private void SplashTimerTick(object sender, EventArgs e)
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

    private void SplashScreenLoad(object sender, EventArgs e)
    {
      SetFormLocationAndBackground(_backgroundImage);

      // Force activation and foreground
      this.SafeActivate();
      BringToFront();

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
