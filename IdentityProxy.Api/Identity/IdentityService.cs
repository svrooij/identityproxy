using Microsoft.Extensions.Caching.Memory;
namespace IdentityProxy.Api.Identity;

public class IdentityService
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityService> _logger;
    
    
}
