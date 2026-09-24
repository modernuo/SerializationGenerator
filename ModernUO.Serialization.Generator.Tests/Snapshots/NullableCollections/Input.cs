#nullable enable
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    public class CaseInsensitiveComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }

    // Lazily created collections: null until the first AddTo/InsertInto/ReplaceIn.
    [SerializationGenerator(0)]
    public partial class NullableCollectionsItem : ISerializable
    {
        [SerializableField(0)]
        private string? _title;

        [SerializableField(1)]
        private List<int>? _charges;

        [SerializableField(2)]
        private HashSet<string>? _keywords;

        [SerializableField(3)]
        private Dictionary<int, string?>? _labels;

        [SerializableField(4)]
        [SortedSetComparer(typeof(CaseInsensitiveComparer))]
        private SortedSet<string>? _names;

        [SerializableField(5)]
        private string?[]? _aliases;

        public int DirtyCount { get; private set; }

        public void MarkDirty() => DirtyCount++;

        public NullableCollectionsItem() { }

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
