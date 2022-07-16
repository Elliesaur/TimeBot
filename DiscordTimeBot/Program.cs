using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace DiscordTimeBot
{
    internal class Program
    {
        #region Fields

        //private IReadOnlyDictionary<int, CommandsNextExtension> _commands;

        private DiscordShardedClient _shardedClient;

        #endregion

        #region Public Methods

        public async Task RunBotAsync()
        {
            // To invite to your server:
            // https://discordapp.com/oauth2/authorize?client_id=522351514266632216&permissions=2048&scope=applications.commands%20bot
            // Additional required commands permission:
            // https://discordapp.com/oauth2/authorize?client_id=522351514266632216&permissions=2048&scope=applications.commands
            // Get them to click above.

            var cfg = new DiscordConfiguration
            {
                Token = File.ReadAllText("token.txt"),
                TokenType = TokenType.Bot,

                AutoReconnect = true,

                MinimumLogLevel = LogLevel.Debug,
            };

            _shardedClient = new DiscordShardedClient(cfg);
            _shardedClient.GuildAvailable += ShardedClientOnGuildAvailable;

            var slash = await _shardedClient.UseSlashCommandsAsync();
            foreach (var pair in slash)
            {
                pair.Value.RegisterCommands<TimeSlashCommands>();
            }

            await _shardedClient.StartAsync();

            await Task.Delay(-1);
        }

        #endregion

        #region Private Methods

        private Task ShardedClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.Log(LogLevel.Information, typeof(Program).Namespace,
                                            $"Guild Available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private static void Main(string[] args)
        {
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        #endregion
    }
}