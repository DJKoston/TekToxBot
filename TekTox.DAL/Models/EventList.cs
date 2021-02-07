using System;
using System.Collections.Generic;
using System.Text;

namespace TekTox.DAL.Models
{
    public class EventList : Entity
    {
        public string DateTime { get; set; }
        public string EventName { get; set; }
        public string Attendees { get; set; }
    }
}
