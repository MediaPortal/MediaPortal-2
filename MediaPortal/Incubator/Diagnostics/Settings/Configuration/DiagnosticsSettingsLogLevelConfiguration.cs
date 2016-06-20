using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UiComponents.Diagnostics.Settings.Configuration
{
    public class DiagnosticsSettingsLogLevelConfiguration : YesNo
    {

        #region Methods

        public override void Load()
        {
            log4net.Core.Level activeLevel = Diagnostics.Service.DiagnosticsHandler.GetLogLevel();
            if (activeLevel == log4net.Core.Level.Debug)
                _yes = true;
            else
                _yes = false;
        }

        public override void Save()
        {
            if (_yes)
                Diagnostics.Service.DiagnosticsHandler.SetLogLevel(log4net.Core.Level.Debug);
            else
                Diagnostics.Service.DiagnosticsHandler.SetLogLevel(log4net.Core.Level.Info);
        }

        #endregion Methods

    }
}