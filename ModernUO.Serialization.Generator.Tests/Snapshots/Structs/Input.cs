using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial struct FactoryStruct
    {
        [SerializableField(0)]
        private int _value;

        public static FactoryStruct Deserialize(IGenericReader reader)
        {
            return new FactoryStruct();
        }
    }

    [SerializationGenerator(0)]
    public partial struct InstanceStruct
    {
        [SerializableField(0)]
        private int _value;

        public void Deserialize(IGenericReader reader)
        {
        }
    }
}
