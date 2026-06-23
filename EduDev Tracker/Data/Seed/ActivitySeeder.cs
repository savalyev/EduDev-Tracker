using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using Microsoft.Maui.Storage;
using TaskStatus = EduDev_Tracker.Data.Models.TaskStatus;

namespace EduDev_Tracker.Data.Seed
{
    public class ActivitySeeder
    {
        private readonly DatabaseService _db;
        private readonly HabitRepository _habits;
        private readonly NoteRepository _notes;
        private readonly PomodoroRepository _pomodoro;

        public ActivitySeeder(
            DatabaseService db,
            HabitRepository habits,
            NoteRepository notes,
            PomodoroRepository pomodoro)
        {
            _db = db;
            _habits = habits;
            _notes = notes;
            _pomodoro = pomodoro;
        }

        private const string SeedKey = "activity_seeded_v2_{0}";

        public async Task SeedIfNeededAsync(int profileId)
        {
            var key = string.Format(SeedKey, profileId);
            if (Preferences.Get(key, false)) return;
            await SeedAllAsync(profileId, DateTime.Today);
            Preferences.Set(key, true);
        }

        private async Task SeedAllAsync(int profileId, DateTime today)
        {
            var conn = _db.Connection;
            // повторный посев (новый ключ): сначала чистим прежние демо-данные профиля → без дублей
            await ClearProfileSeedDataAsync(profileId, conn);
            var habitIds = await SeedHabitsAsync(profileId, conn);
            await SeedHabitLogsAsync(habitIds, today, conn);
            var projectIds = await SeedProjectsAsync(profileId, conn);
            var taskIds = await SeedTasksAsync(profileId, projectIds, today, conn);
            await SeedNotesAsync(profileId, today, conn);
            var presetIds = await SeedPomodoroPresetsAsync(profileId);
            await SeedPomodoroSessionsAsync(profileId, presetIds, taskIds, today, conn);
            await SeedConversionHistoryAsync(profileId, today, conn);
        }

        // Очистка прежних демо-данных профиля (чтобы пересев не плодил дубли).
        // Удаляет ТОЛЬКО данные активного профиля.
        private static async Task ClearProfileSeedDataAsync(int profileId, SQLite.SQLiteAsyncConnection conn)
        {
            // дети → родители
            await conn.ExecuteAsync("DELETE FROM habit_logs WHERE HabitId IN (SELECT Id FROM habits WHERE ProfileId=?)", profileId);
            await conn.ExecuteAsync("DELETE FROM habit_schedules WHERE HabitId IN (SELECT Id FROM habits WHERE ProfileId=?)", profileId);
            await conn.ExecuteAsync("DELETE FROM habits WHERE ProfileId=?", profileId);

            await conn.ExecuteAsync("DELETE FROM task_recurrences WHERE TaskId IN (SELECT Id FROM tasks WHERE ProfileId=?)", profileId);
            await conn.ExecuteAsync("DELETE FROM tasks WHERE ProfileId=?", profileId);
            await conn.ExecuteAsync("DELETE FROM projects WHERE ProfileId=?", profileId);

            await conn.ExecuteAsync("DELETE FROM note_versions WHERE NoteId IN (SELECT Id FROM notes WHERE ProfileId=?)", profileId);
            await conn.ExecuteAsync("DELETE FROM notes WHERE ProfileId=?", profileId); // notes_fts чистится триггером

            await conn.ExecuteAsync("DELETE FROM pomodoro_sessions WHERE ProfileId=?", profileId);
            await conn.ExecuteAsync("DELETE FROM pomodoro_presets WHERE ProfileId=?", profileId);

            await conn.ExecuteAsync("DELETE FROM conversion_history WHERE ProfileId=?", profileId);
        }

        // ─── HABITS ──────────────────────────────────────────────────────────

