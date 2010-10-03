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

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace DGDev
{
    public class SplashScreen : Form
    {
        private const double OpacityDecrement = .08;
        private const double OpacityIncrement = .05;
        private const int TimerInterval = 50;
        private static Boolean FadeMode;
        private static Boolean FadeInOut;
        private static Image BGImage;
        private static String Information;
        private static String Status;
        private static SplashScreen SplashScreenForm;
        private static Thread SplashScreenThread;
        private static Color TransparentKey;
        private Label ProgramInfoLabel;
        private Label StatusLabel;
        private System.Windows.Forms.Timer SplashTimer;
        private IContainer components;
        private delegate void UpdateLabel();
        private delegate void CloseSplash();

        #region Public Properties & Methods

        /// <summary>
        /// These methods can all be called to set configurable 
                  /// parameters for the Splash Screen
        /// </summary>
        public String SetInfo
        {
            get { return Information; }
            set
            {
                Information = value;
                if(ProgramInfoLabel.InvokeRequired)
                {
                    var InfoUpdate = 
                        new UpdateLabel(UpdateInfo);
                    Invoke(InfoUpdate);
                }
                else
                {
                    UpdateInfo();
                }
            }
        }

        public String SetStatus
        {
            get { return Status; }
            set
            {
                Status = value;
                if(StatusLabel.InvokeRequired)
                {
                    var StatusUpdate = 
                        new UpdateLabel(UpdateStatus);
                    Invoke(StatusUpdate);
                }
                else
                {
                    UpdateStatus();
                }
            }
        }

        public Image SetBackgroundImage
        {
            get { return BGImage; }
            set
            {
                BGImage = value;
                if (value != null)
                {
                    BackgroundImage = BGImage;
                    ClientSize = BackgroundImage.Size;
                }
            }
        }

        public Color SetTransparentKey
        {
            get { return TransparentKey; }
            set
            {
                TransparentKey = value;
                if (value != Color.Empty)
                    TransparencyKey = SetTransparentKey;
            }
        }

        public Boolean SetFade
        {
            get { return FadeInOut; }
            set
            {
                FadeInOut = value;
                Opacity = value ? .00 : 1.00;
            }
        }

        public static SplashScreen Current
        {
            get
            {
                if (SplashScreenForm == null)
                    SplashScreenForm = new SplashScreen();
                return SplashScreenForm;
            }
        }

        public void SetStatusLabel(Point StatusLabelLocation, 
                Int32 StatusLabelWidth, Int32 StatusLabelHeight)
        {
            if (StatusLabelLocation != Point.Empty)
                StatusLabel.Location = StatusLabelLocation;
            if (StatusLabelWidth == 0 && StatusLabelHeight == 0)
                StatusLabel.AutoSize = true;
            else
            {
                if (StatusLabelWidth > 0)
                    StatusLabel.Width = StatusLabelWidth;
                if (StatusLabelHeight > 0)
                    StatusLabel.Height = StatusLabelHeight;
            }
        }

        public void SetInfoLabel(Point InfoLabelLocation, 
                Int32 InfoLabelWidth, Int32 InfoLabelHeight)
        {
            if (InfoLabelLocation != Point.Empty)
                ProgramInfoLabel.Location = InfoLabelLocation;
            if (InfoLabelWidth == 0 && InfoLabelHeight == 0)
                ProgramInfoLabel.AutoSize = true;
            else
            {
                if (InfoLabelWidth > 0)
                    ProgramInfoLabel.Width = InfoLabelWidth;
                if (InfoLabelHeight > 0)
                    ProgramInfoLabel.Height = InfoLabelHeight;
            }
        }

        public void ShowSplashScreen()
        {
            SplashScreenThread = new Thread(ShowForm) 
                {IsBackground = true, Name = "SplashScreenThread"};
            SplashScreenThread.Start();
        }

        public void CloseSplashScreen()
        {
            if (SplashScreenForm != null)
            {
                if(InvokeRequired)
                {
                    var ClosingDelegate = 
                        new CloseSplash(HideSplash);
                    Invoke(ClosingDelegate);
                }
                else
                {
                    HideSplash();
                }
            }
        }
        #endregion

        public SplashScreen()
        {
            InitializeComponent();
        }

        private static void ShowForm()
        {
            Application.Run(SplashScreenForm);
        }

        private void UpdateStatus()
        {
            StatusLabel.Text = SetStatus;
        }

        private void UpdateInfo()
        {
            ProgramInfoLabel.Text = SetInfo;
        }

        private void SplashTimer_Tick(object sender, EventArgs e)
        {
            if(FadeMode) // Form is opening (Increment)
            {
                if (Opacity < 1.00)
                    Opacity += OpacityIncrement;
                else
                    SplashTimer.Stop();
            }
            else // Form is closing (Decrement)
            {
                if(Opacity > .00)
                    Opacity -= OpacityDecrement;
                else
                    Dispose();
            }
            
        }

        #region InitComponents

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ProgramInfoLabel = new System.Windows.Forms.Label();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.SplashTimer = new System.Windows.Forms.Timer
                            (this.components);
            this.SuspendLayout();
            // 
            // ProgramInfoLabel
            // 
            this.ProgramInfoLabel.BackColor = 
                    System.Drawing.Color.Transparent;
            this.ProgramInfoLabel.Location = 
                    new System.Drawing.Point(56, 52);
            this.ProgramInfoLabel.Name = "ProgramInfoLabel";
            this.ProgramInfoLabel.Size = 
                    new System.Drawing.Size(100, 23);
            this.ProgramInfoLabel.TabIndex = 0;
            // 
            // StatusLabel
            // 
            this.StatusLabel.BackColor = 
                    System.Drawing.Color.Transparent;
            this.StatusLabel.Location = 
                    new System.Drawing.Point(59, 135);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(100, 23);
            this.StatusLabel.TabIndex = 1;
            // 
            // SplashScreen
            // 
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.ProgramInfoLabel);
            this.FormBorderStyle = 
                System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SplashScreen";
            this.ShowInTaskbar = false;
            this.StartPosition = 
                System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);

        }

        #endregion

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            if (SetFade)
            {
                FadeMode = true;
                SplashTimer.Interval = TimerInterval;
                SplashTimer.Start();
            }
        }

        private void HideSplash()
        {
            if(SetFade)
            {
                FadeMode = false;
                SplashTimer.Start();
            }
            else
                Dispose();
        }
    }
}