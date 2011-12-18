using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPortal.UI.Presentation.Players
{
  public interface ITitlePlayer
  {
    /// <summary>
    /// Gets an ordered list of localized titles/editions.
    /// </summary>
    string[] Titles { get; }

    /// <summary>
    /// Plays the given title/edition.
    /// </summary>
    /// <param name="title">The name of the title/edition to set. Must be one of the title/edition names from the
    /// <see cref="DvdTitles"/> list.</param>
    void SetTitle(string title);

    /// <summary>
    /// Gets the current title/edition.
    /// </summary>
    string CurrentTitle { get; }
  }
}
