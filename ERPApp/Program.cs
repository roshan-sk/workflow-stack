using Microsoft.EntityFrameworkCore;
using ERPApp.Data;
using ERPApp.Models;
using ERPApp.Services;
using DotNetEnv;

using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Org.BouncyCastle.Ocsp;


Env.Load();

var builder = WebApplication.CreateBuilder(args);

var privateKeyPem = File.ReadAllText("keys/private_key.pem");
var rsa = RSA.Create();
rsa.ImportFromPem(privateKeyPem.ToCharArray());

var host = Environment.GetEnvironmentVariable("EMAIL_HOST");
var portStr = Environment.GetEnvironmentVariable("EMAIL_PORT");
var email = Environment.GetEnvironmentVariable("EMAIL_USER");
var password = Environment.GetEnvironmentVariable("EMAIL_PASS");
var baseUrl = Environment.GetEnvironmentVariable("LOCALHOST_URL");
var notify = Environment.GetEnvironmentVariable("NOTIFY_TO");
var serviceToken = Environment.GetEnvironmentVariable("SERVICE_TOKEN");

if (string.IsNullOrWhiteSpace(baseUrl) ||
    string.IsNullOrWhiteSpace(notify) ||
    string.IsNullOrWhiteSpace(host) ||
    string.IsNullOrWhiteSpace(portStr) ||
    string.IsNullOrWhiteSpace(email) ||
    string.IsNullOrWhiteSpace(password) ||
    string.IsNullOrWhiteSpace(serviceToken))
{
    throw new Exception("Missing something in .env");
}

// SQLite 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=erp.db"));
builder.Services.AddScoped<EmailService>();

// Middleware connetion
builder.Services.AddHttpClient("Middleware", client =>
{
    client.BaseAddress = new Uri("http://localhost:5200/");
});

builder.Services.AddScoped<MiddlewareClient>();


builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    var publicKeyPem = File.ReadAllText("keys/public_key.pem");

    var rsa = RSA.Create();
    rsa.ImportFromPem(publicKeyPem.ToCharArray());

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();
// app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/login-page"));  // redirect /baseUrl to /login page

app.MapGet("/request", async (HttpContext ctx) =>
{
    var html = await File.ReadAllTextAsync("Pages/request.html");  // Calling Pages helper to load html page

    ctx.Response.ContentType = "text/html";
    await ctx.Response.WriteAsync(html);
});


// Store request 
app.MapPost("/start", async (HttpRequest req, AppDbContext db, EmailService emai, MiddlewareClient middlewareClient) =>
{
    var form = await req.ReadFormAsync();

    var r = new Request
    {
        Requestor = form["requestor"]!,
        Item = form["item"]!,
        Price = double.Parse(form["price"].ToString()),
        Description = string.IsNullOrWhiteSpace(form["description"]) ? "NA" : form["description"],
        Status = "PENDING"
    };

    var token = req.Headers["Authorization"].ToString();

    db.Requests.Add(r);
    await db.SaveChangesAsync();

    await middlewareClient.StartWorkflowAsync(r.Id, token); // Calling Middleware, that here request created

    return Results.Content("<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Request Submitted</h2>", "text/html");
}).RequireAuthorization();



app.MapGet("/manager/approve/{id}", async (HttpContext ctx, HttpRequest req, int id, AppDbContext db, MiddlewareClient middlewareClient) =>
{
    var token = req.Headers["Authorization"].ToString();

    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    if (r.Status == "MANAGER_APPROVED")
        return Results.Content("<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Already approved</h2>", "text/html");

    r.Status = "MANAGER_APPROVED";
    await db.SaveChangesAsync();

    await middlewareClient.ManagerDecisionAsync(r.Id, "Approved", token);  // Calling Middleware that ManagerDecision Rejected

    return Results.Content("<h2>Approved successfully</h2>", "text/html");
}).RequireAuthorization();


app.MapGet("/manager/reject/{id}", async (HttpContext ctx, HttpRequest req, int id, AppDbContext db, EmailService email, MiddlewareClient middlewareClient) =>
{
    var token = req.Headers["Authorization"].ToString();

    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    r.Status = "MANAGER_REJECTED";
    await db.SaveChangesAsync();

    await middlewareClient.ManagerDecisionAsync(r.Id, "Rejected", token); // Calling Middleware that ManagerDecision Rejected

    return Results.Content("<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Request has been rejected</h2>", "text/html");
}).RequireAuthorization();


