using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
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
            var firstStep = new DateTimeStep("What is the date you wish this event to take place on?", secondStep);

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

            var embed = new DiscordEmbedBuilder
            {
                Title = "See below for an example of the data being moved to the database.",
                Description = $"Date/Time: {newEvent.DateTime}\n\nEvent Name: {newEvent.EventName}\n\nAtendees: {newEvent.Attendees}"
            };

            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }
    }
}
