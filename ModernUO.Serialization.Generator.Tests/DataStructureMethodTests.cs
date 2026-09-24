using System.Runtime.CompilerServices;
using ModernUO.Serialization.Generator.Tests.Helpers;
using Xunit;

namespace ModernUO.Serialization.Generator.Tests;

/// <summary>
/// Runs the generated AddToX/RemoveFromX/InsertIntoX/ReplaceInX/ClearX helpers against the
/// Snapshots/NullableCollections entity: removals and clears tolerate a null collection, adds create
/// it lazily (keeping a [SortedSetComparer]), and MarkDirty fires only when the collection changed.
/// </summary>
public class DataStructureMethodTests
{
    private static string FixtureSource([CallerFilePath] string thisFile = "") =>
        File.ReadAllText(
            Path.Combine(Path.GetDirectoryName(thisFile)!, "Snapshots", "NullableCollections", "Input.cs")
        );

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

                    item.RemoveFromCharges(1);
                    item.RemoveFromChargesAt(0);
                    item.ClearCharges();
                    item.RemoveFromKeywords("a");
                    item.ClearKeywords();
                    item.RemoveFromLabels(1);
                    item.ClearLabels();
                    item.RemoveFromNames("a");
                    item.ClearNames();
                    Step("null", item.Charges == null && item.Keywords == null && item.Labels == null && item.Names == null);

                    item.AddToCharges(5);
                    Step("addList");
                    item.InsertIntoCharges(0, 4);
                    Step("insert", string.Join(",", item.Charges!));

                    item.AddToKeywords("a");
                    Step("addSet");
                    item.AddToKeywords("a");
                    Step("addSetDuplicate");

                    item.AddToLabels(1, "x");
                    Step("addDictionary");
                    item.AddToLabels(1, "y");
                    Step("addDictionaryDuplicate", item.Labels![1]);
                    item.ReplaceInLabels(1, null);
                    Step("replace", item.Labels[1] ?? "null");

                    item.AddToNames("Bob");
                    item.AddToNames("BOB");
                    Step("comparer", item.Names!.Count);

                    item.RemoveFromKeywords("missing");
                    Step("removeMissing");
                    item.RemoveFromKeywords("a");
                    Step("remove");
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

    [Fact]
    public void Helpers_TolerateNullAndMarkDirtyOnlyOnChange()
    {
        var assembly = SourceGeneratorTestHelper.CompileAndLoad("NullableCollections", FixtureSource() + Driver);

        var result = (string)assembly.GetType("Server.TestContent.Driver")!
            .GetMethod("Run")!
            .Invoke(null, null)!;

        Assert.Equal(
            "null=0:True;addList=1;insert=2:4,5;addSet=3;addSetDuplicate=3;addDictionary=4;" +
            "addDictionaryDuplicate=4:x;replace=5:null;comparer=6:1;removeMissing=6;remove=7;" +
            "clearEmpty=7;clear=8:0;replaceNull=1:z",
            result
        );
    }
}
