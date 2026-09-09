using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    // Mirrors the spawner shape: an abstract serializable root, a concrete owner that stores
    // a list of base entries, and a derived owner that stores a list of derived entries.
    // Entries are non-ISerializable sub-objects tracked through the root type.
    [SerializationGenerator(0)]
    public abstract partial class BaseOwner : ISerializable
    {
        [SerializableField(0)]
        private string _name;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }

    [SerializationGenerator(0)]
    public partial class Owner : BaseOwner
    {
        [SerializableField(0)]
        private List<BaseEntry> _entries;
    }

    [SerializationGenerator(0)]
    public partial class ExtendedOwner : Owner
    {
        [SerializableField(0)]
        private List<DerivedEntry> _extendedEntries;
    }

    [SerializationGenerator(0)]
    public partial class BaseEntry
    {
        [DirtyTrackingEntity]
        private BaseOwner _owner;

        [SerializableField(0)]
        private string _label;

        [SerializableField(1)]
        private int _weight;

        public BaseEntry(BaseOwner owner) => _owner = owner;

        [AfterDeserialization]
        private void AfterBaseDeserialization()
        {
        }
    }

    // A derived sub-object must chain to the base serialization: its own version and fields
    // follow the base bytes, and the list element constructor resolves through the base chain.
    [SerializationGenerator(0)]
    public partial class DerivedEntry : BaseEntry
    {
        [SerializableField(0)]
        private string _script;

        [SerializableField(1)]
        private bool _disabled;

        public DerivedEntry(BaseOwner owner) : base(owner)
        {
        }

        [AfterDeserialization]
        private void AfterDerivedDeserialization()
        {
        }
    }
}
