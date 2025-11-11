var builder = WebApplication.CreateBuilder(args);

// 1. Получение параметра из командной строки с дефолтным значением
string configCompanyName = GetArgument(args, "--company=", null); // Здесь null, чтобы проверить дальше
builder.Configuration["CompanyName"] = configCompanyName;
string GetArgument(string[] args, string key, string? defaultValue)
{
	foreach (var arg in args)
	{
		if (arg.StartsWith(key))
		{
			var value = arg.Substring(key.Length);
			return string.IsNullOrWhiteSpace(value) ? defaultValue ?? "default-config" : value;
		}
	}
	return defaultValue ?? "default-config";
}
// 2. Singleton-сервис с дефолтным значением
var singletonCompanyName = configCompanyName ?? "default-singleton";
var companySettings = new CompanySettings { CompanyName = singletonCompanyName };
builder.Services.AddSingleton(companySettings);
var app = builder.Build();

// 3. Middleware, если параметр не передан
app.UseMiddleware<CompanyMiddleware>();

app.MapPost("/api/sse-post", (
	HttpContext context,
	IConfiguration config,
	CompanySettings settings) =>
{
	// 1. Получение из IConfiguration (если null, ставим дефолтное)
	string configCompany = config["CompanyName"] ?? "default-config";

	// 2. Получение из Singleton-сервиса
	string singletonCompany = settings.CompanyName;

	// 3. Получение из HttpContext.Items
	string middlewareCompany = context.Items["CompanyName"]?.ToString() ?? "default-middleware";

	return Results.Ok(new
	{
		FromConfiguration = configCompany,
		FromSingleton = singletonCompany,
		FromMiddleware = middlewareCompany
	});
});

app.Run();

// ------------------ Вспомогательные классы ------------------

public class CompanySettings
{
	public string CompanyName { get; set; } = "default-singleton";
}

public class CompanyMiddleware
{
	private readonly RequestDelegate _next;
	private readonly string _companyName;

	public CompanyMiddleware(RequestDelegate next, IConfiguration config)
	{
		_next = next;
		_companyName = config["CompanyName"] ?? "default-middleware";
	}

	public async Task InvokeAsync(HttpContext context)
	{
		context.Items["CompanyName"] = _companyName;
		await _next(context);
	}
}
