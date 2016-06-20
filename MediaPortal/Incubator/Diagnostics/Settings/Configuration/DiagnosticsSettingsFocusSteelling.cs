using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UiComponents.Diagnostics.Settings.Configuration
{
    internal class DiagnosticsSettingsFocusSteelling : YesNo
    {

        #region Methods

        public override void Load()
        {
            _yes = Diagnostics.Service.FocusSteelingMonitor.Instance.IsMonitoring;
        }

        public override void Save()
        {
            if (_yes)
            {
                Diagnostics.Service.DiagnosticsHandler.SetLogLevel(log4net.Core.Level.Debug);
                Diagnostics.Service.FocusSteelingMonitor.Instance.SubscribeToMessages();
            }
            else
            {
                Diagnostics.Service.DiagnosticsHandler.SetLogLevel(log4net.Core.Level.Info);
                Diagnostics.Service.FocusSteelingMonitor.Instance.UnsubscribeFromMessages();
            }
        }

        #endregion Methods

    }
}