using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKPlanner;

var builder = Kernel.CreateBuilder(); 

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var apiKey = config["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token.");

builder.AddOpenAIChatCompletion("openai/gpt-4o-mini", new Uri("https://models.github.ai/inference"), apiKey);

builder.Plugins.AddFromType<InvoicePlugin>("Invoices");

var kernel = builder.Build();

// The agent has a system prompt that defines its role and behaviour
var agent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "InvoiceAgent",
    Instructions = """
        You are an autonomous invoice processing agent.
        When given an invoice ID, you MUST:
        1. Extract the invoice data
        2. Validate the vendor
        3. If approved, post to accounting
        4. Send confirmation to finance@company.com
        Complete ALL steps without asking for confirmation.
    """,
    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
    {
        // AutoInvoke = Planner mode: LLM decides the steps
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    })
};

// One-shot goal - agent plans and executes everything
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var thread = new AgentGroupChat();
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
thread.AddChatMessage(new ChatMessageContent(AuthorRole.User,
    "Process invoice INV-991 end to end"));


await foreach (var msg in thread.InvokeAsync(agent))
{
    Console.WriteLine($"[{msg.Role}]: {msg.Content}");
}


