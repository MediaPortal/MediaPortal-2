using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;

namespace Test.Backend
{
    class Base
    {
        private static bool DONE = false;

        public static void Setup()
        {
            if(DONE) {
                return;
            }

            ServiceRegistration.Set<IPathManager>(new PathManager());
            ServiceRegistration.Get<IPathManager>().SetPath("LOG", ".");
            ServiceRegistration.Get<IPathManager>().SetPath(@"CONFIG", ".");
            ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));

            DONE = true;
        }
    }
}
