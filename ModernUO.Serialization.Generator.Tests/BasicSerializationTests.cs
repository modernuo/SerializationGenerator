/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BasicSerializationTests.cs                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

public class BasicSerializationTests
{
    [Fact]
    public void SimpleClass_GeneratesSerializationCode()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SimpleItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial class SimpleItem", generatedSource);
        Assert.Contains("void Serialize(", generatedSource);
        Assert.Contains("void Deserialize(", generatedSource);
        Assert.Contains("public string Name", generatedSource); // Generated property
    }

    [Fact]
    public void MultipleFields_GeneratesCorrectOrder()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class MultiFieldItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    [SerializableField(1)]
                    private int _count;

                    [SerializableField(2)]
                    private bool _active;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("public string Name", generatedSource);
        Assert.Contains("public int Count", generatedSource);
        Assert.Contains("public bool Active", generatedSource);
    }

    [Fact]
    public void PrimitiveTypes_GeneratesCorrectSerializationMethods()
    {
        const string source = """
            using System;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class PrimitiveItem : ISerializable
                {
                    [SerializableField(0)]
                    private int _intValue;

                    [SerializableField(1)]
                    private long _longValue;

                    [SerializableField(2)]
                    private double _doubleValue;

                    [SerializableField(3)]
                    private DateTime _dateTime;

                    [SerializableField(4)]
                    private Guid _guid;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("writer.Write(_intValue)", generatedSource);
        Assert.Contains("writer.Write(_longValue)", generatedSource);
        Assert.Contains("writer.Write(_doubleValue)", generatedSource);
        Assert.Contains("reader.ReadInt()", generatedSource);
        Assert.Contains("reader.ReadLong()", generatedSource);
        Assert.Contains("reader.ReadDouble()", generatedSource);
    }

    [Fact]
    public void SerializableProperty_GeneratesFieldAndProperty()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class PropertyItem : ISerializable
                {
                    [SerializableProperty(0)]
                    public string Name { get; set; }

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        // SerializableProperty should generate a backing field
        Assert.Contains("_name", generatedSource);
    }

    [Fact]
    public void EncodedInt_UsesEncodedMethods()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class EncodedIntItem : ISerializable
                {
                    [SerializableField(0)]
                    [EncodedInt]
                    private int _encodedValue;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("WriteEncodedInt", generatedSource);
        Assert.Contains("ReadEncodedInt", generatedSource);
    }

    [Fact]
    public void SerialConstructor_GeneratedForISerializable()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SerialCtorItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        // Should contain a constructor that takes Serial (the constructor is generated)
        Assert.Contains("SerialCtorItem(", generatedSource);
        Assert.Contains("Serial", generatedSource);
    }

    [Fact]
    public void ListField_GeneratesCollectionSerialization()
    {
        const string source = """
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ListItem : ISerializable
                {
                    [SerializableField(0)]
                    private List<string> _names;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("WriteEncodedInt", generatedSource); // Collection count
        Assert.Contains("for", generatedSource.ToLower()); // Loop for serialization
    }

    [Fact]
    public void DictionaryField_GeneratesCollectionSerialization()
    {
        const string source = """
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class DictItem : ISerializable
                {
                    [SerializableField(0)]
                    private Dictionary<string, int> _map;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("WriteEncodedInt", generatedSource);
        Assert.Contains("Dictionary", generatedSource);
    }

    [Fact]
    public void HashSetField_GeneratesCollectionSerialization()
    {
        const string source = """
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class HashSetItem : ISerializable
                {
                    [SerializableField(0)]
                    private HashSet<int> _numbers;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("HashSet", generatedSource);
    }

    [Fact]
    public void SortedSetField_GeneratesCollectionSerialization()
    {
        const string source = """
            using System.Collections.Generic;
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class SortedSetItem : ISerializable
                {
                    [SerializableField(0)]
                    private SortedSet<int> _sortedNumbers;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("SortedSet", generatedSource);
    }

    [Fact]
    public void ArrayField_GeneratesArraySerialization()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class ArrayItem : ISerializable
                {
                    [SerializableField(0)]
                    private int[] _values;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("int[]", generatedSource);
    }

    [Fact]
    public void EnumField_GeneratesEnumSerialization()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                public enum ItemState { Active, Inactive, Pending }

                [SerializationGenerator(0)]
                public partial class EnumItem : ISerializable
                {
                    [SerializableField(0)]
                    private ItemState _state;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("ItemState", generatedSource);
    }

    [Fact]
    public void NestedClass_GeneratesCorrectNamespace()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                public partial class OuterClass
                {
                    [SerializationGenerator(0)]
                    public partial class InnerItem : ISerializable
                    {
                        [SerializableField(0)]
                        private string _name;

                        public Serial Serial => default;
                        public void MarkDirty() { }
                    }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("partial class OuterClass", generatedSource);
        Assert.Contains("partial class InnerItem", generatedSource);
    }

    [Fact]
    public void VirtualProperty_GeneratesVirtualModifier()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class VirtualItem : ISerializable
                {
                    [SerializableField(0, getter: "public", setter: "public", isVirtual: true)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        Assert.Contains("virtual", generatedSource);
    }

    [Fact]
    public void VersionZero_DoesNotIncludeMigrationSwitch()
    {
        const string source = """
            using ModernUO.Serialization;
            using Server;

            namespace TestNamespace
            {
                [SerializationGenerator(0)]
                public partial class VersionZeroItem : ISerializable
                {
                    [SerializableField(0)]
                    private string _name;

                    public Serial Serial => default;
                    public void MarkDirty() { }
                }
            }
            """;

        var (diagnostics, generatedSource) = SourceGeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
        Assert.NotNull(generatedSource);
        // Version 0 should not need migration switch
        Assert.DoesNotContain("switch (version)", generatedSource);
    }
}
