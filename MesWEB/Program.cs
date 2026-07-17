using System.Text;
using MesWEB.Components;
using MesWEB.Logging;
using MesWEB.Shared.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Increase logging for SignalR/Connections/Blazor circuits to gather diagnostics from iOS Safari
// Use Debug level only in Development; use Information in non-development to keep logs moderate
var defaultLogLevel = builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information;
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", defaultLogLevel);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", defaultLogLevel);
builder.Logging.AddFilter("Microsoft.AspNetCore.Components.Server.Circuits", defaultLogLevel);

// File logger: write to ./logs with configured level (Debug in Development, Information otherwise)
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(Directory.GetCurrentDirectory(), "logs"), defaultLogLevel));

// Register code pages encoding provider so ExcelDataReader can read legacy .xls encodings
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// appsettings.json のみを使用（環境別ファイル・環境変数は使用しない）
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services to the container.
var interactiveServerBuilder = builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
        // Increase JSInterop timeout to allow large language download during initialization
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(5);
        options.MaxBufferedUnacknowledgedRenderBatches = 10;

        // iOS Safari 対応: 詳細なエラーメッセージを有効化（開発環境）
        options.DetailedErrors = true; // iOS デバッグのため常に有効化
    });

// SignalR Hub Options (iOS Safari 対応)
interactiveServerBuilder.AddHubOptions(options =>
{
    options.EnableDetailedErrors = true; // iOS デバッグのため常に有効化
    options.MaximumReceiveMessageSize = 256 * 1024; // 256KB に増加（iOS 対応）
    options.HandshakeTimeout = TimeSpan.FromSeconds(60); // iOS Safari でタイムアウトを長めに
    options.KeepAliveInterval = TimeSpan.FromSeconds(10); // より頻繁に接続確認
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120); // より長いタイムアウト
    options.StreamBufferCapacity = 32;
    options.MaximumParallelInvocationsPerClient = 1; // iOS の同時実行制限に対応
});

// DeviceDetectionService をスコープドサービスとして登録
builder.Services.AddScoped<DeviceDetectionService>();

// データベース設定
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("接続文字列 'Default' が appsettings.json に設定されていません。");

var applyMigrations = builder.Configuration.GetValue<bool>("ApplyMigrations", false);

Console.WriteLine($"=== Database Configuration ===");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Connection String: {connectionString}");
Console.WriteLine($"ApplyMigrations: {applyMigrations}");
Console.WriteLine($"==============================");

builder.Services.AddDbContextFactory<MesWEB.Shared.Data.AppDbContext>(options =>
  options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure())
);

var app = builder.Build();

// ルートアクセス時は PathBase 配下へリダイレクト（Circuit host not initialized 対策）
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/NewApp/", permanent: false);
        return;
    }

    await next();
});

app.UsePathBase("/NewApp");

// 環境情報をログ出力
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("=== Environment Information ===");
startupLogger.LogInformation("Environment Name: {EnvironmentName}", app.Environment.EnvironmentName);

startupLogger.LogInformation("ApplyMigrations: {ApplyMigrations}", applyMigrations);

if (applyMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MesWEB.Shared.Data.AppDbContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("データベース接続を確認しています...");

            await using var db = await dbFactory.CreateDbContextAsync();

            var canConnect = await Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                return await db.Database.CanConnectAsync(cts.Token);
            });

            if (!canConnect)
            {
                logger.LogWarning("データベースに接続できません。マイグレーションをスキップします。");
            }
            else
            {
                logger.LogInformation("SQL Serverマイグレーションを適用しています...");

                await Task.Run(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    await db.Database.MigrateAsync(cts.Token);
                });

                logger.LogInformation("SQL Serverマイグレーションの適用が完了しました");
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("データベース初期化がタイムアウトしました。");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "データベースの初期化中にエラーが発生しました。");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseDeveloperExceptionPage();
    // app.UseHsts(); // HTTP運用のため無効化（HSTSはブラウザにhttps強制を記憶させてしまう）
}

// iOS 対応: HTTP でアクセスする場合は HTTPS リダイレクトを無効化
// app.UseHttpsRedirection(); // ← コメントアウト

// Enable WebSocket middleware with a shorter keep-alive to help iOS Safari connections
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(10) });

// Middleware: log incoming negotiate/_blazor requests for diagnostics
app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString();
    if (path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/negotiate", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_blazor/negotiate", StringComparison.OrdinalIgnoreCase))
    {
        var lg = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var ua = context.Request.Headers["User-Agent"].ToString();
        var remote = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        lg.LogInformation("Incoming request {Method} {Path} from {RemoteIp} UA:{UserAgent}", context.Request.Method, context.Request.Path + context.Request.QueryString, remote, ua);
    }

    await next();
});

app.UseAntiforgery();

// 静的ファイルのリクエストをログに記録
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/test-simple.html") ||
        context.Request.Path.StartsWithSegments("/camera-test.html"))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Static file request: {Path}", context.Request.Path);
    }
    await next();
});

// 静的ファイルの配信を有効化（HTMLファイルなど）
app.UseStaticFiles();

// app.MapStaticAssets(); // POST が 405 になる調査中のため一旦無効化

// Endpoint to receive simple client-side logs (from iOS device via fetch/sendBeacon)
app.MapPost("/client-log", async (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    try
    {
        using var sr = new StreamReader(context.Request.Body);
        var body = await sr.ReadToEndAsync();
        logger.LogInformation("Client log: {Body}", body);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to read client log");
    }
    return Results.Ok();
});

// RCLプロジェクトのアセンブリを明示的に追加
var additionalAssemblies = new List<System.Reflection.Assembly>
{
    typeof(MesWEB.ExcelCompare.Components.Pages.ExcelCompare).Assembly,
    typeof(MesWEB.GrowthNote.Components.Pages.GrowthNoteV2).Assembly,
};

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(additionalAssemblies.ToArray());

// 起動時に主要エンドポイントをログ出力（_blazor / client-log の有無確認）
var endpointDataSources = app.Services.GetServices<EndpointDataSource>().ToList();
startupLogger.LogInformation("EndpointDataSource count: {Count}", endpointDataSources.Count);

foreach (var dataSource in endpointDataSources)
{
    foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>())
    {
        var rawText = endpoint.RoutePattern.RawText ?? string.Empty;
        var displayName = endpoint.DisplayName ?? string.Empty;

        var isTarget = rawText.Contains("_blazor", StringComparison.OrdinalIgnoreCase)
            || rawText.Contains("client-log", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("blazor", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("client-log", StringComparison.OrdinalIgnoreCase);

        if (!isTarget)
        {
            continue;
        }

        var methodMetadata = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>();
        var methods = methodMetadata is null ? "(any)" : string.Join(",", methodMetadata.HttpMethods);
        startupLogger.LogInformation(
            "Mapped endpoint: Pattern={Pattern} DisplayName={DisplayName} Methods={Methods}",
            rawText,
            displayName,
            methods);
    }
}

app.Run();
