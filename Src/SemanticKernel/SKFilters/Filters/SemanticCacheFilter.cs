using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKFilters.Filters
{
    public class SemanticCacheFilter(ILogger<SemanticCacheFilter> _logger, IDistributedCache _cache) : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext ctx,
      Func<FunctionInvocationContext, Task> next)
        {
            _logger.LogInformation("[CACHE] Checking cache for function: {Plugin}.{Fn}",
                ctx.Function.PluginName, ctx.Function.Name);

            // Build cache key from function name + arguments hash
            var key = $"sk:{ctx.Function.Name.Split("_")[0]}:{HashArguments(ctx.Arguments)}";
            var cached = await _cache.GetStringAsync(key);

            if (cached is not null)
            {
                // Return cached result - skip the LLM call entirely
                ctx.Result = new FunctionResult(ctx.Function, cached);
                return;
            }

            await next(ctx); // Call LLM

            // Cache the result for 1 hour
            await _cache.SetStringAsync(key, ctx.Result.ToString(),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
        }
        private static string HashArguments(KernelArguments args)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join("|", args.Values))));

    }
}
