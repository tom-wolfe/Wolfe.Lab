using System.Text.Json.Nodes;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Infrastructure.Declarations;

public class YamlDocumentsTests
{
    private static YamlDocuments.Document Single(string yaml) => YamlDocuments.Parse(yaml).Value.ShouldNotBeNull().Documents.ShouldHaveSingleItem();

    [Fact]
    public void Parse_TypesAPlainScalarAsTheCoreSchemaDoes()
    {
        var root = Single("port: 8081\nratio: 0.5\nenabled: true\nnothing: ~\nempty:\nname: immich\n").Root.ShouldBeOfType<JsonObject>();

        root["port"]!.GetValue<long>().ShouldBe(8081);
        root["ratio"]!.GetValue<double>().ShouldBe(0.5);
        root["enabled"]!.GetValue<bool>().ShouldBeTrue();
        root["nothing"].ShouldBeNull();
        root["empty"].ShouldBeNull();
        root["name"]!.GetValue<string>().ShouldBe("immich");
    }

    [Fact]
    public void Parse_KeepsAQuotedScalarTheStringItWasWrittenAs()
    {
        var root = Single("port: \"8081\"\nflag: 'true'\n").Root.ShouldBeOfType<JsonObject>();

        root["port"]!.GetValue<string>().ShouldBe("8081");
        root["flag"]!.GetValue<string>().ShouldBe("true");
    }

    [Fact]
    public void Parse_ReadsEveryDocumentOfAFile() =>
        YamlDocuments.Parse("kind: backup\n---\nkind: backup\n---\nkind: backup\n").Value.ShouldNotBeNull().Documents.Count.ShouldBe(3);

    [Fact]
    public void LineOf_PointsAtWhereAValueWasWritten()
    {
        var document = Single("kind: service\nlinks:\n  - title: Runbook\n    type: runbook\n");

        document.LineOf("/kind").ShouldBe(1);
        document.LineOf("/links/0/type").ShouldBe(4);
        document.LineOf("/links/0/missing").ShouldBe(3);
    }

    [Fact]
    public void Parse_SaysWhereAFileStopsBeingYaml() =>
        YamlDocuments.Parse("kind: service\nname: [unclosed\n").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("not YAML");
}
