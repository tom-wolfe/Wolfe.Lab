using System.Text.Json.Serialization;

namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// How the model's answer is read, and the schema it is held to.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ModelEventDetector.Answer))]
internal sealed partial class ModelJson : JsonSerializerContext;
