using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKFilters.Filters
{
    public class TokenCostFilter(ILogger<TokenCostFilter> _logger) : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext ctx, Func<FunctionInvocationContext, Task> next)
        {
            _logger.LogInformation("[COST] Starting function: {Plugin}.{Fn}",
                ctx.Function.PluginName, ctx.Function.Name);

            var startTime = DateTime.UtcNow;

            // Execute actual LLM / function
            await next(ctx);

            var endTime = DateTime.UtcNow;

            // Extract metadata (depends on connector)
            var metadata = ctx.Result?.Metadata;

            if (metadata != null &&
                metadata.TryGetValue("Usage", out var usageObj) &&
                usageObj is not null)
            {
                dynamic usage = usageObj;

                int promptTokens = usage.InputTokenCount;
                int completionTokens = usage.OutputTokenCount;
                int totalTokens = usage.TotalTokenCount;

                // Example pricing (GPT-4o approx)
                double cost = (promptTokens * 0.00001) +
                              (completionTokens * 0.00003);

                _logger.LogInformation($@"
                Token Usage:
                Prompt: {promptTokens}
                Completion: {completionTokens}
                Total: {totalTokens}
                Cost (approx): ${cost}
            ");
            }
        }
    }
}
