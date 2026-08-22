using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    public class CaseInsensitiveComparer : IComparer<string>
    {
        public int Compare(string x, string y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }

    [SerializationGenerator(0)]
    public partial class SortedSetItem : ISerializable
    {
        [SerializableField(0)]
        [SortedSetComparer(typeof(CaseInsensitiveComparer))]
        private SortedSet<string> _names;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
