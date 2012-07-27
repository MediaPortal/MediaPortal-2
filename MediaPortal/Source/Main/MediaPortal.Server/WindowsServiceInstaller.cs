using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MediaPortal.Server
{
  [RunInstaller(true)]
  public class WindowsServiceInstaller : Installer
  {
    private readonly ServiceInstaller _serviceInstaller;
    private readonly ServiceProcessInstaller _serviceProcessInstaller;

    public WindowsServiceInstaller()
    {
      _serviceInstaller = new ServiceInstaller
                            {
                              ServiceName = "MP2 Server Service", 
                              Description = "Provides MediaPortal2 Server Service for all Clients",
                              StartType = ServiceStartMode.Manual
                            };

      _serviceProcessInstaller = new ServiceProcessInstaller { Account = ServiceAccount.NetworkService };
      
      Installers.Add(_serviceInstaller);
      Installers.Add(_serviceProcessInstaller);
    }
  }
}
