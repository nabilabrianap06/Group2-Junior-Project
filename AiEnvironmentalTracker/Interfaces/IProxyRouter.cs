using Microsoft.AspNetCore.Http;
using AiEnvironmentalTracker.Models;

namespace AiEnvironmentalTracker.Interfaces
{
    public interface IProxyRouter
    {
        UpstreamRoute ResolveRoute(string modelName, HttpRequest request);
    }
}
