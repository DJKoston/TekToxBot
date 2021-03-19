using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using TekTox.Bot.Handlers.Dialogue;
using TekTox.Bot.Handlers.Dialogue.Steps;
using TekTox.Core.Services;
using TekTox.DAL;
using TekTox.DAL.Models;

namespace TekTox.Bot.Commands
{
    public class EventCommands : BaseCommandModule
    {
        private readonly IEventListService _eventService;
        private readonly RPGContext _context;

        public EventCommands(RPGContext context, IEventListService eventService)
        {
            _context = context;
            _eventService = eventService;
        }

        [Command("add")]
        [RequireRoles(RoleCheckMode.Any, "Tokker")]
        public async Task AddEvent(CommandContext ctx)
        {
            var thirdStep = new TextStep("Who will be taking part in this event?", null);
            var secondStep = new TextStep("What is the Event name?", thirdStep);
            var firstStep = new DateTimeStep("What is the date/time you wish this event to take place on?\nStyle of Response Needed: DD/MM/YYYY HH:MM", secondStep);

            var newEvent = new EventList();

            firstStep.OnValidResult += (result) => newEvent.DateTime = $"{result}";
            secondStep.OnValidResult += (result) => newEvent.EventName = $"{result}";
            thirdStep.OnValidResult += (result) => newEvent.Attendees = $"{result}";

            var inputDialogueHandler = new DialogueHandler(
                ctx.Client,
                ctx.Channel,
                ctx.User,
                firstStep
                );

            bool succeeded = await inputDialogueHandler.ProcessDialogue().ConfigureAwait(false);

            if (!succeeded) { return; }

            await _eventService.CreateNewEvent(newEvent).ConfigureAwait(false);

            //Change this to -6 or -7 depending on DST
            var parsedTimeDate = DateTime.Parse(newEvent.DateTime).AddHours(-5);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Event Created:",
                Color = DiscordColor.DarkRed,
                Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {newEvent.EventName}\n\nAttendees: {newEvent.Attendees}",
            };