app.MapGet("/finance/approve/{id}", async (HttpContext ctx, HttpRequest req, int id, AppDbContext db, MiddlewareClient middlewareClient) =>
{
    var token = req.Headers["Authorization"].ToString();

    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    if (r.Status == "FINANCE_APPROVED")
    {
        return Results.Content($@"<h2 style='display:flex; justify-content:center; align-items:center;'>Request is already approved</h2><p style='display:flex; justify-content:center; align-items:center;'>Current Status: {r.Status}</p>", "text/html");
    }

    r.Status = "FINANCE_APPROVED";
    await db.SaveChangesAsync();

    await middlewareClient.FinanceDecisionAsync(r.Id, "Approved", token); // Calling Middleware that FinanceDecision Approved

    return Results.Content("<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Request has been approved successfully</h2>", "text/html");
}).RequireAuthorization();


app.MapGet("/finance/reject/{id}", async (HttpContext ctx, HttpRequest req, int id, AppDbContext db, EmailService email, MiddlewareClient middlewareClient) =>
{
    var token = req.Headers["Authorization"].ToString();

    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    r.Status = "FINANCE_REJECTED";
    await db.SaveChangesAsync();

    await middlewareClient.FinanceDecisionAsync(r.Id, "Rejected", token); // Calling Middleware that FinanceDecision Rejected

    return Results.Content("<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Request has been rejected</h2>", "text/html");
}).RequireAuthorization();


app.MapGet("/hr/approve/{id}", async (HttpRequest req, int id, AppDbContext db, MiddlewareClient middlewareClient) =>
{
    var token = req.Headers["Authorization"].ToString();

    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    if (r.Status == "COMPLETED")
    {
        return Results.Content($@"<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Request has already been processed</h2>", "text/html");
    }

    r.Status = "HR_APPROVED";
    await db.SaveChangesAsync();

    await middlewareClient.HrDecisionAsync(r.Id, "Approved", token); // Calling Middleware that HrDecision Approved

    return Results.Content("<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Request has been approved successfully</h2>", "text/html");
}).RequireAuthorization();


app.MapGet("/hr/reject/{id}", async (HttpRequest req, int id, AppDbContext db, EmailService email, MiddlewareClient middlewareClient) =>
{
    var token = req.Headers["Authorization"].ToString();
    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    r.Status = "HR_REJECTED";
    await db.SaveChangesAsync();

    await middlewareClient.HrDecisionAsync(r.Id, "Rejected", token); // Calling Middleware that HrDecision Rejected

    return Results.Content("<h2 style='display:flex; justify-content:center; height: 100vh; align-items:center;'>Request has been rejected</h2>", "text/html");
}).RequireAuthorization();


app.MapPost("/api/purchase-requests/{id}/notify-manager",
    async (HttpRequest req, int id, AppDbContext db, EmailService email) =>
{
    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    var expected = $"Bearer {serviceToken}";

    var token = req.Headers["Authorization"].ToString();

    if (token != expected)
    {
        return Results.Unauthorized();
    }

    var template = await File.ReadAllTextAsync("Pages/notify-manager.html");

    var approveUrl = $"http://localhost:5100/action?requestId={r.Id}&role=manager&type=approve";
    var rejectUrl = $"http://localhost:5100/action?requestId={r.Id}&role=manager&type=reject";

    var body = template
        .Replace("{{Requestor}}", r.Requestor)
        .Replace("{{Item}}", r.Item)
        .Replace("{{Price}}", r.Price.ToString())
        .Replace("{{Description}}", string.IsNullOrWhiteSpace(r.Description) ? "NA" : r.Description)
        .Replace("{{ApproveUrl}}", approveUrl)
        .Replace("{{RejectUrl}}", rejectUrl);


    await email.SendEmailAsync(notify, $"Approval Needed for PR - {r.Id}", body);

    return Results.Ok("Manager notified");
});


app.MapPost("/api/purchase-requests/{id}/notify-finance",
    async (HttpRequest req, int id, AppDbContext db, EmailService email) =>
{
    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    var expected = $"Bearer {serviceToken}";

    var token = req.Headers["Authorization"].ToString();

    var approveUrl = $"http://localhost:5100/action?requestId={r.Id}&role=finance&type=approve";
    var rejectUrl = $"http://localhost:5100/action?requestId={r.Id}&role=finance&type=reject";

    var template = await File.ReadAllTextAsync("Pages/notify-finance.html");

    var body = template
        .Replace("{{Requestor}}", r.Requestor)
        .Replace("{{Item}}", r.Item)
        .Replace("{{Price}}", r.Price.ToString("0.00"))
        .Replace("{{Description}}", string.IsNullOrWhiteSpace(r.Description) ? "NA" : r.Description)
        .Replace("{{ApproveUrl}}", approveUrl)
        .Replace("{{RejectUrl}}", rejectUrl);

    await email.SendEmailAsync(notify, $"Finance Approval Needed for PR - {r.Id}", body);

    return Results.Ok("Finance notified");
});

