using System.Runtime.CompilerServices;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Runs the generated AddToX/RemoveFromX/InsertIntoX/ReplaceInX/ClearX helpers against the
/// Snapshots/NullableCollections entity: removals and clears tolerate a null collection, adds create
/// it lazily (keeping a [SortedSetComparer]), MarkDirty fires only when the collection changed, and
/// helpers that can be a no-op return whether they changed anything.
/// </summary>
public class DataStructureMethodTests
{
    private static string FixtureSource(string snapshot, [CallerFilePath] string thisFile = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(thisFile)!, "Snapshots", snapshot, "Input.cs"));

    private static string Run(string assemblyName, string source) =>
        (string)SourceGeneratorTestHelper.CompileAndLoad(assemblyName, source)
            .GetType("Server.TestContent.Driver")!
            .GetMethod("Run")!
            .Invoke(null, null)!;

    private const string Driver = """
        namespace Server.TestContent
        {
            public static class Driver
            {
                public static string Run()
                {
                    var item = new NullableCollectionsItem();
                    var log = new System.Text.StringBuilder();
                    void Step(string name, object? state = null) =>
                        log.Append(name).Append('=').Append(item.DirtyCount).Append(state != null ? $":{state}" : "").Append(';');

                    var removed = item.RemoveFromCharges(1) | item.RemoveFromKeywords("a") |
                                  item.RemoveFromLabels(1) | item.RemoveFromNames("a");
                    item.RemoveFromChargesAt(0);
                    item.ClearCharges();
                    item.ClearKeywords();
                    item.ClearLabels();
                    item.ClearNames();
                    Step("null", $"{removed}:{item.Charges == null && item.Keywords == null && item.Labels == null && item.Names == null}");

                    item.AddToCharges(5);
                    Step("addList");
                    item.InsertIntoCharges(0, 4);
                    Step("insert", string.Join(",", item.Charges!));

                    Step("addSet", item.AddToKeywords("a"));
                    Step("addSetDuplicate", item.AddToKeywords("a"));

                    Step("addDictionary", item.AddToLabels(1, "x"));
                    Step("addDictionaryDuplicate", $"{item.AddToLabels(1, "y")}:{item.Labels![1]}");
                    item.ReplaceInLabels(1, null);
                    Step("replace", item.Labels[1] ?? "null");

                    item.AddToNames("Bob");
                    Step("comparer", $"{item.AddToNames("BOB")}:{item.Names!.Count}");

                    Step("removeMissing", item.RemoveFromKeywords("missing"));
                    Step("remove", item.RemoveFromKeywords("a"));
                    Step("removeListMissing", item.RemoveFromCharges(9));
                    Step("removeList", item.RemoveFromCharges(4));
                    Step("removeDictionaryMissing", item.RemoveFromLabels(9));
                    Step("removeDictionary", item.RemoveFromLabels(1));
                    item.ClearKeywords();
                    Step("clearEmpty");
                    item.ClearCharges();
                    Step("clear", item.Charges.Count);

                    var fresh = new NullableCollectionsItem();
                    fresh.ReplaceInLabels(2, "z");
                    log.Append("replaceNull=").Append(fresh.DirtyCount).Append(':').Append(fresh.Labels![2]);

                    return log.ToString();
                }
            }
        }
        """;

    // Without a dirty target the helpers still report whether they changed anything.
    private const string UntrackedSource = """
        using System.Collections.Generic;
        using ModernUO.Serialization;

        namespace Server.TestContent
        {
            [SerializationGenerator(0)]
            public partial class UntrackedRecord
            {
                [SerializableField(0)]
                private List<int> _values = new();

                [SerializableField(1)]
                private HashSet<int> _set = new();

                [SerializableField(2)]
                private Dictionary<int, string> _lookup = new();
            }

            public static class Driver
            {
                public static string Run()
                {
                    var r = new UntrackedRecord();
                    r.AddToValues(1);
                    return string.Join(
                        ",",
                        r.RemoveFromValues(2), r.RemoveFromValues(1),
                        r.AddToSet(1), r.AddToSet(1), r.RemoveFromSet(2), r.RemoveFromSet(1),
                        r.AddToLookup(1, "a"), r.AddToLookup(1, "b"), r.RemoveFromLookup(2), r.RemoveFromLookup(1)
                    );
                }
            }
        }
        """;

    [Fact]
    public void Helpers_TolerateNullAndMarkDirtyOnlyOnChange()
    {
        Assert.Equal(
            "null=0:False:True;addList=1;insert=2:4,5;addSet=3:True;addSetDuplicate=3:False;addDictionary=4:True;" +
            "addDictionaryDuplicate=4:False:x;replace=5:null;comparer=6:False:1;removeMissing=6:False;remove=7:True;" +
            "removeListMissing=7:False;removeList=8:True;removeDictionaryMissing=8:False;removeDictionary=9:True;" +
            "clearEmpty=9;clear=10:0;replaceNull=1:z",
            Run("NullableCollections", FixtureSource("NullableCollections") + Driver)
        );
    }

    [Fact]
    public void Helpers_ReportChangesWithoutADirtyTarget()
    {
        Assert.Equal(
            "False,True,True,False,False,True,True,False,False,True",
            Run("UntrackedRecord", UntrackedSource)
        );
    }
}
