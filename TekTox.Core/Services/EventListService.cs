using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TekTox.DAL;
using TekTox.DAL.Models;

namespace TekTox.Core.Services
{
    public interface IEventListService
    {
        Task CreateNewEvent(EventList eventList);
        Task DeleteEvent(EventList eventList);
        List<EventList> ListEvents();
        Task<EventList> GetEvent(DateTime dateTime);
    }
    public class EventListService : IEventListService
    {
        private readonly DbContextOptions<RPGContext> _options;
        public EventListService(DbContextOptions<RPGContext> options)
        {
            _options = options;
        }

        public async Task CreateNewEvent(EventList eventList)
        {
            using var context = new RPGContext(_options);

            await context.AddAsync(eventList).ConfigureAwait(false);

            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task DeleteEvent(EventList eventList)
        {
            using var context = new RPGContext(_options);

            context.Remove(eventList);

            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<EventList> GetEvent(DateTime dateTime)
        {
            using var context = new RPGContext(_options);

            return await context.EventLists.FirstOrDefaultAsync(x => x.DateTime.ToString() == dateTime.ToString());
        }

        public List<EventList> ListEvents()
        {
            using var context = new RPGContext(_options);

            var events = context.EventLists;

            var list = new List<EventList> { };

            foreach (EventList Event in events)
            {
                list.Add(Event);
            }

            return list;
        }
    }
}
