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
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The "Today" band's chart of the next hours: temperature as a red line and rain chance as a light blue
  /// one above, the millimetres as light blue bars below, every hour standing in a column of its own with
  /// vertical hairlines between the columns and nothing else - no horizontal grid, no dimmed past, as the
  /// design review settled it. The hours' own names and pictures stand in the row above, in XAML; this
  /// control draws only what a line drawing is.
  /// </summary>
  /// <remarks>
  /// The values are put where they fall out of the control's own bounds, so the chart is the same chart at
  /// any size the wall gives it; the labels are drawn at the sizes the review doubled to, because a viewer
  /// reads this from across a room. Where the two lines cross, the percent label steps below the
  /// temperature label rather than over it - the one collision the columns were measured with.
  /// </remarks>
  public class WeatherChartControl : Control
  {
    public static readonly StyledProperty<IReadOnlyList<WeatherSlot>?> SlotsProperty =
        AvaloniaProperty.Register<WeatherChartControl, IReadOnlyList<WeatherSlot>?>(nameof(Slots));

    private static readonly IBrush TEMP_BRUSH = new SolidColorBrush(Color.Parse("#ff6a55"));
    private static readonly IBrush CHANCE_BRUSH = new SolidColorBrush(Color.Parse("#7fd0f7"));
    private static readonly IBrush LABEL_BLUE = new SolidColorBrush(Color.Parse("#bfe8ff"));
    private static readonly IPen HAIRLINE = new Pen(new SolidColorBrush(Color.Parse("#17FFFFFF")), 1);
    private static readonly Pen TEMP_PEN = new Pen(TEMP_BRUSH, 6);
    private static readonly Pen CHANCE_PEN = new Pen(CHANCE_BRUSH, 5);

    static WeatherChartControl()
    {
      AffectsRender<WeatherChartControl>(SlotsProperty);
    }

    public IReadOnlyList<WeatherSlot>? Slots
    {
      get => GetValue(SlotsProperty);
      set => SetValue(SlotsProperty, value);
    }

    public override void Render(DrawingContext context)
    {
      IReadOnlyList<WeatherSlot>? hours = Slots;
      double width = Bounds.Width;
      double height = Bounds.Height;
      if (hours == null || hours.Count < 2 || width < 80 || height < 120)
        return;

      int count = hours.Count;
      double column = width / count;

      // The bands the control spends its height on: the lines up top, the bars at the bottom, and room
      // under the bars for their millimetres. Nothing between them but the hairlines' run.
      double chartTop = 44;
      double barBot = height - 34;
      double barTop = height - 150;
      double chartBot = barTop - 74;
      double maxMm = 0;
      double minTemp = double.MaxValue;
      double maxTemp = double.MinValue;
      foreach (WeatherSlot hour in hours)
      {
        maxMm = Math.Max(maxMm, hour.PrecipMm);
        minTemp = Math.Min(minTemp, hour.Temp);
        maxTemp = Math.Max(maxTemp, hour.Temp);
      }

      double tempSpan = Math.Max(maxTemp - minTemp, 1);
      minTemp -= tempSpan * 0.18;
      maxTemp += tempSpan * 0.18;
      double mmMax = maxMm <= 0 ? 1 : maxMm;

      for (int line = 1; line < count; line++)
        context.DrawLine(HAIRLINE, new Point(line * column, 0), new Point(line * column, barBot));

      for (int index = 0; index < count; index++)
      {
        double mmHeight = hours[index].PrecipMm / mmMax * (barBot - barTop - 6);
        if (mmHeight <= 0)
          continue;
        double barWidth = Math.Min(64, column * 0.55);
        context.DrawRectangle(CHANCE_BRUSH, null,
            new Rect(Center(index) - barWidth / 2, barBot - mmHeight, barWidth, mmHeight), 6, 6);
      }

      Point[] temperatures = new Point[count];
      Point[] chances = new Point[count];
      for (int index = 0; index < count; index++)
      {
        temperatures[index] = new Point(Center(index), TempY(hours[index].Temp));
        chances[index] = new Point(Center(index), ChanceY(Math.Min(hours[index].PrecipChance, 1)));
      }

      for (int index = 1; index < count; index++)
        context.DrawLine(TEMP_PEN, temperatures[index - 1], temperatures[index]);
      for (int index = 1; index < count; index++)
        context.DrawLine(CHANCE_PEN, chances[index - 1], chances[index]);

      NumberFormatInfo numbers = CultureInfo.CurrentCulture.NumberFormat;
      for (int index = 0; index < count; index++)
      {
        WeatherSlot hour = hours[index];

        context.DrawEllipse(TEMP_BRUSH, null, temperatures[index], 9, 9);
        Draw(hour.Temp.ToString("0", numbers) + "\u00B0", TEMP_BRUSH, 32,
            new Point(Center(index), temperatures[index].Y - 48), true, FontWeight.SemiBold);

        context.DrawEllipse(CHANCE_BRUSH, null, chances[index], 7, 7);
        double labelTop = Math.Max(chances[index].Y + 14, temperatures[index].Y + 10);
        Draw(Math.Round(hour.PrecipChance * 100).ToString("0", numbers) + " %", LABEL_BLUE, 26,
            new Point(Center(index), labelTop), true);

        Draw(hour.PrecipMm.ToString("0.#", numbers) + " mm", LABEL_BLUE, 24,
            new Point(Center(index), barBot + 4), true);
      }

      double Center(int index) => (index + 0.5) * column;
      double TempY(double temp) =>
          chartBot - (temp - minTemp) / (maxTemp - minTemp) * (chartBot - chartTop);
      double ChanceY(double chance) =>
          chartBot - chance * (chartBot - chartTop - 20) - 10;

      void Draw(string text, IBrush brush, double size, Point anchor, bool centered,
          FontWeight? weight = null)
      {
        FormattedText line = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI", style: FontStyle.Normal, weight: weight ?? FontWeight.Normal),
            size, brush);
        Point at = centered
            ? new Point(anchor.X - line.Width / 2, anchor.Y)
            : anchor;
        context.DrawText(line, at);
      }
    }
  }
}
