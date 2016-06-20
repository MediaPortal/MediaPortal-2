using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UiComponents.Diagnostics.Settings.Configuration
{
    internal class DiagnosticsSettingsCollectLog : YesNo
    {
        #region Methods

        public override void Load()
        {
            _yes = false;
        }

        public override void Save()
        {
            if (_yes)
            {
                string collectorPath = System.Environment.CurrentDirectory;
                collectorPath = System.IO.Path.Combine(System.IO.Directory.GetParent(collectorPath).FullName, "MP2-LogCollector\\MP2-LogCollector.exe");
                if (System.IO.File.Exists(collectorPath))
                {
                    System.Diagnostics.Process.Start(collectorPath);
                }
            }
        }

        #endregion Methods
    }
}