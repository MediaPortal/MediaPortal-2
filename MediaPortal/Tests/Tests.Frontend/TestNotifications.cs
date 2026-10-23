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
using System.ComponentModel;
using System.Threading;
using MediaPortal.Frontend.Notifications;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The things the household has been told and has not read: the queue they wait in, the hint that counts them in
  /// the head line, the pictures the head line draws for what the server is doing, and the dialog that reads them out
  /// one at a time.
  /// </summary>
  /// <remarks>
  /// The reference had all of this (<c>Source/Core/MediaPortal.UI/Presentation/UiNotifications/INotificationService.cs
  /// </c>, <c>SkinBase/Models/NotificationsModel.cs</c> and
  /// <c>Skin/Nereus/screens/WatchSystemNotifications.xaml</c>), and this front end had a sentence in a corner it did
  /// not belong in. The tests below are the arrangement, asked of the objects rather than of a picture: what waits, in
  /// which order, what the hint may show, and when the hint has to shut up.
  /// </remarks>
  [TestFixture]
  public class TestNotifications
  {
    // ---- the queue itself -----------------------------------------------------------------------------------

    [Test]
    public void ANotificationWaitsUntilItIsRead()
    {
      NotificationQueue queue = new NotificationQueue();
      List<UiNotification> arrived = new List<UiNotification>();
      int changed = 0;
      queue.Arrived += arrived.Add;
      queue.Changed += delegate { changed++; };

      UiNotification told = queue.Enqueue(NotificationType.Info, "The server was quicker", "It dropped 3 frames.");

      Assert.That(queue.Count, Is.EqualTo(1));
      Assert.That(arrived, Has.Count.EqualTo(1), "somebody who wants to be interrupted hears it arrive");
      Assert.That(changed, Is.EqualTo(1));

      Assert.That(queue.Peek(), Is.SameAs(told), "the next one to be read can be looked at without taking it");
      Assert.That(queue.Count, Is.EqualTo(1), "and looking at it does not read it");

      Assert.That(queue.Dequeue(), Is.SameAs(told));
      Assert.That(queue.Count, Is.EqualTo(0));
      Assert.That(queue.Dequeue(), Is.Null, "an empty queue has nothing to hand out, and does not throw about it");
      Assert.That(changed, Is.EqualTo(2), "once in and once out - nothing is said for an ask that changed nothing");
    }

    [Test]
    public void WhatIsUrgentIsReadFirst()
    {
      NotificationQueue queue = new NotificationQueue();
      UiNotification first = queue.Enqueue(NotificationType.Info, "an information", "said first");
      UiNotification second = queue.Enqueue(NotificationType.Info, "another information", "said second");
      UiNotification pressing = queue.Enqueue(NotificationType.Error, "the television stopped", "said third",
          urgent: true);

      Assert.That(queue.Waiting()[0], Is.SameAs(pressing),
          "the reference put an urgent notification in front of the ones already waiting, and so does this");
      Assert.That(queue.Waiting()[1], Is.SameAs(first), "and the rest keep the order they were said in");
      Assert.That(queue.Waiting()[2], Is.SameAs(second));
    }

    [Test]
    public void TheSameThingSaidTwiceIsOnlyReadOnce()
    {
      NotificationQueue queue = new NotificationQueue();
      UiNotification share = queue.Enqueue(NotificationType.Warning, "a share is gone", "Videos cannot be read.");

      // Two corners of the front end noticing the same broken thing at once is normal, and a household that was told
      // twice would only conclude that something very bad happened twice.
      queue.Enqueue(share);

      Assert.That(queue.Count, Is.EqualTo(1));
      Assert.That(queue.Dequeue(), Is.SameAs(share));
      Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void ANotificationWhoseReasonHasGoneAwayStopsBeingCounted()
    {
      NotificationQueue queue = new NotificationQueue();
      UiNotification passing = queue.Enqueue(NotificationType.Info, "a word", "a word about nothing");
      passing.Timeout = DateTime.Now.AddSeconds(-1);
      queue.Enqueue(NotificationType.Error, "a thing that stays", "until somebody deals with it");

      Assert.That(queue.Count, Is.EqualTo(1),
          "the reference's timeouts kept a stale notification from being counted; reading the queue prunes it here");
      Assert.That(queue.Peek(), Is.Not.SameAs(passing));
      Assert.That(queue.CheckTimeouts(), Is.EqualTo(0), "and once it has been forgotten it is not forgotten twice");
    }

    [Test]
    public void ARemovedNotificationIsGone()
    {
      NotificationQueue queue = new NotificationQueue();
      UiNotification share = queue.Enqueue(NotificationType.Warning, "a share is gone", "Videos cannot be read.");
      UiNotification other = queue.Enqueue(NotificationType.Info, "something else", "still worth reading");

      Assert.That(queue.Remove(share), Is.True);
      Assert.That(queue.Remove(share), Is.False, "taking it away twice is not a failure, just nothing");
      Assert.That(queue.Peek(), Is.SameAs(other), "and what is left standing is still in order");
    }

    // ---- what the hint may show ---------------------------------------------------------------------------

    [Test]
    public void ALouderThingWaitingIsWhatTheHintShows()
    {
      NotificationQueue queue = new NotificationQueue();
      queue.Enqueue(NotificationType.Info, "an information", "nothing dramatic");
      queue.Enqueue(NotificationType.Warning, "a warning", "nothing dramatic");

      Assert.That(queue.LoudestType, Is.EqualTo(NotificationType.Warning),
          "a hint that quietly shows an information \"i\" while a warning waits underneath it is a hint that lies");

      queue.Enqueue(NotificationType.UserInteractionRequired, "somebody has to act", "nothing dramatic");
      Assert.That(queue.LoudestType, Is.EqualTo(NotificationType.UserInteractionRequired));

      queue.Enqueue(NotificationType.Error, "something broke", "nothing dramatic");
      Assert.That(queue.LoudestType, Is.EqualTo(NotificationType.Error));
    }

    [Test]
    public void TheHeadLineDrawsTheServerStateAsAPictureAndNothingElse()
    {
      ChromeInfo chrome = new ChromeInfo();

      Assert.That(chrome.HasActivity, Is.False,
          "a screen shown before anybody asked for anything draws no claim about a server");

      chrome.ReportServerActivity(ServerActivityState.Connected, "Listening to the server");
      Assert.That(chrome.ActivityIcon, Is.EqualTo("Notification/connected"));
      Assert.That(chrome.ActivityPercent, Is.EqualTo(""),
          "a connection has no number to stand under its picture");
      Assert.That(chrome.ServerNotice, Is.EqualTo("Listening to the server"),
          "the sentence is kept, because it is what the tooltip has to say");

      chrome.ReportServerActivity(ServerActivityState.Disconnected,
          "Lost the server. Trying again in 5 seconds.");
      Assert.That(chrome.ActivityIcon, Is.EqualTo("Notification/disconnected"));
      Assert.That(chrome.ServerNotice, Does.Contain("5 seconds"),
          "and the one thing the picture cannot say - how long until the next try - survives in the sentence");

      chrome.ReportServerActivity(ServerActivityState.Filing, "Filing 12 of 40 in D:\\Video", 30);
      Assert.That(chrome.ActivityIcon, Is.EqualTo("Notification/import"),
          "the importer's glyph, which is the picture the reference's turning circle stood for");
      Assert.That(chrome.ActivityPercent, Is.EqualTo("30%"),
          "and the percentage where the reference printed its percentage (NotificationsBar.inc:29-31)");
      Assert.That(chrome.IsFiling, Is.True);

      chrome.ReportServerActivity(ServerActivityState.NotListening, null);
      Assert.That(chrome.HasActivity, Is.False);
      Assert.That(chrome.ServerNotice, Is.Null, "a finished import leaves no claim in the head line");
    }

    /// <summary>
    /// The reference's bar never spelled a state out: its importer was a circle with a number, its recording a
    /// blinking picture, and its hint a picture with "(2)" under it once a second thing was waiting. The words the
    /// model still answers are for anything that wants them; the bar is pictures.
    /// </summary>
    [Test]
    public void TheBarCarriesPicturesWhileTheWordsStayBehindThem()
    {
      ChromeInfo chrome = new ChromeInfo();

      chrome.ReportServerActivity(ServerActivityState.Filing, "The server is filing its library");
      Assert.That(chrome.HasActivity, Is.True, "the circle is worth drawing even without a number");
      Assert.That(chrome.ActivityPercent, Is.EqualTo(""),
          "and a filing the server gave no numbers for stands under its picture with nothing written");

      chrome.ReportServerActivity(ServerActivityState.Disconnected, "Lost the server");
      Assert.That(chrome.ActivityWords, Is.EqualTo("disconnected"),
          "the word is still answered, for a screen or a tooltip that would rather say it");
    }

    [Test]
    public void TheHeadLineCountsWhatIsWaiting()
    {
      ChromeInfo chrome = new ChromeInfo();
      NotificationQueue queue = new NotificationQueue();
      chrome.AttachNotifications(queue);

      Assert.That(chrome.HasNotifications, Is.False, "nothing has been said, so nothing waits");

      queue.Enqueue(NotificationType.Error, "the television stopped", "the card fell out");
      Assert.That(chrome.HasNotifications, Is.True);
      Assert.That(chrome.NotificationCount, Is.EqualTo(1));
      Assert.That(chrome.NotificationIcon, Is.EqualTo("Notification/error"));
      Assert.That(chrome.CanOpenNotifications, Is.True);

      queue.Dequeue();
      Assert.That(chrome.HasNotifications, Is.False, "the hint goes away with the last notification, by itself");
      Assert.That(chrome.NotificationIcon, Is.EqualTo(""));
    }

    [Test]
    public void TheHintShutsUpWhileTheQueueIsBeingRead()
    {
      ChromeInfo chrome = new ChromeInfo();
      NotificationQueue queue = new NotificationQueue();
      chrome.AttachNotifications(queue);
      queue.Enqueue(NotificationType.Info, "a word", "and another");

      chrome.HideNotificationHint(true);
      Assert.That(chrome.HasNotifications, Is.False,
          "a count of what is still waiting, next to the one being read, is the same number said twice");

      chrome.HideNotificationHint(false);
      Assert.That(chrome.HasNotifications, Is.True, "and it comes back the moment the dialog is gone");
    }

    [Test]
    public void PressingTheHintAsksForTheDialogAndNotForAnythingElse()
    {
      ChromeInfo chrome = new ChromeInfo();
      NotificationQueue queue = new NotificationQueue();
      chrome.AttachNotifications(queue);
      queue.Enqueue(NotificationType.Info, "a word", "and another");

      int asked = 0;
      chrome.NotificationsRequested += delegate { asked++; };

      Assert.That(chrome.OpenNotificationsCommand.CanExecute(null), Is.True);
      chrome.OpenNotificationsCommand.Execute(null);
      Assert.That(asked, Is.EqualTo(1));

      queue.Clear();
      Assert.That(chrome.OpenNotificationsCommand.CanExecute(null), Is.False,
          "and with nothing waiting there is nothing for the button to do");
    }

    // ---- the recording mark --------------------------------------------------------------------------------

    [Test]
    public void ARecordingBlinksAndOneThatEndedStopsBlinking()
    {
      ChromeInfo chrome = new ChromeInfo();
      Assert.That(chrome.RecordingMarkVisible, Is.True, "nothing is recording, so the mark is not even on the screen");

      chrome.ReportRecording(true);
      Assert.That(chrome.RecordingMarkVisible, Is.True, "a recording that starts is to be noticed at once");

      int flipped = 0;
      chrome.PropertyChanged += delegate (object? sender, PropertyChangedEventArgs args)
      {
        if (args.PropertyName == nameof(ChromeInfo.RecordingMarkVisible))
          flipped++;
      };

      System.DateTime until = System.DateTime.Now + TimeSpan.FromMilliseconds(2400);
      while (System.DateTime.Now < until && flipped < 2)
        Thread.Sleep(50);

      Assert.That(flipped, Is.GreaterThanOrEqualTo(2),
          "the mark blinks for as long as the recording runs, as the reference's recording picture did");

      chrome.ReportRecording(false);
      Assert.That(chrome.RecordingMarkVisible, Is.True,
          "and it stops on the showing side, so that the next recording is not started invisible");

      int afterItEnded = flipped;
      Thread.Sleep(1600);
      Assert.That(flipped, Is.EqualTo(afterItEnded), "a recording that ended leaves nothing pulsing in the head line");
    }

    // ---- the dialog ---------------------------------------------------------------------------------------

    [Test]
    public void TheDialogReadsOneAtATimeAndSaysHowManyRemain()
    {
      NotificationQueue queue = new NotificationQueue();
      UiNotification first = queue.Enqueue(NotificationType.Info, "first", "one");
      queue.Enqueue(NotificationType.Warning, "second", "two");
      queue.Enqueue(NotificationType.Warning, "third", "three");
      NotificationsViewModel dialog = new NotificationsViewModel(queue);

      Assert.That(dialog.Title, Is.EqualTo("first"), "the oldest thing waiting is what gets read first");
      Assert.That(dialog.Icon, Is.EqualTo("Notification/info"), "with the picture that belongs to it");
      Assert.That(dialog.MoreWords, Is.EqualTo("2 more waiting"),
          "the reference said how many more there were, in the same dialog");

      dialog.ReadThat();
      Assert.That(dialog.Title, Is.EqualTo("second"));
      Assert.That(dialog.MoreWords, Is.EqualTo("one more waiting"), "and said it as words for one");

      dialog.ReadThat();
      Assert.That(dialog.Title, Is.EqualTo("third"));
      Assert.That(dialog.MoreWords, Is.EqualTo(""), "the last one is read without being counted twice");

      dialog.ReadThat();
      Assert.That(dialog.HasNotification, Is.False);
      Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void TheDialogOnlyOffersToGoSomewhereWhenSomebodyKnowsWhere()
    {
      NotificationQueue queue = new NotificationQueue();
      queue.Enqueue(NotificationType.Info, "a word", "nowhere to go");
      NotificationsViewModel plain = new NotificationsViewModel(queue);
      Assert.That(plain.CanHandle, Is.False);

      queue.Enqueue(NotificationType.UserInteractionRequired, "sign in", "the server refused us", "server");
      NotificationsViewModel withAHandler = new NotificationsViewModel(queue);
      withAHandler.ReadThat();

      List<string> askedFor = new List<string>();
      withAHandler.HandleRequested += askedFor.Add;
      Assert.That(withAHandler.CanHandle, Is.True);
      withAHandler.Handle();

      Assert.That(askedFor, Is.EqualTo(new[] { "server" }));
      Assert.That(withAHandler.HasNotification, Is.False,
          "a thing the viewer went and dealt with has been dealt with, and should not be said again");
      Assert.That(queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void TheDialogCanBeDismissedWithoutReadingTheRest()
    {
      NotificationQueue queue = new NotificationQueue();
      queue.Enqueue(NotificationType.Info, "a word", "and another");
      NotificationsViewModel dialog = new NotificationsViewModel(queue);

      dialog.ReadNoMore();

      Assert.That(dialog.HasNotification, Is.False);
      Assert.That(queue.Count, Is.EqualTo(0), "and the queue really is empty afterwards, not merely hidden");
    }

    [Test]
    public void TheDialogSaysWhenItHasRunOutOfThingsToSay()
    {
      NotificationQueue queue = new NotificationQueue();
      queue.Enqueue(NotificationType.Info, "one word", "that is all");
      NotificationsViewModel dialog = new NotificationsViewModel(queue);

      int finished = 0;
      dialog.Finished += delegate { finished++; };

      dialog.ReadThat();
      Assert.That(finished, Is.EqualTo(1),
          "which is the only way an empty box standing over the screen behind it ever gets taken away");

      dialog.ReadThat();
      Assert.That(finished, Is.EqualTo(1), "and it is said once, not once per button press afterwards");
    }
  }
}
