# Events API

REST API для управления мероприятиями на ASP.NET Core (.NET 10).

## Стек

- .NET 10 / ASP.NET Core Web API
- Swagger (OpenAPI)
- Mapperly для маппинга DTO ↔ модель
- xUnit + FluentAssertions + NSubstitute для тестов
- In-memory хранилище
- `BackgroundService` для отложенной обработки бронирований

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

### Мероприятия

| Метод  | Путь            | Описание                | Ответы        |
|--------|-----------------|-------------------------|---------------|
| GET    | `/events`       | Поиск мероприятий с фильтрацией и пагинацией | 200, 400 |
| GET    | `/events/{id}`  | Мероприятие по id       | 200, 404      |
| POST   | `/events`       | Создать мероприятие     | 201, 400      |
| PUT    | `/events/{id}`  | Обновить мероприятие    | 204, 400, 404 |
| DELETE | `/events/{id}`  | Удалить мероприятие     | 204, 404      |

### Бронирования

| Метод | Путь                  | Описание                              | Ответы        |
|-------|-----------------------|---------------------------------------|---------------|
| POST  | `/events/{id}/book`   | Создать бронь для мероприятия         | 202, 404      |
| GET   | `/bookings/{id}`      | Получить бронь по идентификатору      | 200, 404      |

#### Модель `Booking`

| Поле          | Тип             | Описание                                             |
|---------------|-----------------|------------------------------------------------------|
| `id`          | Guid            | Уникальный идентификатор брони                       |
| `eventId`     | Guid            | Идентификатор мероприятия                            |
| `status`      | BookingStatus   | Текущий статус брони                                 |
| `createdAt`   | DateTime (UTC)  | Дата и время создания                                |
| `processedAt` | DateTime? (UTC) | Дата и время обработки; `null`, пока бронь `Pending` |

#### Статусы `BookingStatus`

| Значение    | Описание                            |
|-------------|-------------------------------------|
| `Pending`   | Бронь создана, ожидает обработки     |
| `Confirmed` | Бронь подтверждена                   |
| `Rejected`  | Бронь отклонена                      |

Статусы сериализуются строками (`JsonStringEnumConverter`).

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

## Фоновая обработка бронирований

Реализована в `BookingProcessingBackgroundService` (`BackgroundService`),
зарегистрирована через `AddHostedService`.

Алгоритм:

1. Каждую секунду сервис опрашивает хранилище на брони со статусом `Pending`.
2. Для каждой брони выполняется искусственная задержка 2 секунды (`Task.Delay`),
   имитирующая обращение к внешней системе.
3. Затем бронь переводится в `Confirmed` и заполняется `processedAt = DateTime.UtcNow`.

Особенности:

- брони обрабатываются последовательно;
- ошибка обработки одной брони логируется (`ILogger`), цикл продолжается;
- отмена приложения корректно обрабатывается через `CancellationToken` —
  незавершённая бронь остаётся в `Pending`.

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
      "id": "fc65fd3a-b6a5-4272-965a-1323b2ef0cf4",
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
  "id": "fc65fd3a-b6a5-4272-965a-1323b2ef0cf4",
  "title": "Концерт",
  "description": "Рок-концерт",
  "startAt": "2026-06-01T18:00:00Z",
  "endAt": "2026-06-01T21:00:00Z"
}
```

### POST /events/\{id\}/book — успешный ответ (202)

Тело запроса не требуется. Заголовок `Location` содержит ссылку на созданную бронь.

```json
{
  "id": "6f1c2a9e-3b4d-4a7e-9c1f-2d3e4f5a6b7c",
  "eventId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "status": "Pending",
  "statusDescription": "Бронь создана, ожидает обработки",
  "createdAt": "2026-06-01T10:00:00Z",
  "processedAt": null
}
```

### GET /bookings/\{id\} — успешный ответ (200)

```json
{
  "id": "6f1c2a9e-3b4d-4a7e-9c1f-2d3e4f5a6b7c",
  "eventId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "status": "Confirmed",
  "statusDescription": "Бронь подтверждена",
  "createdAt": "2026-06-01T10:00:00Z",
  "processedAt": "2026-06-01T10:00:02Z"
}
```

### Сценарий: создание брони и её обработка

1. Создать мероприятие: `POST /events` → `201`.
2. Забронировать: `POST /events/{id}/book` → `202 Accepted`, `Location: /bookings/{id}`,
   в теле `"status": "Pending"`.
3. Сразу запросить `GET /bookings/{id}` → `200`, `"status": "Pending"`.
4. Подождать 2–3 секунды и повторить запрос → `"status": "Confirmed"`,
   `"processedAt"` заполнен.

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
  "detail": "Event с id 'fc65fd3a-b6a5-4272-965a-1323b2ef0cf4' не найден.",
  "instance": "/events/fc65fd3a-b6a5-4272-965a-1323b2ef0cf4",
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

Данные хранятся **в памяти приложения** (`InMemoryEventRepository`, `InMemoryBookingRepository`).
При **перезапуске приложения все мероприятия и брони сбрасываются** — база данных не используется.

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
- парсер `sort`-параметров (граничные случаи);
- создание брони для существующего события (статус `Pending`);
- получение брони по `Id` и отражение смены статуса;
- ошибки: несуществующее событие, несуществующая бронь;
- сущность `Booking` (`CreatePending`, `Confirm`, `Reject`);
- `InMemoryBookingRepository` (клонирование, фильтр `Pending`, `Update`).

Проект тестов: `EventsApi.Tests`.

## Структура проекта

- `Controllers/` — REST-эндпоинты
- `Services/` — бизнес-логика
- `BackgroundServices/` — фоновые сервисы (обработка `Pending`-броней)
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