        private async Task<int[]> SeedHabitsAsync(int profileId, SQLite.SQLiteAsyncConnection conn)
        {
            var baseDate = DateTime.UtcNow.AddDays(-30);
            var habits = new[]
            {
                new Habit { ProfileId = profileId, Title = "Зарядка по утрам",    Icon = "sport_icon.png",   SortOrder = 0, CreatedAt = baseDate, UpdatedAt = baseDate },
                new Habit { ProfileId = profileId, Title = "Читать 30 минут",     Icon = "book_icon.png",    SortOrder = 1, CreatedAt = baseDate, UpdatedAt = baseDate },
                new Habit { ProfileId = profileId, Title = "Программировать 1 ч", Icon = "brain_icon.png",   SortOrder = 2, CreatedAt = baseDate, UpdatedAt = baseDate },
                new Habit { ProfileId = profileId, Title = "Пить воду (1 л)",     Icon = "water_icon.png",   SortOrder = 3, CreatedAt = baseDate, UpdatedAt = baseDate },
                new Habit { ProfileId = profileId, Title = "Медитация 10 мин",    Icon = "default_icon.png", SortOrder = 4, CreatedAt = baseDate, UpdatedAt = baseDate },
                new Habit { ProfileId = profileId, Title = "Прогулка / пробежка", Icon = "run_icon.png",     SortOrder = 5, CreatedAt = baseDate, UpdatedAt = baseDate },
            };
            var ids = new int[habits.Length];
            for (int i = 0; i < habits.Length; i++)
            {
                await conn.InsertAsync(habits[i]);
                ids[i] = habits[i].Id;

                // Каждой привычке нужна запись HabitSchedule — иначе HabitItemViewModel краш на null
                await conn.InsertAsync(new HabitSchedule
                {
                    HabitId = habits[i].Id,
                    DayMask = 0b1111111, // все 7 дней
                });
            }
            return ids;
        }

        // ─── HABIT LOGS ───────────────────────────────────────────────────────

        private static readonly double[] HabitRates = { 0.78, 0.70, 0.65, 0.92, 0.52, 0.62 };

        private static readonly string[] HabitNotes =
        {
            "Сделал на одном дыхании", "Через силу, но сделал", "Отличная сессия",
            "Сегодня особенно зашло", "На выезде, но не пропустил", "Лёгкий день",
        };

        private static async Task SeedHabitLogsAsync(int[] habitIds, DateTime today, SQLite.SQLiteAsyncConnection conn)
        {
            var logs = new List<HabitLog>();
            for (int h = 0; h < habitIds.Length; h++)
            {
                for (int i = 29; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    bool complete;

                    // последняя неделя — поживее, чтобы календарь выполнения наглядно зеленел
                    var rate = i <= 6 ? Math.Min(0.95, HabitRates[h] + 0.18) : HabitRates[h];

                    if (h == 3 && i <= 9)
                        complete = true; // вода: последние 10 дней — красивая серия
                    else if (h == 2 && date.DayOfWeek == DayOfWeek.Sunday)
                        complete = false; // программирование: пропускаем воскресенья
                    else
                        complete = ShouldComplete(habitIds[h], i, rate);

                    if (!complete) continue;

                    var hourOffset = Math.Abs((habitIds[h] * 97 + i * 37) % 16);
                    var minuteOffset = Math.Abs((habitIds[h] * 13 + i * 7) % 60);
                    var completedAt = date.AddHours(6 + hourOffset).AddMinutes(minuteOffset);

                    // изредка — заметка к отметке (реализм)
                    string? note = Math.Abs((habitIds[h] * 31 + i * 17) % 9) == 0
                        ? HabitNotes[Math.Abs((habitIds[h] + i) % HabitNotes.Length)]
                        : null;

                    logs.Add(new HabitLog
                    {
                        HabitId = habitIds[h],
                        LogDate = date.ToString("yyyy-MM-dd"),
                        Value = 1,
                        Note = note,
                        CompletedAt = completedAt.ToUniversalTime()
                    });
                }
            }
            await conn.InsertAllAsync(logs, "OR REPLACE");
        }

        // ─── PROJECTS ─────────────────────────────────────────────────────────

