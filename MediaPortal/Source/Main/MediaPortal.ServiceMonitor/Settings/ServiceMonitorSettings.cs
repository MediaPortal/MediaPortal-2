using MediaPortal.Common.Settings;

namespace MediaPortal.ServiceMonitor.Settings
{
  public class ServiceMonitorSettings
  {
    // MainWindow Position
    [Setting(SettingScope.Global, 50)]
    public double Top { get; set; }

    [Setting(SettingScope.Global, 50)]
    public double Left { get; set; }

    [Setting(SettingScope.Global, 450)]
    public double Height { get; set; }

    [Setting(SettingScope.Global, 700)]
    public double Width { get; set; }

    [Setting(SettingScope.Global, false)]
    public bool Maximised { get; set; }
  }
}