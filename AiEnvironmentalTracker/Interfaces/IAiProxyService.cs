using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AiEnvironmentalTracker.Interfaces
{
    public interface IAiProxyService
    {
        Task HandleChatCompletionsAsync(HttpContext context);
    }
}
