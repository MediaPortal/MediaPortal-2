/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.Workflow;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The news screen: the feeds on the left, their headlines in the middle, the story on the right.
  /// </summary>
  /// <remarks>
  /// What only a screen can add: the feeds are read when it opens rather than when somebody presses a button -
  /// a screen called "News" that shows nothing until asked is a screen that says the news is over - the two
  /// lists walk as one ring with Enter stepping from a paper to its headlines, and Back leaves the news the way
  /// it was found.
  /// </remarks>
  public partial class NewsView : UserControl, IWorkflowScreen
  {
    private readonly NewsViewModel _news;
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with nowhere to read from shows.
    /// </summary>
    public NewsView()
        : this(new NewsViewModel())
    {
    }

    public NewsView(NewsViewModel news)
    {
      DataContext = news;
      _news = news;
      InitializeComponent();

      // In the order they are drawn: the papers, their headlines. The story panel is read, not walked; there is
      // nothing on it to press.
      _ring.Add(feedsList);
      _ring.Add(headlinesList);
      DpadNavigation.Attach(this, _ring);

      // A double-click on a paper means the Enter that walks to its headlines; a double-click on a headline is
      // nobody's action today, exactly as the key is not either, but the list is where the pointer lands.
      MouseRemote.Attach(this, feedsList, headlinesList);



      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
                        if (args.Key != Key.Enter && args.Key != Key.Return)
              return;

            if (feedsList.IsKeyboardFocusWithin)
            {
              // Enter on a paper is the wish to read it, which is the headlines column across.
              if (headlinesList.Focus())
                args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);
    }
    /// <summary>
    /// Runs every time the workflow shows this screen: the first showing loads, every showing takes
    /// the focus.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenActivated()
    {

      // The same command the button-less screen would have; opening and pressing cannot drift apart.
      _news.RefreshCommand.Execute(null);

      // The papers take the focus so the first Enter of the visit means "read this paper" rather than
      // nothing at all; see HomeWindow for the general reason.
      feedsList.Focus();
    }

    /// <summary>
    /// Runs once, when the frame leaves the workflow for good.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenClosed()
    {

      _news.Dispose();
    }

  }
}
