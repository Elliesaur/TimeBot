using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;

namespace DiscordTimeBot
{
    internal class Program
    {
        #region Fields

        private IReadOnlyDictionary<int, CommandsNextModule> _commands;

        private DiscordShardedClient _shardedClient;

        #endregion

        #region Public Methods

        public async Task RunBotAsync()
        {
            // To invite to your server:
            // https://discordapp.com/oauth2/authorize?client_id=522351514266632216&permissions=2048&scope=bot


            var cfg = new DiscordConfiguration
            {
                Token = File.ReadAllText("token.txt"),
                TokenType = TokenType.Bot,

                AutoReconnect = true,

                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            _shardedClient = new DiscordShardedClient(cfg);
            _shardedClient.GuildAvailable += ShardedClientOnGuildAvailable;

            var ccfg = new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableMentionPrefix = true,
                StringPrefix = "!",
                EnableDms = true,
                EnableDefaultHelp = true,
                IgnoreExtraArguments = false
            };

            _commands = _shardedClient.UseCommandsNext(ccfg);
            foreach (KeyValuePair<int, CommandsNextModule> pair in _commands)
                pair.Value.RegisterCommands<TimeCommands>();

            await _shardedClient.StartAsync();

            await Task.Delay(-1);
        }

        #endregion

        #region Private Methods

        private Task ShardedClientOnGuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, typeof(Program).Namespace,
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