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
using System.Collections.Generic;
using System.Globalization;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>What one run of the skip-step ladder decided to do with the film.</summary>
  public enum SkipStepAction
  {
    /// <summary>Nothing was pending, or the pending step was the ladder's "don't skip" rung.</summary>
    Nothing,

    /// <summary>Move by the pending step.</summary>
    Skip,

    /// <summary>The requested step ran off the beginning; go to the very start.</summary>
    ToStart,

    /// <summary>The requested step ran off the end; go to the very end.</summary>
    ToEnd
  }

  /// <summary>
  /// The same escalating skip the reference video player puts behind its left and right keys
  /// (<c>ExtendedVideoSkip</c>): one press waits a moment and jumps a small step, presses held one after
  /// the other climb a ladder of bigger and bigger steps, and the jump only happens once the pressing
  /// stops - so a viewer tapping right four times ends up four minutes along rather than two, without
  /// the film stuttering four times in between.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The state machine is carried over rung for rung from the reference: the step list gains a "don't
  /// skip" rung when it has none and is sorted; a press in the same direction climbs one rung while the
  /// last step was still inside the media; a press in the opposite direction steps one rung back, and
  /// when the ladder is already at its bottom it turns around and takes the first real step the other
  /// way. A step that would leave the media is refused rather than climbed, and the ladder remembers it
  /// is refused - every further press stays parked at the border until the timer finally throws the
  /// film to that border, which is how the reference answers "skip forward" from two minutes before
  /// the end.
  /// </para>
  /// <para>
  /// Nothing here waits or seeks: a press hands back what the screen should show, and <see cref="Elapsed"/>
  /// hands back what to do when the pause between presses ran out. Whoever drives this owns the clock.
  /// </para>
  /// </remarks>
  public sealed class SkipStepLadder
  {
    private readonly List<int> _steps;
    private int _index;
    private int _direction = 1;
    private bool _valid = true;

    /// <summary>
    /// Builds the ladder from the reference's comma-separated seconds list ("15,30,60,180,..."); pieces
    /// that are not positive numbers are dropped, a rung of 0 joins when the list has none, and what is
    /// left is sorted - all of this is exactly what the reference does to its setting before using it.
    /// An empty or hopeless list gets the reference default list.
    /// </summary>
    public SkipStepLadder(string? stepList)
    {
      _steps = Parse(stepList);
    }

    /// <summary>The reference's own ladder, used when nobody configured one.</summary>
    public const string DEFAULT_LIST = "15,30,60,180,300,600,900,1800,3600,7200";

    /// <summary>Which way the ladder is currently pointing: 1 forwards, -1 backwards.</summary>
    public int Direction
    {
      get { return _direction; }
    }

    /// <summary>
    /// The step the last press landed on, signed: positive jumps forwards, negative backwards, and zero
    /// is the ladder's "don't skip" rung - a press that turned around without jumping anywhere.
    /// </summary>
    public int PendingStepSeconds
    {
      get { return _direction * _steps[_index]; }
    }

    /// <summary>
    /// Whether the pending step is inside the media. A screen shows its border message instead of a
    /// number while this is false, and waits for <see cref="Elapsed"/> to throw the film to that border.
    /// </summary>
    public bool PendingIsValid
    {
      get { return _valid; }
    }

    /// <summary>
    /// One press of a skip key in direction <paramref name="direction"/> (1 forwards, -1 backwards).
    /// <paramref name="canSkipRelative"/> answers whether a jump of the given signed seconds fits inside
    /// the media, which is the same question the reference asks its player. The return says what the
    /// screen should show for this press.
    /// </summary>
    public SkipStepAction Press(int direction, Func<int, bool> canSkipRelative)
    {
      if (direction >= 0 && _direction < 0 || direction < 0 && _direction > 0)
      {
        // The other way round: step one rung back if we are up the ladder, otherwise turn around and
        // take the first real rung pointing the new way, which is what the reference does.
        if (_index > 0)
          _index--;
        else
        {
          _direction = direction == 0 ? 1 : direction;
          _index = _steps.Count > 1 ? 1 : 0;
        }
      }
      else if (_valid && _index < _steps.Count - 1)
      {
        _index++;
      }

      int step = _direction * _steps[_index];
      _valid = canSkipRelative(step);
      if (!_valid)
        return _direction < 0 ? SkipStepAction.ToStart : SkipStepAction.ToEnd;
      return step == 0 ? SkipStepAction.Nothing : SkipStepAction.Skip;
    }

    /// <summary>
    /// The pause between presses ran out: do the pending jump, or the jump to the border a refused step
    /// was parked at. The ladder falls back to its bottom rung afterwards but keeps pointing the way it
    /// was pointing, so the next press starts climbing again from the smallest step - the reference's
    /// answer to "forward, forward, forward" arriving as two runs of the ladder rather than one.
    /// </summary>
    public SkipStepAction Elapsed()
    {
      SkipStepAction answer;
      if (!_valid)
        answer = _direction < 0 ? SkipStepAction.ToStart : SkipStepAction.ToEnd;
      else
        answer = _direction * _steps[_index] == 0 ? SkipStepAction.Nothing : SkipStepAction.Skip;

      _index = 0;
      _valid = true;
      return answer;
    }

    /// <summary>
    /// Forget a pending jump without doing it - the reference drops the timer the same way when an
    /// instant skip or new media interrupts the ladder.
    /// </summary>
    public void Cancel()
    {
      _index = 0;
      _valid = true;
    }

    private static List<int> Parse(string? stepList)
    {
      List<int> steps = new List<int>();
      if (stepList != null)
      {
        foreach (string piece in stepList.Split(','))
        {
          if (int.TryParse(piece.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) &&
              seconds > 0)
          {
            steps.Add(seconds);
          }
        }
      }
      if (steps.Count == 0)
        steps.AddRange(Parse(DEFAULT_LIST));
      if (!steps.Contains(0))
        steps.Add(0);
      steps.Sort();
      return steps;
    }
  }
}
