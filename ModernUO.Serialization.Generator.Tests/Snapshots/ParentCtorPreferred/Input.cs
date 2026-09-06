using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class Roster : ISerializable
    {
        [SerializableField(0)]
        private List<RosterEntry> _entries;

        [SerializableField(1)]
        private Dictionary<int, RosterEntry> _byId;

        [SerializableField(2)]
        private RosterEntry _leader;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }

    // The parameterless constructor is declared first; the generator must still pick the
    // parent-accepting one so deserialized entries are linked to their owner.
    [SerializationGenerator(0)]
    public partial class RosterEntry
    {
        [DirtyTrackingEntity]
        private Roster _roster;

        [SerializableField(0)]
        private string _name;

        public RosterEntry()
        {
        }

        public RosterEntry(Roster roster) => _roster = roster;
    }
}
