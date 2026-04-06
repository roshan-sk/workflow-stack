using Microsoft.EntityFrameworkCore;
using Temporalio.Client;
using Temporalio.Worker;
using Workflow.Application.DTOs;
using Workflow.Application.Interfaces;
using Workflow.Infrastructure.Persistence;
using Workflow.Infrastructure.Services;
using Workflow.Infrastructure.Temporal.Activities;
using Workflow.Infrastructure.Temporal.Workflows;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using DotNetEnv;


using Temporalio.Api.WorkflowService.V1;
using Google.Protobuf.WellKnownTypes;

// Use full namespace to avoid conflict with Temporalio.Client.WorkflowService
using AppWorkflowService = Workflow.Infrastructure.Services.WorkflowService;

using System.Security.Cryptography.X509Certificates;


Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Authorization validation
var publicKeyPem = File.ReadAllText("keys/public_key.pem");
var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem.ToCharArray());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new RsaSecurityKey(rsa),

        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── MySQL ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<WorkflowDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection"))));

// ── Repositories & Services ───────────────────────────────────────
builder.Services.AddScoped<IWorkflowRunRepository, WorkflowRunRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IWorkflowService, AppWorkflowService>();

// ── Workflow Registry ─────────────────────────────────────────────
builder.Services.Configure<List<WorkflowRegistryEntry>>(
    builder.Configuration.GetSection("WorkflowRegistry"));
builder.Services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();

// ── HTTP Client for Activities ────────────────────────────────────
builder.Services.AddHttpClient<IGenericHttpActivityClient, GenericHttpActivityClient>();

// ── Temporal Client ───────────────────────────────────────────────
var temporalAddress   = builder.Configuration["Temporal:Address"]     ?? "localhost:7233";
var temporalNamespace = builder.Configuration["Temporal:Namespace"]   ?? "default";
var caCertPath        = builder.Configuration["Temporal:CaCertPath"];
var clientCertPath    = builder.Configuration["Temporal:ClientCertPath"];
var clientKeyPath     = builder.Configuration["Temporal:ClientKeyPath"];

TemporalClientConnectOptions connectOptions = new(temporalAddress)
{
    Namespace = temporalNamespace
};


if (!string.IsNullOrEmpty(caCertPath) &&
    !string.IsNullOrEmpty(clientCertPath) &&
    !string.IsNullOrEmpty(clientKeyPath))
{
    // var clientCert = X509Certificate2.CreateFromPemFile(clientCertPath, clientKeyPath);

    connectOptions.Tls = new TlsOptions
    {
        ServerRootCACert = File.ReadAllBytes(caCertPath),
        ClientCert       = File.ReadAllBytes(clientCertPath),
        ClientPrivateKey = File.ReadAllBytes(clientKeyPath),
    };
} else
{
    Console.WriteLine("WARNING: Temporal TLS cert paths not fully configured. Connecting without TLS.");
}

// ── TLS Connection Test ───────────────────────────────────────────
TemporalClient temporalClient;
try
{
    temporalClient = await TemporalClient.ConnectAsync(connectOptions);
    Console.WriteLine("Temporal connected successfully over mTLS.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("BrokenPipe") || ex.Message.Contains("stream closed") || ex.Message.Contains("transport error"))
{
    Console.WriteLine("Temporal connection failed: Server rejected plaintext. TLS is required.");
    throw;
}
catch (Exception ex)
{
    Console.WriteLine($"Temporal connection failed: {ex.Message}");
    throw;
}

var workflowService = temporalClient.Connection.WorkflowService;
var listReq = new ListNamespacesRequest();
var listResp = await workflowService.ListNamespacesAsync(listReq);

var existingNamespaces = listResp.Namespaces
    .Select(ns => ns.NamespaceInfo?.Name)
    .Where(n => n is not null)
    .ToList();


bool namespaceExists = existingNamespaces.Contains(temporalNamespace);

if (!namespaceExists)
{
    Console.WriteLine($"Namespace '{temporalNamespace}' not found. Creating...");

    var registerRequest = new RegisterNamespaceRequest
    {
        Namespace = temporalNamespace,
        WorkflowExecutionRetentionPeriod = Duration.FromTimeSpan(TimeSpan.FromDays(3))
    };

    await workflowService.RegisterNamespaceAsync(registerRequest);

    Console.WriteLine($"Namespace '{temporalNamespace}' created successfully.");
}
else
{
    Console.WriteLine($"Namespace '{temporalNamespace}' already exists.");
}

builder.Services.AddSingleton<ITemporalClient>(temporalClient);

// ── Temporal Workers ──────────────────────────────────────────────
builder.Services.AddScoped<GenericWorkflowActivities>();

var app = builder.Build();

// ── Auto-migrate database ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
    await db.Database.MigrateAsync();
}

// ── Start workers dynamically from registry ───────────────────────
var taskQueues = builder.Configuration
    .GetSection("WorkflowRegistry")
    .Get<List<WorkflowRegistryEntry>>()!
    .Select(e => e.TaskQueue)
    .Distinct();

foreach (var taskQueue in taskQueues)
{
    var queue = taskQueue;
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var activities = scope.ServiceProvider
                             .GetRequiredService<GenericWorkflowActivities>();

        using var worker = new TemporalWorker(
            temporalClient,
            new TemporalWorkerOptions(queue)
                .AddWorkflow<GenericApprovalWorkflow>()
                .AddAllActivities(activities));

        await worker.ExecuteAsync(CancellationToken.None);
    });
}

// ── HTTP Pipeline ─────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();