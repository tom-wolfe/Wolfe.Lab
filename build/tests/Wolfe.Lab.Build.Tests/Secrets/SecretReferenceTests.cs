using System.Text.Json;
using Vogen;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Secrets;

public class SecretReferenceTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private sealed record Holder(SecretReference? Token);

    [Fact]
    public void From_KeepsAFullReference()
    {
        SecretReference.From("op://Wolfe.Lab/forgejo-obsidian-token/credential").Value
            .ShouldBe("op://Wolfe.Lab/forgejo-obsidian-token/credential");
    }

    [Fact]
    public void From_TrimsWhatAnEditorLeavesAround()
    {
        SecretReference.From("  op://Wolfe.Lab/item/field\n").Value.ShouldBe("op://Wolfe.Lab/item/field");
    }

    [Theory]
    [InlineData("https://example.com/secret")]
    [InlineData("op://Wolfe.Lab/item")]
    [InlineData("op://Wolfe.Lab//credential")]
    [InlineData("op://Wolfe.Lab/item/field/extra")]
    public void From_RefusesAnythingButAFullReference(string text)
    {
        Should.Throw<ValueObjectValidationException>(() => SecretReference.From(text));
        SecretReference.TryFrom(text, out _).ShouldBeFalse();
    }

    [Fact]
    public void Json_ReadsTheReferenceAsASetting()
    {
        var holder = JsonSerializer.Deserialize<Holder>("""{"token":"op://Wolfe.Lab/item/field"}""", CamelCase);

        holder.ShouldNotBeNull().Token.ShouldNotBeNull().Value.ShouldBe("op://Wolfe.Lab/item/field");
    }

    [Fact]
    public void Json_RefusesABadReferenceAsAJsonError()
    {
        // Ritten reports a JsonException as "could not read ritten.json"; anything else escapes as a crash.
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Holder>("""{"token":"not-a-reference"}""", CamelCase));
    }
}
