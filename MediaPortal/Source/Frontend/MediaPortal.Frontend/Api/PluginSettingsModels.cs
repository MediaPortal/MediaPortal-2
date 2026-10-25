using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.Api;

/// <summary>
/// The three plugin-settings calls of <see cref="ServerApiClient"/> on their own, so a screen model can
/// be handed a stub.
/// </summary>
public interface IPluginSettingsApi
{
    /// <param name="area">
    /// <c>"media"</c> for the answers about the library's own pictures, <c>"plugin"</c> for what a plugin can
    /// be told, <c>null</c> for both. The reference kept the two apart in its configuration tree, so a screen
    /// asks for the half it shows.
    /// </param>
    Task<SettingSectionsAnswer> GetSettingSectionsAsync(string? area = null,
        CancellationToken cancellation = default);

    Task<SettingSectionState> GetSettingSectionAsync(string key, CancellationToken cancellation = default);

    Task<SettingSectionState> PutSettingSectionAsync(string key, SettingSectionWish wish,
        CancellationToken cancellation = default);
}

/// <summary>
/// One value the server lets a screen change, together with everything needed to draw its row.
/// </summary>
/// <remarks>
/// The server decides what a field is called, what it does, and which of the three kinds of row it
/// wants - the client does not know what a media library can be told, the server does. A field whose
/// kind no screen knows how to draw is a server speaking a word the client has never heard;
/// <see cref="SettingSectionViewModelKinds"/> lists the words both sides agreed on.
/// </remarks>
public sealed record SettingFieldState
{
    /// <summary>How the field is named in the change; never shown.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>What the row is called, as the server's own people call it.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>The sentence under the row. Allowed to be empty.</summary>
    public string Help { get; init; } = string.Empty;

    /// <summary>One of <see cref="SettingSectionViewModelKinds"/>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Smallest number accepted, when the field is a number.</summary>
    public int? Min { get; init; }

    /// <summary>Largest number accepted, when the field is a number.</summary>
    public int? Max { get; init; }

    /// <summary>The names a choice offers, in the order the server stores them.</summary>
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The value as the server holds it now: true or false for a switch, a whole number for a number, a
    /// name for a choice. Kept as the raw JSON because only the server knows which of the three arrives.
    /// </summary>
    public JsonElement Value { get; init; }

    /// <summary>Whether the server would have to start again for a new value of this field to take effect.</summary>
    public bool RestartNeeded { get; init; }
}

/// <summary>The three kinds of row a settings screen has to be able to draw.</summary>
public static class SettingSectionViewModelKinds
{
    public const string Toggle = "toggle";
    public const string Number = "number";
    public const string Choice = "choice";

    /// <summary>
    /// A word the viewer types, for the answers that are words rather than numbers - a country's two letters,
    /// for instance. Empty is one of its answers, which is exactly why it is not a choice: nothing labels a
    /// button that stands for "nothing in particular".
    /// </summary>
    public const string Text = "text";
}

/// <summary>The two halves of the server's settings, as the reference split them in its configuration tree.</summary>
public static class SettingSectionAreas
{
    /// <summary>What the library's media itself answers: its pictures and where they come from.</summary>
    public const string Media = "media";

    /// <summary>What a plugin answers: what a reader reads, what the server logs.</summary>
    public const string Plugin = "plugin";
}

/// <summary>One screen's worth of settings: what it is called and the values it holds.</summary>
public sealed record SettingSectionState
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;

    /// <summary>Which screen this section belongs to, as <see cref="SettingSectionAreas"/>.</summary>
    public string Area { get; init; } = string.Empty;
    public IReadOnlyList<SettingFieldState> Fields { get; init; } = Array.Empty<SettingFieldState>();

    /// <summary>Which of the fields the server would only act on after a restart.</summary>
    public IReadOnlyList<string> RestartNeededFor { get; init; } = Array.Empty<string>();
}

/// <summary>One row of the list of screens: enough to offer the entry without opening it.</summary>
public sealed record SettingSectionSummary
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;

    /// <summary>Which screen this section belongs to, as <see cref="SettingSectionAreas"/>.</summary>
    public string Area { get; init; } = string.Empty;
    public int FieldCount { get; init; }
}

/// <summary>What the server lets a screen change at all.</summary>
public sealed record SettingSectionsAnswer
{
    public IReadOnlyList<SettingSectionSummary> Sections { get; init; } = Array.Empty<SettingSectionSummary>();
}

/// <summary>The change to send: only the fields that arrive are touched.</summary>
public sealed record SettingSectionWish
{
    /// <summary>
    /// Field key to new value - a bool for a switch, an int for a number, a name for a choice. Anything
    /// else in here is refused by the server with its reason, and nothing is stored.
    /// </summary>
    public Dictionary<string, object> Values { get; init; } = new Dictionary<string, object>();
}
