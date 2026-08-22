using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class UlongFlagsItem : ISerializable
    {
        [SerializableField(0)]
        private string _field0;

        [SerializableFieldSaveFlag(0)]
        private bool ShouldSerializeField0() => _field0 != null;

        [SerializableField(1)]
        private string _field1;

        [SerializableFieldSaveFlag(1)]
        private bool ShouldSerializeField1() => _field1 != null;

        [SerializableField(2)]
        private string _field2;

        [SerializableFieldSaveFlag(2)]
        private bool ShouldSerializeField2() => _field2 != null;

        [SerializableField(3)]
        private string _field3;

        [SerializableFieldSaveFlag(3)]
        private bool ShouldSerializeField3() => _field3 != null;

        [SerializableField(4)]
        private string _field4;

        [SerializableFieldSaveFlag(4)]
        private bool ShouldSerializeField4() => _field4 != null;

        [SerializableField(5)]
        private string _field5;

        [SerializableFieldSaveFlag(5)]
        private bool ShouldSerializeField5() => _field5 != null;

        [SerializableField(6)]
        private string _field6;

        [SerializableFieldSaveFlag(6)]
        private bool ShouldSerializeField6() => _field6 != null;

        [SerializableField(7)]
        private string _field7;

        [SerializableFieldSaveFlag(7)]
        private bool ShouldSerializeField7() => _field7 != null;

        [SerializableField(8)]
        private string _field8;

        [SerializableFieldSaveFlag(8)]
        private bool ShouldSerializeField8() => _field8 != null;

        [SerializableField(9)]
        private string _field9;

        [SerializableFieldSaveFlag(9)]
        private bool ShouldSerializeField9() => _field9 != null;

        [SerializableField(10)]
        private string _field10;

        [SerializableFieldSaveFlag(10)]
        private bool ShouldSerializeField10() => _field10 != null;

        [SerializableField(11)]
        private string _field11;

        [SerializableFieldSaveFlag(11)]
        private bool ShouldSerializeField11() => _field11 != null;

        [SerializableField(12)]
        private string _field12;

        [SerializableFieldSaveFlag(12)]
        private bool ShouldSerializeField12() => _field12 != null;

        [SerializableField(13)]
        private string _field13;

        [SerializableFieldSaveFlag(13)]
        private bool ShouldSerializeField13() => _field13 != null;

        [SerializableField(14)]
        private string _field14;

        [SerializableFieldSaveFlag(14)]
        private bool ShouldSerializeField14() => _field14 != null;

        [SerializableField(15)]
        private string _field15;

        [SerializableFieldSaveFlag(15)]
        private bool ShouldSerializeField15() => _field15 != null;

        [SerializableField(16)]
        private string _field16;

        [SerializableFieldSaveFlag(16)]
        private bool ShouldSerializeField16() => _field16 != null;

        [SerializableField(17)]
        private string _field17;

        [SerializableFieldSaveFlag(17)]
        private bool ShouldSerializeField17() => _field17 != null;

        [SerializableField(18)]
        private string _field18;

        [SerializableFieldSaveFlag(18)]
        private bool ShouldSerializeField18() => _field18 != null;

        [SerializableField(19)]
        private string _field19;

        [SerializableFieldSaveFlag(19)]
        private bool ShouldSerializeField19() => _field19 != null;

        [SerializableField(20)]
        private string _field20;

        [SerializableFieldSaveFlag(20)]
        private bool ShouldSerializeField20() => _field20 != null;

        [SerializableField(21)]
        private string _field21;

        [SerializableFieldSaveFlag(21)]
        private bool ShouldSerializeField21() => _field21 != null;

        [SerializableField(22)]
        private string _field22;

        [SerializableFieldSaveFlag(22)]
        private bool ShouldSerializeField22() => _field22 != null;

        [SerializableField(23)]
        private string _field23;

        [SerializableFieldSaveFlag(23)]
        private bool ShouldSerializeField23() => _field23 != null;

        [SerializableField(24)]
        private string _field24;

        [SerializableFieldSaveFlag(24)]
        private bool ShouldSerializeField24() => _field24 != null;

        [SerializableField(25)]
        private string _field25;

        [SerializableFieldSaveFlag(25)]
        private bool ShouldSerializeField25() => _field25 != null;

        [SerializableField(26)]
        private string _field26;

        [SerializableFieldSaveFlag(26)]
        private bool ShouldSerializeField26() => _field26 != null;

        [SerializableField(27)]
        private string _field27;

        [SerializableFieldSaveFlag(27)]
        private bool ShouldSerializeField27() => _field27 != null;

        [SerializableField(28)]
        private string _field28;

        [SerializableFieldSaveFlag(28)]
        private bool ShouldSerializeField28() => _field28 != null;

        [SerializableField(29)]
        private string _field29;

        [SerializableFieldSaveFlag(29)]
        private bool ShouldSerializeField29() => _field29 != null;

        [SerializableField(30)]
        private string _field30;

        [SerializableFieldSaveFlag(30)]
        private bool ShouldSerializeField30() => _field30 != null;

        [SerializableField(31)]
        private string _field31;

        [SerializableFieldSaveFlag(31)]
        private bool ShouldSerializeField31() => _field31 != null;

        [SerializableField(32)]
        private string _field32;

        [SerializableFieldSaveFlag(32)]
        private bool ShouldSerializeField32() => _field32 != null;

        [SerializableField(33)]
        private string _field33;

        [SerializableFieldSaveFlag(33)]
        private bool ShouldSerializeField33() => _field33 != null;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