        private static async Task<int[]> SeedProjectsAsync(int profileId, SQLite.SQLiteAsyncConnection conn)
        {
            var projects = new[]
            {
                new Project { ProfileId = profileId, Name = "EduDev Tracker",                Color = "#2DD4BF", CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new Project { ProfileId = profileId, Name = "Алгоритмы и структуры данных", Color = "#3B82F6", CreatedAt = DateTime.UtcNow.AddDays(-25) },
            };
            var ids = new int[projects.Length];
            for (int i = 0; i < projects.Length; i++)
            {
                await conn.InsertAsync(projects[i]);
                ids[i] = projects[i].Id;
            }
            return ids;
        }

        // ─── TASKS ────────────────────────────────────────────────────────────

        private static async Task<int[]> SeedTasksAsync(
            int profileId, int[] projectIds, DateTime today, SQLite.SQLiteAsyncConnection conn)
        {
            int eduId = projectIds[0];
            int algId = projectIds[1];

            // dueOffset: отрицательный = в прошлом (просрочено/выполнено), положительный = в будущем
            var allTasks = new List<TaskItem>
            {
                // ── Done (8) ──────────────────────────────────────────────────────────
                MakeTask(profileId, "Реализовать модуль аналитики",
                    "Построить графики, метрики и экспорт отчётов в Excel/HTML.",
                    "Разработка", TaskPriority.High, TaskStatus.Done, eduId, today,
                    createdDaysAgo: 10, dueOffset: -3, completedDaysAgo: 3),

                MakeTask(profileId, "Настроить экспорт отчётов",
                    "Реализовать экспорт аналитики в HTML и Excel с помощью ClosedXML.",
                    "Разработка", TaskPriority.Medium, TaskStatus.Done, eduId, today,
                    createdDaysAgo: 14, dueOffset: -6, completedDaysAgo: 6),

                MakeTask(profileId, "Решить 10 задач на LeetCode",
                    "Задачи на массивы, строки и хеш-таблицы — уровень Easy/Medium.",
                    "Алгоритмы", TaskPriority.High, TaskStatus.Done, algId, today,
                    createdDaysAgo: 16, dueOffset: -8, completedDaysAgo: 8),

                MakeTask(profileId, "Пройти модуль по сортировкам",
                    "QuickSort, MergeSort, HeapSort — теория + реализация на C#.",
                    "Алгоритмы", TaskPriority.Medium, TaskStatus.Done, algId, today,
                    createdDaysAgo: 12, dueOffset: -5, completedDaysAgo: 5),

                MakeTask(profileId, "Обновить README проекта",
                    "Добавить описание архитектуры, скриншоты и инструкцию по запуску.",
                    "Разработка", TaskPriority.Low, TaskStatus.Done, eduId, today,
                    createdDaysAgo: 25, dueOffset: -20, completedDaysAgo: 20),

                MakeTask(profileId, "Настроить SQLite-репозитории",
                    "Создать BaseRepository<T> и конкретные репозитории для всех сущностей.",
                    "Разработка", TaskPriority.High, TaskStatus.Done, eduId, today,
                    createdDaysAgo: 22, dueOffset: -15, completedDaysAgo: 15),

                MakeTask(profileId, "Добавить конвертер единиц",
                    "Числа, цвета, время, JSON/XML, URL — все конвертеры с историей.",
                    "Разработка", TaskPriority.Medium, TaskStatus.Done, eduId, today,
                    createdDaysAgo: 18, dueOffset: -11, completedDaysAgo: 11),

                MakeTask(profileId, "Изучить паттерн MVVM",
                    "CommunityToolkit.Mvvm: ObservableProperty, RelayCommand, INavigationService.",
                    "Учёба", TaskPriority.Medium, TaskStatus.Done, null, today,
                    createdDaysAgo: 29, dueOffset: -24, completedDaysAgo: 24),

                // ── InProgress (4) ────────────────────────────────────────────────────
                MakeTask(profileId, "Оптимизировать запросы к SQLite",
                    "Добавить индексы, профилировать медленные запросы в аналитике.",
                    "Разработка", TaskPriority.High, TaskStatus.InProgress, eduId, today,
                    createdDaysAgo: 4, dueOffset: 3),

                MakeTask(profileId, "Изучить паттерны GoF",
                    "Observer, Strategy, Command, Decorator — примеры на C# MAUI.",
                    "Учёба", TaskPriority.Medium, TaskStatus.InProgress, algId, today,
                    createdDaysAgo: 6, dueOffset: 5),

                MakeTask(profileId, "Написать статью для конференции",
                    "Архитектура EduDev Tracker: feature-first MVVM + SQLite + MAUI.",
                    "Карьера", TaskPriority.Urgent, TaskStatus.InProgress, null, today,
                    createdDaysAgo: 2, dueOffset: 1),

                MakeTask(profileId, "Рефакторинг ViewModel слоя",
                    "Вынести дублирующуюся логику в BaseViewModel, упростить команды.",
                    "Разработка", TaskPriority.Medium, TaskStatus.InProgress, eduId, today,
                    createdDaysAgo: 5, dueOffset: 4),

                // ── Open (3) ──────────────────────────────────────────────────────────
                MakeTask(profileId, "Добавить поддержку тёмной темы",
                    "Переменные цветов в ResourceDictionary, переключатель в профиле.",
                    "Разработка", TaskPriority.Low, TaskStatus.Open, eduId, today,
                    createdDaysAgo: 7, dueOffset: 14),

                MakeTask(profileId, "Разобраться с MAUI lifecycle",
                    "OnAppearing / OnDisappearing / OnNavigatedTo — когда что вызывается.",
                    "Учёба", TaskPriority.Medium, TaskStatus.Open, null, today,
                    createdDaysAgo: 3, dueOffset: 7),

                MakeTask(profileId, "Прочитать Clean Architecture",
                    "Книга Robert C. Martin — применить принципы к слоям EduDev Tracker.",
                    "Учёба", TaskPriority.Low, TaskStatus.Open, null, today,
                    createdDaysAgo: 8, dueOffset: 10),

                // ── Overdue (2) ───────────────────────────────────────────────────────
                MakeTask(profileId, "Подготовить презентацию",
                    "Слайды с демо приложения: архитектура, ключевые фичи, результаты.",
                    "Карьера", TaskPriority.Urgent, TaskStatus.Open, null, today,
                    createdDaysAgo: 5, dueOffset: -2),

                MakeTask(profileId, "Сдать ДЗ по алгоритмам",
                    "Реализовать BFS/DFS на графах, сдать с тестами на GitHub.",
                    "Алгоритмы", TaskPriority.High, TaskStatus.Open, algId, today,
                    createdDaysAgo: 4, dueOffset: -1),
            };

            var inProgressIds = new int[4];
            int ip = 0;
            foreach (var t in allTasks)
            {
                await conn.InsertAsync(t);
                if (t.Status == TaskStatus.InProgress && ip < 4)
                    inProgressIds[ip++] = t.Id;
            }
            return inProgressIds.Where(id => id > 0).ToArray();
        }

        private static TaskItem MakeTask(int profileId, string title, string description, string category,
            TaskPriority priority, TaskStatus status, int? projectId, DateTime today,
            int createdDaysAgo, int dueOffset = 7, int? completedDaysAgo = null)
        {
            var t = new TaskItem
            {
                ProfileId    = profileId,
                ProjectId    = projectId,
                Title        = title,
                Description  = description,
                Category     = category,
                Priority     = priority,
                Status       = status,
                // DueAt хранится как локальное время (как в AddTaskViewModel: DueDate.Date + DueTime, без конвертации)
                DueAt        = today.AddDays(dueOffset).AddHours(23).AddMinutes(59),
                CreatedAt    = today.AddDays(-createdDaysAgo).ToUniversalTime(),
                UpdatedAt    = today.AddDays(-1).ToUniversalTime(),
            };
            if (completedDaysAgo.HasValue)
                t.CompletedAt = today.AddDays(-completedDaysAgo.Value).AddHours(14).ToUniversalTime();
            return t;
        }

        // ─── NOTES ────────────────────────────────────────────────────────────

        private async Task SeedNotesAsync(int profileId, DateTime today, SQLite.SQLiteAsyncConnection conn)
        {
            var noteData = new (string Title, bool Pinned, int DaysAgo, int Versions, string Content)[]
            {
                ("Конспект: MAUI архитектура", true, 28, 2,
"""
# MAUI архитектура

## Shell навигация
- AppShell.xaml описывает структуру приложения
- `Shell.Current.GoToAsync("//route")` — навигация
- Flyout menu — боковое меню с пунктами

## MVVM паттерн
```
View → ViewModel → Service → Repository → DB
```

## Dependency Injection
- Все сервисы регистрируются в `MauiProgram.cs`
- Singleton — один экземпляр на весь lifecycle приложения
- Transient — создаётся заново при каждом запросе

## Lifecycle
- `OnAppearing` — вызывается при каждом показе страницы
- `OnDisappearing` — при скрытии страницы
- Загружать данные в `OnAppearing`, не в конструкторе
"""),
                ("TODO: рефакторинг проекта", true, 14, 0,
"""
# TODO: Рефакторинг

## Высокий приоритет
- [ ] Вынести бизнес-логику из ViewModel в Service
- [ ] Добавить интерфейсы для всех сервисов
- [ ] Покрыть юнит-тестами HabitService

## Средний приоритет
- [ ] Переименовать методы по конвенции
- [ ] Убрать дублирующийся код в репозиториях
- [ ] Оптимизировать SQL-запросы (добавить индексы)

## Низкий приоритет
- [ ] Добавить документацию к публичным методам
- [ ] Настроить Roslyn-анализаторы
"""),
                ("Идеи для новых фич", true, 7, 0,
"""
# Идеи для новых функций

## Статистика и аналитика
- Тепловая карта активности (GitHub-style)
- Прогноз выполнения цели на основе тренда
- Экспорт данных в Google Sheets

## Привычки
- Привычки с гибким расписанием (не каждый день)
- Цепочки привычек (habit stacking)
- Уведомления с мотивационными цитатами

## Задачи
- Интеграция с Telegram для уведомлений
- Шаблоны задач для типовых проектов
- Автоматическая оценка времени

## Геймификация
- Очки опыта за выполнение привычек
- Достижения и бейджи
- Еженедельные вызовы
"""),
                ("Чеклист к конференции", true, 3, 1,
"""
# Подготовка к конференции

## За день до
- [x] Установить приложение на демо-устройство
- [x] Заполнить тестовые данные в БД
- [x] Проверить все основные сценарии
- [ ] Подготовить краткий скрипт выступления
- [ ] Зарядить ноутбук

## На конференции
- [ ] Показать Dashboard с реальными данными
- [ ] Продемонстрировать модуль Привычек со стриком
- [ ] Показать аналитику за месяц с графиком
- [ ] Экспорт отчёта в Excel и HTML

## Ключевые фичи для демо
1. Трекинг привычек с историей и стриками
2. Задачи с приоритетами и статусами
3. Помодоро-таймер с аналитикой
4. Аналитика и экспорт отчётов
"""),
                ("Паттерн Repository", false, 25, 0,
"""
# Паттерн Repository

## Описание
Repository — абстракция над источником данных.
Изолирует бизнес-логику от деталей работы с БД.

## Пример реализации
```csharp
public interface IHabitRepository
{
    Task<List<Habit>> GetActiveAsync(int profileId);
    Task<int> SaveAsync(Habit habit);
    Task DeleteAsync(Habit habit);
}

public class HabitRepository : BaseRepository<Habit>, IHabitRepository
{
    public Task<List<Habit>> GetActiveAsync(int profileId)
        => Connection.Table<Habit>()
            .Where(h => h.ProfileId == profileId && !h.IsArchived)
            .ToListAsync();
}
```

## В EduDev Tracker
- BaseRepository<T> — общая логика CRUD
- Конкретные репозитории — домен-специфичные запросы
- DI регистрация как Singleton
"""),
                ("Алгоритмы сортировки — шпаргалка", false, 21, 0,
"""
# Алгоритмы сортировки

| Алгоритм | Лучший | Средний | Худший | Память |
|----------|--------|---------|--------|--------|
| Bubble Sort | O(n) | O(n²) | O(n²) | O(1) |
| Insertion Sort | O(n) | O(n²) | O(n²) | O(1) |
| Merge Sort | O(n log n) | O(n log n) | O(n log n) | O(n) |
| Quick Sort | O(n log n) | O(n log n) | O(n²) | O(log n) |
| Heap Sort | O(n log n) | O(n log n) | O(n log n) | O(1) |

## Когда применять
- **Insertion Sort**: маленький массив (n < 20)
- **Merge Sort**: стабильная сортировка, связные списки
- **Quick Sort**: общий случай (быстрее всех на практике)
- **Heap Sort**: нужна O(1) память и гарантированный O(n log n)
"""),
                ("SQLite: полезные запросы", false, 18, 0,
"""
# SQLite — полезные паттерны

## Индексы
```sql
CREATE INDEX idx_tasks_profile ON tasks(ProfileId);
CREATE INDEX idx_sessions_profile ON pomodoro_sessions(ProfileId);
```

## UPSERT (INSERT OR REPLACE)
```sql
INSERT INTO habit_logs(HabitId, LogDate, Value)
VALUES (?, ?, ?)
ON CONFLICT(HabitId, LogDate) DO UPDATE SET Value = excluded.Value;
```

## FTS5 Full-Text Search
```sql
SELECT n.* FROM notes n
JOIN notes_fts f ON f.rowid = n.Id
WHERE notes_fts MATCH 'ключевое слово'
ORDER BY rank;
```

## Агрегация с группировкой по дате
```sql
SELECT date(StartedAt, 'localtime') as Day, COUNT(*) as Count
FROM pomodoro_sessions
WHERE ProfileId = ? AND Phase = 'Work'
GROUP BY Day ORDER BY Day;
```
"""),
                ("Встреча с ментором", false, 10, 0,
"""
# Встреча с ментором — заметки

## Обсудили
- Архитектуру проекта EduDev Tracker
- Подход к тестированию MAUI-приложений
- Паттерн CQRS и где он применим

## Фидбек по проекту
- Хорошая структура папок (feature-first MVVM)
- Стоит добавить интеграционные тесты для сервисов
- Рассмотреть использование MediatR для команд

## Задачи до следующей встречи
1. Покрыть юнит-тестами HabitService и TaskService
2. Написать статью об архитектуре приложения
3. Изучить паттерны GoF — Observer и Strategy

## Следующая встреча
Договорились через 2 недели
"""),
                ("Ссылки на ресурсы", false, 5, 0,
"""
# Полезные ресурсы

## .NET MAUI
- Официальная документация: docs.microsoft.com/maui
- CommunityToolkit.Mvvm: learn.microsoft.com/mvvm-toolkit

## SQLite & ORM
- sqlite-net-pcl: github.com/praeclarum/sqlite-net
- SQLiteNetExtensions: bitbucket.org/twincoders/sqlite-net-extensions

## Алгоритмы
- LeetCode: leetcode.com
- NeetCode roadmap: neetcode.io
- Visualgo (визуализация): visualgo.net

## C# Best Practices
- C# Design Patterns: refactoring.guru
- Async/Await: blog.stephencleary.com
"""),
                ("Рефлексия за месяц", false, 1, 0,
"""
# Рефлексия — прошедший месяц

## Что удалось
- Реализовал все запланированные модули приложения
- Поддерживал стрик по основным привычкам
- Закрыл 8 задач из бэклога
- Прошёл курс по алгоритмам

## Что не удалось
- Не успел добавить автоматические тесты
- Несколько дней пропустил зарядку
- Два задания сдал с небольшим опозданием

## Наблюдения
- Самые продуктивные дни — среда и четверг
- Помодоро 50/10 помогает лучше, чем 25/5 для глубокой работы
- Заметки-рефлексии — отличный инструмент

## Цели на следующий месяц
1. Выступить на конференции с хорошим результатом
2. Покрыть сервисы юнит-тестами
3. Поддерживать стрик привычек ≥ 25/30 дней
"""),
            };

            foreach (var (title, pinned, daysAgo, versions, content) in noteData)
            {
                var note = new Note
                {
                    ProfileId = profileId,
                    Title = title,
                    Content = content,
                    IsPinned = pinned,
                    CreatedAt = today.AddDays(-daysAgo).ToUniversalTime(),
                    UpdatedAt = today.AddDays(-Math.Max(1, daysAgo / 2)).ToUniversalTime(),
                };
                await conn.InsertAsync(note); // FTS-триггер сработает автоматически, Id проставится

                for (int v = versions; v > 0; v--)
                {
                    var preview = content.Length > 200 ? content[..200] + "..." : content;
                    await _notes.SaveVersionAsync(new NoteVersion
                    {
                        NoteId = note.Id,
                        Content = $"[Версия {v}]\n{preview}",
                        SavedAt = today.AddDays(-daysAgo - v).ToUniversalTime()
                    });
                }
            }
        }

        // ─── POMODORO PRESETS ─────────────────────────────────────────────────

        private async Task<int[]> SeedPomodoroPresetsAsync(int profileId)
        {
            var presets = new[]
            {
                new PomodoroPreset { ProfileId = profileId, Name = "Стандартный 25/5",   WorkMinutes = 25, ShortBreakMin = 5,  LongBreakMin = 15, CyclesToLong = 4, IsDefault = true  },
                new PomodoroPreset { ProfileId = profileId, Name = "Глубокая работа 50", WorkMinutes = 50, ShortBreakMin = 10, LongBreakMin = 20, CyclesToLong = 3, IsDefault = false },
            };
            var ids = new int[presets.Length];
            for (int i = 0; i < presets.Length; i++)
            {
                await _pomodoro.SavePresetAsync(presets[i]);
                ids[i] = presets[i].Id;
            }
            return ids;
        }

        // ─── POMODORO SESSIONS ────────────────────────────────────────────────

        private static async Task SeedPomodoroSessionsAsync(
            int profileId, int[] presetIds, int[] taskIds, DateTime today, SQLite.SQLiteAsyncConnection conn)
        {
            var sessions = new List<PomodoroSession>();
            int seed = profileId * 31 + 7;

            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                bool isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                int workCount = isWeekend
                    ? 1 + Math.Abs((seed + i * 7) % 3)     // 1–3 в выходные
                    : 3 + Math.Abs((seed + i * 11) % 4);   // 3–6 в будни

                int hour = 9;
                for (int w = 0; w < workCount && hour < 22; w++)
                {
                    bool deepWork = Math.Abs((seed + i * 3 + w * 17) % 10) < 3; // 30%
                    var presetId = deepWork && presetIds.Length > 1 ? presetIds[1] : presetIds[0];
                    int planned = deepWork ? 50 : 25;

                    bool interrupted = Math.Abs((seed + i * 13 + w * 7) % 100) < 15; // 15%
                    int actual = interrupted ? Math.Max(5, planned / 3) : planned;

                    int? taskId = null;
                    if (taskIds.Length > 0 && Math.Abs((seed + i * 7 + w * 11) % 10) < 4) // 40%
                        taskId = taskIds[Math.Abs((seed + i + w) % taskIds.Length)];

                    var minuteOffset = Math.Abs((seed + i * 19 + w * 23) % 50);
                    var startedAt = date.AddHours(hour).AddMinutes(minuteOffset);
                    var endedAt = startedAt.AddMinutes(actual);

                    sessions.Add(new PomodoroSession
                    {
                        ProfileId      = profileId,
                        PresetId       = presetId,
                        TaskId         = taskId,
                        Phase          = PomodoroPhase.Work,
                        StartedAt      = startedAt,
                        EndedAt        = endedAt,
                        PlannedMinutes = planned,
                        ActualMinutes  = actual,
                        WasInterrupted = interrupted,
                    });

                    hour += actual / 60 + 1 + Math.Abs((seed + w) % 2);
                }

                // 1 ShortBreak в будни
                if (!isWeekend && presetIds.Length > 0)
                {
                    var breakStart = date.AddHours(13);
                    sessions.Add(new PomodoroSession
                    {
                        ProfileId      = profileId,
                        PresetId       = presetIds[0],
                        Phase          = PomodoroPhase.ShortBreak,
                        StartedAt      = breakStart,
                        EndedAt        = breakStart.AddMinutes(5),
                        PlannedMinutes = 5,
                        ActualMinutes  = 5,
                        WasInterrupted = false,
                    });
                }
            }
            await conn.InsertAllAsync(sessions);
        }

