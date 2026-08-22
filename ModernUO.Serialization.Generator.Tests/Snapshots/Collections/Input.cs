using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class CollectionsItem : ISerializable
    {
        [SerializableField(0)]
        private int[] _levels;

        [SerializableField(1)]
        private List<int> _charges;

        [SerializableField(2)]
        private HashSet<string> _keywords;

        [SerializableField(3)]
        private Dictionary<int, string> _labels;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
