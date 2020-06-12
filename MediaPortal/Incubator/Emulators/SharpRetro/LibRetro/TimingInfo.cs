using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.LibRetro
{
  public class TimingInfo
  {
    public TimingInfo(double fps, double sampleRate)
    {
      FPS = fps;
      SampleRate = sampleRate;
    }

    public double FPS { get; set; }
    public double SampleRate { get; set; }
  }
}
