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
using System.Reflection;
using System.Text.Json;
using MediaPortal.Common;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The plugins' settings, over the same HTTP API the library is read from: a screen asks which sections stand
  /// open, reads one, and sends the fields it changed back.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this exists beside the hand-written settings endpoints.</b> <see cref="SettingsController"/>,
  /// <see cref="MetadataSettingsController"/> and <see cref="NetworkCredentialsController"/> each know their own
  /// subject: what a living room plausibly changes on the server, what the extractors fetch, what credentials the
  /// shares are read with. Each of them is one endpoint, one screen and one ViewModel, and that is the right price
  /// for a subject worth its own screen. It is the wrong price for the long tail - the plugin's dozen fields that
  /// the reference's desktop configuration tool showed in a window nobody reaches from a sofa. Those become one
  /// endpoint and one screen here: the server says which sections it has and what their fields are called, and the
  /// screen draws whatever it is told.
  /// </para>
  /// <para>
  /// <b>Nothing is editable that is not listed.</b> <see cref="PluginSettingsCatalog"/> names the sections and,
  /// inside them, the fields. A key that is not in the table is refused with the reason, which is also the answer
  /// to a caller that sends a made-up property hoping this endpoint is a shell. Settings classes are reachable
  /// through <see cref="ISettingsManager"/> by type name; that is exactly why the reachable-by-name part stays
  /// behind a table rather than behind a <c>Type.GetType</c> call.
  /// </para>
  /// <para>
  /// <b>What is written where.</b> Reading and writing go through the same
  /// <see cref="ISettingsManager"/> the reference's configuration tool used, so a value changed here is the value
  /// the plugin reads when it next loads its settings - one store, not a copy that drifts. A field whose stored
  /// value the running process does not pick up until it comes back names itself in
  /// <see cref="SectionResult.RestartNeededFor"/>, and only when this request actually moved it.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/media")]
  [MediaPortalAuthorize]
  public class PluginSettingsController : ControllerBase
  {
    /// <summary>
    /// One field as a screen sees it: its words, its kind, what it accepts, and what the server holds now.
    /// </summary>
    public class FieldResult
    {
      public string Key { get; set; }

      public string Label { get; set; }

      public string Help { get; set; }

      /// <summary><c>toggle</c>, <c>number</c> or <c>choice</c>. A screen that does not know the word leaves the
      /// field out rather than drawing a control nothing would read.</summary>
      public string Kind { get; set; }

      public int Min { get; set; }

      public int Max { get; set; }

      /// <summary>The names a choice offers. Empty for every other kind.</summary>
      public List<string> Options { get; set; } = new List<string>();

      /// <summary>
      /// What the server holds: a number for a number, a name for a choice, <c>true</c> or <c>false</c> for a
      /// switch.
      /// </summary>
      public object Value { get; set; }

      public bool RestartNeeded { get; set; }
    }

    /// <summary>One section, its words, and its fields with their current values.</summary>
    public class SectionResult
    {
      public string Key { get; set; }

      public string Title { get; set; }

      public string Description { get; set; }

      public string Owner { get; set; }

      public List<FieldResult> Fields { get; set; } = new List<FieldResult>();

      /// <summary>
      /// The keys this request stored but the running server will only act on after a restart. Empty on a read,
      /// and empty when nothing that needs one was touched.
      /// </summary>
      public List<string> RestartNeededFor { get; set; } = new List<string>();
    }

    /// <summary>What stands open, without the fields - enough for a list.</summary>
    public class SectionSummary
    {
      public string Key { get; set; }

      public string Title { get; set; }

      public string Description { get; set; }

      public string Owner { get; set; }

      public int FieldCount { get; set; }
    }

    /// <summary>The answer to <c>GET api/media/settings/sections</c>.</summary>
    public class SectionsResult
    {
      public List<SectionSummary> Sections { get; set; } = new List<SectionSummary>();
    }

    /// <summary>
    /// What a caller sends: the fields to change, by the key the section named them under. Anything left out
    /// stays as it is, which is what lets a screen change one number without knowing the other six.
    /// </summary>
    public class SectionRequest
    {
      public Dictionary<string, JsonElement> Values { get; set; }
    }

    /// <summary>The body of an answer that refused: the reason, in words a screen can show.</summary>
    public class ErrorResponse
    {
      public string Error { get; set; }
    }

    /// <summary>
    /// The sections a household may open, in the order the server lists them.
    /// </summary>
    [HttpGet("settings/sections")]
    [ProducesResponseType(typeof(SectionsResult), StatusCodes.Status200OK)]
    public ActionResult<SectionsResult> GetSections()
    {
      SectionsResult answer = new SectionsResult();
      foreach (PluginSettingsCatalog.Section section in PluginSettingsCatalog.Sections)
      {
        // A section whose own part of the server is not running is left off the list. The household that
        // switched the reader off does not get a door that opens onto a wall - and the day it is switched back
        // on, the door is there again by itself, because nobody had to be told to add it.
        if (!section.IsOnThisServer)
          continue;

        answer.Sections.Add(new SectionSummary
        {
          Key = section.Key,
          Title = section.Title,
          Description = section.Description,
          Owner = section.Owner,
          FieldCount = section.Fields.Length
        });
      }
      return Ok(answer);
    }

    /// <summary>
    /// One section with its fields and the values the server holds.
    /// </summary>
    /// <returns>
    /// <c>404</c> when no section stands open under that name - which includes every section this server
    /// deliberately does not open, and says so without naming what a stranger could not have guessed anyway.
    /// </returns>
    [HttpGet("settings/sections/{key}")]
    [ProducesResponseType(typeof(SectionResult), StatusCodes.Status200OK)]
    public ActionResult<SectionResult> GetSection(string key)
    {
      PluginSettingsCatalog.Section section = PluginSettingsCatalog.Find(key);
      if (section == null)
        return NotFound(new ErrorResponse { Error = "no settings section is open under \"" + key + "\"" });

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
      if (settingsManager == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      Type settingsType = section.ResolveSettings();
      if (settingsType == null)
        return NotRunning(section);

      try
      {
        return Ok(Of(section, settingsManager.Load(settingsType), null));
      }
      catch (IllegalCallException ex)
      {
        return new ObjectResult(new ErrorResponse { Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
      catch (Exception ex) when (ex is TargetInvocationException || ex is MissingMethodException
          || ex is InvalidOperationException)
      {
        // Either a settings class the running server cannot read - a broken installation, not a caller's
        // mistake - or a row of the settings table naming a property the class has lost. Both are said as the
        // server's own fault, because the person in front of the television can fix neither.
        return new ObjectResult(new ErrorResponse
        {
          Error = ex is InvalidOperationException ? ex.Message
              : "the server cannot read its own \"" + section.Title + "\" settings"
        })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
    }

    /// <summary>
    /// Changes the fields that were sent, stores the section, and answers with the whole section as it now
    /// stands - so the screen that sent a value shows what the server holds rather than what it hoped for.
    /// </summary>
    /// <remarks>
    /// Every key has to be one the section named, and every value has to be of the kind that field is: a
    /// number outside its range, a name a choice does not offer, or a word where a switch was asked for is
    /// refused for the whole request. Saving the good half of a request nobody can account for is not kindness.
    /// </remarks>
    [HttpPut("settings/sections/{key}")]
    [ProducesResponseType(typeof(SectionResult), StatusCodes.Status200OK)]
    public ActionResult<SectionResult> PutSection(string key, [FromBody] SectionRequest request)
    {
      PluginSettingsCatalog.Section section = PluginSettingsCatalog.Find(key);
      if (section == null)
        return NotFound(new ErrorResponse { Error = "no settings section is open under \"" + key + "\"" });

      if (request == null || request.Values == null || request.Values.Count == 0)
        return BadRequest(new ErrorResponse { Error = "no fields were sent" });

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
      if (settingsManager == null)
        return StatusCode(StatusCodes.Status503ServiceUnavailable);

      Type settingsType = section.ResolveSettings();
      if (settingsType == null)
        return NotRunning(section);

      object settings;
      try
      {
        settings = settingsManager.Load(settingsType);
      }
      catch (IllegalCallException ex)
      {
        return new ObjectResult(new ErrorResponse { Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }

      // Checked before anything is written: the whole request is applied or none of it is.
      Dictionary<PluginSettingsCatalog.Field, object> changes =
          new Dictionary<PluginSettingsCatalog.Field, object>();
      foreach (KeyValuePair<string, JsonElement> sent in request.Values)
      {
        PluginSettingsCatalog.Field field = section.FieldFor(sent.Key);
        if (field == null)
          return BadRequest(new ErrorResponse
          {
            Error = "\"" + sent.Key + "\" is not a field of the settings section \"" + key + "\""
          });

        ActionResult<SectionResult> problem = ValueOf(field, sent.Value, out object value);
        if (problem != null)
          return problem;

        changes[field] = value;
      }

      List<string> restartNeededFor = new List<string>();
      bool changed = false;
      try
      {
        foreach (KeyValuePair<PluginSettingsCatalog.Field, object> change in changes)
        {
          PropertyInfo property = settingsType.GetProperty(change.Key.Property);
          if (property == null || !property.CanWrite)
            // The table names a property the class no longer has. That is a stale row, not a caller's fault,
            // and it is said as its own error rather than saved as a value that goes nowhere.
            return BadRequest(new ErrorResponse
            {
              Error = "\"" + change.Key.Key + "\" cannot be written on this server"
            });

          object before = property.GetValue(settings);
          if (Equals(before, change.Value))
            continue;

          property.SetValue(settings, change.Value);
          changed = true;
          if (change.Key.RestartNeeded)
            restartNeededFor.Add(change.Key.Key);
        }

        if (changed)
          settingsManager.Save(settings);

        return Ok(Of(section, settings, restartNeededFor));
      }
      catch (IllegalCallException ex)
      {
        return new ObjectResult(new ErrorResponse { Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
      catch (Exception ex) when (ex is ArgumentException || ex is TargetInvocationException
          || ex is TargetParameterCountException || ex is InvalidOperationException)
      {
        return new ObjectResult(new ErrorResponse
        {
          Error = ex is InvalidOperationException ? ex.Message
              : "the server could not store \"" + section.Title + "\". Nothing was changed"
        })
        {
          StatusCode = StatusCodes.Status500InternalServerError
        };
      }
    }

    /// <summary>
    /// The value one sent field carries, in the type the property behind it wants, or the answer that refuses
    /// the request.
    /// </summary>
    private static ActionResult<SectionResult> ValueOf(PluginSettingsCatalog.Field field, JsonElement sent,
        out object value)
    {
      value = null;

      if (field.Kind == PluginSettingsCatalog.KIND_TOGGLE)
      {
        if (sent.ValueKind != JsonValueKind.True && sent.ValueKind != JsonValueKind.False)
          return Refuse("\"" + field.Key + "\" is a switch. It wants true or false.");
        value = sent.ValueKind == JsonValueKind.True;
        return null;
      }

      if (field.Kind == PluginSettingsCatalog.KIND_NUMBER)
      {
        if (sent.ValueKind != JsonValueKind.Number || !sent.TryGetInt32(out int number))
          return Refuse("\"" + field.Key + "\" wants a whole number.");
        if (number < field.Min || number > field.Max)
          return Refuse("\"" + field.Key + "\" has to be between " + field.Min + " and " + field.Max + ".");
        value = number;
        return null;
      }

      if (field.Kind == PluginSettingsCatalog.KIND_CHOICE)
      {
        if (sent.ValueKind != JsonValueKind.String)
          return Refuse("\"" + field.Key + "\" wants one of the names it was offered.");

        string name = sent.GetString();
        if (!Array.Exists(field.Options, option => string.Equals(option, name, StringComparison.Ordinal)))
          return Refuse("\"" + field.Key + "\" is \"" + name + "\", which is not one of: "
              + string.Join(", ", field.Options) + ".");

        if (field.EnumType == null)
          return Refuse("\"" + field.Key + "\" is offered as a choice, but this server has no list of names to "
              + "write it back as.");

        try
        {
          value = Enum.Parse(field.EnumType, name);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ArgumentNullException)
        {
          // Unreachable while the table's options are the enum's own names, and said rather than assumed,
          // because a table row and a class are free to drift apart.
          return Refuse("\"" + field.Key + "\" could not be read as one of its names (" + ex.Message + ").");
        }
        return null;
      }

      return Refuse("\"" + field.Key + "\" is of a kind this server does not know how to write.");
    }

    /// <summary>
    /// The answer for a section whose own part of the server is not running here. It is not a <c>404</c>,
    /// because the section is open in the catalogue and the caller is not mistaken; it is this server saying it
    /// cannot answer right now, which is what it means.
    /// </summary>
    private static ActionResult<SectionResult> NotRunning(PluginSettingsCatalog.Section section)
    {
      return new ObjectResult(new ErrorResponse
      {
        Error = "this server does not run the part that keeps \"" + section.Title
            + "\", so there is nothing here to change"
      })
      {
        StatusCode = StatusCodes.Status503ServiceUnavailable
      };
    }

    private static ActionResult<SectionResult> Refuse(string reason)
    {
      return new ObjectResult(new ErrorResponse { Error = reason })
      {
        StatusCode = StatusCodes.Status400BadRequest
      };
    }

    /// <summary>
    /// The whole section as it stands in <paramref name="settings"/>, with the keys this request moved and
    /// cannot act on yet.
    /// </summary>
    private static SectionResult Of(PluginSettingsCatalog.Section section, object settings,
        List<string> restartNeededFor)
    {
      SectionResult answer = new SectionResult
      {
        Key = section.Key,
        Title = section.Title,
        Description = section.Description,
        Owner = section.Owner
      };

      foreach (PluginSettingsCatalog.Field field in section.Fields)
      {
        PropertyInfo property = settings.GetType().GetProperty(field.Property);
        if (property == null)
          // A row of the table naming a property the class no longer has. Nothing on the caller's side can fix
          // that, and a blank field on a television is not something the person standing in front of it can
          // report, so the server says out loud what is broken.
          throw new InvalidOperationException("the settings section \"" + section.Key
              + "\" names the property \"" + field.Property + "\", which its settings class does not have");
        object value = property.GetValue(settings);

        FieldResult result = new FieldResult
        {
          Key = field.Key,
          Label = field.Label,
          Help = field.Help,
          Kind = field.Kind,
          Min = field.Min,
          Max = field.Max,
          RestartNeeded = field.RestartNeeded,
          // A choice travels as its name, because a number means nothing to a screen that was told the names
          // and because a name survives a class that inserts a value in the middle of its enum.
          Value = field.Kind == PluginSettingsCatalog.KIND_CHOICE && value != null
              ? value.ToString()
              : value
        };
        if (field.Options.Length > 0)
          result.Options.AddRange(field.Options);
        answer.Fields.Add(result);
      }

      if (restartNeededFor != null)
        answer.RestartNeededFor.AddRange(restartNeededFor);
      return answer;
    }
  }
}
