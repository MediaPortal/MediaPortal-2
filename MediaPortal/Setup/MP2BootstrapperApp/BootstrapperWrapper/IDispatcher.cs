using System;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public interface IDispatcher
  {
    void Run();
    void Invoke(Action action);
    void InvokeShutdown();
  }
}
