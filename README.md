# Events API

REST API для управления мероприятиями на ASP.NET Core (.NET 10).

## Стек

- .NET 10 / ASP.NET Core Web API
- Swagger (OpenAPI)
- Mapperly для маппинга DTO ↔ модель
- xUnit + FluentAssertions + NSubstitute для тестов
- In-memory хранилище

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Сборка и запуск

```bash
git clone https://github.com/tgs-dev-x/events-api.git
cd events-api
dotnet build
dotnet run --project EventsApi
```

После запуска Swagger UI доступен по адресу:

- http://localhost:5000/swagger

> **⚠️ Swagger доступен только в окружении `Development`.**
> При запуске с `ASPNETCORE_ENVIRONMENT=Production` эндпоинты `/openapi` и `/swagger` не регистрируются.
> По умолчанию `dotnet run` использует профиль из `launchSettings.json` с `ASPNETCORE_ENVIRONMENT=Development`, поэтому Swagger работает.

## Эндпоинты

| Метод  | Путь            | Описание                | Ответы        |
|--------|-----------------|-------------------------|---------------|
| GET    | `/events`       | Поиск мероприятий с фильтрацией и пагинацией | 200, 400 |
| GET    | `/events/{id}`  | Мероприятие по id       | 200, 404      |
| POST   | `/events`       | Создать мероприятие     | 201, 400      |
| PUT    | `/events/{id}`  | Обновить мероприятие    | 204, 400, 404 |
| DELETE | `/events/{id}`  | Удалить мероприятие     | 204, 404      |

## Фильтрация, сортировка и пагинация

`GET /events` поддерживает следующие query-параметры (все опциональны):

| Параметр   | Тип       | По умолчанию   | Описание                                                              |
|------------|-----------|----------------|-----------------------------------------------------------------------|
| `title`    | string    | —              | Поиск по названию. Регистронезависимый, частичное совпадение          |
| `from`     | DateTime  | —              | События, начинающиеся не раньше указанной даты (`StartAt >= from`)    |
| `to`       | DateTime  | —              | События, заканчивающиеся не позже указанной даты (`EndAt <= to`)      |
| `page`     | int       | `1`            | Номер страницы (>= 1)                                                  |
| `pageSize` | int       | `10`           | Размер страницы (1..100)                                               |
| `sort`     | string[]  | `StartAt:asc`  | Сортировка в формате `field:direction`. Можно передавать несколько раз |

Допустимые поля сортировки: `Title`, `StartAt`, `EndAt`.
Допустимые направления: `asc` (по умолчанию), `desc`.

Все фильтры объединяются логическим **И**.

### Поведение при некорректных параметрах

- `sort` с неизвестным полем или направлением — `400 Bad Request` (`DataValidationException`);
- `page` < 1 — нормализуется в `1`;
- `pageSize` вне диапазона `1..100` — обрезается до ближайшей границы.

## Примеры

### Фильтрация по названию и датам + пагинация

```http
GET /events?title=конц&from=2026-01-01&to=2027-01-01&page=2&pageSize=5
```

### Несколько сортировок

```http
GET /events?sort=Title:asc&sort=StartAt:desc
```

### Успешный ответ (200)

```json
{
  "items": [
    {
      "id": 1,
      "title": "Концерт",
      "description": "Рок-концерт",
      "startAt": "2026-06-01T18:00:00Z",
      "endAt": "2026-06-01T21:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 25,
  "totalPages": 3
}
```

### POST /events — запрос

```json
{
  "title": "Концерт",
  "description": "Рок-концерт",
  "startAt": "2026-06-01T18:00:00Z",
  "endAt": "2026-06-01T21:00:00Z"
}
```

### POST /events — успешный ответ (201)

```json
{
  "id": 1,
  "title": "Концерт",
  "description": "Рок-концерт",
  "startAt": "2026-06-01T18:00:00Z",
  "endAt": "2026-06-01T21:00:00Z"
}
```

## Валидация

- `title` — обязателен, не более 100 символов
- `startAt` — обязателен
- `endAt` — обязателен и должен быть позже `startAt`
- `description` — опционален, не более 100 символов

## Формат ошибок

Все ошибки возвращаются в формате [Problem Details (RFC 9457)](https://datatracker.ietf.org/doc/html/rfc9457) с `Content-Type: application/problem+json`.

### 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Ошибка валидации",
  "status": 400,
  "detail": "Ошибка валидации входных данных.",
  "instance": "/events",
  "errors": {
    "Title": ["Title обязателен"],
    "StartAt": ["StartAt обязателен"],
    "EndAt": ["EndAt обязателен"]
  },
  "traceId": "00-..."
}
```

### 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Ресурс не найден",
  "status": 404,
  "detail": "Event с id '999' не найден.",
  "instance": "/events/999",
  "traceId": "00-..."
}
```

### 500 Internal Server Error

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Внутренняя ошибка сервера",
  "status": 500,
  "detail": "Произошла непредвиденная ошибка. Пожалуйста, обратитесь в поддержку",
  "instance": "/events",
  "traceId": "00-..."
}
```

## Хранилище

Данные хранятся **в памяти приложения** (`InMemoryEventRepository`).
При **перезапуске приложения все мероприятия сбрасываются** — база данных не используется.

## Тесты

Проект покрыт юнит-тестами (xUnit + FluentAssertions + NSubstitute).

```bash
dotnet test
```

Тесты покрывают:

- CRUD-операции `EventService` (успешные и неуспешные сценарии);
- валидацию входных DTO (`DataValidationException`);
- фильтрацию по `title`, `from`, `to` и их комбинации;
- пагинацию;
- сортировку;
- парсер `sort`-параметров (граничные случаи).

Проект тестов: `EventsApi.Tests`.

## Структура проекта

- `Controllers/` — REST-эндпоинты
- `Services/` — бизнес-логика
- `Data/Repositories/` — in-memory хранилище
- `Models/` — доменная модель
- `Dtos/` — модели запросов и ответов
- `Filtering/` — критерии поиска и поля сортировки
- `Common/` — пагинация и сортировка
- `Mappers/` — маппинг DTO ↔ модель (Mapperly)
- `Validation/` — валидация DTO
- `Exceptions/` — исключения
- `ExceptionHandlers/` — глобальный обработчик ошибок
- `Extensions/` — регистрация сервисов и Swagger
- `EventsApi.Tests/` — юнит-тесты