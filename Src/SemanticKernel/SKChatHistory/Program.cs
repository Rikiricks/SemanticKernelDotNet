using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using System.Text.Json;

var builder = Kernel.CreateBuilder();

IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var apiKey = config["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token.");

builder.AddOpenAIChatCompletion("openai/gpt-4o-mini", new Uri("https://models.github.ai/inference"), apiKey);
builder.Services.AddDistributedMemoryCache(); // For demo purposes; replace with Redis/SQL/CosmosDB in production
var kernel = builder.Build();


#region Basic Multi-Turn with ChatHistory
var chatService = kernel.GetRequiredService<IChatCompletionService>();

// ChatHistory acts as the 'memory' of the conversation
// System message sets the assistant's persona and rules
ChatHistory history = new ChatHistory(systemMessage: """
    You are a B2B software sales assistant for CloudSuite ERP.
    Remember user preferences throughout the conversation.
    Always reference what the user told you earlier when relevant.
    Be concise but personable.
""");

// Simulating a multi-turn conversation:
async Task<string> ChatAsync(string userMessage)
{
    history.AddUserMessage(userMessage);

    var response = await chatService.GetChatMessageContentAsync(history);

    // Add assistant response to history - crucial for context continuity
    history.AddAssistantMessage(response.Content!);

    return response.Content!;
}

Console.WriteLine(await ChatAsync("Hi, I'm looking for an ERP with strong inventory management."));
// Assistant: "Great! CloudSuite ERP has a real-time inventory module with..."

Console.WriteLine(await ChatAsync("Our budget is around $2000/month for 50 users."));
// Assistant: "With $2000/month for 50 users, the Professional tier at $38/user..."

Console.WriteLine(await ChatAsync("Does it integrate with QuickBooks?"));
// Assistant: "Yes, and given your inventory focus we mentioned earlier, the QB
//             integration syncs stock levels automatically..."  ← uses earlier context!
#endregion


#region Token Budget Management (Production Essential)
// Automatically truncates history when it gets too long
// Preserves: system message, most recent N messages
// Removes: oldest messages (except system) when budget is exceeded
var reducer = new ChatHistoryTruncationReducer(
    targetCount: 3,    // Keep last 3 messages
    thresholdCount: 4  // Start trimming after 4 messages
);

async Task<string> ChatWithMemoryManagement(string userMessage)
{
    history.AddUserMessage(userMessage);

    // Reduce history if it's getting too long (non-destructive - returns new list)
    var reducedHistory = await reducer.ReduceAsync(history);
    
    var historyToUse = reducedHistory is not null ? new ChatHistory(reducedHistory) : history; // Fallback to full history if reduction fails for some reason
    var response = await chatService.GetChatMessageContentAsync(historyToUse);
    history.AddAssistantMessage(response.Content!);

    return response.Content!;
}
Console.WriteLine(await ChatWithMemoryManagement("Hi, I'm looking for an ERP with strong inventory management."));
// Assistant: "Great! CloudSuite ERP has a real-time inventory module with..."

Console.WriteLine(await ChatWithMemoryManagement("Our budget is around $2000/month for 50 users."));

#endregion


# region - Persisting Chat History (Session Continuity)
// Serialize history to JSON for storage in Redis/SQL/CosmosDB
var json = JsonSerializer.Serialize(history.Select(m => new Microsoft.SemanticKernel.ChatMessageContent
{
    Role = m.Role,
    Content = m.Content
}));

var cache = kernel.GetRequiredService<IDistributedCache>();
var sessionId = "user-Ricks";
await cache.SetStringAsync($"chat:{sessionId}", json);

var cachedJson = await cache.GetStringAsync($"chat:{sessionId}");

if (cachedJson is not null)
{
    var cachedHistory = JsonSerializer.Deserialize<List<Microsoft.SemanticKernel.ChatMessageContent>>(cachedJson);
    // Restore the chat history
    var restoredHistory = new ChatHistory();

    if (history.Count > 0)
    {
        Console.WriteLine("Restored chat history:");
        foreach (var msg in history)
        {
            if(msg.Role == AuthorRole.User)
            {
                restoredHistory.AddUserMessage(msg.Content);
            }
            else if(msg.Role == AuthorRole.Assistant)
            {
                restoredHistory.AddAssistantMessage(msg.Content);
            }
            else if(msg.Role == AuthorRole.System)
            {
                restoredHistory.AddSystemMessage(msg.Content);
            }
        }
    }
}
#endregion