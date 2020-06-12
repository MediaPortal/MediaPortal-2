using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpRetro.Info
{
  public class Firmware
  {
    protected string _path;
    protected string _description;
    protected bool _optional;

    public Firmware(string path, string description, bool optional)
    {
      Path = path;
      Description = description;
      Optional = optional;
    }

    public string Path { get; private set; }
    public string Description { get; private set; }
    public bool Optional { get; private set; }
  }
}