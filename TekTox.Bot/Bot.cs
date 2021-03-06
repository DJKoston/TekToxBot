﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TekTox.Bot.Commands;
using TekTox.Core.Services;
using TekTox.DAL.Models;

namespace TekTox.Bot
{
    public class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public bool OnTimerEnabled { get; }

        public System.Timers.Timer Timer;

        public Bot(IServiceProvider services, IConfiguration configuration)
        {
            _eventListService = services.GetService<IEventListService>();

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

            Client.Ready += OnClientReady;

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

            Timer = new System.Timers.Timer(60000);

            Timer.Elapsed += OnTimerElapsed;
            Timer.AutoReset = true;
            Timer.Enabled = true;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var allEvents = _eventListService.ListEvents();

            if (allEvents == null) { return; }

            foreach (EventList anEvent in allEvents)
            {
                var membersString = anEvent.Attendees.Replace("<@", "");
                var membersString2 = membersString.Replace("!", "");
                var allMembersString = membersString2.Replace(">", "");

                List<string> memberIdsList = allMembersString.Split(new char[] { ' ' }).ToList();

                List<ulong> ulongList = new List<ulong>();

                foreach (string memberId in memberIdsList)
                {
                    var newMemberId = UInt64.Parse(memberId);

                    ulongList.Add(newMemberId);
                }

                var eventDateTime = anEvent.DateTime;
                var parsedDate = DateTime.Parse(eventDateTime);

                var fiveDayWarningDate = parsedDate.AddDays(-5);
                var oneDayWarningDate = parsedDate.AddDays(-1);
                var tenMinWarning = parsedDate.AddMinutes(-10);

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Time Conversions:",
                    Description = $"{parsedDate.AddHours(-7).ToShortTimeString()} (MST)\n\n{parsedDate.AddHours(-5).ToShortTimeString()} (CST)\n\n{parsedDate.ToShortTimeString()} (GMT)",
                    Color = DiscordColor.Yellow
                };

                if (DateTime.Now.Day == fiveDayWarningDate.Day && DateTime.Now.Month == fiveDayWarningDate.Month && DateTime.Now.Year == fiveDayWarningDate.Year && DateTime.Now.Hour == fiveDayWarningDate.Hour && DateTime.Now.Minute == fiveDayWarningDate.Minute)
                {
                    var guild = Client.GetGuildAsync(804875172157980704).Result;

                    var channel = guild.GetChannel(804875175883178076);

                    channel.SendMessageAsync($"Reminder: {anEvent.Attendees} don't forget!\n\nYou are Scheduled for TekTox recording on {parsedDate.ToLongDateString()} at {parsedDate.AddHours(-5).ToShortTimeString()} (CST) to discuss {anEvent.EventName}.\n\nLooking forward to seeing you there.\n\nDon't forget to give at least 48 hrs notice if you cannot make it.", embed: embed).ConfigureAwait(false);
                }

                if (DateTime.Now.Day == oneDayWarningDate.Day && DateTime.Now.Month == oneDayWarningDate.Month && DateTime.Now.Year == oneDayWarningDate.Year && DateTime.Now.Hour == oneDayWarningDate.Hour && DateTime.Now.Minute == oneDayWarningDate.Minute)
                {
                    var guild = Client.GetGuildAsync(804875172157980704).Result;

                    foreach (ulong ULong in ulongList)
                    {
                        var member = guild.GetMemberAsync(ULong).Result;

                        member.SendMessageAsync($"Reminder: Don't forget!\n\nYou are Scheduled for TekTox recording tomorrow at {parsedDate.AddHours(-5).ToShortTimeString()} (CST) to discuss {anEvent.EventName}.\n\nLooking forward to seeing you there.\n\nYou should have already have given us your notice if you cannot make it by this point.", embed: embed).ConfigureAwait(false);
                    }
                }

                if (DateTime.Now.Day == tenMinWarning.Day && DateTime.Now.Month == tenMinWarning.Month && DateTime.Now.Year == tenMinWarning.Year && DateTime.Now.Hour == tenMinWarning.Hour && DateTime.Now.Minute == tenMinWarning.Minute)
                {
                    var guild = Client.GetGuildAsync(804875172157980704).Result;

                    var channel = guild.GetChannel(804875175883178076);

                    foreach (ulong ULong in ulongList)
                    {
                        var member = guild.GetMemberAsync(ULong).Result;

                        member.SendMessageAsync($"Reminder: Don't forget!\n\nYou are Scheduled for TekTox recording in 10 mins to discuss {anEvent.EventName}.\n\nLooking forward to seeing you there.\n\nIf you can't make it, please DM Dude ASAP.").ConfigureAwait(false);
                    }

                    channel.SendMessageAsync($"Reminder: We are recording a TekTox episode in 10 minutes to discuss {anEvent.EventName}.\n\nAll attendee's have been DM'ed\n\nIf you can't make it or if you wish to take part all of a sudden, please DM Dude ASAP.").ConfigureAwait(false);
                }

                if (DateTime.Now > parsedDate)
                {
                    Console.WriteLine($"Event Passed: {anEvent.EventName}");

                    _eventListService.DeleteEvent(anEvent);

                    var guild = Client.GetGuildAsync(804875172157980704).Result;
                    var eventChannel = guild.GetChannel(anEvent.EventChannelId);
                    var messageToDelete = eventChannel.GetMessageAsync(anEvent.EventMessageId).Result;

                    messageToDelete.DeleteAsync();

                    Console.WriteLine($"Event Deleted: {anEvent.EventName}");
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"Timer Elapsed @ {DateTime.Now}");
            Console.ResetColor();
        }

        private async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            await Client.UpdateStatusAsync(new DiscordActivity
            {
                ActivityType = ActivityType.Watching,
                Name = "for tech news!",
            }, UserStatus.Online);
        }

        private readonly IEventListService _eventListService;


    }
}
