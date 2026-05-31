using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LocalCalendar.Data
{
    public static class JsonDto
    {
        // --- JSON DTOs ---
        // These exist solely for import/export serialization.
        // Domain models (CalendarItem etc.) are not used directly for JSON.

        [Serializable]
        internal class JsonWrapper
        {
            /*
              !!! On DB-Schema Change !!!
              Update the comment below.

              --- JSON format versions ---

              The JSON format is shared between all implementations of
              LocalCalendar (Android, web, etc.). The format should strive to be as
              out-of-the-box universally compatible as possible (lol). Anyway,
              the format definitions are as follows:

              * Keys in PascalCase
              * Enum values as lowercase strings (not numbers)
              * Datetimes as ISO strings

              v1: Initial format. Fields: Id, Type, Title, Note, StartUtc, EndUtc, AllDay, Color, RepeatRule (Interval, Unit), Reminder (Offset)
              v2: ...

            */

            /* !!! On DB-Schema Change !!!
               Increment CurrentVersion
             */
            public const int CurrentVersion = 1; // Current JSON format version
            public int Version { get; set; } = CurrentVersion;
            public List<JsonItemDto> Items { get; set; }
        }

        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"
        };

        /* !!! On DB-Schema Change !!!
           Update these DTOs
        */
        
        internal class JsonItemDto
        {
            public string Id { get; set; }

            [JsonConverter(typeof(StringEnumConverter), true)] // true = camelCase
            public CalendarItemType Type { get; set; }

            public string Title { get; set; }
            public string Note { get; set; }

            [JsonProperty("StartUtc")]
            public DateTime StartUtc { get; set; }

            [JsonProperty("EndUtc")]
            public DateTime EndUtc { get; set; }

            public bool AllDay { get; set; }

            [JsonConverter(typeof(StringEnumConverter), true)]
            public CalendarItemColor Color { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public JsonRepeatRuleDto RepeatRule { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<JsonReminderDto> Reminders { get; set; }
        }

        internal class JsonRepeatRuleDto
        {
            public int Interval { get; set; }

            [JsonConverter(typeof(StringEnumConverter), true)]
            public RepeatUnit Unit { get; set; }

            [JsonProperty("UntilUtc", NullValueHandling = NullValueHandling.Ignore)]
            public DateTime? UntilUtc { get; set; }
        }

        internal class JsonReminderDto
        {
            public int OffsetSeconds { get; set; }
        }
    }
}
