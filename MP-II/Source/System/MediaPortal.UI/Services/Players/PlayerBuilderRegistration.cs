using System.Collections.Generic;
using MediaPortal.Presentation.Players;

namespace MediaPortal.Services.Players
{
    /// <summary>
    /// Registration structure holding data about a registered player builder and its
    /// built players.
    /// </summary>
    /// <remarks>
    /// A player builder is a plugin resource and can be seen as a sort of parent resource for all
    /// of its built players. To be able to revoke the player builder usage, we need to track
    /// all of its players. So when building a player from the underlaying player builder,
    /// we mark its slot index in the <see cref="UsingSlots"/> array.
    /// </remarks>
    internal class PlayerBuilderRegistration
    {
      protected IPlayerBuilder _builder = null;
      protected IList<IPlayerSlotController> _usingSlots = new List<IPlayerSlotController>(2);
      protected bool _suspended = false;

      public PlayerBuilderRegistration(IPlayerBuilder builder)
      {
        _builder = builder;
      }

      public IPlayerBuilder PlayerBuilder
      {
        get { return _builder; }
      }

      /*
       * Managed by the PlayerSlotController.
       */
      public IList<IPlayerSlotController> UsingSlots
      {
        get { return _usingSlots; }
      }

      public bool IsInUse
      {
        get { return _usingSlots.Count > 0; }
      }

      public bool Suspended
      {
        get { return _suspended; }
        set { _suspended = value; }
      }
    }
}