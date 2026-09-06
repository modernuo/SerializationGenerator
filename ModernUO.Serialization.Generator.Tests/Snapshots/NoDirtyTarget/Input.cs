using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    // Neither ISerializable nor [DirtyTrackingEntity]: SG3019 warns, and the generated
    // setters and collection mutators must not emit a bare ';' where MarkDirty would go.
    [SerializationGenerator(0)]
    public partial class NoDirtyTargetRecord
    {
        [SerializableField(0)]
        private string _name;

        [SerializableField(1)]
        private List<int> _values;

        [SerializableField(2)]
        private Dictionary<int, string> _lookup;
    }
}
