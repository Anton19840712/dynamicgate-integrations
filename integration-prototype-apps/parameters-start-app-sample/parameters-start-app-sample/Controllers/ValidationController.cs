using Microsoft.AspNetCore.Mvc;
using servers_api.validation.headers;

[ApiController]
[Route("api/validation")]
public class ValidationController : ControllerBase
{
	private readonly IHeadersValidator _simpleValidationService;
	private readonly IHeadersValidator _detailedValidationService;
	private readonly ILogger<ValidationController> _logger;

	public ValidationController(IServiceProvider serviceProvider, ILogger<ValidationController> logger)
	{
		try
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			// Получаем конкретные реализации по типам
			_simpleValidationService = serviceProvider.GetService<SimpleHeadersValidator>();
			if (_simpleValidationService == null)
			{
				_logger.LogWarning("SimpleHeadersValidator не был зарегистрирован.");
			}

			_detailedValidationService = serviceProvider.GetService<DetailedHeadersValidator>();
			if (_detailedValidationService == null)
			{
				_logger.LogWarning("DetailedHeadersValidator не был зарегистрирован.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Ошибка при инъекции сервисов валидации: {ex.Message}");
			_simpleValidationService = null;
			_detailedValidationService = null;
		}
	}

	[HttpGet("check")]
	public async Task<IActionResult> CheckValidation()
	{
		// Получаем текущие заголовки из HttpContext
		var context = HttpContext;

		// Если ни один валидатор headers не был зарегистрирован(мы не указали необходимость валидации заголовков), то мы продолжаем дальнейшие выполнения запросов данного enpoint без учета валидации headers.
		if (_simpleValidationService == null && _detailedValidationService == null)
		{
			_logger.LogWarning("Сервисы валидации headers не были зарегистрированы, выполнение будет продолжено без валидации headers.");
			// какая-то логика сохранения данных в шину или базу для приземления.
		}
		else
		{
			// если же у нас была подключена валидация заголовков (означает мы в параметрах запуска динамического шлюза это указали и они были зарегистрированы)
			// выбираем валидатор в зависимости от переданного в запросе заголовка:

			var useDetailedValidation = context.Request.Headers.ContainsKey("X-Use-Detailed-Validation");
			IHeadersValidator selectedValidator = useDetailedValidation ? _detailedValidationService : _simpleValidationService;

			// Выполняем валидацию headers, так как мы указали при запуске динамического шлюза, что они должны учитываться:
			var validationResponse = selectedValidator?.ValidateHeaders(context.Request.Headers);

			// Если валидация не прошла или валидатор не найден, возвращаем ошибку
			if (validationResponse == null || !validationResponse.Result)
			{
				context.Response.StatusCode = 400;
				await context.Response.WriteAsJsonAsync(validationResponse);
				return BadRequest();
			}

			// Валидация прошла успешно
			if (useDetailedValidation)
			{
				_logger.LogInformation("Детализированная валидация заголовков прошла успешно.");
			}
			else
			{
				_logger.LogInformation("Простая валидация заголовков прошла успешно.");
			}
		}

		// Основная логика, которая будет выполнена после валидации или если валидация не была выполнена
		// Пример:
		return Ok(new { message = "✅ Валидация включена и работает!" });
	}
}
