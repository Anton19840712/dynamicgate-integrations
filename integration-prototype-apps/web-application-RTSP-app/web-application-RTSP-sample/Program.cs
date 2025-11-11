using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using WebApplicationRTSPSample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); // For MVC

builder.Services.AddControllers();
builder.Services.Configure<RtspSettings>(builder.Configuration.GetSection("RtspSettings"));

// Register RtspStreamingService as a singleton for injection
builder.Services.AddSingleton<RtspStreamingService>();

// Optionally, register it as a hosted service if you want it to start automatically
builder.Services.AddHostedService(provider => provider.GetRequiredService<RtspStreamingService>());

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll",
		policy => policy.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader());
});

// Add MIME type for .m3u8
var contentTypeProvider = new FileExtensionContentTypeProvider();
if (!contentTypeProvider.Mappings.ContainsKey(".m3u8"))
{
	contentTypeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
}

// Configure static file serving
var staticFileOptions = new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
	RequestPath = "",
	OnPrepareResponse = ctx =>
	{
		ctx.Context.Response.Headers.Append("Cache-Control", "no-cache"); // Optional: for live streaming
	},
	ContentTypeProvider = contentTypeProvider
};

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Serve Index.html automatically
app.UseDefaultFiles(); // <-- Важно: подключается до UseStaticFiles
app.UseStaticFiles(staticFileOptions);

// Routing and authorization
app.UseRouting();
app.UseAuthorization();

// Map controller routes
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // For API endpoints

app.Run();
