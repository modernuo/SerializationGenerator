using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class FieldModifiersItem : ISerializable
    {
        [SerializableField(0)]
        [CanBeNull]
        private string _description;

        [SerializableField(1)]
        [Tidy]
        private Dictionary<int, string> _entries;

        [SerializableField(2)]
        [InvalidateProperties]
        private int _level;

        [SerializableFieldChanged(2)]
        private void OnLevelChanged(int oldValue, int newValue)
        {
        }

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
        public void InvalidateProperties() { }
    }
}
