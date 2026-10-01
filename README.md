# Events API

REST API для управления мероприятиями на ASP.NET Core (.NET 10).

## Стек

- .NET 10 / ASP.NET Core Web API
- Swagger (OpenAPI)
- Mapperly для маппинга DTO ↔ модель
- In-memory хранилище

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Сборка и запуск

```bash
git clone https://github.com/tgs-dev-x/events-api.git
cd events-api/EventsApi
dotnet build
dotnet run
```

После запуска Swagger UI доступен по адресу:

- http://localhost:5000/swagger

> **⚠️ Swagger доступен только в окружении `Development`.**
> При запуске с `ASPNETCORE_ENVIRONMENT=Production` эндпоинты `/openapi` и `/swagger` не регистрируются.
> По умолчанию `dotnet run` использует профиль из `launchSettings.json` с `ASPNETCORE_ENVIRONMENT=Development`, поэтому Swagger работает.

## Эндпоинты

| Метод  | Путь            | Описание                | Ответы        |
|--------|-----------------|-------------------------|---------------|
| GET    | `/events`       | Список всех мероприятий | 200           |
| GET    | `/events/{id}`  | Мероприятие по id       | 200, 404      |
| POST   | `/events`       | Создать мероприятие     | 201, 400      |
| PUT    | `/events/{id}`  | Обновить мероприятие    | 204, 400, 404 |
| DELETE | `/events/{id}`  | Удалить мероприятие     | 204, 404      |

## Примеры

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

### POST /events — ошибка валидации (400)

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

## Валидация

- `title` — обязателен
- `startAt` — обязателен
- `endAt` — обязателен и должен быть позже `startAt`
- `description` — опционален

## Формат ошибок

Все ошибки возвращаются в формате [Problem Details (RFC 9457)](https://datatracker.ietf.org/doc/html/rfc9457) с `Content-Type: application/problem+json`.

## Хранилище

Данные хранятся **в памяти приложения** (`InMemoryEventRepository`).
При **перезапуске приложения все мероприятия сбрасываются** — база данных не используется.

## Структура проекта

- `Controllers/` — REST-эндпоинты
- `Services/` — бизнес-логика
- `Data/Repositories/` — in-memory хранилище
- `Dtos/`, `Models/` — модели запроса/ответа и домена
- `Exceptions/`, `ExceptionHandlers/` — обработка ошибок
- `Mappers/`, `Validation/`, `Extensions/` — инфраструктура