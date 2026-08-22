using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial struct PlainStruct
    {
        [SerializableField(0)]
        private int _value;
    }

    [SerializationGenerator(0)]
    public partial record struct WideRecordStruct
    {
        [SerializableField(0)]
        private int _amount;

        [SerializableField(1)]
        private string _label;
    }
}
