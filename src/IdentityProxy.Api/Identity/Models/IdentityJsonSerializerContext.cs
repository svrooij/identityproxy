// This is needed to support json (de)serialization in a minimal api with AOT support
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace IdentityProxy.Api.Identity.Models;

[JsonSerializable(typeof(DuplicateTokenRequest))]
[JsonSerializable(typeof(OpenIdConfiguration))]
[JsonSerializable(typeof(Jwks))]
[JsonSerializable(typeof(TokenRequest))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(IdentityProxyDescription))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class IdentityJsonSerializerContext : JsonSerializerContext
{

}