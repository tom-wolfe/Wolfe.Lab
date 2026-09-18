using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Secrets;

public class SecretsFileTests
{
    [Fact]
    public void Parse_ReadsNamesAndReferences()
    {
        var result = SecretsFile.Parse("""
            # The database password.
            DB_PASSWORD="op://Wolfe.Lab/immich-postgres/credential"

            OTHER=op://Wolfe.Lab/item/field
            """, "secrets.env");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Keys.ShouldBe(["DB_PASSWORD", "OTHER"], ignoreOrder: true);
        result.Value["DB_PASSWORD"].Value.ShouldBe("op://Wolfe.Lab/immich-postgres/credential");
    }

    [Fact]
    public void Parse_NamesTheLineOfABadReference()
    {
        var result = SecretsFile.Parse("DB_PASSWORD=\"hunter2\"\nnonsense\n", "secrets.env");

        result.IsError.ShouldBeTrue();
        result.Errors!.Select(e => e.Message).ShouldContain(m => m.StartsWith("secrets.env:1:"));
        result.Errors.Select(e => e.Message).ShouldContain(m => m.StartsWith("secrets.env:2:"));
    }
}
