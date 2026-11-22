using Lab_01.Data;
using Lab_01.Validation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Allow the Angular dev server (ng serve, http://localhost:4200) to call this API
// from the browser. Without this, browsers block the cross-origin requests.
const string CorsPolicy = "AllowAngularDev";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Register EF Core with SQL Server. The connection string is read from configuration
// (ConnectionStrings:Default). It is supplied at runtime by docker-compose via the
// ConnectionStrings__Default environment variable - never hard-coded in the app.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Apply any pending EF Core migrations on startup so the schema is always up to date.
// SQL Server may take a few seconds to accept connections, so we retry a few times.
await MigrateDatabaseAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Serve the OpenAPI document at /openapi/v1.json ...
    app.MapOpenApi();
    // ... and a Swagger UI at /swagger that reads that document.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Lab-01 API v1"));
}

app.UseHttpsRedirection();

// Apply the CORS policy before the endpoints so cross-origin requests are allowed.
app.UseCors(CorsPolicy);

app.UseAuthorization();

app.MapControllers();

// --- Health endpoints ---

// Liveness: is the process up and serving?
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Readiness: can we actually reach the database? 200 if yes, 503 if not.
app.MapGet("/health/db", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

// --- Task endpoints ---

// List every task, oldest first.
app.MapGet("/api/tasks", async (AppDbContext db) =>
    await db.Tasks.OrderBy(t => t.Id).ToListAsync());

// Fetch one task, or 404 if the id does not exist.
app.MapGet("/api/tasks/{id:int}", async (int id, AppDbContext db) =>
    await db.Tasks.FindAsync(id) is TaskItem task
        ? Results.Ok(task)
        : Results.NotFound());

// Create a task. The title is validated by TaskValidator; invalid input returns 400.
app.MapPost("/api/tasks", async (CreateTaskRequest request, AppDbContext db) =>
{
    var errors = TaskValidator.Validate(request.Title);
    if (errors.Count > 0)
    {
        return Results.BadRequest(new { errors });
    }

    var task = new TaskItem
    {
        Title = request.Title!.Trim(),
        IsDone = false,
        CreatedAt = DateTime.UtcNow
    };

    db.Tasks.Add(task);
    await db.SaveChangesAsync();

    // 201 Created with a Location header pointing at the new resource.
    return Results.Created($"/api/tasks/{task.Id}", task);
});

// Mark a task as done.
app.MapPut("/api/tasks/{id:int}/done", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null)
    {
        return Results.NotFound();
    }

    task.IsDone = true;
    await db.SaveChangesAsync();

    return Results.Ok(task);
});

app.Run();

// Retries Database.Migrate() so the API can start even if SQL Server is not
// ready yet (10 attempts, 3 seconds apart, warning logged each time).
static async Task MigrateDatabaseAsync(WebApplication app)
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(3);

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrated and ready (attempt {Attempt}/{MaxAttempts}).", attempt, maxAttempts);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Database not ready (attempt {Attempt}/{MaxAttempts}). Retrying in {Seconds}s...",
                attempt, maxAttempts, delay.TotalSeconds);

            if (attempt == maxAttempts)
            {
                throw; // Out of retries - fail loudly so the container restarts.
            }

            await Task.Delay(delay);
        }
    }
}

// Request body for POST /api/tasks. Only the title is accepted from the client.
record CreateTaskRequest(string? Title);
