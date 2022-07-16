using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
using NodaTime;
using TinyTime;

namespace DiscordTimeBot
{
    public class TimeCommands : BaseCommandModule
    {
        #region Fields

        private static readonly UserTimeDatabase _userTimeDb;

        #endregion

        static TimeCommands()
        {
            _userTimeDb = UserTimeDatabase.FromFile();
        }

        #region Public Methods

        [Command("time")]
        [Description("Get the time of a user, a city or you!")]
        public async Task Time(CommandContext ctx,
                               [RemainingText]
                               [Description(
                                   "The place to get the time, 'Adelaide' or 'Stockholm'. Alternatively the user.")]
                               string query)
        {
            var msg = ctx.Message;
            var formattedMessage = new Message(msg.Content);

            var u = ctx.User;
            if (msg.MentionedUsers.Count > 0) u = msg.MentionedUsers[0];

            if (_userTimeDb.Data.Any(x => x.UID == u.Id) &&
                (formattedMessage.Arguments == null || formattedMessage.Arguments.Count == 0 || u != ctx.User))
                formattedMessage = new Message(_userTimeDb.Data.FirstOrDefault(x => x.UID == u.Id).Timezone);


            List<string> args = formattedMessage.Arguments;

            var time = GetTime(formattedMessage, out string location);

            // Default to ADL
            if (time == default)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{ctx.User.Mention} -> " + "Sorry, I couldn\'t seem to find the time! :(");
                return;
            }


            string t = $"{time.TimeOfDay} on {time.Date}";

            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync($"{ctx.User.Mention} -> " + $"Currently it is {t} " + $"in {location}");
        }

        [Command("settime")]
        [Description("Sets your time to the city provided.")]
        public async Task SetTime(CommandContext ctx,
                                  [RemainingText]
                                  [Description(
                                      "The city to set your time to, 'Adelaide' or 'Stockholm'.")]
                                  string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{ctx.User.Mention} -> " +
                                       "You need to give me a city to set your time to, silly :P");
                return;
            }

            var msg = ctx.Message;
            var formattedMessage = new Message(msg.Content);
            List<string> args = formattedMessage.Arguments;
            if (args == null || args.Count <= 0) return;

            var time = GetTime(formattedMessage, out string location);
            if (time == default)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{ctx.User.Mention} -> I can't seem to find that timezone... :/");
                return;
            }

            // Associate with user id.
            // On disk in file
            if (_userTimeDb.Data.Any(x => x.UID == ctx.User.Id))
                _userTimeDb.Data.FirstOrDefault(x => x != null && x.UID == ctx.User.Id).Timezone = msg.Content;
            else
                _userTimeDb.Data.Add(new UserTimeData
                {
                    UID = ctx.User.Id,
                    Timezone = msg.Content
                });
            _userTimeDb.ToFile();

            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync($"{ctx.User.Mention} -> I set your timezone for you, try !time now.");
        }

        [Command("removetime")]
        [Description("Removes the time associated with your user in our database. Effectively makes you disappear from our infrastructure.")]
        public async Task RemoveTime(CommandContext ctx)
        {
            var u = ctx.User;

            if (_userTimeDb.Data.Any(x => x.UID == u.Id))
            {
                // There's an entry for them in our db.
                // Remove them from the database and update.
                _userTimeDb.Data.RemoveAll(x => x.UID == u.Id);
                _userTimeDb.ToFile();

                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{ctx.User.Mention} -> I've removed you from the time database.");
            }
        }
        #endregion

        #region Private Methods

        private static ZonedDateTime GetTime(Message msg, out string location)
        {
            List<string> args = msg.Arguments;

            if (args == null || args.Count <= 0)
            {
                location = "";
                return default;
            }


            string city = args[0];
            string country = args.Count > 1 ? args[1] : null;
            string combined = string.Join(" ", args);

            var time = TinyTime.TinyTime.TimeIn(out var loc, city, country);

            // Basic swap
            if (time == default) time = TinyTime.TinyTime.TimeIn(out loc, country, city);

            // Combinations
            if (time == default) time = TinyTime.TinyTime.TimeIn(out loc, combined);

            if (time == default) time = TinyTime.TinyTime.TimeIn(out loc, null, combined);

            if (time == default)
            {
                var res = TimeIsAPI.Search(msg.OriginalArguments).FirstOrDefault();
                time = res?.GetZonedDateTime() ?? default;
                location = res?.FriendlyName ?? "Unknown";
            }
            else
            {
                location = loc.ZoneId.Split('/')[1].Replace("_", " ") + ", " + loc.CountryName;
            }

            return time;
        }

        #endregion

        #region Nested type: UserTimeData

        private class UserTimeData
        {
            #region Public Properties

            public ulong UID { get; set; }
            public string Timezone { get; set; }

            #endregion
        }

        #endregion

        #region Nested type: UserTimeDatabase

        private class UserTimeDatabase
        {
            #region Public Properties

            public List<UserTimeData> Data { get; set; }

            #endregion

            #region Public Methods

            public void ToFile()
            {
                File.WriteAllText("time.db", JsonConvert.SerializeObject(this));
            }

            public static UserTimeDatabase FromFile()
            {
                if (File.Exists("time.db"))
                    return JsonConvert.DeserializeObject<UserTimeDatabase>(File.ReadAllText("time.db"));
                return new UserTimeDatabase { Data = new List<UserTimeData>() };
            }

            #endregion
        }

        #endregion
    }
}