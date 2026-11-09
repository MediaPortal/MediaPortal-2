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

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What the machine has to say about a trailer that was asked for, on a dialog of its own.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A trailer is not answered on the spot. A film at a video site stands behind a page that has to be asked, and a
  /// big picture sometimes has to be brought over to this machine first, which costs up to a few minutes. Then the
  /// answer can as easily be a refusal as a film, and a refusal has a reason that is a whole sentence long. All of
  /// that belongs on a dialog the trailer owns, where a sentence stands in full: the shelf's own question - what
  /// shall be done with this title, which trailer, which picture size - is a different thing, and an answer written
  /// into it is a sentence in a place built for one-word lines.
  /// </para>
  /// <para>
  /// One dialog serves the whole errand: the wait, then either the going-away or the reason. The viewer is never
  /// left with a box that says nothing, and never with two boxes about the same trailer.
  /// </para>
  /// <para>
  /// Its way out is the frame's own answer row, like every other dialog's: the data only says the word and what the
  /// word does (<see cref="IDialogAnswers"/>), and the skin dresses the button.
  /// </para>
  /// </remarks>
  public sealed partial class TrailerNoticeModel : ObservableObject, IDialogAnswers
  {
    /// <summary>The reason the dialog has - a refusal, or the words the engine gave. Empty while the work goes on.</summary>
    [ObservableProperty]
    private string _text = "";

    /// <summary>
    /// Whether the machine is still working on the film. While it is, the dialog shows the one word "Loading …" and
    /// the moving bar and nothing else: what the machine is doing behind a page at a video site is nothing a viewer
    /// can read off a television from across the room, and a sentence about it is a sentence in the way.
    /// </summary>
    [ObservableProperty]
    private bool _waiting = true;

    public TrailerNoticeModel()
    {
      DialogOkCommand = new RelayCommand(delegate { Finish(); });
    }

    /// <summary>
    /// Said when the dialog has nothing left to say, so that whoever raised it takes it away - which happens both
    /// when the trailer arrives and when the viewer presses the frame's answer.
    /// </summary>
    public event Action? Finished;

    /// <summary>The heading over the frame, in the word the reference used for what this is.</summary>
    public string Heading
    {
      get { return "Trailer"; }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The word stands from the first moment, although the first thing the dialog says is a wait: a dialog the
    /// viewer cannot leave is a dialog they are held in, and a wait is nothing they agreed to. Pressing it takes the
    /// box away while the errand goes on - an answer that arrives afterwards raises its own box again.
    /// </remarks>
    public string DialogOkWord
    {
      get { return "OK"; }
    }

    /// <inheritdoc/>
    public ICommand? DialogOkCommand { get; }

    /// <inheritdoc/>
    public string DialogCancelWord
    {
      get { return ""; }
    }

    /// <inheritdoc/>
    public ICommand? DialogCancelCommand
    {
      get { return null; }
    }

    /// <summary>
    /// Puts the dialog into its waiting shape: the one word and the moving bar, no sentence. The errand may still
    /// end in a refusal minutes later, which arrives on the same dialog through <see cref="Say"/>.
    /// </summary>
    public void Wait()
    {
      Text = "";
      Waiting = true;
    }

    /// <summary>
    /// Puts an answer on the dialog - the reason nothing is coming - and takes the waiting away, which is the point
    /// at which there is something to have read.
    /// </summary>
    public void Say(string words)
    {
      Waiting = false;
      Text = words;
    }

    /// <summary>The viewer has read it, or the trailer arrived: the dialog is done.</summary>
    public void Finish()
    {
      Finished?.Invoke();
    }
  }
}
