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
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace MediaPortal.Frontend.Theming
{
    /// <summary>
    /// The one line a view writes to let the skin draw a list's items: name the key, and the list answers to
    /// whichever skin is worn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is how the reference's screens worked. A screen's lists were written in code and wired there once,
    /// but the tree every item drew was the skin's - <c>Contents_Template</c> and its siblings, looked up by name
    /// at the moment the item was made. Here a view says <c>SkinTemplates.TileTemplate="HomeMenuTileTemplate"</c>
    /// on its ListBox and keeps its own template as long as the skin says nothing; the moment a worn skin answers
    /// that key, its DataTemplate - read off disk, by hand, without a recompile - is what the television draws,
    /// and the day the skin comes off the view's own template is back. The screen never reloads and the
    /// selection, the focus and the scroll position all stay exactly where they were; only the trees the list
    /// builds for its items change underneath them.
    /// </para>
    /// <para>
    /// A template is applied while the list is on screen and re-applied whenever a skin is put on, changed, or
    /// taken off, which is the same moment the window swaps its chrome. A list that has left the screen keeps
    /// nothing: the attachment listens to the visual tree and lets go when the list does.
    /// </para>
    /// </remarks>
    public static class SkinTemplates
    {
        /// <summary>
        /// The resource key this list asks the skin for. Setting it is what attaches the list; a list that never
        /// names a key never consults a skin and behaves exactly as it did before this existed.
        /// </summary>
        public static readonly AttachedProperty<string> TileTemplateProperty =
            AvaloniaProperty.RegisterAttached<Target, ItemsControl, string>("TileTemplate");

        /// <summary>
        /// The resource key whose DataTemplate this content control draws its one piece of content with.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A list swaps the tree of every item it holds; a screen also has one-off areas whose whole shape belongs
        /// to the skin, which is what the reference's master screens called the overlays - the playback bar a
        /// screen put over its picture was drawn in the skin's markup, not in the screen's. The lookup is the same
        /// one the lists ask with, because a template is a template: the worn skin answers the name or says nothing
        /// and the control keeps the tree the view was written with.
        /// </para>
        /// <para>
        /// A view therefore keeps its own content template in its own file and merely names a key, exactly like a
        /// list does - which is how the playback screen can carry the bar it was written with, wear Nereus' slab
        /// or WMC's band over it, and lose nothing when the skin comes off.
        /// </para>
        /// </remarks>
        public static readonly AttachedProperty<string> OverlayTemplateProperty =
            AvaloniaProperty.RegisterAttached<Target, ContentControl, string>("OverlayTemplate");

        /// <summary>
        /// Avalonia wants a registrant class for an attached property, and a static class cannot be a type
        /// argument, so this is the quiet one it gets. No list is ever this type; the name is what appears in
        /// a property store when somebody inspects one.
        /// </summary>
        private sealed class Target
        {
        }

        private static readonly ConditionalWeakTable<ItemsControl, Subscription> SUBSCRIPTIONS =
            new ConditionalWeakTable<ItemsControl, Subscription>();

        private static readonly ConditionalWeakTable<ContentControl, OverlaySubscription> OVERLAY_SUBSCRIPTIONS =
            new ConditionalWeakTable<ContentControl, OverlaySubscription>();

        static SkinTemplates()
        {
            Watcher watcher = new Watcher();
            TileTemplateProperty.Changed.Subscribe(watcher);
            OverlayTemplateProperty.Changed.Subscribe(watcher);
        }

        /// <summary>
        /// The property store's observer shape, for one purpose: when a control gains a key, it gains its deal
        /// with the skins. Avalonia hands observers, not delegates, to whoever asks to watch a property.
        /// </summary>
        private sealed class Watcher : IObserver<AvaloniaPropertyChangedEventArgs<string>>
        {
            public void OnNext(AvaloniaPropertyChangedEventArgs<string> args)
            {
                if (string.IsNullOrEmpty(args.NewValue.GetValueOrDefault()))
                    return;

                // Two kinds of control ask, and the key says nothing about which: a list hands its items over
                // to the skin, a content control hands the one thing it holds over.
                if (args.Sender is ItemsControl list)
                    Attach(list);
                else if (args.Sender is ContentControl overlay)
                    Attach(overlay);
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }
        }

        public static void SetTileTemplate(ItemsControl list, string key)
        {
            list.SetValue(TileTemplateProperty, key);
        }

        public static string GetTileTemplate(ItemsControl list)
        {
            return list.GetValue(TileTemplateProperty);
        }

        public static void SetOverlayTemplate(ContentControl overlay, string key)
        {
            overlay.SetValue(OverlayTemplateProperty, key);
        }

        public static string GetOverlayTemplate(ContentControl overlay)
        {
            return overlay.GetValue(OverlayTemplateProperty);
        }

        private static void Attach(ItemsControl list)
        {
            if (SUBSCRIPTIONS.TryGetValue(list, out Subscription? existing))
            {
                existing.Detach();
                SUBSCRIPTIONS.Remove(list);
            }

            Subscription subscription = new Subscription(list);
            SUBSCRIPTIONS.Add(list, subscription);
            Start(subscription, list);
        }

        /// <summary>
        /// The same deal for a content control's one piece of content. It is a separate subscription and not a
        /// second line in the list's, because the list hands over the tree of every item it holds and this hands
        /// over the tree of the one thing it holds - two Avalonia properties, two kinds of control, and a screen
        /// should not be able to break its menu tiles by wearing a playback bar.
        /// </summary>
        private static void Attach(ContentControl overlay)
        {
            if (OVERLAY_SUBSCRIPTIONS.TryGetValue(overlay, out OverlaySubscription? existing))
            {
                existing.Detach();
                OVERLAY_SUBSCRIPTIONS.Remove(overlay);
            }

            OverlaySubscription subscription = new OverlaySubscription(overlay);
            OVERLAY_SUBSCRIPTIONS.Add(overlay, subscription);
            Start(subscription, overlay);
        }

        /// <summary>
        /// What both kinds of subscription do the moment they are made: listen for the skin going on and off, and
        /// wait for the control to be in the visual tree before applying anything.
        /// </summary>
        /// <remarks>
        /// Applying waits because an attached property is set while the XAML is still being read - before the
        /// control's own template has been assigned - and applying a skin at that moment remembers "null" as the
        /// view's own tree and hands that null back the day the skin comes off, which is the television spelling
        /// out <c>MediaPortal.Frontend.ViewModels.MainMenuEntry</c> where a label should stand. A control that is
        /// already on screen (a code-behind naming its key, say) is whole already and is served at once.
        /// </remarks>
        private static void Start(TemplateSwap subscription, Control target)
        {
            SkinEngine.Changed += subscription.OnSkinChanged;

            if (target.IsAttachedToVisualTree())
                subscription.ApplyNow();
            target.AttachedToVisualTree += delegate { subscription.ApplyNow(); };
            target.DetachedFromVisualTree += delegate { subscription.Detach(); };
        }

        /// <summary>
        /// The half of a subscription's job that every control shares: ask the worn skin for the key's template,
        /// remember the view's own tree the first time a skin answers, and hand it back when the skin stops.
        /// </summary>
        private abstract class TemplateSwap
        {
            private IDataTemplate? _viewTemplate;
            private bool _wearingSkin;

            /// <summary>The key this control asked the skin for, read when the skin changed.</summary>
            protected abstract string Key { get; }

            /// <summary>The template this control draws with right now - the view's own, or a skin's.</summary>
            protected abstract IDataTemplate? Current { get; set; }

            public void ApplyNow()
            {
                IDataTemplate? skin = SkinEngine.TryTileTemplate(Key);
                if (skin != null)
                {
                    if (!_wearingSkin)
                    {
                        // The first swap is the one that has to remember what the view was written with;
                        // skin over skin afterwards only replaces the skin's tree, never the view's.
                        _viewTemplate = Current;
                        _wearingSkin = true;
                    }
                    Current = skin;
                    return;
                }

                if (_wearingSkin)
                {
                    Current = _viewTemplate;
                    _wearingSkin = false;
                }
            }

            public void Detach()
            {
                SkinEngine.Changed -= OnSkinChanged;
            }

            public void OnSkinChanged(object? sender, EventArgs e)
            {
                ApplyNow();
            }
        }

        /// <summary>
        /// One list's standing deal with the skin: remember the view's own template, swap in the skin's while it
        /// answers, swap back when it stops.
        /// </summary>
        private sealed class Subscription : TemplateSwap
        {
            private readonly ItemsControl _list;

            public Subscription(ItemsControl list)
            {
                _list = list;
            }

            protected override string Key
            {
                get { return SkinTemplates.GetTileTemplate(_list); }
            }

            protected override IDataTemplate? Current
            {
                get { return _list.ItemTemplate; }
                set { _list.ItemTemplate = value; }
            }
        }

        /// <summary>
        /// One content control's standing deal with the skin, over its ContentTemplate instead of a list's
        /// ItemTemplate - which is the whole difference, and the reason it is its own class.
        /// </summary>
        private sealed class OverlaySubscription : TemplateSwap
        {
            private readonly ContentControl _overlay;

            public OverlaySubscription(ContentControl overlay)
            {
                _overlay = overlay;
            }

            protected override string Key
            {
                get { return SkinTemplates.GetOverlayTemplate(_overlay); }
            }

            protected override IDataTemplate? Current
            {
                get { return _overlay.ContentTemplate; }
                set { _overlay.ContentTemplate = value; }
            }
        }
    }
}
