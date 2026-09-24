using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    public enum Mood
    {
        Calm,
        Angry
    }

    [SerializationGenerator(0)]
    public partial struct Stats
    {
        [SerializableField(0)]
        private int _strength;
    }

    // Nullable<T> fields: a HasValue bool, then T through its own rule.
    [SerializationGenerator(1)]
    public partial class NullableValuesItem : ISerializable
    {
        [SerializableField(0)]
        private int? _count;

        [SerializableField(1)]
        [EncodedInt]
        private int? _encoded;

        [SerializableField(2)]
        private Mood? _mood;

        [SerializableField(3)]
        private Point3D? _location;

        [SerializableField(4)]
        private TimeSpan? _cooldown;

        [SerializableField(5)]
        private Stats? _stats;

        [SerializableField(6)]
        private List<int?> _samples;

        [SerializableField(7)]
        private Dictionary<int, double?> _weights;

        [SerializableField(8)]
        [SaveFlag(nameof(ShouldSerializeBonus))]
        private int? _bonus;

        private bool ShouldSerializeBonus() => _bonus != null;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }

        private void MigrateFrom(V0Content content)
        {
            _count = content.Count;
            _bonus = content.Bonus;
        }
    }
}
