
using System;
using MediaPortal.Presentation.DataObjects;

namespace Presentation.SkinEngine
{
  public class TimeUtils
  {

    Property _currentTimeProperty = new Property(typeof(string), DateTime.Now.ToShortTimeString());
    Property _hourAngleProperty = new Property(typeof(double), 0.0);
    Property _minuteAngleProperty = new Property(typeof(double), 0.0);

    public TimeUtils()
    {

    }
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
