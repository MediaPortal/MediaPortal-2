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
        /// Avalonia wants a registrant class for an attached property, and a static class cannot be a type
        /// argument, so this is the quiet one it gets. No list is ever this type; the name is what appears in
        /// a property store when somebody inspects one.
        /// </summary>
        private sealed class Target
        {
        }

        private static readonly ConditionalWeakTable<ItemsControl, Subscription> SUBSCRIPTIONS =
            new ConditionalWeakTable<ItemsControl, Subscription>();

        static SkinTemplates()
        {
            TileTemplateProperty.Changed.Subscribe(new Watcher());
        }

        /// <summary>
        /// The property store's observer shape, for one purpose: when a list gains a key, it gains its deal
        /// with the skins. Avalonia hands observers, not delegates, to whoever asks to watch a property.
        /// </summary>
        private sealed class Watcher : IObserver<AvaloniaPropertyChangedEventArgs<string>>
        {
            public void OnNext(AvaloniaPropertyChangedEventArgs<string> args)
            {
                if (args.Sender is ItemsControl list && !string.IsNullOrEmpty(args.NewValue.GetValueOrDefault()))
                    Attach(list);
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

        private static void Attach(ItemsControl list)
        {
            if (SUBSCRIPTIONS.TryGetValue(list, out Subscription? existing))
            {
                existing.Detach();
                SUBSCRIPTIONS.Remove(list);
            }

            Subscription subscription = new Subscription(list);
            SUBSCRIPTIONS.Add(list, subscription);
            SkinEngine.Changed += subscription.OnSkinChanged;

            // Applying waits for the list to be in the visual tree when it is not there yet: an attached
            // property is set while the XAML is still being read - before the list's own ItemTemplate has
            // been assigned - and applying a skin at that moment remembers "null" as the view's own tree and
            // hands that null back the day the skin comes off, which is the television spelling out
            // `MediaPortal.Frontend.ViewModels.MainMenuEntry` where a label should stand. A list that is
            // already on screen (a code-behind naming its key, say) is whole already and is served at once.
            if (list.IsAttachedToVisualTree())
                subscription.ApplyNow();
            list.AttachedToVisualTree += delegate { subscription.ApplyNow(); };
            list.DetachedFromVisualTree += delegate { subscription.Detach(); };
        }

        /// <summary>
        /// One list's standing deal with the skin: remember the view's own template, swap in the skin's while it
        /// answers, swap back when it stops.
        /// </summary>
        private sealed class Subscription
        {
            private readonly ItemsControl _list;
            private IDataTemplate? _viewTemplate;
            private bool _wearingSkin;

            public Subscription(ItemsControl list)
            {
                _list = list;
            }

            public void ApplyNow()
            {
                IDataTemplate? skin = SkinEngine.TryTileTemplate(SkinTemplates.GetTileTemplate(_list));
                if (skin != null)
                {
                    if (!_wearingSkin)
                    {
                        // The first swap is the one that has to remember what the view was written with;
                        // skin over skin afterwards only replaces the skin's tree, never the view's.
                        _viewTemplate = _list.ItemTemplate;
                        _wearingSkin = true;
                    }
                    _list.ItemTemplate = skin;
                    return;
                }

                if (_wearingSkin)
                {
                    _list.ItemTemplate = _viewTemplate;
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
    }
}
