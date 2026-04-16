using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI;
using SKRag;
using System.ClientModel;
using System.Net;

var builder = Kernel.CreateBuilder();

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var apiKey = config["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token.");

var options = new OpenAIClientOptions()
{
    Endpoint = new Uri("https://models.github.ai/inference")
};

builder.AddOpenAIChatCompletion(
    modelId: "openai/gpt-4o-mini",
    openAIClient: new OpenAIClient(new ApiKeyCredential(apiKey), options)
);

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    new OpenAIClient(new ApiKeyCredential(apiKey), options).GetEmbeddingClient("openai/text-embedding-3-small").AsIEmbeddingGenerator();

var kernel = builder.Build();

var vectorStore = new InMemoryVectorStore();

var policyStore = vectorStore.GetCollection<string, PolicyDocument>("Policy");
await policyStore.EnsureCollectionExistsAsync();

var documents = new[]
{
    new PolicyDocument { Title = "Remote Work",
        Content = "Employees may work remotely up to 3 days per week. " +
                  "A manager approval is required for full-remote arrangements." },
    new PolicyDocument { Title = "Expense Policy",
        Content = "Expenses above $500 require VP approval. " +
                  "Submit receipts within 30 days of incurring the expense." },
};

foreach (var policy in documents)
{
    policy.ContentEmbedding = await embeddingGenerator.GenerateVectorAsync(policy.Content);
    await policyStore.UpsertAsync(policy);
}

// -- RETRIEVAL PHASE (at query time) --
string userQuestion = "Do I need approval to work from home full time?";

// Embed the question - same model must be used for consistency
var questionEmbedding = await embeddingGenerator.GenerateVectorAsync(userQuestion);

var searchResults = policyStore.SearchAsync(questionEmbedding, top: 2);

var context = new System.Text.StringBuilder();
await foreach (var result in searchResults)
    context.AppendLine($"[{result.Record.Title}]: {result.Record.Content}");

Console.WriteLine($"Retrieved context:{context}");

// Ground the LLM answer in retrieved context - prevents hallucination
var answer = await kernel.InvokePromptAsync($"""
    Answer the question using ONLY the context below. If not found, say 'I don't know'.
    Context: {context}
    Question: {userQuestion}
    """);

Console.WriteLine(answer);





