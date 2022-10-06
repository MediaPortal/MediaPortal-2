namespace MediaPortal2.Alexa.Configuration
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Windows.Forms;

    public class LoginForm : Form
    {
        private WebBrowser mBrowser = new WebBrowser();
        private ManualResetEvent mStart;

        public event WebBrowserProxy.CompletedCallback Completed;

        public LoginForm(ManualResetEvent start)
        {
            base.Width = 480;
            base.Height = 720;
            base.StartPosition = FormStartPosition.CenterScreen;
            base.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.Text = "Alexa Plugin Account Linking";
            base.ShowInTaskbar = false;
            this.mBrowser.Dock = DockStyle.Fill;
            base.Controls.Add(this.mBrowser);
            this.mBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(this.mBrowser_DocumentCompleted);
            this.mStart = start;
        }

        private void mBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this.Completed(this.mBrowser);
        }

        public void Navigate(Uri url)
        {
            base.BeginInvoke(() => this.mBrowser.Navigate(url));
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!base.IsHandleCreated)
            {
                this.CreateHandle();
                base.BeginInvoke(() => this.mStart.Set());
            }
            base.SetVisibleCore(value);
        }

        public void Terminate()
        {
            base.Invoke(() => base.Close());
        }
    }
}

