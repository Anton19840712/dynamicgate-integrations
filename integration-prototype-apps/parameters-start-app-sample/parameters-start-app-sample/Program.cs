using servers_api.validation.headers;

public class Program
{
	public static void Main(string[] args)
	{
		// Загружаем настройки из launchSettings.json и environment variables
		var builder = WebApplication.CreateBuilder(args);

		// Получаем параметры из командной строки
		var port = GetArgument(args, "--port=", GetConfigValue(builder, "applicationUrl", 5000));
		var host = GetArgument(args, "--host=", "localhost");
		var enableValidation = GetArgument(args, "--validate=", false);

		// Добавляем сервисы
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddAuthorization();
		builder.Services.AddControllers();

		// Если валидация включена, регистрируем сервис валидации
		//var enableValidation1 = true;
		if (enableValidation)
		{
			builder.Services.AddScoped<SimpleHeadersValidator>();
			builder.Services.AddScoped<DetailedHeadersValidator>();
			Console.WriteLine("Валидация включена");
		}
		else
		{
			Console.WriteLine("Валидация отключена");
		}

		var app = builder.Build();

		app.UseAuthorization();
		app.MapControllers();

		// Устанавливаем URL с переданными параметрами
		var url = $"http://{host}:{port}";
		app.Urls.Add(url);

		Console.WriteLine($"Сервер запущен на {url}");

		app.Run();
	}

	// Метод для парсинга аргументов командной строки
	private static T GetArgument<T>(string[] args, string key, T defaultValue)
	{
		var arg = args.FirstOrDefault(a => a.StartsWith(key));
		if (arg != null)
		{
			var value = arg.Substring(key.Length);
			try
			{
				return (T)Convert.ChangeType(value, typeof(T));
			}
			catch
			{
				Console.WriteLine($"Ошибка при разборе аргумента {key}. Используется значение по умолчанию: {defaultValue}");
			}
		}
		return defaultValue;
	}

	// Метод для получения значения из конфигурации (например, из launchSettings.json)
	private static T GetConfigValue<T>(WebApplicationBuilder builder, string key, T defaultValue)
	{
		var configValue = builder.Configuration[key];
		if (configValue != null)
		{
			try
			{
				return (T)Convert.ChangeType(configValue, typeof(T));
			}
			catch
			{
				Console.WriteLine($"Ошибка при разборе конфигурации по ключу {key}. Используется значение по умолчанию: {defaultValue}");
			}
		}
		return defaultValue;
	}
}
