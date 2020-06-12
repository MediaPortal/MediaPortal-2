using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Game
{
  public interface IGameLauncher
  {
    void LaunchGame(MediaItem mediaItem);
  }
}