app.MapPost("/api/purchase-requests/{id}/notify-hr", async (HttpRequest req, int id, AppDbContext db, EmailService email) =>
{
    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    var expected = $"Bearer {serviceToken}";

    var token = req.Headers["Authorization"].ToString();

    if (token != expected)
    {
        return Results.Unauthorized();
    }

    var approveUrl = $"http://localhost:5100/action?requestId={r.Id}&role=hr&type=approve";
    var rejectUrl = $"http://localhost:5100/action?requestId={r.Id}&role=hr&type=reject";

    var template = await File.ReadAllTextAsync("Pages/notify-hr.html");
    var body = template.Replace("{{Requestor}}", r.Requestor)
        .Replace("{{Item}}", r.Item)
        .Replace("{{Price}}", r.Price.ToString("F2"))
        .Replace("{{Description}}", r.Description)
        .Replace("{{ApproveUrl}}", approveUrl)
        .Replace("{{RejectUrl}}", rejectUrl);

    await email.SendEmailAsync(notify, $"HR Approval Needed for PR - {r.Id}", body);

    return Results.Ok("Hr notified");
});


app.MapPost("/api/purchase-requests/{id}/notify-rejection",
    async (int id, HttpRequest req, AppDbContext db, EmailService email) =>
{
    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    var expected = $"Bearer {serviceToken}";

    var token = req.Headers["Authorization"].ToString();

    if (token != expected)
    {
        return Results.Unauthorized();
    }

    var body = $@"
        <h3>Your Purchase Request {r.Id} (ID) was rejected</h3>
        <h4>Purchase Request Details</h4>
        <p>Item Name: {r.Item}</p>
        <p>Item Price: {r.Price}</p>
        <p>Request Description: {r.Description}</p>
    ";

    await email.SendEmailAsync(notify, $"Request Rejected PR - {r.Id}", body);

    return Results.Ok();
});


app.MapPost("/api/purchase-requests/{id}/close",
    async (int id, HttpRequest req, AppDbContext db, EmailService email) =>
{
    var expected = $"Bearer {serviceToken}";

    var token = req.Headers["Authorization"].ToString();

    if (token != expected)
    {
        return Results.Unauthorized();
    }
    var r = await db.Requests.FindAsync(id);
    if (r == null) return Results.NotFound();

    r.Status = "COMPLETED";
    await db.SaveChangesAsync();

    var body = $@"
        <h3>Your Purchase Request {r.Id} (ID) was Completed Successfully</h3>
        <p>Requestor: {r.Requestor}</p>
        <p>Item: {r.Item}</p>
        <p>Price: {r.Price}</p>
        <p>Status: {r.Status}</p>
    ";

    await email.SendEmailAsync(notify, $"Request Completed PR - {r.Id}", body);

    return Results.Ok();
});

app.MapGet("/register-page", async (HttpContext ctx) =>
{
    var html = await File.ReadAllTextAsync("Pages/register.html");
    ctx.Response.ContentType = "text/html";
    await ctx.Response.WriteAsync(html);
});

app.MapPost("/register", async (RegisterModel model, AppDbContext db) =>
{
    // Check if user already exists
    var existingUser = db.Users.FirstOrDefault(u => u.Username == model.Username || u.Email == model.Email);
    if (existingUser != null)
    {
        return Results.BadRequest("User already exists");
    }

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

    var user = new User
    {
        Username = model.Username,
        Email = model.Email,
        Password = hashedPassword,
        Role = model.Role
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok("User registered successfully");
});


app.MapGet("/login-page", async (HttpContext ctx) =>
{
    var requestId = ctx.Request.Query["requestId"];
    var type = ctx.Request.Query["type"];
    var role = ctx.Request.Query["role"];

    var html = await File.ReadAllTextAsync("Pages/login.html");

    ctx.Response.ContentType = "text/html";
    await ctx.Response.WriteAsync(html);
});


app.MapPost("/login", async (LoginModel model, AppDbContext db) =>
{
    var user = db.Users.FirstOrDefault(u => u.Email == model.Email);

    if (user == null)
        return Results.Unauthorized();

    bool isValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);

    if (!isValid)
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim("userId", user.Id.ToString()),
        new Claim("email", user.Email),
        new Claim("role", user.Role!)
    };

    var credentials = new SigningCredentials(
        new RsaSecurityKey(rsa),
        SecurityAlgorithms.RsaSha256);

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(3),
        signingCredentials: credentials);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new
    {
        message = "Login successful",
        userId = user.Id,
        role = user.Role,
        token = jwt
    });
});

app.MapGet("/action", (int requestId, string type, string role) =>
{
    return Results.Redirect($"/login-page?requestId={requestId}&type={type}&role={role}");
});

app.Run();

