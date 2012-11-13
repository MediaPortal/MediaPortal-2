using System;
using MediaPortal.Common;
using MediaPortal.Common.PluginManager;

namespace MediaPortal.UiComponents.News
{
	public class NewsProvider : IPluginStateTracker
	{
		public void Activated(PluginRuntime pluginRuntime)
		{
			ServiceRegistration.Set<INewsCollector>(new NewsCollector());
		}

		public bool RequestEnd()
		{
      return true;
		}

		public void Stop()
		{
      ServiceRegistration.RemoveAndDispose<INewsCollector>();
		}

		public void Continue() { }

		public void Shutdown() { }
	}
}
