namespace MediaPortal2.Alexa.Configuration
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Windows.Forms;

    public class WebBrowserProxy : IDisposable
    {
        private ManualResetEvent mStart = new ManualResetEvent(false);
        private LoginForm mSyncProvider;

        public event CompletedCallback Completed;

        public WebBrowserProxy()
        {
            Thread thread = new Thread(new ThreadStart(this.startPump));
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            this.mStart.WaitOne();
        }

        public void Dispose()
        {
            this.mSyncProvider.Terminate();
        }

        private void mSyncProvider_Completed(WebBrowser wb)
        {
            CompletedCallback completed = this.Completed;
            if (completed != null)
            {
                completed(wb);
            }
        }

        public void Navigate(Uri url)
        {
            this.mSyncProvider.Navigate(url);
        }

        private void startPump()
        {
            this.mSyncProvider = new LoginForm(this.mStart);
            this.mSyncProvider.Completed += new CompletedCallback(this.mSyncProvider_Completed);
            Application.Run(this.mSyncProvider);
        }

        public delegate void CompletedCallback(WebBrowser wb);
    }
}

