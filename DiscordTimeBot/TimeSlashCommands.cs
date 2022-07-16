using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using NodaTime;
using TinyTime;

namespace DiscordTimeBot
{
    public class TimeSlashCommands : ApplicationCommandModule
    {
        #region Fields

        private static readonly UserTimeDatabase _userTimeDb;

        #endregion

        static TimeSlashCommands()
        {
            _userTimeDb = UserTimeDatabase.FromFile();
        }

        #region Public Methods

        [SlashCommand("time", "Get the time of a city or you!")]
        public async Task TimeByText(
            InteractionContext ctx,
            [Option("query", "The place to get the time, 'Adelaide' or 'Stockholm'. Optionally none if you want your time!")]
            string query = null)
        {

            // Looking up the time could take a little bit. Let's defer as it could take >3s.
            await ctx.DeferAsync();

            if (ctx.Member == null)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Sorry, I can't seem to see who you are, is this a ... DM???")
                );
                return;
            }

            var uid = ctx.Member.Id;
            var msg = query;
            Message formattedMessage = null;

            if (string.IsNullOrEmpty(query) && _userTimeDb.Data.Any(x => x.UID == uid))
            {
                // Get the time of the person callig.
                formattedMessage = new Message(_userTimeDb.Data.FirstOrDefault(x => x.UID == uid).Timezone);
            }
            else if (!string.IsNullOrEmpty(query))
            {
                // Regular old query for time somewhere.
                formattedMessage = new Message("!time " + query);
            }
            else
            {
                await ctx.EditResponseAsync(
                   new DiscordWebhookBuilder().WithContent($"You appear to be trying to get your own time, without having a time! Try /settime first!")
               );
                return;
            }

            var time = GetTime(formattedMessage, out string location);

            if (time == default)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Sorry, I couldn\'t seem to find the time! :(")
                );
                return;
            }

            string t = $"{time.TimeOfDay} on {time.Date}";
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"Currently it is {t} in {location}")
            );

        }

        [SlashCommand("timeuser", "Get the time of a user by mentioning them!")]
        public async Task TimeByUser(
            InteractionContext ctx,
            [Option("user", "The user who you wish to view time of.")]
            DiscordUser user)
        {

            // Looking up the time could take a little bit. Let's defer as it could take >3s.
            await ctx.DeferAsync();

            if (ctx.Member == null)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Sorry, I can't seem to see who you are, is this a ... DM???")
                );
                return;
            }

            Message formattedMessage = null;

            if (!_userTimeDb.Data.Any(x => x.UID == user.Id))
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Sorry, I couldn\'t seem to find the time for that user! :(")
                );
                return;
            }

            // Get the time of the person targeted. No need to prepend "!time"
            formattedMessage = new Message(_userTimeDb.Data.FirstOrDefault(x => x.UID == user.Id).Timezone);

            var time = GetTime(formattedMessage, out string location);

            if (time == default)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Sorry, I couldn\'t seem to find the time! :(")
                );
                return;
            }

            string t = $"{time.TimeOfDay} on {time.Date}";
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"Currently it is {t} in {location}")
            );
        }

        [SlashCommand("settime", "Sets your time to the city provided.")]
        public async Task SetTime(InteractionContext ctx,
            [Option("query", "The city to set your time to, 'Adelaide' or 'Stockholm'.")]
            string query)
        {
            // Setting the time could take a little bit. Let's defer as it could take >3s.
            await ctx.DeferAsync();

            if (string.IsNullOrEmpty(query))
            {
                await ctx.EditResponseAsync(
                      new DiscordWebhookBuilder().WithContent($"You need to give me a city to set your time to, silly! :P")
                  );
                return;
            }

            var formattedMessage = new Message("!settime " + query);
            List<string> args = formattedMessage.Arguments;
            if (args == null || args.Count <= 0)
            {
                await ctx.EditResponseAsync(
                      new DiscordWebhookBuilder().WithContent($"Something funky happened. I sorry :(")
                  );
                return;
            }
            var time = GetTime(formattedMessage, out string location);
            if (time == default)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Can't seem to find that timezone... :/")
                );
                return;
            }

            // Associate with user id.
            // On disk in file
            if (_userTimeDb.Data.Any(x => x.UID == ctx.User.Id))
                _userTimeDb.Data.FirstOrDefault(x => x != null && x.UID == ctx.User.Id).Timezone = "!settime " + query;
            else
                _userTimeDb.Data.Add(new UserTimeData
                {
                    UID = ctx.User.Id,
                    // Legacy support.
                    Timezone = "!settime " + query
                });
            _userTimeDb.ToFile();

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"I've set your time, try /time now!")
            );
            return;
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