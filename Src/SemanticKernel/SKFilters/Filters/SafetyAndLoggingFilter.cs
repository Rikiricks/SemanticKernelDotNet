using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SKFilters.Filters
{
    // IPromptRenderFilter fires BEFORE the prompt is sent to the LLM
    // IFunctionInvocationFilter fires BEFORE/AFTER any function/plugin call

    public class SafetyAndLoggingFilter : IPromptRenderFilter, IFunctionInvocationFilter
    {
        private readonly ILogger<SafetyAndLoggingFilter> _logger;

        public SafetyAndLoggingFilter(ILogger<SafetyAndLoggingFilter> logger)
        {
            _logger = logger;
        }        

        // FUNCTION INVOCATION FILTER: wraps every plugin/function call
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext ctx, Func<FunctionInvocationContext, Task> next)
        {

            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[INVOKE] Plugin={Plugin} Function={Fn}",
                ctx.Function.PluginName, ctx.Function.Name);
            try
            {
                await next(ctx); // Execute the actual function
                sw.Stop();
                _logger.LogInformation("[INVOKE] Completed in {Ms}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[INVOKE] Failed: {Plugin}.{Fn}",
                    ctx.Function.PluginName, ctx.Function.Name);
                throw;
            }

        }

        public async Task OnPromptRenderAsync(PromptRenderContext ctx, Func<PromptRenderContext, Task> next)
        {
            _logger.LogInformation("[PRE-LLM] Function: {Name}", ctx.Function.Name);

            // Execute prompt rendering
            await next(ctx);

            // PII Check - inspect rendered prompt after variable substitution
            if (ctx.RenderedPrompt is not null)
            {
                string prompt = ctx.RenderedPrompt;
                // Email
                prompt = Regex.Replace(prompt,
                    @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-z]{2,}",
                    "[REDACTED_EMAIL]");

                // Phone (India)
                prompt = Regex.Replace(prompt,
                    @"\b[6-9]\d{9}\b",
                    "[REDACTED_PHONE]");

                // Credit Card
                prompt = Regex.Replace(prompt,
                    @"\b\d{4}[- ]?\d{4}[- ]?\d{4}[- ]?\d{4}\b",
                    "[REDACTED_CARD]");

                // Password
                prompt = Regex.Replace(prompt,
                    @"(?i)password\s*[:=]?\s*\S+",
                    "password=[REDACTED]");

                // Aadhaar (India)
                prompt = Regex.Replace(prompt,
                    @"\b\d{4}\s\d{4}\s\d{4}\b",
                    "[REDACTED_AADHAAR]");

                ctx.RenderedPrompt = prompt;
            }

        }
    }
}
