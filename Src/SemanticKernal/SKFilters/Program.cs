using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenAI.Graders;
using SKFilters.Filters;
using System.Numerics;
using System.Security.Cryptography;

var builder = Kernel.CreateBuilder();

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var apiKey = config["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token.");

builder.AddOpenAIChatCompletion("openai/gpt-4o-mini", new Uri("https://models.github.ai/inference"), apiKey);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddSingleton<IPromptRenderFilter, SafetyAndLoggingFilter>();
builder.Services.AddSingleton<IFunctionInvocationFilter, SafetyAndLoggingFilter>();
builder.Services.AddSingleton<IFunctionInvocationFilter, TokenCostFilter>();
builder.Services.AddSingleton<IFunctionInvocationFilter, SemanticCacheFilter>();

var kernel = builder.Build();

var result = await kernel.InvokePromptAsync(@"Hi, my name is Rushik Prajapati.
My email is rushik123@gmail.com and phone is 9876543210.
Please check my order ORD-1234.");

var result2 = await kernel.InvokePromptAsync(@"Hi, my name is Rushik Prajapati.
My email is rushik123@gmail.com and phone is 9876543210.
Please check my order ORD-1234.");

Console.WriteLine(result);


