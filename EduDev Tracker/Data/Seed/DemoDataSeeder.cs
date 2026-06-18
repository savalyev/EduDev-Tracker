using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using Microsoft.Maui.Storage;

namespace EduDev_Tracker.Data.Seed
{
    public class DemoDataSeeder
    {
        private readonly NoteRepository _notes;

        public DemoDataSeeder(NoteRepository notes)
        {
            _notes = notes;
        }

        public async Task SeedIfNeededAsync(int profileId)
        {
            var key = $"cheatsheets_seeded_{profileId}";
            if (Preferences.Get(key, false)) return;

            var category = new NoteCategory
            {
                ProfileId = profileId,
                Name = "Шпаргалки",
                Color = "#5B5FEF"
            };
            await _notes.SaveCategoryAsync(category);

            foreach (var (title, content) in Cheatsheets)
            {
                var note = new Note
                {
                    ProfileId = profileId,
                    CategoryId = category.Id,
                    Title = title,
                    Content = content,
                    IsPinned = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _notes.SaveNoteDirectAsync(note);
            }

            Preferences.Set(key, true);
        }

        private static readonly (string Title, string Content)[] Cheatsheets =
        [
            (
                "Шпаргалка: HTTP коды",
                """
# HTTP Коды состояния

## 1xx — Информационные
| Код | Описание |
|-----|----------|
| 100 | Continue — продолжай отправку |
| 101 | Switching Protocols — переключение протокола |

## 2xx — Успех
| Код | Описание |
|-----|----------|
| 200 | OK — запрос выполнен |
| 201 | Created — ресурс создан |
| 204 | No Content — ответ без тела |
| 206 | Partial Content — частичный контент |

## 3xx — Перенаправление
| Код | Описание |
|-----|----------|
| 301 | Moved Permanently — постоянный редирект |
| 302 | Found — временный редирект |
| 304 | Not Modified — кэш актуален |

## 4xx — Ошибки клиента
| Код | Описание |
|-----|----------|
| 400 | Bad Request — неверный запрос |
| 401 | Unauthorized — требуется аутентификация |
| 403 | Forbidden — доступ запрещён |
| 404 | Not Found — ресурс не найден |
| 405 | Method Not Allowed — метод не разрешён |
| 409 | Conflict — конфликт состояния |
| 422 | Unprocessable Entity — ошибка валидации |
| 429 | Too Many Requests — превышен лимит запросов |

## 5xx — Ошибки сервера
| Код | Описание |
|-----|----------|
| 500 | Internal Server Error — ошибка сервера |
| 502 | Bad Gateway — плохой шлюз |
| 503 | Service Unavailable — сервис недоступен |
| 504 | Gateway Timeout — таймаут шлюза |
"""
            ),
            (
                "Шпаргалка: Git",
                """
# Git — основные команды

## Инициализация
```
git init                  # новый репозиторий
git clone <url>           # клонировать
git clone <url> --depth 1 # shallow clone
```

## Статус и история
```
git status                # текущее состояние
git log --oneline         # короткая история
git diff                  # несохранённые изменения
git diff --staged         # проиндексированные изменения
```

## Индексирование и коммит
```
git add <file>            # добавить файл
git add .                 # добавить всё
git commit -m "message"   # создать коммит
git commit --amend        # изменить последний коммит
```

## Ветки
```
git branch                # список веток
git branch <name>         # создать ветку
git checkout <name>       # переключиться
git checkout -b <name>    # создать и переключиться
git merge <branch>        # влить ветку
git rebase <branch>       # перебазировать
git branch -d <name>      # удалить ветку
```

## Удалённый репозиторий
```
git remote -v             # список remote
git fetch                 # скачать без merge
git pull                  # fetch + merge
git push origin <branch>  # отправить ветку
git push -u origin main   # первый push
```

## Откат и stash
```
git stash                 # сохранить изменения
git stash pop             # восстановить
git restore <file>        # отменить изменения
git reset --hard HEAD~1   # откатить коммит (осторожно!)
git revert <commit>       # безопасный откат
```

## Теги
```
git tag v1.0.0            # создать тег
git push origin --tags    # отправить теги
```
"""
            ),
            (
                "Шпаргалка: C#",
                """
# C# — шпаргалка

## Async / Await
```csharp
// Правильно: await Task, не .Result
async Task<string> GetDataAsync()
{
    var result = await httpClient.GetStringAsync(url);
    return result;
}

// ConfigureAwait(false) в библиотеках
var data = await GetDataAsync().ConfigureAwait(false);
```

## LINQ
```csharp
var list = new List<int> { 1, 2, 3, 4, 5 };

list.Where(x => x > 2)          // фильтрация
    .Select(x => x * 2)         // преобразование
    .OrderBy(x => x)            // сортировка
    .Take(3)                    // первые N
    .ToList();                  // материализация

list.FirstOrDefault(x => x > 3) // первый или null
list.Any(x => x > 4)            // есть хоть один
list.All(x => x > 0)            // все подходят
list.Count(x => x % 2 == 0)     // количество
list.Sum() / list.Average()     // агрегация
```

## Nullable
```csharp
string? name = null;
int length = name?.Length ?? 0;       // null-conditional
string upper = name ?? "default";     // null-coalescing
name ??= "fallback";                  // присвоить если null
```

## Pattern Matching
```csharp
object obj = 42;
if (obj is int n && n > 10) { }

string result = obj switch
{
    int i when i > 0 => "положительное",
    int i            => "ноль или меньше",
    string s         => $"строка: {s}",
    null             => "null",
    _                => "другое"
};
```

## Records
```csharp
record Person(string Name, int Age);
var p1 = new Person("Иван", 25);
var p2 = p1 with { Age = 26 };  // копия с изменением
```

## Полезные типы
```csharp
// Span<T> — без аллокаций
Span<int> span = stackalloc int[10];

// ValueTask — для hot path
ValueTask<int> FastAsync() => ValueTask.FromResult(42);

// IAsyncEnumerable
await foreach (var item in GetItemsAsync()) { }
```
"""
            ),
            (
                "Шпаргалка: Markdown",
                """
# Markdown — синтаксис

## Заголовки
```
# H1
## H2
### H3
#### H4
```

## Текст
```
**жирный**
*курсив*
~~зачёркнутый~~
`инлайн-код`
> цитата
```

## Списки
```
- пункт 1
- пункт 2
  - вложенный

1. нумерованный
2. список
```

## Ссылки и изображения
```
[текст](url)
![alt](url)
```

## Код
````
```csharp
var x = 42;
```
````

## Таблицы
```
| Колонка 1 | Колонка 2 |
|-----------|-----------|
| ячейка    | ячейка    |
| ячейка    | ячейка    |
```

## Разделитель
```
---
```

## Чекбоксы (GitHub Markdown)
```
- [x] сделано
- [ ] не сделано
```
"""
            )
        ];
    }
}
