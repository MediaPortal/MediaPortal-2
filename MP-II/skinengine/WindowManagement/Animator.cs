using System;
using System.Collections.Generic;
using System.Text;
using SkinEngine;
using SkinEngine.Controls;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Animations;
namespace SkinEngine
{
  public class Animator
  {
    List<StoryboardContext> _runningAnimations;

    /// <summary>
    /// Initializes a new instance of the <see cref="Animator"/> class.
    /// </summary>
    public Animator()
    {
      _runningAnimations = new List<StoryboardContext>();
    }

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <param name="board">The board.</param>
    /// <returns></returns>
    StoryboardContext GetContext(Storyboard board)
    {
      foreach (StoryboardContext context in _runningAnimations)
      {
        if (context.Storyboard == board) return context;
      }
      return null;
    }

    /// <summary>
    /// Starts the storyboard.
    /// </summary>
    /// <param name="board">The board.</param>
    public void StartStoryboard(Storyboard board, UIElement element)
    {
      lock (_runningAnimations)
      {

        if (null == GetContext(board))
        {
          StoryboardContext context = new StoryboardContext(board);
          _runningAnimations.Add(context);
          context.Setup(element);
          context.Start(SkinContext.TimePassed);
        }
      }
    }

    /// <summary>
    /// Stops the storyboard.
    /// </summary>
    /// <param name="board">The board.</param>
    public void StopStoryboard(Storyboard board)
    {
      lock (_runningAnimations)
      {
        StoryboardContext context = GetContext(board);
        if (context == null) return;
        context.Stop();
        _runningAnimations.Remove(context);
      }
    }

    /// <summary>
    /// Animates any timelines 
    /// </summary>
    public virtual void Animate()
    {
      if (_runningAnimations.Count == 0) return;
      List<StoryboardContext> stoppedAnimations = new List<StoryboardContext>();
      lock (_runningAnimations)
      {
        foreach (StoryboardContext line in _runningAnimations)
        {
          line.Animate(SkinContext.TimePassed);
          if (line.IsStopped)
            stoppedAnimations.Add(line);
        }
        foreach (StoryboardContext line in stoppedAnimations)
        {
          line.Stop();
          _runningAnimations.Remove(line);
        }
      }
    }

    public void StopAll()
    {
      foreach (StoryboardContext line in _runningAnimations)
      {
        line.Stop();
      }
      _runningAnimations.Clear();
    }
  }
}
