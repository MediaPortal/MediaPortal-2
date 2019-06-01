using System;
using System.Windows.Threading;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public class DispatcherWrapper : IDispatcher
  {
    private readonly Dispatcher _dispatcher;

    public DispatcherWrapper()
    {
      _dispatcher = Dispatcher.CurrentDispatcher;
    }

    public void Run()
    {
      Dispatcher.Run();
    }

    public void Invoke(Action action)
    {
      _dispatcher.Invoke(action);
    }

    public void InvokeShutdown()
    {
      _dispatcher.InvokeShutdown();
    }
  }
}
