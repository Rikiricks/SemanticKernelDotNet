using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKPlugin;

var builder = Kernel.CreateBuilder();

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// get credentials from user secrets
var apiKey = config["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token.");

builder.AddOpenAIChatCompletion("openai/gpt-4o-mini", new Uri("https://models.github.ai/inference"), apiKey);

builder.Plugins.AddFromType<OrderPlugin>();


var kernal = builder.Build();


// Enable function calling for the kernel, so it can automatically call our plugin methods when relevant to the user's prompt
var settings = new OpenAIPromptExecutionSettings
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
};


var result = await kernal.InvokePromptAsync("Hi! My order ORD-1234 seems delayed. Can you check it and if it's an issue, create a ticket for John Smith?",
    new KernelArguments(settings));


Console.WriteLine(result);
