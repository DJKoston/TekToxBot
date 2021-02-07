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

            var parsedTimeDate = DateTime.Parse(newEvent.DateTime).AddHours(-6);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Event Created:",
                Color = DiscordColor.DarkRed,
                Description = $"Date/Time: {parsedTimeDate.ToShortDateString()} at {parsedTimeDate.ToShortTimeString()}\n\nEvent Name: {newEvent.EventName}\n\nAttendees: {newEvent.Attendees}",
            };
            embed.WithFooter($"Event ID: {newEvent.Id}");

            var eventChannel = ctx.Guild.GetChannel(808076578725822474);

            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            await eventChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        [Command("delete")]
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

                await ctx.Channel.SendMessageAsync("Event Deleted!").ConfigureAwait(false);
            }

            else if(reactionResult.Result.Emoji == thumbsdownEmoji)
            {
                await confirmMessage.DeleteAsync();

                await ctx.Channel.SendMessageAsync("Cancelled deleting the event.").ConfigureAwait(false);
            }
        }
    }
}
