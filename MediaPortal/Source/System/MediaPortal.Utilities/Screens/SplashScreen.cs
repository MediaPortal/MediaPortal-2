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

// This file was taken from DGDev from CodeProject (http://www.codeproject.com/KB/dialog/DGDevSplashScreen.aspx).
// Some refactorings and minimal reworks have been made.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer=System.Windows.Forms.Timer;

namespace MediaPortal.Utilities.Screens
{
  public class SplashScreen : Form
  {
    private const double _opacityDecrement = .08;
    private const double _opacityIncrement = .05;
    private const int _timerInterval = 50;
    private static Boolean _fadeMode;
    private static Boolean _fadeInOut;
    private static Image _splashBGImage;
    private static String _infoText;
    private static String _statusText;
    private static SplashScreen _splashScreenForm;
    private static Thread _splashScreenThread;
    private static Color _transparentColor;
    private Label ProgramInfoLabel;
    private Label _statusLabel;
    private Timer _splashTimer;
    private IContainer _components;
    private delegate void UpdateLabel();
    private delegate void CloseSplash();

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
        if (ProgramInfoLabel.InvokeRequired)
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
    /// Sets the background image and automatically sets the size to match the image size.
    /// </summary>
    public Image SplashBackgroundImage
    {
      get { return _splashBGImage; }
      set
      {
        _splashBGImage = value;
        if (value != null)
        {
          BackgroundImage = _splashBGImage;
          ClientSize = _splashBGImage.Size;
        }
      }
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

    public Boolean Fade
    {
      get { return _fadeInOut; }
      set
      {
        _fadeInOut = value;
        Opacity = value ? .00 : 1.00;
      }
    }

    public static SplashScreen Current
    {
      get
      {
        if (_splashScreenForm == null)
          _splashScreenForm = new SplashScreen();
        return _splashScreenForm;
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
        ProgramInfoLabel.Location = location;
      if (width == 0 && height == 0)
        ProgramInfoLabel.AutoSize = true;
      else
      {
        if (width > 0)
          ProgramInfoLabel.Width = width;
        if (height > 0)
          ProgramInfoLabel.Height = height;
      }
    }

    public void ShowSplashScreen()
    {
      _splashScreenThread = new Thread(ShowForm) {IsBackground = true, Name = "SplashScreenThread"};
      _splashScreenThread.Start();
    }

    public void CloseSplashScreen()
    {
      if (_splashScreenForm != null)
      {
        if (InvokeRequired)
        {
          CloseSplash dlgt = HideSplash;
          Invoke(dlgt);
        }
        else
          HideSplash();
      }
    }

    #endregion

    public SplashScreen()
    {
      InitializeComponent();
    }

    private static void ShowForm()
    {
      Application.Run(_splashScreenForm);
    }

    private void UpdateStatus()
    {
      _statusLabel.Text = _statusText;
    }

    private void UpdateInfo()
    {
      ProgramInfoLabel.Text = _infoText;
    }

    #region InitComponents

    private void InitializeComponent()
    {
      _components = new Container();
      ProgramInfoLabel = new Label();
      _statusLabel = new Label();
      _splashTimer = new Timer(_components);
      SuspendLayout();
      // 
      // ProgramInfoLabel
      // 
      ProgramInfoLabel.BackColor = Color.Transparent;
      ProgramInfoLabel.Location = new Point(56, 52);
      ProgramInfoLabel.Name = "ProgramInfoLabel";
      ProgramInfoLabel.Size = new Size(100, 23);
      ProgramInfoLabel.TabIndex = 0;
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
      Controls.Add(ProgramInfoLabel);
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
        if (Opacity < 1.00)
          Opacity += _opacityIncrement;
        else
          _splashTimer.Stop();
      }
      else // Form is closing (Decrement)
      {
        if (Opacity > .00)
          Opacity -= _opacityDecrement;
        else
          Dispose();
      }
    }

    private void SplashScreen_Load(object sender, EventArgs e)
    {
      if (Fade)
      {
        _fadeMode = true;
        _splashTimer.Interval = _timerInterval;
        _splashTimer.Start();
      }
    }

    private void HideSplash()
    {
      if (Fade)
      {
        _fadeMode = false;
        _splashTimer.Start();
      }
      else
        Dispose();
    }
  }
}