            embed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-2).ToShortTimeString()}");
            embed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

            embed.WithFooter($"Event ID: {newEvent.Id}");

            var eventChannel = ctx.Guild.GetChannel(808076578725822474);

            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            var eventMessage = await eventChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);

            newEvent.EventChannelId = eventChannel.Id;
            newEvent.EventMessageId = eventMessage.Id;

            await _eventService.EditEvent(newEvent);
        }

        [Command("delete")]
        [RequireRoles(RoleCheckMode.Any, "Tokker")]
        public async Task DeleteEvent(CommandContext ctx, int eventId)
        {
            var selectedEvent = _context.EventLists.Where(x => x.Id == eventId).First();

            var embed = new DiscordEmbedBuilder
            {
                Title = "Event Found:",
                Color = DiscordColor.DarkRed,
                Description = $"Date/Time: {selectedEvent.DateTime}\n\nEvent Name: {selectedEvent.EventName}\n\nAttendees: {selectedEvent.Attendees}",
            };
            embed.WithFooter($"Event ID: {selectedEvent.Id}");

            var confirmMessage = await ctx.Channel.SendMessageAsync("Please confirm you wish to delete this event", embed: embed).ConfigureAwait(false);

            var thumbsupEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
            var thumbsdownEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");

            await confirmMessage.CreateReactionAsync(thumbsupEmoji).ConfigureAwait(false);
            await confirmMessage.CreateReactionAsync(thumbsdownEmoji).ConfigureAwait(false);

            var interactivity = ctx.Client.GetInteractivity();

            var reactionResult = await interactivity.WaitForReactionAsync(x => x.Message == confirmMessage && x.User == ctx.User && (x.Emoji == thumbsupEmoji || x.Emoji == thumbsdownEmoji));

            if(reactionResult.Result.Emoji == thumbsupEmoji)
            {
                await confirmMessage.DeleteAsync();

                await _eventService.DeleteEvent(selectedEvent);

                var eventChannel = ctx.Guild.GetChannel(selectedEvent.EventChannelId);
                var messageToDelete = await eventChannel.GetMessageAsync(selectedEvent.EventMessageId);

                await messageToDelete.DeleteAsync();

                await ctx.Channel.SendMessageAsync("Event Deleted!").ConfigureAwait(false);
            }

            else if(reactionResult.Result.Emoji == thumbsdownEmoji)
            {
                await confirmMessage.DeleteAsync();

                await ctx.Channel.SendMessageAsync("Cancelled deleting the event.").ConfigureAwait(false);
            }
        }

        [Command("edit")]
        [RequireRoles(RoleCheckMode.Any, "Tokker")]
        public async Task EditEvent(CommandContext ctx, int eventId, string itemToEdit)
        {
            var eventToEdit = await _eventService.GetEventByID(eventId);

            if (itemToEdit.ToLower() == "datetime")
            {
                var firstStep = new EditDateTimeStep("What is the date/time you wish this event to take place on?\nStyle of Response Needed: DD/MM/YYYY HH:MM", null);

                firstStep.OnValidResult += (result) => eventToEdit.DateTime = $"{result}";

                var inputDialogueHandler = new DialogueHandler(
                ctx.Client,
                ctx.Channel,
                ctx.User,
                firstStep
                );

                bool succeeded = await inputDialogueHandler.ProcessDialogue().ConfigureAwait(false);

                if (!succeeded) { return; }

                await _eventService.EditEvent(eventToEdit).ConfigureAwait(false);

                //Change this to -6 or -5 depending on DST
                var parsedTimeDate = DateTime.Parse(eventToEdit.DateTime).AddHours(-5);

                var editedEmbed = new DiscordEmbedBuilder
                {
                    Title = "Event Edited:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Event Created:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                embed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-2).ToShortTimeString()}");
                embed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

                embed.WithFooter($"Event ID: {eventToEdit.Id}");

                DiscordEmbed newEmbed = embed;

                var eventChannel = ctx.Guild.GetChannel(eventToEdit.EventChannelId);
                var eventMessage = await eventChannel.GetMessageAsync(eventToEdit.EventMessageId);

                await ctx.Channel.SendMessageAsync(embed: editedEmbed).ConfigureAwait(false);
                await eventMessage.ModifyAsync(embed: newEmbed).ConfigureAwait(false);

                return;
            }

            if (itemToEdit.ToLower() == "name")
            {
                var firstStep = new EditTextStep("What is the Event name?", null);

                firstStep.OnValidResult += (result) => eventToEdit.EventName = $"{result}";

                var inputDialogueHandler = new DialogueHandler(
                ctx.Client,
                ctx.Channel,
                ctx.User,
                firstStep
                );

                bool succeeded = await inputDialogueHandler.ProcessDialogue().ConfigureAwait(false);

                if (!succeeded) { return; }

                await _eventService.EditEvent(eventToEdit).ConfigureAwait(false);

                //Change this to -6 or -5 depending on DST
                var parsedTimeDate = DateTime.Parse(eventToEdit.DateTime).AddHours(-5);

                var editedEmbed = new DiscordEmbedBuilder
                {
                    Title = "Event Edited:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                editedEmbed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-2).ToShortTimeString()}");
                editedEmbed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

                editedEmbed.WithFooter($"Event ID: {eventToEdit.Id}");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Event Created:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                embed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-2).ToShortTimeString()}");
                embed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

                embed.WithFooter($"Event ID: {eventToEdit.Id}");

                DiscordEmbed newEmbed = embed;

                var eventChannel = ctx.Guild.GetChannel(eventToEdit.EventChannelId);
                var eventMessage = await eventChannel.GetMessageAsync(eventToEdit.EventMessageId);

                await ctx.Channel.SendMessageAsync(embed: editedEmbed).ConfigureAwait(false);
                await eventMessage.ModifyAsync(embed: newEmbed).ConfigureAwait(false);

                return;
            }

            if (itemToEdit.ToLower() == "attendees")
            {
                var firstStep = new EditTextStep("Who will be taking part in this event?", null);

                firstStep.OnValidResult += (result) => eventToEdit.Attendees = $"{result}";

                var inputDialogueHandler = new DialogueHandler(
                ctx.Client,
                ctx.Channel,
                ctx.User,
                firstStep
                );

                bool succeeded = await inputDialogueHandler.ProcessDialogue().ConfigureAwait(false);

                if (!succeeded) { return; }

                await _eventService.EditEvent(eventToEdit).ConfigureAwait(false);

                //Change this to -6 or -7 depending on DST
                var parsedTimeDate = DateTime.Parse(eventToEdit.DateTime).AddHours(-5);

                var editedEmbed = new DiscordEmbedBuilder
                {
                    Title = "Event Edited:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                editedEmbed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-2).ToShortTimeString()}");
                editedEmbed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

                editedEmbed.WithFooter($"Event ID: {eventToEdit.Id}");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Event Created:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                embed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-1).ToShortTimeString()}");
                embed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

                embed.WithFooter($"Event ID: {eventToEdit.Id}");

                DiscordEmbed newEmbed = embed;

                var eventChannel = ctx.Guild.GetChannel(eventToEdit.EventChannelId);
                var eventMessage = await eventChannel.GetMessageAsync(eventToEdit.EventMessageId);

                await ctx.Channel.SendMessageAsync(embed: editedEmbed).ConfigureAwait(false);
                await eventMessage.ModifyAsync(embed: newEmbed).ConfigureAwait(false);

                return;
            }

            if (itemToEdit.ToLower() == "all")
            {
                var thirdStep = new EditTextStep("Who will be taking part in this event?", null);
                var secondStep = new EditTextStep("What is the Event name?", thirdStep);
                var firstStep = new EditDateTimeStep("What is the date/time you wish this event to take place on?\nStyle of Response Needed: DD/MM/YYYY HH:MM", secondStep);

                firstStep.OnValidResult += (result) => eventToEdit.DateTime = $"{result}";
                secondStep.OnValidResult += (result) => eventToEdit.EventName = $"{result}";
                thirdStep.OnValidResult += (result) => eventToEdit.Attendees = $"{result}";

                var inputDialogueHandler = new DialogueHandler(
                ctx.Client,
                ctx.Channel,
                ctx.User,
                firstStep
                );

                bool succeeded = await inputDialogueHandler.ProcessDialogue().ConfigureAwait(false);

                if (!succeeded) { return; }

                await _eventService.EditEvent(eventToEdit).ConfigureAwait(false);

                //Change this to -6 or -7 depending on DST
                var parsedTimeDate = DateTime.Parse(eventToEdit.DateTime).AddHours(-5);

                var editedEmbed = new DiscordEmbedBuilder
                {
                    Title = "Event Edited:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                editedEmbed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-2).ToShortTimeString()}");
                editedEmbed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

                editedEmbed.WithFooter($"Event ID: {eventToEdit.Id}");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Event Created:",
                    Color = DiscordColor.DarkRed,
                    Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()} (CST)\n\nEvent Name: {eventToEdit.EventName}\n\nAttendees: {eventToEdit.Attendees}",
                };

                embed.AddField("MST:", $"{parsedTimeDate.AddHours(-2).ToLongDateString()} at {parsedTimeDate.AddHours(-2).ToShortTimeString()}");
                embed.AddField("GMT:", $"{parsedTimeDate.AddHours(5).ToLongDateString()} at {parsedTimeDate.AddHours(5).ToShortTimeString()}");

                embed.WithFooter($"Event ID: {eventToEdit.Id}");

                DiscordEmbed newEmbed = embed;

                var eventChannel = ctx.Guild.GetChannel(eventToEdit.EventChannelId);
                var eventMessage = await eventChannel.GetMessageAsync(eventToEdit.EventMessageId);

                await ctx.Channel.SendMessageAsync(embed: editedEmbed).ConfigureAwait(false);
                await eventMessage.ModifyAsync(embed: newEmbed).ConfigureAwait(false);

                return;
            }
        }
    }
}
