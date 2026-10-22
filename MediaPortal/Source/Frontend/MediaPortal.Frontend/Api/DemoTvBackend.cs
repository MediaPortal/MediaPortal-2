#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// A television backend that is not there: the same questions <see cref="TvHeadendApiClient"/> asks of a
  /// TVHeadend are answered here, out of channels and programmes this front end makes up.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Why this exists at all: an empty guide is the correct answer to "where is the television" when nobody has
  /// written an address down, and it is also the answer that tells a viewer nothing about what the screen would
  /// do if there were one. Nothing here is a second code path above the client - the invented backend sits
  /// <i>underneath</i> it, answering the backend's own JSON at the backend's own addresses - so a demo guide is
  /// built by the same parsing, the same paging, the same window arithmetic and the same screens as a real one.
  /// That is also what makes it useful as a fixture: a guide that looks wrong in demo mode looks wrong in the
  /// client, not in the TVHeadend.
  /// </para>
  /// <para>
  /// What it answers: the bouquets (<c>api/channeltag/list</c>), the channels (<c>api/channel/grid</c>), the
  /// services with their DVB type bits (<c>api/service/grid</c>), the genre names
  /// (<c>api/epg/content_type/list</c>), the programmes (<c>api/epg/events</c>, with the window, the channel,
  /// the bouquet and the title search honoured and with the paging respected) and the recording tables and their
  /// write endpoints (<c>api/dvr/entry/…</c>). A programme is written the way a real backend writes one: seconds
  /// since 1970, a hexadecimal content type beside a spoken label, a DVB four-bit genre list, and the episode
  /// fields only on the programmes that are episodes.
  /// </para>
  /// <para>
  /// What it does not answer, on purpose: a <b>picture</b>. A channel carries a stream address like every other
  /// channel, and pointing a player at it finds nothing behind it, because this front end is not going to ship a
  /// video file or point a box at somebody else's server to make the demo look finished. Live TV in demo mode
  /// therefore stops with the player's own complaint, which is the honest answer; everything a viewer can look at
  /// without a tuner - the guide, the channel lists, the details strip, the programme search, the recording
  /// timers - is complete.
  /// </para>
  /// <para>
  /// The programmes are generated from the clock rather than stored, walking backwards from whatever window was
  /// asked for, so the demo is full at three in the afternoon in October and full at four in the morning in
  /// March. Recording entries are the one thing that is stored, because a viewer who asks for something to be
  /// recorded has to see it on the timers screen afterwards; they live in memory for as long as the front end
  /// runs, which is the right lifetime for something that was never on a disk.
  /// </para>
  /// </remarks>
  public sealed class DemoTvBackend
  {
    #region How a screen gets one

    /// <summary>
    /// The client a television screen should use: the machine on the settings when one is written down, the
    /// invented backend when a viewer asked for demo data. This is the only place that decides, so that the four
    /// screens cannot disagree about which television they are talking to.
    /// </summary>
    public static TvHeadendApiClient Open(TvBackendSettings settings)
    {
      if (settings == null)
        throw new ArgumentNullException(nameof(settings));

      return settings.DemoData
          ? new TvHeadendApiClient(settings, new HttpClient(new Answers()))
          : new TvHeadendApiClient(settings);
    }

    /// <summary>
    /// What a screen adds to a sentence when it is telling somebody how many programmes it read, so that nobody
    /// mistakes an invented evening for a real one. Deliberately short: it stands at the end of a status line.
    /// </summary>
    public const string NOTICE = "demo data, invented by this front end";

    private DemoTvBackend()
    {
    }

    #endregion

    #region What a programme day is made of

    /// <summary>
    /// One programme of one channel's day: what it is called, what it is, how long it runs, and whether it is an
    /// episode of something. Lengths are counted in half hours because that is the smallest thing a broadcaster
    /// schedules in and the unit the whole generator works in.
    /// </summary>
    private struct Slot
    {
      internal readonly string Title;
      internal readonly string? Subtitle;
      internal readonly string Genre;
      internal readonly int Kind;
      internal readonly int HalfHours;
      internal readonly int Dvb;
      internal readonly bool Serial;
      internal readonly int Age;
      internal readonly int Stars;

      internal Slot(string title, string? subtitle, string genre, int kind, int halfHours, int dvb,
          bool serial = false, int age = 0, int stars = 0)
      {
        Title = title;
        Subtitle = subtitle;
        Genre = genre;
        Kind = kind;
        HalfHours = halfHours;
        Dvb = dvb;
        Serial = serial;
        Age = age;
        Stars = stars;
      }
    }

    /// <summary>One channel: what a viewer calls it, what its programmes are filed under, and its day.</summary>
    private sealed class Station
    {
      internal readonly int Number;
      internal readonly string Name;
      internal readonly string EpgId;
      internal readonly bool Radio;
      internal readonly string[] Tags;
      internal readonly Slot[] Day;

      /// <summary>Position in <see cref="Stations"/>, which is what a programme's id is built from.</summary>
      internal int Index;

      internal Station(int number, string name, bool radio, string[] tags, Slot[] day)
      {
        Number = number;
        Name = name;
        Radio = radio;
        Tags = tags;
        Day = day;
        EpgId = "demo-ch-" + number.ToString("000", CultureInfo.InvariantCulture);
      }

      /// <summary>The service behind the channel. One per channel, which is all a demo needs.</summary>
      internal string ServiceId
      {
        get { return "demo-svc-" + Number.ToString("000", CultureInfo.InvariantCulture); }
      }

      /// <summary>How many half hours this channel's day is, which is what the cycle wraps at.</summary>
      internal int DayLength
      {
        get { return Day.Sum(delegate (Slot slot) { return slot.HalfHours; }); }
      }
    }

    // The content types, written as the backend writes them: a hexadecimal id and a word beside it. The words are
    // the ones TvGenreColors knows, so every colour the skin carries is used somewhere in the demo week.
    private const int KIND_MOVIE = 0x01;
    private const int KIND_NEWS = 0x11;
    private const int KIND_SHOW = 0x13;
    private const int KIND_series = 0x10;
    private const int KIND_DOC = 0x20;
    private const int KIND_CHILDREN = 0x30;
    private const int KIND_SPORT = 0x40;
    private const int KIND_MUSIC = 0x50;
    private const int KIND_OTHER = 0x60;

    private static readonly string[][] CONTENT_TYPES = new[]
    {
      new[] { "0x01", "Movie" },
      new[] { "0x10", "Series" },
      new[] { "0x11", "News" },
      new[] { "0x13", "Show" },
      new[] { "0x20", "Documentary" },
      new[] { "0x30", "Children" },
      new[] { "0x40", "Sport" },
      new[] { "0x50", "Music" },
      new[] { "0x60", "Education" }
    };

    /// <summary>
    /// Sixteen channels: twelve to watch and four to listen to, in the numbers a viewer would say. None of them
    /// exists, which is the point - a demo carrying real broadcasters' names and real programme titles would be
    /// quoting somebody's programme data at a screen that is only there to be looked at.
    /// </summary>
    private static readonly Station[] Stations = BuildStations();

    private static Station[] BuildStations()
    {
      Station[] all = new[]
      {
        new Station(1, "Aurora TV", false, new[] { "demo-tag-all", "demo-tag-fav", "demo-tag-hd" }, new[]
        {
          new Slot("Früh bei Aurora", "Der Morgen mit Musik und Wetter", "Show", KIND_SHOW, 3, 0x02),
          new Slot("Wetter heute", "Der Tag, Stunde für Stunde", "News", KIND_NEWS, 1, 0x04),
          new Slot("Küchenparty", null, "Show", KIND_SHOW, 2, 0x03),
          new Slot("Aurora Krankenhaus", "Folge mit neuer Fallgeschichte", "Series", KIND_series, 2, 0x06, true),
          new Slot("Frage des Tages", null, "Show", KIND_SHOW, 2, 0x02),
          new Slot("Büro und Leben", "Neue Folge, neue Panne", "Series", KIND_series, 2, 0x06, true),
          new Slot("Aurora Abendnachrichten", "Das Wichtigste des Tages", "News", KIND_NEWS, 1, 0x01),
          new Slot("Krimi von Nord", "Staffel und Folge zählen mit", "Series", KIND_series, 2, 0x06, true),
          new Slot("Der große Abendfilm", "Ein Film aus dem Archiv", "Movie", KIND_MOVIE, 3, 0x06, false, 12, 4),
          new Slot("Spätgespräch", "Gäste, Fragen, lange Antworten", "Show", KIND_SHOW, 3, 0x02),
          new Slot("Aurora Nachtprogramm", "Musik ohne Moderation", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Früh bei Aurora", "Der Morgen mit Musik und Wetter", "Show", KIND_SHOW, 3, 0x02)
        }),

        new Station(2, "Kultur Eins", false, new[] { "demo-tag-all", "demo-tag-hd" }, new[]
        {
          new Slot("Lesestunde", "Ein Buch, eine Stimme", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Morgenkonzert", "Junge Streicher", "Music", KIND_MUSIC, 3, 0x07),
          new Slot("Werkstatt der Künste", null, "Documentary", KIND_DOC, 2, 0x08),
          new Slot("Debatte", "Zwei Meinungen, ein Thema", "Show", KIND_SHOW, 2, 0x04),
          new Slot("Kultur Eins Journal", "Die Kultur des Tages", "News", KIND_NEWS, 1, 0x01),
          new Slot("Theater aus dem Hafen", "Eine Aufnahme von gestern Abend", "Show", KIND_SHOW, 4, 0x0D),
          new Slot("Spielfilm: Der lange Weg", "Ein Klassiker, restauriert", "Movie", KIND_MOVIE, 3, 0x06, false, 6, 5),
          new Slot("Nachtgespräch", "Lesung und Frage", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Kammermusik", null, "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Archiv: Große Bauten", "Wie eine Halle entstand", "Documentary", KIND_DOC, 2, 0x0A)
        }),

        new Station(3, "Nordlicht", false, new[] { "demo-tag-all", "demo-tag-fav" }, new[]
        {
          new Slot("Tagesschau vom Nordlicht", "Die Nachrichten des Nordens", "News", KIND_NEWS, 1, 0x01),
          new Slot("Hafenkasse", "Geschichten vom Wasser", "Series", KIND_series, 2, 0x06, true),
          new Slot("Fahrrad und Feldweg", "Eine Radtour, sechs Stationen", "Documentary", KIND_DOC, 2, 0x05),
          new Slot("Markt am Morgen", "Preise, Menschen, Streit", "Show", KIND_SHOW, 2, 0x03),
          new Slot("Küche mit Seeblick", "Ein Gericht, drei Versuche", "Show", KIND_SHOW, 2, 0x03),
          new Slot("Nordlicht Doku: Sturm über Helgoland", null, "Documentary", KIND_DOC, 3, 0x04),
          new Slot("Vorabendserie: Familienbande", "Neue Folge, alter Streit", "Series", KIND_series, 2, 0x06, true),
          new Slot("Nordlicht Aktuell", "Das Magazin", "News", KIND_NEWS, 1, 0x01),
          new Slot("Sonntagsfilm: Die Brücke", "Ein Film über ein Dorf", "Movie", KIND_MOVIE, 3, 0x06, false, 12, 3),
          new Slot("Nachtlicht", "Lesung, Wetter, Musik", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Hafenkasse", "Geschichten vom Wasser", "Series", KIND_series, 2, 0x06, true)
        }),

        new Station(4, "Planet HD", false, new[] { "demo-tag-all", "demo-tag-hd" }, new[]
        {
          new Slot("Sonnenaufgang über Wüsten", null, "Documentary", KIND_DOC, 3, 0x0A),
          new Slot("Tiere der Dämmerung", "Was nachts jagt", "Documentary", KIND_DOC, 2, 0x0A),
          new Slot("Maschinen der Welt", "Eine Fabrik von innen", "Documentary", KIND_DOC, 2, 0x0C),
          new Slot("Planet Wissen: Vulkane", "Ein Berg, viele Fragen", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Reise per Nachtschicht", "Zugfahrt durch drei Länder", "Documentary", KIND_DOC, 3, 0x04),
          new Slot("Planet Nachrichten", "Kurz und weltweit", "News", KIND_NEWS, 1, 0x01),
          new Slot("Spielfilm: Rand der Nacht", "Weltraum, sehr nah dran", "Movie", KIND_MOVIE, 3, 0x06, false, 16, 4),
          new Slot("Unter Wasser in drei Tiefen", null, "Documentary", KIND_DOC, 3, 0x0A),
          new Slot("Baustelle Zukunft", "Eine Stadt plant um", "Documentary", KIND_DOC, 2, 0x0C),
          new Slot("Nacht der Natur", "Töne ohne Kommentar", "Music", KIND_MUSIC, 4, 0x0A)
        }),

        new Station(5, "Krimi Eins", false, new[] { "demo-tag-all", "demo-tag-fav" }, new[]
        {
          new Slot("Frühstreife", "Ein Fall vor dem Frühstück", "Series", KIND_series, 2, 0x06, true),
          new Slot("Kommissariat West", "Neue Folge, neues Alibi", "Series", KIND_series, 2, 0x06, true),
          new Slot("Akte für Akte", "Ein Fall, neu aufgerollt", "Documentary", KIND_DOC, 2, 0x04),
          new Slot("Spurensicherung", "Labor, Gericht, Zweifel", "Series", KIND_series, 2, 0x06, true),
          new Slot("Krimi Eins Nachrichten", "Kurze Meldung", "News", KIND_NEWS, 1, 0x01),
          new Slot("Vorabend: Nachtschicht Klinik", "Folge mit Neuankömmling", "Series", KIND_series, 2, 0x06, true),
          new Slot("Abendfilm: Der Zeuge", "Ein Prozess, viele Versionen", "Movie", KIND_MOVIE, 3, 0x06, false, 12, 4),
          new Slot("Kommissariat West", "Neue Folge, neues Alibi", "Series", KIND_series, 2, 0x06, true),
          new Slot("Späte Befragung", "Ein Geständnis, zwei Deutungen", "Series", KIND_series, 2, 0x06, true),
          new Slot("Nachtarchiv", "Alte Fälle, kurze Fassungen", "Education", KIND_OTHER, 5, 0x04),
          new Slot("Frühstreife", "Ein Fall vor dem Frühstück", "Series", KIND_series, 2, 0x06, true)
        }),

        new Station(6, "Sport 14", false, new[] { "demo-tag-all", "demo-tag-hd" }, new[]
        {
          new Slot("Morgenpresse", "Sport, Tabellen, Stimmen", "News", KIND_NEWS, 2, 0x05),
          new Slot("Training live", "Eine Einheit, zwei Vereine", "Sport", KIND_SPORT, 3, 0x05),
          new Slot("Radsport aus den Alpen", "Eine Etappe, lang erzählt", "Sport", KIND_SPORT, 4, 0x05),
          new Slot("Sport 14 Aktuell", "Die Meldung zur Halbzeit", "News", KIND_NEWS, 1, 0x05),
          new Slot("Talk im Stadion", "Drei Gäste, ein Verein", "Show", KIND_SHOW, 2, 0x05),
          new Slot("Abendübertragung: Pokal", "Zwei Mannschaften, ein Platz", "Sport", KIND_SPORT, 4, 0x05),
          new Slot("Analyse danach", "Zweimal Zeitlupe", "Sport", KIND_SPORT, 2, 0x05),
          new Slot("Nacht der Rekorde", "Elf Jahre Wettkampf", "Documentary", KIND_DOC, 3, 0x05),
          new Slot("Morgenpresse", "Sport, Tabellen, Stimmen", "News", KIND_NEWS, 2, 0x05),
          new Slot("Training live", "Eine Einheit, zwei Vereine", "Sport", KIND_SPORT, 3, 0x05)
        }),

        new Station(7, "Kinderstern", false, new[] { "demo-tag-all" }, new[]
        {
          new Slot("Guten Morgen, kleiner Stern", "Lieder zum Aufwachen", "Children", KIND_CHILDREN, 2, 0x0F),
          new Slot("Pippa und der Leuchtturm", "Folge mit neuem Fund", "Children", KIND_CHILDREN, 1, 0x0F, true),
          new Slot("Bauwagen Bande", "Folge mit einem Plan", "Children", KIND_CHILDREN, 1, 0x0F, true),
          new Slot("Frag den Stern", "Kinderfragen, Antworten", "Education", KIND_OTHER, 1, 0x09),
          new Slot("Kino für Kleine", "Ein Film in Kurzfilmen", "Children", KIND_CHILDREN, 2, 0x0F),
          new Slot("Nachmittagskiste", "Basteln, backen, streiten", "Children", KIND_CHILDREN, 2, 0x0F),
          new Slot("Pippa und der Leuchtturm", "Folge mit neuem Fund", "Children", KIND_CHILDREN, 1, 0x0F, true),
          new Slot("Abenteuergeschichte", "Ein Kapitel, dann Pause", "Children", KIND_CHILDREN, 1, 0x0F),
          new Slot("Gute Nacht, kleiner Stern", "Zwei Lieder und ein Traum", "Children", KIND_CHILDREN, 2, 0x0F),
          new Slot("Sternenhimmel für Kinder", "Was über dem Bett steht", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Wiederholung am Morgen", "Folgen von gestern", "Children", KIND_CHILDREN, 6, 0x0F),
          new Slot("Musik ohne Worte", "Töne zum Einschlafen", "Music", KIND_MUSIC, 7, 0x07)
        }),

        new Station(8, "Musik Kanone", false, new[] { "demo-tag-all" }, new[]
        {
          new Slot("Frühschicht", "Lieder, die niemand bestellt hat", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Die Charts von früher", "Zwanzig Lieder, ein Jahr", "Music", KIND_MUSIC, 3, 0x07),
          new Slot("Konserve vom Konzert", "Eine Band, eine Halle", "Music", KIND_MUSIC, 3, 0x07),
          new Slot("Kanone Nachrichten", "Neue Platten, kurze Meldungen", "News", KIND_NEWS, 1, 0x07),
          new Slot("Vorstellung: Neue Platte", "Ein Album, vier Lieder", "Music", KIND_MUSIC, 2, 0x07),
          new Slot("Abendkonzert", "Aufgenommen im Theater", "Music", KIND_MUSIC, 3, 0x07),
          new Slot("Gitarrenabend", "Sechs Saiten, zwei Stunden", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Nachtliste", "Lieder in zufälliger Reihenfolge", "Music", KIND_MUSIC, 8, 0x07)
        }),

        new Station(9, "Dokumentativ", false, new[] { "demo-tag-all" }, new[]
        {
          new Slot("Frühe Doku: Die Werkbahn", "Eine Kleinbahn, viel Geschichte", "Documentary", KIND_DOC, 2, 0x0A),
          new Slot("Beruf: Nachtwache", "Ein Haus, acht Stunden", "Documentary", KIND_DOC, 2, 0x04),
          new Slot("Wissen gemacht", "Wie eine Zahl entsteht", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Große Bauten", "Eine Brücke, vier Jahre", "Documentary", KIND_DOC, 3, 0x0C),
          new Slot("Dokumentativ Kurz", "Drei kurze Filme", "Documentary", KIND_DOC, 2, 0x08),
          new Slot("Abenddoku: Ernte im Herbst", "Ein Hof, sechs Wochen", "Documentary", KIND_DOC, 3, 0x0A),
          new Slot("Streifzug durchs Archiv", "Aufnahmen von 1970", "Documentary", KIND_DOC, 2, 0x04),
          new Slot("Frage an die Forschung", "Ein Labor, ein Ergebnis", "Education", KIND_OTHER, 2, 0x0C),
          new Slot("Nachtbuch", "Lesung über eine Stadt", "Education", KIND_OTHER, 4, 0x09),
          new Slot("Frühe Doku: Die Werkbahn", "Eine Kleinbahn, viel Geschichte", "Documentary", KIND_DOC, 4, 0x0A)
        }),

        new Station(10, "Markt und Preis", false, new[] { "demo-tag-all" }, new[]
        {
          new Slot("Börsenbeginn", "Der Morgen mit Zahlen", "News", KIND_NEWS, 2, 0x04),
          new Slot("Verbraucherschutz", "Ein Streit, drei Ratgeber", "Show", KIND_SHOW, 2, 0x0B),
          new Slot("Werkstatttest", "Vier Geräte, ein任务", "Documentary", KIND_DOC, 2, 0x0C),
          new Slot("Markt und Preis Journal", "Die Meldung zur Mitte", "News", KIND_NEWS, 1, 0x01),
          new Slot("Immobilien von unten", "Ein Haus, viele Rechnungen", "Documentary", KIND_DOC, 2, 0x0B),
          new Slot("Talkrunde Geld", "Vier Gäste, eine Frage", "Show", KIND_SHOW, 2, 0x0B),
          new Slot("Börsenschluss", "Was der Tag gekostet hat", "News", KIND_NEWS, 1, 0x04),
          new Slot("Sparen oder nicht", "Rechnungen für den Alltag", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Wiederholung am Abend", "Die Sendungen des Tages", "Education", KIND_OTHER, 8, 0x04),
          new Slot("Zahlen der Nacht", "Asien macht auf", "News", KIND_NEWS, 2, 0x04),
          new Slot("Börsenbeginn", "Der Morgen mit Zahlen", "News", KIND_NEWS, 2, 0x04)
        }),

        new Station(11, "Wetter Kanal", false, new[] { "demo-tag-all" }, new[]
        {
          new Slot("Wetter am Morgen", "Der Tag im Detail", "News", KIND_NEWS, 1, 0x04),
          new Slot("Klima erklärt", "Eine Karte, zwei Fragen", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Wetter für Landwirte", "Feld, Wald, Vorhersage", "Documentary", KIND_DOC, 2, 0x0A),
          new Slot("Wetter Kanal Aktuell", "Die Meldung zur Stunde", "News", KIND_NEWS, 1, 0x01),
          new Slot("Extremwetter", "Ein Tag, zehn Messstationen", "Documentary", KIND_DOC, 3, 0x0C),
          new Slot("Wetter am Abend", "Morgen, übermorgen, danach", "News", KIND_NEWS, 1, 0x04),
          new Slot("Blick zum Himmel", "Wolken, erklärt", "Education", KIND_OTHER, 2, 0x09),
          new Slot("Seen und Strömungen", "Was Wasser mit Wind macht", "Documentary", KIND_DOC, 3, 0x0A),
          new Slot("Nachtwerte", "Temperaturen ohne Moderation", "News", KIND_NEWS, 5, 0x04),
          new Slot("Wetter am Morgen", "Der Tag im Detail", "News", KIND_NEWS, 4, 0x04),
          new Slot("Klima erklärt", "Eine Karte, zwei Fragen", "Education", KIND_OTHER, 6, 0x09)
        }),

        new Station(12, "Nachtprogramm", false, new[] { "demo-tag-all" }, new[]
        {
          new Slot("Früher Film", "Ein Film aus den achtzigern", "Movie", KIND_MOVIE, 3, 0x06, false, 6, 3),
          new Slot("Testbild und Ansage", "Ein Programm, das Pausen macht", "Education", KIND_OTHER, 1, 0x0E),
          new Slot("Serie aus dem Archiv", "Folge in schlechter Laune", "Series", KIND_series, 2, 0x06, true),
          new Slot("Konzertmitschnitt", "Eine Band, ein Zelt", "Music", KIND_MUSIC, 3, 0x07),
          new Slot("Nachtgespräch", "Zwei Menschen, ein Sofa", "Show", KIND_SHOW, 2, 0x02),
          new Slot("Film um Zwölf", "Ein Krimi aus dem Archiv", "Movie", KIND_MOVIE, 3, 0x06, false, 16, 3),
          new Slot("Doku über etwas", "Ein Thema, keine Fragen", "Documentary", KIND_DOC, 2, 0x04),
          new Slot("Musik bis zum Morgen", "Lieder in langer Fassung", "Music", KIND_MUSIC, 8, 0x07),
          new Slot("Früher Film", "Ein Film aus den achtzigern", "Movie", KIND_MOVIE, 4, 0x06, false, 6, 3),
          new Slot("Testbild und Ansage", "Ein Programm, das Pausen macht", "Education", KIND_OTHER, 1, 0x0E),
          new Slot("Serie aus dem Archiv", "Folge in schlechter Laune", "Series", KIND_series, 2, 0x06, true),
          new Slot("Konzertmitschnitt", "Eine Band, ein Zelt", "Music", KIND_MUSIC, 4, 0x07)
        }),

        new Station(101, "Radio Puls", true, new[] { "demo-tag-all", "demo-tag-fav", "demo-tag-radio" }, new[]
        {
          new Slot("Puls am Morgen", "Nachrichten, Wetter, Lieder", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Puls Nachrichten", "Alle halben Stunde eine Meldung", "News", KIND_NEWS, 1, 0x01),
          new Slot("Vormittagsliste", "Lieder ohne Ansage", "Music", KIND_MUSIC, 5, 0x07),
          new Slot("Anruf der Hörer", "Ein Thema, viele Meinungen", "Show", KIND_SHOW, 2, 0x02),
          new Slot("Puls am Nachmittag", "Verkehr, Wetter, Lieder", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Abendliste", "Neues und Bekannte", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Puls Spätschicht", "Lesung und zwei Lieder", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Nachtliste", "Musik, die niemand moderiert", "Music", KIND_MUSIC, 6, 0x07)
        }),

        new Station(102, "Welle Nord", true, new[] { "demo-tag-all", "demo-tag-radio" }, new[]
        {
          new Slot("Welle Nord Morgen", "Der Norden wird wach", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Nordradio Nachrichten", "Die Meldung zur Stunde", "News", KIND_NEWS, 1, 0x01),
          new Slot("Schlager und Schelf", "Lieder vom Wasser", "Music", KIND_MUSIC, 5, 0x07),
          new Slot("Hafenfunk", "Was der Norden bewegt", "Show", KIND_SHOW, 2, 0x02),
          new Slot("Welle Nord Nachmittag", "Liste, Verkehr, Wetter", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Abendwelle", "Zwanzig Lieder, eine Stunde", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Nachtwelle", "Leise durch die Nacht", "Music", KIND_MUSIC, 8, 0x07),
          new Slot("Welle Nord Morgen", "Der Norden wird wach", "Music", KIND_MUSIC, 4, 0x07)
        }),

        new Station(103, "Bassline", true, new[] { "demo-tag-all", "demo-tag-radio" }, new[]
        {
          new Slot("Bassline Frühstart", "Schnelle Lieder früh", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Mix des Tages", "Ein Set, kein Wort", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Bassline Nachrichten", "Kurze Meldung, langer Bass", "News", KIND_NEWS, 1, 0x01),
          new Slot("Neue Platte, laut", "Ein Album, fünf Lieder", "Music", KIND_MUSIC, 3, 0x07),
          new Slot("Abendset", "Aufgelegt für zwei Stunden", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Nachtset", "Bis der Morgen kommt", "Music", KIND_MUSIC, 8, 0x07),
          new Slot("Bassline Frühstart", "Schnelle Lieder früh", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Mix des Tages", "Ein Set, kein Wort", "Music", KIND_MUSIC, 4, 0x07)
        }),

        new Station(104, "Leise 90", true, new[] { "demo-tag-all", "demo-tag-radio" }, new[]
        {
          new Slot("Leise in den Tag", "Sanfte Lieder, leise Stimme", "Music", KIND_MUSIC, 5, 0x07),
          new Slot("Leise Nachrichten", "Die Ruhe der Meldung", "News", KIND_NEWS, 1, 0x01),
          new Slot("Klavier und Streicher", "Instrumental ohne Pause", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Vorstellung: Leise", "Ein Lied, seine Geschichte", "Music", KIND_MUSIC, 2, 0x07),
          new Slot("Abend für sich", "Lieder zum Lesen", "Music", KIND_MUSIC, 6, 0x07),
          new Slot("Nacht für sich", "Klänge ohne Ansage", "Music", KIND_MUSIC, 8, 0x07),
          new Slot("Leise in den Tag", "Sanfte Lieder, leise Stimme", "Music", KIND_MUSIC, 4, 0x07),
          new Slot("Klavier und Streicher", "Instrumental ohne Pause", "Music", KIND_MUSIC, 4, 0x07)
        })
      };

      for (int i = 0; i < all.Length; i++)
        all[i].Index = i;

      return all;
    }

    /// <summary>The bouquets, in the order a screen is to offer them.</summary>
    private static readonly string[][] Bouquets = new[]
    {
      new[] { "demo-tag-all", "Alle Kanäle" },
      new[] { "demo-tag-fav", "Favoriten" },
      new[] { "demo-tag-hd", "HD" },
      new[] { "demo-tag-radio", "Radio" }
    };

    #endregion

    #region The clock the programmes are built against

    /// <summary>
    /// The instant every programme time is counted from: midnight at the start of last year, in the viewer's own
    /// offset, so that a programme day begins when the viewer's day begins and the half-hour numbers stay small
    /// enough to fit inside a programme id.
    /// </summary>
    private static readonly DateTimeOffset Base = new DateTimeOffset(
        DateTime.Now.Year - 1, 1, 1, 0, 0, 0, DateTimeOffset.Now.Offset);

    /// <summary>Half hours in a programme id. A day is forty-eight of them, so an id holds eighty years.</summary>
    private const long SLOTS_IN_ID = 1000000;

    /// <summary>
    /// How far before a window the walk starts. Nothing invented here runs longer than four hours, and a
    /// programme that began five hours ago is not on any screen that asks for this window.
    /// </summary>
    private const int LEAD_IN_SLOTS = 8;

    /// <summary>One programme, as the generator stands on it.</summary>
    private struct Program
    {
      internal Station Channel;
      internal long Slot;
      internal Slot Template;
      internal DateTimeOffset Start;
      internal DateTimeOffset Stop;

      internal long Id
      {
        get { return EventId(Channel.Index, Slot); }
      }
    }

    /// <summary>
    /// The id of one programme: which channel it is on, and which half hour it starts in. Reversible, which is
    /// what lets <c>api/dvr/entry/create</c> be asked with nothing but an event id and still make a timer for
    /// the right evening.
    /// </summary>
    private static long EventId(int channelIndex, long slot)
    {
      return channelIndex * SLOTS_IN_ID + slot;
    }

    private static void SplitEventId(long id, out int channelIndex, out long slot)
    {
      channelIndex = (int) (id / SLOTS_IN_ID);
      slot = id % SLOTS_IN_ID;
    }

    /// <summary>Which half hour an instant falls in, counted from <see cref="Base"/>.</summary>
    private static long SlotOf(DateTimeOffset when)
    {
      long slot = (long) Math.Floor((when.ToOffset(Base.Offset) - Base).TotalSeconds / 1800.0);
      return slot < 0 ? 0 : slot;
    }

    /// <summary>The instant a half hour begins.</summary>
    private static DateTimeOffset At(long slot)
    {
      return Base.AddSeconds(slot * 1800.0);
    }

    private static long Mod(long value, long by)
    {
      long rest = value % by;
      return rest < 0 ? rest + by : rest;
    }

    /// <summary>
    /// Walks one channel's programmes across a window. The channel's day is a cycle of slots that repeats for
    /// ever, so the walk finds its place by counting half hours rather than by remembering anything: it starts
    /// a little before the window, stands on every programme's first half hour, and hands the programmes whose
    /// window overlaps the asked one.
    /// </summary>
    private static void EachProgram(Station channel, DateTimeOffset from, DateTimeOffset to,
        List<Program> into)
    {
      Slot[] day = channel.Day;
      long cycle = channel.DayLength;

      long[] begins = new long[day.Length];
      long running = 0;
      for (int i = 0; i < day.Length; i++)
      {
        begins[i] = running;
        running += day[i].HalfHours;
      }

      long last = SlotOf(to) + 1;

      // Walked half hour by half hour rather than leapt programme by programme. The walk starts before the window
      // so that a programme which began two hours ago and is still running is found, and a leap to the end of
      // whatever is on at the start of the walk is exactly how that programme gets stepped over. A guide window
      // is a few hours wide, so there is nothing to save by leaping.
      for (long slot = SlotOf(from) - LEAD_IN_SLOTS; slot < last; slot++)
      {
        long pos = Mod(slot, cycle);

        int which = -1;
        for (int i = 0; i < begins.Length; i++)
        {
          if (begins[i] == pos)
          {
            which = i;
            break;
          }
        }

        if (which < 0)
          continue;

        Slot template = day[which];
        DateTimeOffset start = At(slot);
        DateTimeOffset stop = At(slot + template.HalfHours);

        if (stop > from && start < to)
        {
          into.Add(new Program
          {
            Channel = channel,
            Slot = slot,
            Template = template,
            Start = start,
            Stop = stop
          });
        }
      }
    }

    /// <summary>Every channel's programmes in a window, oldest first: what the backend's table holds.</summary>
    private static List<Program> ProgramsIn(DateTimeOffset from, DateTimeOffset to)
    {
      List<Program> all = new List<Program>();
      foreach (Station channel in Stations)
        EachProgram(channel, from, to, all);

      all.Sort(delegate (Program a, Program b)
      {
        int byStart = a.Start.CompareTo(b.Start);
        return byStart != 0 ? byStart : a.Channel.Number.CompareTo(b.Channel.Number);
      });
      return all;
    }

    /// <summary>
    /// The words a programme says about itself. Written the way a broadcaster writes them, which is to say
    /// reluctantly: an invented programme with an invented two-line description looks far more like a real
    /// evening than one with a title alone. The programme's own subtitle is not made here - it is the slot's, and
    /// it goes out as the backend's <c>subtitle</c> field, because a search over titles is answered by titles and
    /// subtitles and a screen that could not show what it matched on would be a strange one.
    /// </summary>
    private static void WordsOf(Program program, out string summary, out string description)
    {
      summary = string.Format(CultureInfo.InvariantCulture, "Demo-{0} auf {1} um {2} Uhr.",
          program.Template.Genre, program.Channel.Name,
          program.Start.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture));

      description = string.Format(CultureInfo.InvariantCulture,
          "{0}. Ein Demo-Programm auf {1}, das diese Oberfläche selbst erfindet - kein Sender, keine Rechte, "
          + "kein Bild dahinter.",
          program.Template.Title, program.Channel.Name);
    }

    #endregion

    #region The recordings the demo remembers

    /// <summary>One recording entry, in memory for as long as the front end runs.</summary>
    private sealed class Entry
    {
      internal string Uuid = "";
      internal int EntryId;
      internal string ChannelId = "";
      internal string ChannelName = "";
      internal string Title = "";
      internal string? Subtitle;
      internal string? Description;
      internal DateTimeOffset Start;
      internal DateTimeOffset Stop;
      internal string State = "Scheduled";
      internal bool Enabled = true;
      internal bool AutoRenew;
      internal bool AutoScheduled;
      internal int Prio;
      internal int Retention;
      internal long Size;
      internal string? Url;
      internal string EventUuid;

      /// <summary>The table row, with the entry's own words put in where the demo has them.</summary>
      internal string Json
      {
        get
        {
          StringBuilder row = new StringBuilder();
          row.Append("{\"uuid\":\"").Append(Uuid).Append("\",\"entryId\":")
              .Append(EntryId.ToString(CultureInfo.InvariantCulture))
              .Append(",\"channel\":\"").Append(Esc(ChannelId)).Append("\",\"name\":\"")
              .Append(Esc(ChannelName)).Append("\",\"title\":\"").Append(Esc(Title)).Append("\"");
          if (!string.IsNullOrEmpty(Subtitle))
            row.Append(",\"subtitle\":\"").Append(Esc(Subtitle!)).Append("\"");
          if (!string.IsNullOrEmpty(Description))
            row.Append(",\"description\":\"").Append(Esc(Description!)).Append("\"");
          row.Append(",\"start\":").Append(ToSeconds(Start))
              .Append(",\"stop\":").Append(ToSeconds(Stop))
              .Append(",\"state\":\"").Append(State).Append("\"")
              .Append(",\"enabled\":").Append(Enabled ? "true" : "false")
              .Append(",\"autoscheduled\":").Append(AutoScheduled ? "true" : "false")
              .Append(",\"autorenew\":").Append(AutoRenew ? "1" : "0")
              .Append(",\"prio\":").Append(Prio.ToString(CultureInfo.InvariantCulture))
              .Append(",\"retention\":").Append(Retention.ToString(CultureInfo.InvariantCulture))
              .Append(",\"size\":").Append(Size.ToString(CultureInfo.InvariantCulture));
          if (!string.IsNullOrEmpty(Dir))
            row.Append(",\"dir\":\"").Append(Esc(Dir)).Append("\"");
          if (!string.IsNullOrEmpty(Url))
            row.Append(",\"url\":\"").Append(Esc(Url!)).Append("\"");
          row.Append('}');
          return row.ToString();
        }
      }

      /// <summary>Where the backend would keep the file. The demo names a folder it has never written to.</summary>
      internal string Dir = "/var/demo/records";
    }

    private static readonly List<Entry> Entries = new List<Entry>();
    private static readonly object Gate = new object();
    private static int _nextEntry = 100;

    /// <summary>
    /// Gives the demo its starting evening. Done once per run rather than per question, because a recording a
    /// viewer asked for has still to be there when they walk to the timers screen and back.
    /// </summary>
    static DemoTvBackend()
    {
      Seed();
    }

    /// <summary>
    /// Puts the demo back to the evening it started with. The timers are the only thing the demo remembers, so
    /// this is also the answer to "somebody left rubbish on the timers screen".
    /// </summary>
    public static void ResetRecordings()
    {
      lock (Gate)
      {
        Entries.Clear();
        _nextEntry = 100;
        Seed();
      }
    }

    /// <summary>
    /// The four timers the demo starts with: two recordings finished yesterday and the evening before, one
    /// running right now, and one planned for later today. Built out of the same generator as the programmes, so
    /// their titles are the titles the guide shows at those times - which is the whole trick behind a demo that
    /// hangs together.
    /// </summary>
    private static void Seed()
    {
      DateTimeOffset now = DateTimeOffset.Now;

      AddFromProgram(Stations[7], now.AddHours(-26), "Completed", 3400000000L, true);
      AddFromProgram(Stations[3], now.AddHours(-50), "Completed", 5100000000L, true);
      AddFromProgram(Stations[0], now.AddMinutes(-25), "Recording", 900000000L, false);
      AddFromProgram(Stations[4], now.AddHours(4), "Scheduled", 0L, false);
    }

    /// <summary>
    /// A timer for whatever a channel is showing at one moment. The programme is fetched from the generator
    /// rather than written down again, so a seeded recording and the guide can never disagree about a title.
    /// </summary>
    private static void AddFromProgram(Station channel, DateTimeOffset around, string state, long size,
        bool autoRenew)
    {
      List<Program> found = new List<Program>();
      EachProgram(channel, around.AddMinutes(-30), around.AddMinutes(5), found);
      Program program = found.FirstOrDefault(delegate (Program p)
      {
        return p.Start <= around && p.Stop > around;
      });

      if (program.Channel == null)
        return;

      string summary;
      string description;
      WordsOf(program, out summary, out description);

      Entry entry = new Entry();
      entry.EntryId = ++_nextEntry;
      entry.Uuid = "demo-dvr-" + entry.EntryId.ToString(CultureInfo.InvariantCulture);
      entry.ChannelId = program.Channel.EpgId;
      entry.ChannelName = program.Channel.Name;
      entry.Title = program.Template.Title;
      entry.Subtitle = summary;
      entry.Description = description;
      entry.Start = program.Start;
      entry.Stop = program.Stop;
      entry.State = state;
      entry.AutoRenew = autoRenew;
      entry.AutoScheduled = state == "Completed";
      entry.Size = size;
      entry.EventUuid = program.Id.ToString(CultureInfo.InvariantCulture);
      if (state != "Scheduled")
      {
        entry.Url = "file:///var/demo/records/" + SafeName(program.Template.Title) + ".ts";
        entry.Prio = 1;
        entry.Retention = 7;
      }

      Entries.Add(entry);
    }

    /// <summary>A file name out of a title, so that the demo's fake paths look like something wrote them.</summary>
    private static string SafeName(string title)
    {
      StringBuilder name = new StringBuilder();
      foreach (char c in title)
      {
        if (char.IsLetterOrDigit(c))
          name.Append(char.ToLowerInvariant(c));
        else if (c == ' ' || c == '-')
          name.Append('-');
      }

      string said = name.ToString();
      return said.Length == 0 ? "aufnahme" : (said.Length <= 40 ? said : said.Substring(0, 40));
    }

    /// <summary>The timer standing for one programme, if anybody asked for its recording.</summary>
    private static Entry? EntryFor(Program program)
    {
      lock (Gate)
      {
        foreach (Entry entry in Entries)
        {
          if (string.Equals(entry.ChannelId, program.Channel.EpgId, StringComparison.Ordinal)
              && entry.Start == program.Start)
            return entry;
        }
      }

      return null;
    }

    #endregion

    #region The answers

    /// <summary>
    /// The backend: a message handler that answers TVHeadend's JSON out of the data above. It is the same shape
    /// the tests in <c>Tests.Frontend</c> script their backends with, which is the point - from here down the
    /// front end cannot tell the difference, and neither can a viewer.
    /// </summary>
    private sealed class Answers : HttpMessageHandler
    {
      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        await Task.Yield();

        Uri url = request.RequestUri!;
        string path = url.AbsolutePath;
        string query = url.Query;

        // A channel's picture is the one answer here that is not JSON, because a picture is not JSON.
        if (path.Contains("/imagecache/", StringComparison.Ordinal))
        {
          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            RequestMessage = request,
            Content = new ByteArrayContent(LogoBytes(LogoIndexOf(path)))
            {
              Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/bmp") }
            }
          };
        }

        string body = BodyFor(path, query);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
      }

      /// <summary>Which invented picture one icon address is asking for.</summary>
      private static int LogoIndexOf(string path)
      {
        int at = path.LastIndexOf('/');
        string said = at >= 0 ? path.Substring(at + 1) : path;
        int.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out int which);
        return which;
      }

      /// <summary>
      /// The colours the invented pictures are drawn in. Eight, chosen to be told apart at the size a guide row
      /// shows them, and none of them the grey the guide itself is made of.
      /// </summary>
      private static readonly int[,] LOGO_COLOURS =
      {
        { 0x2E, 0x6F, 0xB0 }, { 0xB0, 0x4A, 0x3A }, { 0x3F, 0x8F, 0x5F }, { 0x8A, 0x63, 0xA8 },
        { 0xC1, 0x8A, 0x2E }, { 0x37, 0x9D, 0xA8 }, { 0xA8, 0x5A, 0x7B }, { 0x6B, 0x7A, 0x8C }
      };

      /// <summary>
      /// One invented channel picture, as the bytes of a bitmap: a square of one of the palette's colours, a
      /// lighter wedge across it so that it is visibly a picture rather than a block of paint, and a dark edge so
      /// that its own size can be seen on a dark screen.
      /// </summary>
      /// <remarks>
      /// A bitmap rather than the common PNG because a bitmap needs no compression to be written: the demo is a
      /// backend, and a backend that pulled in an imaging library to make up the logos it invented would be
      /// carrying a dependency for nobody's television. Twenty-four-bit BMP is the one picture format whose whole
      /// file is arithmetic - two headers and the pixels, bottom row first, each line padded to four bytes.
      /// </remarks>
      private static byte[] LogoBytes(int which)
      {
        const int SIDE = 64;
        const int BYTES = 54 + SIDE * SIDE * 3;
        int colour = which < 0 ? 0 : which % LOGO_COLOURS.GetLength(0) ;

        byte[] file = new byte[BYTES];
        file[0] = (byte) 'B';
        file[1] = (byte) 'M';
        WriteInt(file, 2, BYTES);
        WriteInt(file, 10, 54);
        WriteInt(file, 14, 40);
        WriteInt(file, 18, SIDE);
        WriteInt(file, 22, SIDE);
        file[26] = 1;
        file[28] = 24;
        WriteInt(file, 34, SIDE * SIDE * 3);

        int red = LOGO_COLOURS[colour, 0];
        int green = LOGO_COLOURS[colour, 1];
        int blue = LOGO_COLOURS[colour, 2];

        for (int y = 0; y < SIDE; y++)
        {
          for (int x = 0; x < SIDE; x++)
          {
            // The wedge is the part a viewer reads as a picture; the edge is where the picture ends.
            bool edge = x < 3 || y < 3 || x >= SIDE - 3 || y >= SIDE - 3;
            bool wedge = (x + y) % 24 < 12;

            int r = edge ? 24 : (wedge ? Light(red) : red);
            int g = edge ? 24 : (wedge ? Light(green) : green);
            int b = edge ? 24 : (wedge ? Light(blue) : blue);

            // Bottom row first, and blue-green-red, which is what a bitmap is rather than what is convenient.
            int at = 54 + (SIDE - 1 - y) * SIDE * 3 + x * 3;
            file[at] = (byte) b;
            file[at + 1] = (byte) g;
            file[at + 2] = (byte) r;
          }
        }

        return file;
      }

      private static int Light(int part)
      {
        return 255 - (255 - part) / 3;
      }

      private static void WriteInt(byte[] into, int at, int value)
      {
        into[at] = (byte) (value & 0xFF);
        into[at + 1] = (byte) ((value >> 8) & 0xFF);
        into[at + 2] = (byte) ((value >> 16) & 0xFF);
        into[at + 3] = (byte) ((value >> 24) & 0xFF);
      }

      /// <summary>The answer to one address. Every question the client asks has one; everything else is empty.</summary>
      internal static string BodyFor(string path, string query)
      {
        if (path.EndsWith("/api/channeltag/list", StringComparison.Ordinal))
          return BouquetsJson();
        if (path.EndsWith("/api/channel/grid", StringComparison.Ordinal))
          return ChannelsJson();
        if (path.EndsWith("/api/service/grid", StringComparison.Ordinal))
          return ServicesJson();
        if (path.EndsWith("/api/epg/content_type/list", StringComparison.Ordinal))
          return ContentTypesJson();
        if (path.EndsWith("/api/epg/events", StringComparison.Ordinal))
          return EventsJson(query);

        if (path.IndexOf("/api/dvr/entry/", StringComparison.Ordinal) >= 0)
          return DvrJson(path, query);

        return "{\"total\":0,\"entries\":[]}";
      }

      private static string BouquetsJson()
      {
        StringBuilder body = new StringBuilder("{\"total\":")
            .Append(Bouquets.Length.ToString(CultureInfo.InvariantCulture))
            .Append(",\"entries\":[");

        for (int i = 0; i < Bouquets.Length; i++)
        {
          if (i > 0)
            body.Append(',');

          string id = Bouquets[i][0];
          List<Station> inBouquet = Stations.Where(delegate (Station s)
          {
            return s.Tags.Contains(id, StringComparer.Ordinal);
          }).ToList();

          body.Append("{\"uuid\":\"").Append(id).Append("\",\"name\":\"")
              .Append(Esc(Bouquets[i][1])).Append("\",\"services\":[");
          for (int j = 0; j < inBouquet.Count; j++)
          {
            if (j > 0)
              body.Append(',');
            body.Append("\"").Append(inBouquet[j].ServiceId).Append('"');
          }

          body.Append("]}");
        }

        return body.Append("]}").ToString();
      }

      private static string ChannelsJson()
      {
        StringBuilder body = new StringBuilder("{\"total\":")
            .Append(Stations.Length.ToString(CultureInfo.InvariantCulture))
            .Append(",\"entries\":[");

        for (int i = 0; i < Stations.Length; i++)
        {
          if (i > 0)
            body.Append(',');

          Station channel = Stations[i];
          body.Append("{\"number\":")
              .Append(channel.Number.ToString(CultureInfo.InvariantCulture))
              .Append(",\"name\":\"").Append(Esc(channel.Name))
              .Append("\",\"epgchannelid\":\"").Append(channel.EpgId)
              .Append("\",\"streamurl\":\"//stream/channelid/")
              .Append(channel.Number.ToString(CultureInfo.InvariantCulture))
              .Append("\",\"tags\":[");
          for (int t = 0; t < channel.Tags.Length; t++)
          {
            if (t > 0)
              body.Append(',');
            body.Append('"').Append(channel.Tags[t]).Append('"');
          }

          body.Append("],\"services\":[\"").Append(channel.ServiceId).Append("\"]");

          // Most of the invented channels carry a picture and a couple do not, because a guide that only ever met
          // channels with icons has never had to draw a row without one, and the household whose real backend
          // carries no icons at all is the commoner case of the two.
          if (i % 5 != 3)
          {
            body.Append(",\"icon_public_url\":\"/imagecache/")
                .Append(i.ToString(CultureInfo.InvariantCulture))
                .Append('"');
          }

          body.Append('}');
        }

        return body.Append("]}").ToString();
      }

      private static string ServicesJson()
      {
        StringBuilder body = new StringBuilder("{\"total\":")
            .Append(Stations.Length.ToString(CultureInfo.InvariantCulture))
            .Append(",\"entries\":[");

        for (int i = 0; i < Stations.Length; i++)
        {
          if (i > 0)
            body.Append(',');
          body.Append("{\"uuid\":\"").Append(Stations[i].ServiceId).Append("\",\"type\":")
              .Append(Stations[i].Radio ? "2" : "1")
              .Append(",\"name\":\"").Append(Esc(Stations[i].Name)).Append("\"}");
        }

        return body.Append("]}").ToString();
      }

      private static string ContentTypesJson()
      {
        StringBuilder body = new StringBuilder("{\"total\":")
            .Append(CONTENT_TYPES.Length.ToString(CultureInfo.InvariantCulture))
            .Append(",\"entries\":[");

        for (int i = 0; i < CONTENT_TYPES.Length; i++)
        {
          if (i > 0)
            body.Append(',');
          body.Append("{\"id\":\"").Append(CONTENT_TYPES[i][0]).Append("\",\"val\":\"")
              .Append(Esc(CONTENT_TYPES[i][1])).Append("\"}");
        }

        return body.Append("]}").ToString();
      }

      /// <summary>
      /// The programmes one question wants: the window out of the backend's own filter clauses, then the channel,
      /// the bouquet or the title the question singled out, then the page it asked for.
      /// </summary>
      private static string EventsJson(string query)
      {
        DateTimeOffset from = DateTimeOffset.Now.AddHours(-12);
        DateTimeOffset to = DateTimeOffset.Now.AddHours(12);

        long? windowEnd = WindowBound(query, "start");
        long? windowStart = WindowBound(query, "stop");
        if (windowEnd.HasValue)
          to = DateTimeOffset.FromUnixTimeSeconds(windowEnd.Value);
        if (windowStart.HasValue)
          from = DateTimeOffset.FromUnixTimeSeconds(windowStart.Value);
        if (to <= from)
          to = from.AddHours(12);

        string? channel = QueryValue(query, "channel");
        string? tag = QueryValue(query, "channelTag");
        string? title = QueryValue(query, "title");

        List<Program> wanted = ProgramsIn(from, to);
        if (!string.IsNullOrEmpty(channel))
        {
          wanted = wanted.Where(delegate (Program p)
          {
            return string.Equals(p.Channel.EpgId, channel, StringComparison.OrdinalIgnoreCase);
          }).ToList();
        }

        if (!string.IsNullOrEmpty(tag))
        {
          wanted = wanted.Where(delegate (Program p)
          {
            return p.Channel.Tags.Contains(tag!, StringComparer.Ordinal);
          }).ToList();
        }

        if (!string.IsNullOrEmpty(title))
        {
          wanted = wanted.Where(delegate (Program p)
          {
            return p.Template.Title.IndexOf(title!, StringComparison.OrdinalIgnoreCase) >= 0
                || (p.Template.Subtitle ?? "").IndexOf(title!, StringComparison.OrdinalIgnoreCase) >= 0;
          }).ToList();
        }

        int skip = QueryInt(query, "start", 0);
        int take = QueryInt(query, "limit", 2000);
        if (take <= 0)
          take = 2000;

        StringBuilder body = new StringBuilder("{\"total\":")
            .Append(wanted.Count.ToString(CultureInfo.InvariantCulture))
            .Append(",\"entries\":[");

        int written = 0;
        for (int i = skip; i < wanted.Count && written < take; i++, written++)
        {
          if (written > 0)
            body.Append(',');
          ProgramRow(wanted[i], body);
        }

        return body.Append("]}").ToString();
      }

      /// <summary>One programme, written the way a TVHeadend writes one.</summary>
      private static void ProgramRow(Program program, StringBuilder body)
      {
        Slot template = program.Template;
        string summary;
        string description;
        WordsOf(program, out summary, out description);

        body.Append("{\"event_id\":")
            .Append(program.Id.ToString(CultureInfo.InvariantCulture))
            .Append(",\"start\":").Append(ToSeconds(program.Start))
            .Append(",\"stop\":").Append(ToSeconds(program.Stop))
            .Append(",\"title\":\"").Append(Esc(template.Title)).Append("\"");

        if (!string.IsNullOrEmpty(template.Subtitle))
          body.Append(",\"subtitle\":\"").Append(Esc(template.Subtitle!)).Append("\"");

        body.Append(",\"summary\":\"").Append(Esc(summary)).Append("\"")
            .Append(",\"description\":\"").Append(Esc(description)).Append("\"")
            .Append(",\"contentType\":\"0x")
            .Append(template.Kind.ToString("X2", CultureInfo.InvariantCulture))
            .Append("\"")
            .Append(",\"contentLabel\":\"").Append(Esc(template.Genre)).Append("\"")
            .Append(",\"genre\":[").Append(template.Dvb.ToString(CultureInfo.InvariantCulture)).Append("]");

        if (template.Age > 0)
          body.Append(",\"ageRating\":").Append(template.Age.ToString(CultureInfo.InvariantCulture));
        if (template.Stars > 0)
          body.Append(",\"starRating\":").Append(template.Stars.ToString(CultureInfo.InvariantCulture));

        if (template.Serial)
        {
          // Season and episode counted out of the calendar rather than remembered: a demo has no schedule to
          // keep them consistent with, and a number that changes every day is what a series does anyway.
          int season = 1 + (program.Channel.Index % 6);
          int episode = 1 + (int) Mod(program.Slot / 48, 24);
          body.Append(",\"seasonNumber\":").Append(season.ToString(CultureInfo.InvariantCulture))
              .Append(",\"episodeNumber\":").Append(episode.ToString(CultureInfo.InvariantCulture))
              .Append(",\"episodeOnscreen\":\"S")
              .Append(season.ToString("00", CultureInfo.InvariantCulture))
              .Append(" E").Append(episode.ToString("00", CultureInfo.InvariantCulture)).Append('"');
        }

        body.Append(",\"nextEventId\":")
            .Append(EventId(program.Channel.Index, program.Slot + template.HalfHours)
                .ToString(CultureInfo.InvariantCulture))
            .Append(",\"channel\":\"").Append(program.Channel.EpgId)
            .Append("\",\"channelname\":\"").Append(Esc(program.Channel.Name)).Append('"');

        Entry? entry = EntryFor(program);
        if (entry != null)
        {
          body.Append(",\"dvrUuid\":\"").Append(entry!.Uuid)
              .Append("\",\"dvrState\":\"").Append(entry.State).Append('"');
        }

        body.Append('}');
      }

      /// <summary>The recording tables and the four things a viewer asks a recording to do.</summary>
      private static string DvrJson(string path, string query)
      {
        if (path.EndsWith("/api/dvr/entry/grid_upcoming", StringComparison.Ordinal))
        {
          return EntriesJson(query, delegate (Entry entry)
          {
            return entry.State != "Completed" && entry.State != "Failed";
          });
        }

        if (path.EndsWith("/api/dvr/entry/grid_finished", StringComparison.Ordinal))
        {
          return EntriesJson(query, delegate (Entry entry)
          {
            return entry.State == "Completed" || entry.State == "Failed";
          });
        }

        if (path.EndsWith("/api/dvr/entry/grid_all", StringComparison.Ordinal))
          return EntriesJson(query, null);

        if (path.EndsWith("/api/dvr/entry/create", StringComparison.Ordinal))
          return CreateJson(query);

        // Throwing a recording away answers the same way as every other change, with the entry as it was; the
        // entry is gone from the next question onwards.
        if (path.EndsWith("/api/dvr/entry/delete", StringComparison.Ordinal))
          return ChangeJson(query, delegate (Entry entry) { return false; });
        if (path.EndsWith("/api/dvr/entry/cancel", StringComparison.Ordinal))
        {
          return ChangeJson(query, delegate (Entry entry)
          {
            entry.State = "Completed";
            return true;
          });
        }

        if (path.EndsWith("/api/dvr/entry/autorenew", StringComparison.Ordinal))
        {
          // The backend's own name for the switch on this endpoint is "active", not "autorenew", which is the kind
          // of thing a demo has to get right or it teaches the wrong lesson.
          bool renew = QueryInt(query, "active", 0) != 0;
          return ChangeJson(query, delegate (Entry entry)
          {
            entry.AutoRenew = renew;
            return true;
          });
        }

        if (path.EndsWith("/api/dvr/entry/get", StringComparison.Ordinal))
        {
          string? uuid = QueryValue(query, "uuid");
          lock (Gate)
          {
            Entry? found = Entries.FirstOrDefault(delegate (Entry entry)
            {
              return string.Equals(entry.Uuid, uuid, StringComparison.Ordinal);
            });
            if (found == null)
              return Refused("the demo backend knows no recording with that id");
            return "{\"success\":true,\"data\":" + found.Json + "}";
          }
        }

        return "{\"success\":true}";
      }

      /// <summary>
      /// One of the recording tables, in the backend's own transposed spelling of its row list. Paged like every
      /// other table, because the client pages every table and an answer that ignored the page number would be
      /// read twice.
      /// </summary>
      private static string EntriesJson(string query, Func<Entry, bool>? keep)
      {
        List<Entry> rows;
        lock (Gate)
        {
          rows = Entries.Where(keep ?? delegate (Entry entry) { return true; })
              .OrderBy(delegate (Entry entry) { return entry.Start; })
              .ToList();
        }

        int skip = QueryInt(query, "start", 0);
        int take = QueryInt(query, "limit", 2000);
        if (take <= 0)
          take = 2000;

        StringBuilder body = new StringBuilder("{\"totalCount\":")
            .Append(rows.Count.ToString(CultureInfo.InvariantCulture))
            .Append(",\"limit\":").Append(take.ToString(CultureInfo.InvariantCulture))
            .Append(",\"start\":").Append(skip.ToString(CultureInfo.InvariantCulture))
            .Append(",\"dlrEntries\":[");

        int written = 0;
        for (int i = skip; i < rows.Count && written < take; i++, written++)
        {
          if (written > 0)
            body.Append(',');
          body.Append(rows[i].Json);
        }

        return body.Append("]}").ToString();
      }

      

      /// <summary>
      /// A timer made out of a programme id. Everything the backend would fill in from its own EPG - channel,
      /// title, both ends - is asked of the generator here, because that is where this demo keeps its EPG.
      /// </summary>
      private static string CreateJson(string query)
      {
        long eventId = QueryLong(query, "event_id", 0);
        int index;
        long slot;
        SplitEventId(eventId, out index, out slot);

        if (index < 0 || index >= Stations.Length)
          return Refused("the demo backend has no programme with that id");

        Station channel = Stations[index];
        List<Program> found = new List<Program>();
        EachProgram(channel, At(slot), At(slot + 1), found);

        Program program = found.FirstOrDefault(delegate (Program p)
        {
          return p.Slot == slot;
        });

        if (program.Channel == null)
          return Refused("the demo backend has no programme at that time on that channel");

        string summary;
        string description;
        WordsOf(program, out summary, out description);

        lock (Gate)
        {
          foreach (Entry existing in Entries)
          {
            if (string.Equals(existing.ChannelId, channel.EpgId, StringComparison.Ordinal)
                && existing.Start == program.Start)
              return Refused("this programme is already on the demo's timer list");
          }

          Entry entry = new Entry();
          entry.EntryId = ++_nextEntry;
          entry.Uuid = "demo-dvr-" + entry.EntryId.ToString(CultureInfo.InvariantCulture);
          entry.ChannelId = channel.EpgId;
          entry.ChannelName = channel.Name;
          entry.Title = program.Template.Title;
          entry.Subtitle = summary;
          entry.Description = description;
          entry.Start = program.Start;
          entry.Stop = program.Stop;
          entry.State = program.Start <= DateTimeOffset.Now && program.Stop > DateTimeOffset.Now
              ? "Recording"
              : "Scheduled";
          entry.AutoRenew = QueryInt(query, "autorenew", 0) != 0;
          entry.EventUuid = eventId.ToString(CultureInfo.InvariantCulture);
          if (entry.State == "Recording")
          {
            entry.Size = 120000000L;
            entry.Url = "file:///var/demo/records/" + SafeName(entry.Title) + ".ts";
          }

          Entries.Add(entry);
          return "{\"success\":true,\"data\":" + entry.Json + "}";
        }
      }

      /// <summary>
      /// A recording somebody asked to be changed or thrown away. The answer says no when the id is not one of
      /// the demo's, because a demo that agrees to everything teaches nobody what a refusal looks like.
      /// </summary>
      /// <param name="keep">
      /// What to do with the entry: keep it, with whatever was changed, or say no and have it taken off the list.
      /// Throwing an entry away is the one answer that cannot be spelled as a change, so it is the one that
      /// answers false.
      /// </param>
      private static string ChangeJson(string query, Func<Entry, bool> keep)
      {
        string? uuid = QueryValue(query, "uuid");
        lock (Gate)
        {
          Entry? found = Entries.FirstOrDefault(delegate (Entry entry)
          {
            return string.Equals(entry.Uuid, uuid, StringComparison.Ordinal);
          });

          if (found == null)
            return Refused("the demo backend knows no recording with that id");

          if (!keep(found))
            Entries.Remove(found);

          return "{\"success\":true,\"data\":" + found.Json + "}";
        }
      }

      private static string Refused(string why)
      {
        return "{\"success\":false,\"errors\":[{\"text\":\"" + Esc(why) + "\"}]}";
      }
    }

    #endregion

    #region Writing and reading a little JSON by hand

    /// <summary>A word as the backend's JSON wants it. Quotes and backslashes are the two things to mind.</summary>
    private static string Esc(string? text)
    {
      if (string.IsNullOrEmpty(text))
        return "";

      StringBuilder escaped = new StringBuilder(text.Length + 8);
      foreach (char c in text!)
      {
        if (c == '"' || c == '\\')
          escaped.Append('\\').Append(c);
        else if (c == '\r')
          escaped.Append(' ');
        else if (c == '\n')
          escaped.Append(' ');
        else if (c < 0x20)
          escaped.Append(' ');
        else
          escaped.Append(c);
      }

      return escaped.ToString();
    }

    private static long ToSeconds(DateTimeOffset when)
    {
      return when.ToUnixTimeSeconds();
    }

    /// <summary>One value of a query string, taken as written and then unescaped. Null when it is not there.</summary>
    private static string? QueryValue(string query, string name)
    {
      if (string.IsNullOrEmpty(query))
        return null;

      string said = query.StartsWith("?", StringComparison.Ordinal) ? query.Substring(1) : query;
      string needle = name + "=";

      foreach (string piece in said.Split('&'))
      {
        if (piece.StartsWith(needle, StringComparison.OrdinalIgnoreCase))
          return Uri.UnescapeDataString(piece.Substring(needle.Length));
      }

      return null;
    }

    private static int QueryInt(string query, string name, int missing)
    {
      string? said = QueryValue(query, name);
      int read;
      return said != null && int.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out read)
          ? read
          : missing;
    }

    private static long QueryLong(string query, string name, long missing)
    {
      string? said = QueryValue(query, name);
      long read;
      return said != null && long.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out read)
          ? read
          : missing;
    }

    /// <summary>
    /// The instant one of the client's two window clauses names. The clause arrives as the backend's own JSON
    /// - <c>{"field":"start","type":"numeric","comparison":"lt","value":1800000000}</c> - so the field's name is
    /// found and the number after it is read, rather than the whole clause being parsed for one number.
    /// </summary>
    private static long? WindowBound(string query, string field)
    {
      string? filter = QueryValue(query, "filter");
      if (string.IsNullOrEmpty(filter))
        return null;

      string needle = "\"field\":\"" + field + "\"";
      int at = filter!.IndexOf(needle, StringComparison.Ordinal);
      if (at < 0)
        return null;

      int valueAt = filter.IndexOf("\"value\":", at, StringComparison.Ordinal);
      if (valueAt < 0)
        return null;

      StringBuilder digits = new StringBuilder();
      for (int i = valueAt + 8; i < filter.Length; i++)
      {
        char c = filter[i];
        if (c == '-' || (c >= '0' && c <= '9'))
          digits.Append(c);
        else if (digits.Length > 0)
          break;
      }

      long read;
      return digits.Length > 0
             && long.TryParse(digits.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out read)
          ? read
          : (long?) null;
    }

    #endregion
  }
}