        // ─── CONVERSION HISTORY ───────────────────────────────────────────────

        private static async Task SeedConversionHistoryAsync(
            int profileId, DateTime today, SQLite.SQLiteAsyncConnection conn)
        {
            var h = new List<ConversionHistory>
            {
                new() { ProfileId = profileId, Type = ConversionType.Numeral, InputText = "255",              OutputText = "FF",                               CreatedAt = today.AddDays(-1).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Numeral, InputText = "1024",             OutputText = "400",                              CreatedAt = today.AddDays(-2).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Numeral, InputText = "0b11001100",       OutputText = "CC",                               CreatedAt = today.AddDays(-3).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Numeral, InputText = "42",               OutputText = "101010",                           CreatedAt = today.AddDays(-9).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Numeral, InputText = "0xFF",             OutputText = "255",                              CreatedAt = today.AddDays(-12).ToUniversalTime() },
                new() { ProfileId = profileId, Type = ConversionType.Color,   InputText = "#2DD4BF",          OutputText = "rgb(45, 212, 191)",                CreatedAt = today.AddDays(-2).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Color,   InputText = "#3B82F6",          OutputText = "rgb(59, 130, 246)",                CreatedAt = today.AddDays(-4).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Color,   InputText = "#EF4444",          OutputText = "rgb(239, 68, 68)",                 CreatedAt = today.AddDays(-5).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Color,   InputText = "hsl(174, 63%, 50%)", OutputText = "#2DD4BF",                       CreatedAt = today.AddDays(-10).ToUniversalTime() },
                new() { ProfileId = profileId, Type = ConversionType.Time,    InputText = "2024-06-01 12:00 UTC", OutputText = "15:00 (UTC+03:00)",            CreatedAt = today.AddDays(-3).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.Time,    InputText = "2024-06-15 09:00 UTC", OutputText = "12:00 (UTC+03:00)",            CreatedAt = today.AddDays(-6).ToUniversalTime()  },
                new() { ProfileId = profileId, Type = ConversionType.JsonXml, InputText = """{"name":"test","value":42}""", OutputText = "<root><name>test</name><value>42</value></root>", CreatedAt = today.AddDays(-4).ToUniversalTime() },
                new() { ProfileId = profileId, Type = ConversionType.JsonXml, InputText = "<data><id>1</id><name>item</name></data>", OutputText = """{"id":"1","name":"item"}""", CreatedAt = today.AddDays(-7).ToUniversalTime() },
                new() { ProfileId = profileId, Type = ConversionType.Url,     InputText = "https://example.com/search?q=hello world", OutputText = "https%3A%2F%2Fexample.com%2Fsearch%3Fq%3Dhello%20world", CreatedAt = today.AddDays(-5).ToUniversalTime() },
                new() { ProfileId = profileId, Type = ConversionType.Url,     InputText = "имя пользователя",  OutputText = "%D0%B8%D0%BC%D1%8F+%D0%BF%D0%BE%D0%BB%D1%8C%D0%B7%D0%BE%D0%B2%D0%B0%D1%82%D0%B5%D0%BB%D1%8F", CreatedAt = today.AddDays(-8).ToUniversalTime() },
            };
            await conn.InsertAllAsync(h);
        }

        // ─── HELPER ───────────────────────────────────────────────────────────

        private static bool ShouldComplete(int seed, int dayOffset, double rate)
        {
            var hash = Math.Abs((seed * 7 + dayOffset * 13 + (int)(rate * 100)) % 100);
            return hash < (int)(rate * 100);
        }
    }
}
