using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine
{
  public class Utils
  {

    Property _currentTimeProperty = new Property(DateTime.Now.ToShortTimeString());
    Property _hourAngleProperty = new Property(0.0);
    Property _minuteAngleProperty = new Property(0.0);

    public Property CurrentTimeProperty
    {
      get
      {
        return _currentTimeProperty;
      }
      set
      {
        _currentTimeProperty = value;
      }
    }

    public string CurrentTime
    {
      get
      {
        return _currentTimeProperty.GetValue() as string;
      }
      set
      {
        _currentTimeProperty.SetValue(value);
      }
    }


    public Property HourAngleProperty
    {
      get
      {
        return _hourAngleProperty;
      }
      set
      {
        _hourAngleProperty = value;
      }
    }

    public double HourAngle
    {
      get
      {
        return (double)_hourAngleProperty.GetValue();
      }
      set
      {
        _hourAngleProperty.SetValue(value);
      }
    }
    public Property MinuteAngleProperty
    {
      get
      {
        return _minuteAngleProperty;
      }
      set
      {
        _minuteAngleProperty = value;
      }
    }

    public double MinuteAngle
    {
      get
      {
        return (double)_minuteAngleProperty.GetValue();
      }
      set
      {
        _minuteAngleProperty.SetValue(value);
      }
    }


    public void Update()
    {
      CurrentTime = DateTime.Now.ToShortTimeString();
      double Angle = DateTime.Now.Hour * 30;
      HourAngle = (double)(Angle + 12 * DateTime.Now.Minute / 60);

      MinuteAngle = (double)(DateTime.Now.Minute * 6);
    }
  }
}
