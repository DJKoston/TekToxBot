using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TekTox.Bot.Commands;

namespace TekTox.Bot
{
    public class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public CommandsNextExtension Commands { get; private set; }

        public Bot(IServiceProvider services, IConfiguration configuration)
        {
            var token = configuration["token"];
            var prefix = configuration["prefix"];

            var config = new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug,
            };

            Client = new DiscordClient(config);

            Client.Heartbeated += OnHeartbeat;

            var botVersion = typeof(Bot).Assembly.GetName().Version.ToString();

            Console.WriteLine($"Current Bot Version: {botVersion}");

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(5)
            });

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { prefix },
                EnableDms = true,
                EnableMentionPrefix = true,
                Services = services,
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.RegisterCommands<EventCommands>();

            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Connecting to Discord...");
            Console.ResetColor();
            Client.ConnectAsync();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Connected to Discord!");
            Console.ResetColor();
        }

        private Task OnHeartbeat(DiscordClient sender, HeartbeatEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}
