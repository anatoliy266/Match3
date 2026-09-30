# Match-3 Core — Техническая спецификация

Документ для проведения аудита репозитория. Описывает код в том виде, в каком он
реализован. Каждое утверждение ниже прослеживается до файла и, где это полезно, до номера
строки. Разделы, помеченные **Замечания аудита**, содержат проверенные дефекты и техдолг;
это наблюдения, а не рекомендации.

---

## 1. Обзор проекта

Игра match-3 с core-логикой, написанной с нуля. Ни стороннего match-3 фреймворка, ни ECS,
ни ассетов на основе дерева поведения или utility-AI. Симуляция поля, детект совпадений,
гравитация, спавн, разрешение блокеров и игровой поток реализованы на собственном C# в
`Assets/Game`.

| Свойство | Значение | Источник |
|---|---|---|
| Движок | Unity `6000.4.5f1` | `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | URP `17.4.0` | `Packages/manifest.json` |
| Ввод | Input System `1.19.0` (`InputActionReference`, `Pointer.current`) | `Assets/Game/Components/DragManager.cs` |
| Твины | PrimeTween (локальный `file:`-пакет) | `Packages/manifest.json`, `FieldViewController.cs` |
| API пулинга | `UnityEngine.Pool` (`ObjectPool<T>`, `ListPool<T>`, `DictionaryPool<K,V>`, `HashSetPool<T>`, `CollectionPool<T,E>`, `ArrayPool<T>`) | `Assets/Game` |
| SDK издателя | PluginYourGames / Yandex Games (`YG2.saves`, interstitial-реклама) | `Assets/PluginYourGames`, `LevelController.cs` |
| UI | uGUI + TextMeshPro | `LevelController.cs`, `TaskListController.cs` |
| Assembly definition | **Отсутствует.** Весь рантайм-код компилируется в `Assembly-CSharp` | нет ни одного `.asmdef` в `Assets/Game` |
| Автотесты | **Отсутствуют.** `com.unity.test-framework` установлен; тестовых сборок нет | — |

**Модель поля.** Поле представляет собой плотный (без jagged-массивов) двумерный массив
`LogicalTile?[,]`, элементы которого идентифицированы через `System.Guid`. Каждая клетка
несёт value-type `TileKind`, дискриминируемый полем `TileKindType`
(`Regular` / `Bonus` / `Blocker`). Имеется 6 обычных цветов, 3 варианта бонусов и 3
варианта блокеров (`Assets/Game/Components/RegularType.cs:3-5`,
`Assets/Game/ScriptableObjects/BlockerType.cs:1`).

**Игровой поток.** Ввод → переход FSM → обмен → оценка совпадений → удаление → гравитация →
заполнение → повторная оценка (каскад) → стабилизация → следующий ход. Переходы состояний
описаны данными в таблице ScriptableObject, а не захардкожены в автомате.

---

## 2. Обзор архитектуры

Четыре слоя, разделённые по направлению потока данных. Слой симуляции не хранит ссылок на
`UnityEngine.Object` и не обращается к сцене — однако он зависит от Unity-типов-значений
(`Vector2Int`, `Mathf`, `UnityEngine.Random`), то есть не является независимым от движка.
Слой представления не хранит состояние поля и никогда не мутирует `Field`.

```
┌──────────────────────────────────────────────────────────────────────┐
│ СЛОЙ ВВОДА / ПРЕДСТАВЛЕНИЯ  (MonoBehaviour)                          │
│   DragManager ── GameplayEventBus<SwapInfo> ──► IdleState           │
│   FieldViewController ── последовательности PrimeTween               │
│   WoolController, TaskListController, StepsController, SFX, панели   │
└──────────────────────────────────────────────────────────────────────┘
                                    │  снапшоты (LogicalTile?[,])
┌──────────────────────────────────┴───────────────────────────────────┐
│ СЛОЙ СИМУЛЯЦИИ  (обычные C# class / struct)                          │
│   Field          — авторитетное состояние поля, клетки по Guid         │
│   MatchEvaluator — детект групп (делегирует в BFS)                   │
│   SpawnEvaluator — выбор цвета, взвешивание по сложности             │
│   BFS / TwoPointers / Euclide — статические алгоритмы                │
└──────────────────────────────────────────────────────────────────────┘
                                    │  FiniteStateMachine.Switch(StateEvent)
┌──────────────────────────────────┴───────────────────────────────────┐
│ СЛОЙ ОРКЕСТРАЦИИ  (граф ScriptableObject)                            │
│   подклассы GameState + таблица переходов FieldStates                │
│   FieldBlackboard — обмен данными в пределах хода                    │
└──────────────────────────────────────────────────────────────────────┘
                                    │  имена шин в виде строк
┌──────────────────────────────────┴───────────────────────────────────┐
│ СЛОЙ ДАННЫХ  (ассеты ScriptableObject)                               │
│   LevelSettings, Levels, MatchRules, SpawnRules, TileTypeData,       │
│   BlockerDestroyRules, Events, Sounds, ScoreData, SessionData        │
└──────────────────────────────────────────────────────────────────────┘
```

**Модель коммуникации.** Два механизма, без прямых межслойных вызовов:

- **Канал команд (симуляция → оркестрация):** `FiniteStateMachine.Switch(StateEvent)`
  находит следующий `GameState` в `FieldStates` и вызывает `Enter`. Все передаваемые данные
  идут через `FieldBlackboard`.
- **Канал событий (всё остальное):** `GameplayEventBus<T>` — статический generic
  publish/subscribe с ключом-строкой. Имена шин **не** захардкожены: они резолвятся в рантайме
  через `Events.GetBusName(GameEvent)` из ассета, настроенного в инспекторе
  (`Assets/Game/ScriptableObjects/Events/Events.cs:61-71`).

Слой симуляции публикует снапшоты в шину и никогда не ссылается на объекты представления.

---

## 3. Сетка и управление полем

**Владелец: `Assets/Game/Components/Field.cs`** (93 строки).

`Field` наследует `MonoBehaviour`, но не использует Unity API — он инстанцируется как
префаб сцены в `LevelController.StartLevel` (`LevelController.cs:116-117`) исключительно
ради сериализации. Это мог бы быть обычный класс без изменения поведения.

### 3.1 Хранилище

```csharp
// Field.cs:7-11
public struct LogicalTile
{
    public Guid Id { get; set; }
    public TileKind Type { get; set; }
}

// Field.cs:18, 20-25
private LogicalTile?[,] _logicalTiles;              // [строка, столбец], row-major

public void Initialize(LevelSettings data)
{
    _rows = data.Rows;
    _cols = data.Columns;
    _logicalTiles = new LogicalTile?[_rows, _cols];
}
```

Проектные решения, релевантные для аудита:

- **`Nullable<T>` для признака пустоты.** `LogicalTile?` различает «пустую клетку» и
  «клетку со значением `default(TileKind)``. `default` для структуры — это валидное
  значение `TileKindType.Regular`, поэтому оно было бы неоднозначным.
- **Идентичность по `Guid`, а не по индексу.** `LogicalTile.Id` выдаётся через
  `Field.GenerateUniqueId()` (`Field.cs:91`, `Guid.NewGuid()`). Позиция *не* является
  идентичностью: упавшая плитка сохраняет свой `Id`, хотя её индекс в массиве меняется. Это
  инвариант, на который опирается весь слой синхронизации представления.
- **Соглашение по индексам.** `Vector2Int.x` — это **строка**, `.y` — **столбец**.
  Подтверждается `FieldView.GetWorldPos` (`FieldView.cs:59-62`), который отображает столбец
  в world X, а строку в world Y, а также `IsInBounds` (`Field.cs:53`), ограничивающим `x`
  величиной `_rows`.

### 3.2 Публичный API

| Член | Сложность | Назначение |
|---|---|---|
| `Initialize(LevelSettings)` | O(R·C) | Выделяет внутренний массив |
| `GetBounds()` → `Vector2Int` | O(1) | `(rows, cols)` |
| `IsInBounds(Vector2Int)` | O(1) | Проверка границ, **не** используется в `GetTileAt`/`SetTileAt` |
| `GetTileAt(Vector2Int)` | O(1) | Прямое чтение массива, **без проверок** |
| `SetTileAt` / `ClearTileAt` | O(1) | Прямая запись / обнуление, **без проверок** |
| `GetTileAt(Guid)` | **O(R·C)** | Полный линейный скан (`Field.cs:57-67`) |
| `TryGetPosition(Guid, out Vector2Int)` | **O(R·C)** | Полный линейный скан (`Field.cs:69-84`) |
| `ToSnapshot()` | O(R·C) | Глубокая копия всего массива (`Field.cs:28-35`) |
| `ToPositionChCache(Dictionary<Guid, Vector2Int>)` | O(R·C) | Заполняет переданный вызывающим словарь `Id → клетка` (`Field.cs:37-49`) |

`ToPositionChache` — это оптимизированная альтернатива `GetTileAt(Guid)` /
`TryGetPosition(Guid)`: состояниям, которым нужен поиск позиции по `Id`, арендуется
`DictionaryPool<Guid, Vector2Int>`, заполняемый за один проход, вместо выполнения O(R·C)
сканов на каждый запрос. В кодовой базе сосуществуют оба подхода —
`SwapState.cs:14-23` строит кэш и *всё равно* дважды вызывает линейный
`GetTileAt(Guid)` за обмен.

`IsInBounds` — мёртвый код: ни один вызывающий его не использует, и сами аксессоры массива
его тоже не используют.

### 3.3 Гравитация: уплотнение столбца двумя указателями

**Владелец: `Assets/Game/ScriptableObjects/TwoPointers.cs`**, вызывается из
`FillUpState.Enter` (`FillUpState.cs:40-47`).

`TwoPointers.Run(Vector2Int startPos, AlgoritmContext context, List<TileTransitionData> groupResult)`
вызывается один раз на столбец и работает за O(rows) — линейно, без аллокаций в куче.

Алгоритм:

1. **Сегментация столбца** (`TwoPointers.cs:18-30`). Скан `read` идёт от `0` до `r`. Пустые
   клетки пропускаются и границей сегмента не считаются. Непустая плитка, у которой
   `TileKind.IsAnchored` равно `true`, закрывает текущий сегмент. `IsAnchored` — это
   `BlockerType.Box || BlockerType.Frozen` (`RegularType.cs:35`), то есть такие блокеры
   сбрасывают сегмент гравитации и не падают.
2. **Уплотнение каждого сегмента** (`TwoPointers.cs:33-49`). Классическое уплотнение на
   месте двумя указателями: `write` отстаёт от `read`; каждая непустая клетка на позиции
   `read` порождает `TileTransitionData { From = (read, col), To = (write, col) }` и
   увеличивает `write`. Клетки, уже стоящие на `write == read`, ничего не порождают.
3. **Применение** (`FillUpState.cs:49-63`). Переходы применяются как **обмены**
   `SetTileAt(From, To)` / `SetTileAt(To, From)`, и рабочий `snapshot` обновляется в том же
   цикле, так что последующие столбцы видят уже устоявшиеся.

Результат — список дельт `From → To`. Гравитация не перезапускается каждый кадр: она
разрешается один раз на итерацию каскада и воспроизводится визуально слоем представления.

**`Euclide.Run(int totalCells)`** (`Assets/Game/ScriptableObjects/Euclide.cs`) возвращает
наибольшее нечётное `c < n`, для которого `gcd(c, n) == 1`, уменьшая кандидата на 2 и
прогоняя алгоритм Евклида. `FinalState.ConvertRemainingStepsToBonuses` использует его как
шаг (`currentIndex = (startIndex + i * step) % totalCells`, `FinalState.cs:39-48`), чтобы
конверсия оставшихся ходов распределялась по полю, а не скапливалась в одной области.

---

## 4. Алгоритм детекта совпадений

**Основной тип: `Assets/Game/ScriptableObjects/BFS.cs` — `public static class BFS`
(66 строк).** Драйвер: `Assets/Game/ScriptableObjects/MatchEvaluator.cs` (109 строк).

### 4.1 Что это за алгоритм на самом деле

Это **обход в ширину (flood fill) по 4-связному окружению двумерного массива, где предикат
принятия/отклонения клетки задаётся data-driven списком правил.** Это *не* классическое
сканирование по длине серий в match-3 (подсчёт 3+ одинаковых клеток вдоль строки, затем
столбца).

Это различие — самое важное, что должен удерживать в голове аудитор, поскольку
поведение «по горизонтали / по вертикали» содержится **не** в обходе, а в правилах:

- Обход в `BFS.Run` всегда посещает всех четырёх соседей
  (`BFS.cs:17-19`: `up`, `down`, `left`, `right`).
- Поведение в форме линии возникает исключительно потому, что отдельные ассеты правил
  сравнивают проверяемую клетку с **началом заливки** (`source`), а не с клеткой фронтира.

```csharp
// TileHorizontalMatchRule.cs:6-10   — та же строка, что и origin  => горизонтальная линия
return target.Position.x == source.Position.x;

// TileVerticalBombMatchRule.cs:6-10  — тот же столбец, что и origin => вертикальная линия
return target.Position.y == source.Position.y;
```

Следствие: линия из 4 одноцветных плиток обнаруживается только если заливка *стартует* на
этой линии. `MatchEvaluator.Evaluate` запускает заливку из каждой непосещённой клетки, так
что на практике покрытие полное — однако гарантия обеспечивается циклом инициализации, а не
самим обходом.

### 4.2 `BFS.Run` — пошагово

Сигнатура: `BFS.Run(Vector2Int startPos, AlgoritmContext context, List<Guid> groupResult)`.

**Структура контекста** (`BFS.cs:5-11`) — передаётся одним `struct`, поэтому BFS не хранит
персистентного состояния, а буферы вызывающего (`Queue`, `Visited`) переиспользуются между
вызовами:

```csharp
public struct AlgoritmContext
{
    public LogicalTile?[,] Snapshot;
    public IReadOnlyList<TileMatchRuleBase> Rules;
    public Queue<Vector2Int> Queue;
    public bool[] Visited;
}
```

Исполнение:

| Шаг | Строки | Поведение |
|---|---|---|
| 1. Проверка | `BFS.cs:24` | Выход, если стартовая клетка `null` |
| 2. Проверка | `BFS.cs:26` | Выход, если `Rules.Count == 0` — тип без настроенных правил не может дать совпадение |
| 3. Инициализация | `BFS.cs:32-35` | Создать `TileSnapshot(startPos, startTile.Type)`; отметить `Visited[x*cols+y] = true`; добавить в очередь; добавить `Id` в `groupResult` |
| 4. Фронтир | `BFS.cs:38-40` | `while (Queue.Count > 0)`, извлечь, прочитать `TileKind` клетки |
| 5. Соседи | `BFS.cs:43-50` | Для каждого из 4 направлений вычислить цель и проверить границы по `rows`/`cols`, полученным из `Snapshot.GetLength` |
| 6. Пропуск | `BFS.cs:53-55` | Пропустить, если `Visited[x*cols+y]` или цель `null` |
| 7. Предикат | `BFS.cs:60-68` | Для каждого правила `i` вызвать `Rules[i].IsMatch(in sourceSnapshot, in currentSnapshot, in targetSnapshot)`; первый `true` завершает цикл (`break`) |
| 8. Принятие | `BFS.cs:71-76` | Отметить `Visited`, добавить в очередь, добавить `Id` |

Свойства корректности:

- **`Visited` устанавливается в момент постановки в очередь** (`BFS.cs:73`), а не при
  извлечении. Поэтому клетка попадает в очередь не более одного раза, `groupResult` не
  содержит дубликатов, а размер фронтира ограничен числом клеток. Классический инвариант BFS
  соблюдён.
- **`sourceSnapshot` создаётся один раз** вне цикла (`BFS.cs:32`) и переиспользуется при
  каждом вызове предиката, тем самым корректно сохраняя начало заливки для линейных правил.
- **Индексация `Visited` плоская:** `x * cols + y` (`BFS.cs:33`, `BFS.cs:53`, `BFS.cs:73`),
  что соответствует размеру аренды `r * c` у вызывающего.
- **Вычисление правил с коротким замыканием** — цикл прерывается на первом совпадении,
  поэтому порядок правил в ассете является решением о стоимости и приоритете.

### 4.3 Набор правил — подклассы `TileMatchRuleBase`

**Базовый класс** (`Assets/Game/ScriptableObjects/TileMatchRules/TileMatchRule.cs:15-18`):

```csharp
public abstract class TileMatchRuleBase : ScriptableObject
{
    public abstract bool IsMatch(in TileSnapshot source, in TileSnapshot current, in TileSnapshot target);
}
```

`TileSnapshot` — это `readonly struct` (`TileMatchRule.cs:3-13`), и все три параметра
передаются как `in`, поэтому защитных копий не создаётся ни в одной точке вызова.

| Класс правила | Файл | Предикат |
|---|---|---|
| `TileCommonMatchRule` | `TileCommonMatchRule.cs:8-22` | Манхэттенское расстояние `== 1`, обе `Regular`, и `current.RegularType == target.RegularType` |
| `TileBombMatchRule` | `TileBombMatchRule.cs:11-20` | Расстояние Чебышёва от `source` `<= _explosionRadius` (сериализуемое, по умолчанию `1`) — квадратная область поражения |
| `TileHorizontalBombMatchRule` | `TileHorizontalMatchRule.cs:6-10` | `target.x == source.x` (та же строка) |
| `TileVerticalBombMatchRule` | `TileVerticalBombMatchRule.cs:6-10` | `target.y == source.y` (тот же столбец) |

Обратите внимание, что `TileCommonMatchRule` заново выводит смежность по Манхэттену,
которую обход по 4 соседям уже гарантирует, и игнорирует параметр `source`.

**Резолв правил** — ScriptableObject `MatchRules` (`MatchRules.cs`). `OnEnable` →
`BuildLookup()` (`MatchRules.cs:46-72`) разворачивает списки из инспектора
`RegularTileTypeRuleMapping` / `BonusTileTypeRuleMapping` в
`Dictionary<RegularType, IReadOnlyList<TileMatchRuleBase>>` и
`Dictionary<BonusType, IReadOnlyList<TileMatchRuleBase>>`. `GetRules(TileKind)`
(`MatchRules.cs:77-85`) переключается по `KindType` и при промахе возвращает общий
`IReadOnlyList.EmptyRules`. **Резолв правил на уровне клетки — это O(1) доступ к словарю, а
не скан списка.**

### 4.4 Драйвер — `MatchEvaluator`

`MatchEvaluator` — обычный класс, инстанцируемый в `FiniteStateMachine.Init`
(`FiniteStateMachine.cs:27`). Он содержит одно поле в духе `static readonly`:

```csharp
// MatchEvaluator.cs:16
private static Queue<Vector2Int> _queue = new Queue<Vector2Int>();
```

Статическое, не потокобезопасное, общее для всех экземпляров — см. **Замечание аудита A-4**.

#### `Evaluate(LogicalTile?[,] snapshot, MatchRules rules, List<MatchInfo> matches)` — `MatchEvaluator.cs:19-58`

Полный скан поля:

1. `ArrayPool<bool>.Shared.Rent(r * c)` под флаги посещённости (`MatchEvaluator.cs:24`),
   `Array.Clear(visited, 0, visited.Length)` — очищается **весь** арендованный буфер, поэтому
   устаревшие хвостовые данные от предыдущей, возможно большей, аренды утечь не могут.
2. `_queue.Clear()` (`MatchEvaluator.cs:27`).
3. Вложенные `for` по всем клеткам (`MatchEvaluator.cs:29-56`):
   - пропуск `null`; пропуск `TileKindType.Bonus` и `TileKindType.Blocker`
     (`MatchEvaluator.cs:34-35`) — они никогда не являются стартовыми и никогда не входят в
     группу при обычном скане;
   - пропуск уже посещённых (`MatchEvaluator.cs:36`);
   - `rules.GetRules(type)` один раз на стартовую клетку (`MatchEvaluator.cs:40`);
   - `BFS.Run(pos, data, group)` (`MatchEvaluator.cs:49`), записывающий в
     `new List<Guid>()`, аллоцируемый на каждую стартовую клетку (`MatchEvaluator.cs:41`);
   - **`if (group.Count > 2)`** (`MatchEvaluator.cs:51`) → порог в 3 клетки, соответствующий
     конвенции match-3. Результат добавляется в `matches` как
     `new MatchInfo { GroupType = type, Positions = group }`.
4. `ArrayPool<bool>.Shared.Return(visited)` (`MatchEvaluator.cs:57`).

**Сложность.** Каждую клетку посещает не более одной заливки (массив `Visited` общий для всех
стартов), поэтому суммарно обход занимает O(R·C). Каждое принятое ребро вычисляет до
`Rules.Count` предикатов, что даёт в худшем случае **O(R·C·R_rules)** — для поставляемой
конфигурации (1 правило на обычный тип) это O(R·C).

#### `EvaluateTile(...)` — `MatchEvaluator.cs:60-79`

Вариант для одной клетки. Арендует собственный массив посещённости и **записывает
синтетическую клетку в снапшот** перед запуском BFS:

```csharp
// MatchEvaluator.cs:76
snapshot[pos.x, pos.y] = new LogicalTile { Id = new Guid(), Type = kind };
```

Используется исключительно `SpawnEvaluator.Evaluate`, чтобы спросить «образует ли
размещение цвета X здесь совпадение?» — именно это и управляет взвешиванием цветов против
каскадов (см. §4.5).

#### `AddBonusGroup(...)` — `MatchEvaluator.cs:82-108`

Аналогично `EvaluateTile`, но для стартовой клетки типа `Bonus`, и группа добавляется
**безусловно** — без порога `> 2`, поскольку область поражения бомбы является корректной
группой размера 1. Используется для цепной детонации бонусов в `BonusState`.

### 4.5 Выбор спавна и взвешивание по сложности

**`Assets/Game/ScriptableObjects/SpawnEvaluator.cs`**, вызывается из `LoadingState.cs:20`,
`FillUpState.cs:73`, а также внутренне из `Evaluate`.

```csharp
// SpawnEvaluator.cs:34-45
for (var val = 0; val < values.Length; val++)
{
    var type = (RegularType)val;
    group.Clear();
    machine.MatchEvaluator.EvaluateTile(snapshot, new Vector2Int(i, j), TileKind.Regular(type), rules, group);

    var currentweight = group.Count > 2 ? 1.0f / (1.0f + difficultModified) : 1.0f;
    totalWeight += currentweight;

    if (UnityEngine.Random.Range(0, totalWeight) <= currentweight) choosen = val;
}
```

Для каждой пустой клетки каждый кандидат-цвет пробно вставляется через синтетический BFS. Цвет,
который *образовал бы* совпадение, получает вес, делённый на `(1 + cascadeIteration)`;
безопасные цвета сохраняют вес `1.0`. `totalWeight` накапливается инкрементально, что делает
это взвешенным резервуарным отбором, а не двухпроходным взвешенным выбором. Замысел — явно
сформулированный в комментарии исходника на `SpawnEvaluator.cs:15-16` — состоит в том, чтобы
делать случайные совпадения всё менее вероятными по мере накопления каскадов.

`LoadingState.cs:20` передаёт `1000` в качестве `difficultModified`; `FillUpState.cs:73`
передаёт `Blackboard.CascadeIteration`. Стоимость — **O(клеток × 6) запусков BFS на одно
заполнение**, каждый с арендой и очисткой массива — это самый горячий путь в симуляции
(см. **A-6**).

### 4.6 Сопоставление форм бонусов

**`Assets/Game/ScriptableObjects/TileSpawnRules/TileSpawnRule.cs`** —
`TileSpawnRuleBase.IsMatch(List<Vector2Int> groupCells)`:

- Отклоняет `null` на входе, затем требует `groupCells.Count == activeCells.Count`.
- `CheckShapeMatch` (`TileSpawnRule.cs:36-58`): нормализует оба множества вычитанием
  собственного минимального угла, затем проверяет членство. O(n²) по размеру формы, но n — это
  размер формы, заданный вручную (3–5).
- `CheckShapeMatchRotated` (`TileSpawnRule.cs:60-109`): опционально пробует повороты на
  90/180/270° (флаг `CheckRotations`), поворачивая через `p = new Vector2Int(p.y, -p.x)`.
  Использует `ArrayPool<Vector2Int>.Shared.Rent(count)` для повёрнутой копии — **обратите
  внимание, что буфер арендуется на `count`, но читаются только индексы `< count`, и
  записываются только индексы `< count` на `TileSpawnRule.cs:75`; остаток не читается до
  `Return`.**
- `SpawnRules` (`SpawnRules.cs:30-66`) индексирует правила по `activeCells.Count`, поэтому
  `GetRules(groupCount)` — это O(1) обращение к словарю.

Существуют три конкретных подкласса с пустыми телами, не переопределяющими ничего:
`TileBombSpawnRule`, `TileHorizontalBombSpawnRule`, `TileVerticalBombSpawnRule`
(`BonusType` — это данные, а не поведение).

### 4.7 Разрешение блокеров

**`BlockerDestroyRuleBase.ShouldDestroy(Vector2Int pos, LogicalTile?[,] snapshot, List<MatchInfo> currentMatches)`**,
диспетчеризуется через `BlockerDestroyRules.GetRule(BlockerType)` — O(1) обращение к словарю
(`BlockerDestroyRules.cs:38-44`).

| Правило | Файл | Предикат |
|---|---|---|
| `BlockerAdjacentMatchRule` | `BlockerAdjacentMatchRule.cs:12-41` | `Id` любого из 4 соседей ∈ объединения id текущих совпадений. Строит `new HashSet<Guid>()` **на каждый вызов** |
| `BlockerBottomRowRule` | `BlockerBottomRowRule.cs:6-10` | `pos.x <= 0` (строка 0 — нижняя по соглашению об индексах) |
| `BlockerIndestructibleRule` | `BlockerIndestructibleRule.cs:6-10` | `false` |

Вызывается из двух мест: `RemoveState.ProcessBlockers` (`RemoveState.cs:53-79`) со списком
совпадений и `FillUpState.ProcessSafeBlockers` (`FillUpState.cs:96-123`) с `null` (поэтому
правила смежности самопротивосточатся, и после гравитации применяются только позиционные
правила). Оба сканируют каждую клетку поля в поисках блокеров.

---

## 5. Оптимизация производительности

### 5.1 Пулинг объектов плиток

**`Assets/Game/Components/FieldView.cs:22-28`** — действующая реализация:

```csharp
_pool = new ObjectPool<Tile>(
    () => Instantiate(Tile, this.transform),          // createFunc
    (tile) => tile.gameObject.SetActive(true),          // actionOnGet
    (tile) => tile.gameObject.SetActive(false),         // actionOnRelease
    (tile) => Destroy(tile.gameObject),                 // actionOnDestroy
    true, 100, 1000                                    // collectionCheck, defaultCapacity, maxSize
);
```

- `FieldView` владеет одним `ObjectPool<Tile>`; `FieldViewController` никогда не
  инстанцирует и не уничтожает плитку напрямую.
- `CreateVisualTile(Guid, TileKind, from, to)` (`FieldView.cs:38-47`) вызывает `_pool.Get()`,
  присваивает `tile.Id`, вызывает `tile.SetData(type)`, позиционирует и регистрирует в
  `_visualTiles`.
- `ClearVisualTile(Guid)` (`FieldView.cs:49-57`) вызывает `tile.CleanData()`,
  `_pool.Release(tile)`, затем удаляет запись из словаря.
- Время жизни тем самым ограничено `maxSize = 1000` пула; плитки переиспользуются на протяжении
  всего уровня с нулевой нагрузкой на `Instantiate`/`Destroy` после прогрева — переключается
  только `SetActive`.

> **`Assets/Game/Components/ObjectPool.cs` — удалён.** До коммита `954f091` это был
> мёртвый файл: все 63 строки закомментированы, самодельный синглтон-пул на
> `Queue<Tile>`, вытесненный `UnityEngine.Pool.ObjectPool<T>`, без единой ссылки.
> Сейчас в проекте единственный путь пулинга плиток — тот, что в `FieldView`
> (`FieldView.cs:22-28`), и он остаётся единственным.

### 5.2 Пулинг коллекций

Пулы коллекций `UnityEngine.Pool` повсеместно используются для временных данных в пределах
хода:

| API | Места использования |
|---|---|
| `DictionaryPool<Guid, Vector2Int>` | `FieldViewController.cs:110-113`, `EvaluationState.cs:29`, `RemoveState.cs:16`, состояния рядом с `FillUpState`, `SwapState.cs:14`, `SwapBackState.cs:14`, `BonusState.cs:19` |
| `ListPool<AnimationData>` / `<TileKind>` / `<Guid>` | `FieldViewController.cs:268,309`, `WoolController.cs:53,115`, `TaskListController.cs:92,121`, `SpawnEvaluator.cs:29,47` |
| `HashSetPool<Guid>` | `BonusState.cs:23,71` |
| `CollectionPool<List<SpawnInfo>, SpawnInfo>` | `LoadingState.cs:18,33`, `FillUpState.cs:70,88` |
| `CollectionPool<List<Vector2Int>, Vector2Int>` | `EvaluationState.cs:34,58` |
| `CollectionPool<List<TileTransitionData>, TileTransitionData>` | `FillUpState.cs:38,64` |
| `ArrayPool<bool>.Shared` | `MatchEvaluator.cs:24,64,87` (флаги посещённости) |
| `ArrayPool<Vector2Int>.Shared` | `TileSpawnRule.cs:65,107` (буфер повёрнутой формы) |

### 5.3 Дизайн на типах-значениях и без копирований

- `TileSnapshot` (`TileMatchRule.cs:3-13`) и `BonusSource`
  (`FieldViewController.cs:31-36`) — `readonly struct`.
- `TileMatchRuleBase.IsMatch` принимает три параметра `in` — защитных копий в горячем цикле
  предикатов нет (`BFS.cs:63`).
- `AlgoritmContext` передаётся по значению как `struct`; буферы `Queue` и `Visited` вызывающего
  переиспользуются, а не перевыделяются на каждую заливку.
- `MatchEvaluator._queue` — единственный переиспользуемый `Queue<Vector2Int>`, очищаемый на
  каждом вызове (`MatchEvaluator.cs:27`).
- `Sounds.BuildLookup` использует индексированный `for` с явным комментарием о предотвращении
  аллокации итератора `foreach` (`Sounds.cs:41-45`).

### 5.4 Избегание аллокаций на стороне рендера

`Tile` (`Assets/Game/Components/Tile.cs`):

- `MaterialPropertyBlock` создаётся один раз в `Awake` (`Tile.cs:26`) и переиспользуется в
  `SetData` (`Tile.cs:38-44`).
- ID свойств шейдера резолвятся один раз через `Shader.PropertyToID` и кэшируются как `int`
  (`Tile.cs:28-30`) — хеширования строк на каждый вызов нет.
- Цвета пишутся через property block, а не через экземпляр материала, поэтому клонирования
  материалов на каждую плитку не происходит.

### 5.5 Таблицы резолва ассетов

Каждый конфигурационный ассет ScriptableObject строит `Dictionary` в `OnEnable`, чтобы резолв
на каждый кадр и на каждую клетку был O(1):

| Ассет | Метод сборки | Ключ |
|---|---|---|
| `MatchRules` | `BuildLookup()` (`MatchRules.cs:46`) | `RegularType` / `BonusType` → правила |
| `SpawnRules` | `BuildLookup()` (`SpawnRules.cs:30`) | `activeCells.Count` → правила |
| `TileTypeData` | `BuildLookup()` (`TileTypeData.cs:53`) | вид → `TileDataBase` |
| `BlockerDestroyRules` | `BuildLookup()` (`BlockerDestroyRules.cs:24`) | `BlockerType` → правило |
| `FieldStates` | `BuildLookup()` (`FieldStates.cs:63`) | `GameState` → `StateEvent` → `GameState` |
| `Events` | `BuildLookup()` (`Events.cs:42`) | `GameEvent` → имя шины `string` |
| `Sounds` | `BuildLookup()` (`Sounds.cs:35`) | `GameSound` → `AudioClip` |
| `Levels` | `BuildLookup()` (`Levels.cs:31`) | `levelNumber` → `LevelSettings` |

`FieldStates.GetTransition` и `BlockerDestroyRules.GetRule` дополнительно делают null-проверку
таблицы и перестраивают её лениво (`FieldStates.cs:78`, `BlockerDestroyRules.cs:40`).

### 5.6 Отсутствие `Update` на кадр

Ни один `MonoBehaviour` в пути симуляции или представления не использует `Update()`. Прогресс
геймплея обеспечивается исключительно событиями шины и колбэками завершения PrimeTween,
поэтому простое потребление на кадр для поля равно нулю.

---

## 6. Архитектура и данные

### 6.1 Разделение логики и представления

Авторитетное состояние поля — `Field`. Представление его никогда не читает и не пишет.
Вместо этого:

1. Состояние мутирует `Field`, затем вызывает `Field.ToSnapshot()`.
2. Публикует снапшот: `GameplayEventBus<LogicalTile?[,]>.Trigger(name, snapshot)` в шине
   `GameEvent.Animation`.
3. `FieldViewController.OnPackageReceived` (`FieldViewController.cs:98-105`) ставит снапшот в
   очередь и, если система простаивает, вызывает `PlayNext()`.
4. `MatchField(LogicalTile?[,] snapshot, List<AnimationData> animData)`
   (`FieldViewController.cs:108-251`) **сопоставляет `_prevSnapshot` с входящим снапшотом** и
   формирует три типа действий:
   - `AnimateAction.Move` — `Id` есть в обоих, позиция изменилась
   - `AnimateAction.Spawn` — `Id` отсутствует в `_prevSnapshot`
   - `AnimateAction.Destroy` — `Id` есть в `_prevSnapshot`, отсутствует в `snapshot`
5. `PlayNext` (`FieldViewController.cs:255-320`) строит `Sequence` из сгруппированных
   `Sequence`, по одному на плитку, каждому предшествует `Tween.Delay(rowIndex * 0.05f)`, где
   `rowIndex = RoundToInt(item.To.x)` (`FieldViewController.cs:296-302`) — каскад сверху вниз
   по строкам.
6. По завершении присваивает `_prevSnapshot = snapshot`, вызывает `GameEvent.ShaderDestroyTile`
   и рекурсивно переходит в `PlayNext()`.

`_isPlaying` (`FieldViewController.cs:53`) управляет очередью, так что пачка снапшотов от
одного каскада воспроизводится последовательно, а не с наложением.

**Идентичность по позиции — это контракт.** Сопоставление ведётся исключительно по `Guid`.
Плитка, упавшая, сохраняет свой `Id`, поэтому она реконструируется как `Move` (падение), а не
как `Destroy` + `Spawn`. Если бы присвоение `Id` оказалось неуникальным, сопоставление молча
классифицировало бы события неверно.

**Точка появления спавна** (`FieldViewController.cs:177-180`): плитка, отсутствовавшая
ранее, входит из строки `r + 1` (на строку ниже поля, то есть за пределами экрана), кроме
плиток типа `Bonus`, которые появляются на месте в позиции `pos` с масштабированием.

**Область поражения бонуса разрешается визуально, а не логически.**
`CheckIsBonusAffected` (`FieldViewController.cs:323-345`) переиспользует тот же список
правил `MatchRules`, вызывая `rules[r].IsMatch(in sourceSnap, in sourceSnap, in targetSnap)`
для каждой уничтоженной плитки, чтобы выбрать анимацию уничтожения
(`GetBonusDestroySequence`, `FieldViewController.cs:461-513`).

Таким образом, слой представления является чистой функцией от
`(prevSnapshot, snapshot) → анимация`, с единственным исключением: очередь и флаг
`_isPlaying` хранят состояние последовательности.

### 6.2 Использование ScriptableObject

Каждое геймплейное правило и каждый набор контента — сериализованный ассет, редактируемый в
инспекторе, без захардкоженных значений в коде.

| Ассет | Тип | Роль |
|---|---|---|
| `LevelSettings` | `ScriptableObject` | Размер сетки (`Rows`, `Columns`), `Steps`, `Difficulty`, списки допустимых плиток, `backgroundSprite`, `ropesGoalsList` (`RopeGoal{TileKind, count}`), `Blockers` (`BlockerPlacement{Type, Position}`) |
| `Levels` | `ScriptableObject` | `List<LevelSettings>` + словарь `levelNumber → LevelSettings`; `GetLevelSettings(int)` возвращает `null` за пределами списка, что `LevelController.StartLevel` обрабатывает показом `EndOfLevelsController` |
| `MatchRules` | `ScriptableObject` | цвет/бонус → `List<TileMatchRuleBase>` |
| `SpawnRules` | `ScriptableObject` | размер группы → `List<TileSpawnRuleBase>` (сопоставление формы с бонусом) |
| `TileTypeData` | `ScriptableObject` | `RegularType`/`BonusType`/`BlockerType` → `TileDataBase` |
| `TileDataBase` | `ScriptableObject` | `Color`, `HighlightColor`, `ShadowColor`, `Sprite` — используются `Tile.SetData`, `WoolController`, `TaskListController`, `GoalController` |
| `BlockerDestroyRules` | `ScriptableObject` | `BlockerType` → `BlockerDestroyRuleBase` |
| `Events` | `ScriptableObject` | `GameEvent` → имя шины `string` |
| `Sounds` | `ScriptableObject` | `GameSound` → `AudioClip` |
| `FieldStates` | `ScriptableObject` | таблица переходов FSM |
| `ScoreData`, `SessionData` | `ScriptableObject` | счёт/рекорд, id текущего уровня |

**Все правила совпадений и блокеров — это `ScriptableObject`** (`TileMatchRuleBase`,
`TileSpawnRuleBase`, `BlockerDestroyRuleBase`), то есть поведение правил является контентом,
а не кодом.

### 6.3 Конечный автомат

**`Assets/Game/Components/FSM/FiniteStateMachine.cs`** — `MonoBehaviour`, хранящий текущий
ассет `GameState`, `FieldBlackboard` и два экземпляра оценщиков.

```csharp
// FiniteStateMachine.cs:22-34
public void Init(LevelSettings settings, Field currentFieldInstance)
{
    _state = State;
    Field = currentFieldInstance;
    Blackboard = new FieldBlackboard();
    MatchEvaluator = new MatchEvaluator();
    SpawnEvaluator = new SpawnEvaluator();
    Blackboard.MaxSteps = settings.Steps;
    Blackboard.Step = 0;
    Blackboard.CascadeIteration = 0;
    Blackboard.IsFinalState = false;
    Blackboard.LevelSettings = settings;
}

// FiniteStateMachine.cs:43-53
public void Switch(StateEvent e)
{
    var nextState = States.GetTransition(_state, e);
    if (nextState is not null) { _state = nextState; _state.Enter(this); }
}
```

- `GameState` — абстрактный `ScriptableObject` с единственным `Enter(FiniteStateMachine)`
  (`GameState.cs`). Методов `Exit`, `Update`, `Tick` нет — автомат управляется исключительно
  входами в состояния, и каждое состояние ожидаемо либо синхронно делает `Switch` дальше,
  либо регистрирует слушателя шины и ждёт.
- Таблица переходов: `FieldStates.GetTransition(GameState, StateEvent)`
  (`FieldStates.cs:76-81`) → O(1) через двухуровневый словарь. Если переход не сконфигурирован,
  состояние равно `null` и автомат молча останавливается — обработки ошибок нет.
- `FieldBlackboard` (`Assets/Game/Components/FSM/BlackBoard.cs`) — единственный канал между
  состояниями: `SourceDest` (обмен), `CurrentMatches`, `CurrentBonuses`, `CascadeIteration`,
  `Step` / `MaxSteps`, `IsWin`, `IsFinalState`, `BonusesToActivate`,
  `CurrentBlockersToRemove`. `Reset()` (`BlackBoard.cs:55-70`) очищает и обнуляет два списка и
  сбрасывает `CascadeIteration`; `EnsureBlockerList()` (`BlackBoard.cs:72-76`) лениво
  пересоздаёт список блокеров. Обратите внимание, что `Reset()` нигде в кодовой базе не
  вызывается.

**Состояния** (`Assets/Game/ScriptableObjects/States/`):

| Состояние | Класс | Поведение |
|---|---|---|
| `LoadingState` | `LoadingState.cs` | Заполняет пустое поле через `SpawnEvaluator` (сложность `1000`), затем `PlaceBlockers` из `LevelSettings.Blockers`, затем `Switch(FinishLoading)` |
| `IdleState` | `IdleState.cs` | Регистрируется в шине `Input`, вызывает `FieldSettled` с текущим шагом. При стабилизации: если конец уровня → `Switch(Final)`, иначе `Step++`, вызов `StepIncreased`, повторное включение ввода |
| `SwapState` | `SwapState.cs` | Резолвит два `Guid` в клетки, меняет их местами в `Field`. Если хотя бы одна плитка — `Bonus` → кладёт оба id в `BonusesToActivate` и делает `Switch(SwapBonus)`; иначе вызывает `AnimationSync` и `Switch(Swap)`. Если хотя бы один id не резолвится → `Switch(SwapBack)` |
| `SwapBackState` | `SwapBackState.cs` | Отменяет обмен и вызывает шину анимации |
| `Evaluation` | `EvaluationState.cs` | `MatchEvaluator.Evaluate` → затем по каждой группе резолвит id в позиции и вызывает `SpawnEvaluator.EvaluateBonusSpawn`. `CascadeIteration == 0` → `Switch(NoMatches)`, иначе `CascadeIteration++` и `Switch(MatchesFound)` |
| `Bonus` | `BonusState.cs` | Собственная очередь для цепной детонации бонусов: инициализирует `_bonusQueue` из `BonusesToActivate`, вызывает `MatchEvaluator.AddBonusGroup` и ставит в очередь любую найденную в группе детонации бонусную плитку — транзитивная цепь поражения |
| `RemoveState` | `RemoveState.cs` | Сначала `ProcessBlockers`, затем `ClearTileAt` для каждого id совпадения. Вызывает `Animation`, `ShaderImpact` и `Score` с `(prevSnapshot, snapshot)`; `Switch(DestroyTiles)` |
| `FillUpState` | `FillUpState.cs` | Размещает отложенные бонусы, выполняет `TwoPointers.Run` по каждому столбцу и применяет обмены, `ProcessSafeBlockers`, затем `SpawnEvaluator.Evaluate` для оставшихся дыр; вызывает `Animation`; `Switch(FillUpTiles)` |
| `WaitingState` | `WaitingState.cs` | Регистрируется на `AnimationEnd` и переадресует в `Switch(AnimationEnd)` |
| `FinalState` | `FinalState.cs` | Если не победа → вызывает `Final(false)`. Если остались ходы → `ConvertRemainingStepsToBonuses` (с шагом по Евклиду), затем `Switch(FillUpTiles)`; иначе `ActivateAllFieldBonuses` → если есть бонусы, `Switch(SwapBonus)`; иначе вызывает `Final(true)` |

### 6.4 Шина событий

**`Assets/Game/Bus/GameplayEventBus.cs`** — `public static class GameplayEventBus<T>`, то есть
`static readonly Dictionary<string, Action<T>>` с `Register` / `Unregister` / `Trigger`
(`GameplayEventBus.cs:8-34`).

Имена шин резолвятся через ассет `Events`, поэтому проект может переименовать каналы без
правки кода. `GameEvent` объявляет 14 значений (`Events.cs:4-20`); фактически используются 12
каналов: `Input`, `Animation`, `AnimationEnd`, `AnimationSync`, `Score`, `ShaderImpact`,
`ShaderDestroyTile`, `FieldSettled`, `StepIncreased`, `PlaySFX`, `Final`, `SettingChanged`.
`PlaySwapSFX` и `PlayDestroyRegularSFX` объявлены, но никогда не вызываются и на них никто не
подписывается (вытеснены единым каналом `PlaySFX`).

Публикаторы и подписчики:

| Шина | Полезная нагрузка | Публикатор | Подписчик |
|---|---|---|---|
| `Input` | `SwapInfo` | `DragManager.cs:107` | `IdleState.OnFieldEvent` |
| `Input` | `bool` (гейт) | `IdleState.cs:48,60`, `LoadingState.cs:14` | `DragManager.OnInputEventReceived` |
| `Animation` | `LogicalTile?[,]` | `LoadingState`, `RemoveState`, `FillUpState`, `SwapState`, `SwapBackState`, `FinalState` | `FieldViewController.OnPackageReceived` |
| `AnimationSync` | `LogicalTile?[,]` | `SwapState.cs:39` | `FieldViewController.SyncSnapshot` |
| `AnimationEnd` | `bool` | `FieldViewController.cs:261` | `WaitingState.OnAnimationEnd` |
| `ShaderImpact` | `(prev, cur)` | `RemoveState.cs:43` | `WoolController.HandleMatchDestroyedLogical` |
| `ShaderDestroyTile` | `bool` | `FieldViewController.cs:316` | `WoolController.HandleMatchDestroyedVisual`, `TaskListController.HandleMatchDestroyedVisual` |
| `Score` | `(prev, cur)` | `RemoveState.cs:46` | `TaskListController.OnScoreCalculation` |
| `StepIncreased` | `int` | `IdleState.cs:57` | `StepsController.OnStepChanged` |
| `FieldSettled` | `int` / `bool` | `IdleState.cs:34`, `LevelController.cs:134,147,161` | `LevelController.OnFieldSettled` |
| `FieldSettled` | `bool` | `SuccessNotificationController.OnTimeout` | `IdleState.OnFinalEvent` |
| `PlaySFX` | `GameSound` | `SwapState.cs:32`, `FieldViewController.cs:360,443,473,486,501` | `GameplaySFXManager.OnSFX` |
| `SettingChanged` | `bool` | `ToggleManager.cs:51` | `BackgroundMusicController.OnSettingsChanged` |
| `Final` | `bool` | `FinalState.cs:16,101` | `LevelController.OnFinished` |

Пара `ShaderImpact` / `Score` публикует кортеж `(prevSnapshot, snapshot)`, который и
`WoolController`, и `TaskListController` **ставят в очередь** и потребляют по более позднему
сигналу `ShaderDestroyTile` (`WoolController.cs:118-121`, `TaskListController.cs:82-85`). Это
развязывает логическое разрешение (немедленное) и визуальное (после анимации) и требует, чтобы
две шины срабатывали строго синхронно — `RemoveState.cs:42-46` и
`FieldViewController.cs:315-316` являются единственными двумя продавцами, и они всегда
вызывают одну пару `ShaderImpact`/`Score` на один `ShaderDestroyTile`. Оба потребителя
делают ранний выход, если очередь пуста (`WoolController.cs:50`,
`TaskListController.cs:89`), а не бросают исключение.

### 6.5 Ввод

`DragManager` (`Assets/Game/Components/DragManager.cs`) использует
`InputActionReference` из Input System, без `Update()`. Нажатие определяется рейкастом
`Physics2D.Raycast` по коллайдерам `Tile`. Перетаскивание фиксировано по оси: побеждает
доминирующая из `dx`/`dy`, а смещение ограничивается ±1 клеткой
(`DragManager.cs:150-165`). Если указатель выходит за радиус 1.2 единицы от клетки-цели, цель
возвращается на место и очищается (`DragManager.cs:142-148`). Плитки-блокеры
отклоняются и как источник, и как цель (`DragManager.cs:67,130`). При отпускании позиции
меняются местами и публикуется `SwapInfo{SourceId, DestId}`. Действия ввода включаются и
выключаются через канал `bool`, поэтому полем нельзя управлять посреди каскада.

---

## 7. Структура репозитория

```
D:\Unity\Match3\
├── Assets\
│   ├── Game\                        ← весь собственный геймплейный код проекта
│   │   ├── Components\              ← MonoBehaviour: представление, ввод, UI, хост FSM
│   │   │   ├── FSM\                 ← FiniteStateMachine, FieldBlackboard
│   │   │   └── MainMenuScene\       ← контроллеры меню и выбора уровня
│   │   ├── ScriptableObjects\       ← данные + правила + алгоритмы
│   │   │   ├── States\              ← подклассы GameState + таблица FieldStates
│   │   │   ├── Levels\              ← LevelSettings, Levels, LevelDifficulty
│   │   │   ├── TileMatchRules\      ← предикаты совпадений (4 конкретных правила)
│   │   │   ├── TileSpawnRules\      ← правила сопоставления формы с бонусом
│   │   │   ├── BlockerDestroyRules\ ← предикаты уничтожения блокеров
│   │   │   ├── Tiles\               ← TileTypeData, TileDataBase
│   │   │   ├── Events\              ← enum GameEvent + маппер имён шин
│   │   │   ├── Sounds\              ← enum GameSound + маппер клипов
│   │   │   └── Session\
│   │   ├── Bus\                     ← GameplayEventBus<T>
│   │   ├── Drawers\                 ← атрибут [Req] + валидирующий drawer инспектора
│   │   ├── Editor\                  ← TileGeometryEditor (авторинг форм на UI Toolkit)
│   │   ├── Shaders\                 ← shader graph URP / авторские шейдеры
│   │   ├── UI\, Prefabs\, Animations\, Sounds\, Fonts\, Materials\, Scenes\ Types\
│   ├── Plugins\PrimeTween\          ← локальный пакет твинов (зависимость file:)
│   ├── PluginYourGames\             ← SDK Yandex Games
│   ├── Settings\, Resources\, Scenes\, TextMesh Pro\, UI Toolkit\, WebGLTemplates\
│   └── …
├── Packages\manifest.json
├── ProjectSettings\ProjectVersion.txt
├── Library\  Logs\  MemoryCaptures\  ProfilerCaptures\   ← генерируемое, не исходники
└── Build-desktop\                   ← вывод сборки, не отслеживается git
```

### Ключевые скрипты

#### Поле и алгоритмы

| Скрипт | Ответственность |
|---|---|
| `Components/Field.cs` | Авторитетное поле `LogicalTile?[,]`; клетки по `Guid`; `ToSnapshot`, `ToPositionChCache`. |
| `Components/Tile.cs` | Визуальная плитка `MonoBehaviour`; применяет цвета `TileDataBase` через кэшированный `MaterialPropertyBlock` и кэшированные ID свойств шейдера. |
| `ScriptableObjects/TileMatchRules/TileMatchRule.cs` | `TileSnapshot` readonly struct + абстрактный `TileMatchRuleBase.IsMatch(in, in, in)`. |
| `ScriptableObjects/TileMatchRules/TileCommonMatchRule.cs` | Предикат смежных одноцветных обычных плиток. |
| `ScriptableObjects/TileMatchRules/TileBombMatchRule.cs` | Предикат квадратной области поражения по радиусу Чебышёва для бомб. |
| `ScriptableObjects/TileMatchRules/TileHorizontalMatchRule.cs` | Предикат «та же строка, что и origin» (горизонтальные бомбы-линии). |
| `ScriptableObjects/TileMatchRules/TileVerticalBombMatchRule.cs` | Предикат «тот же столбец, что и origin» (вертикальные бомбы-линии). |
| `ScriptableObjects/TileMatchRules/MatchRules.cs` | Ассет соответствия вид плитки → список правил с O(1)-резолвом в рантайме. |
| `ScriptableObjects/BFS.cs` | Статическая BFS-заливка по 4 соседям; принимает клетки по списку правил; пишет совпавшие `Guid`. |
| `ScriptableObjects/MatchEvaluator.cs` | Полный скан поля (`Evaluate`), проба одной клетки (`EvaluateTile`), AOE бонуса (`AddBonusGroup`); арендованный `bool[]` флагов посещённости. |
| `ScriptableObjects/TwoPointers.cs` | Уплотнение гравитации двумя указателями по столбцам с сегментацией по закреплённым блокерам. |
| `ScriptableObjects/Euclide.cs` | Наибольший нечётный взаимно простой шаг для равномерного распределения финальных конверсий. |
| `ScriptableObjects/SpawnEvaluator.cs` | Взвешенный по сложности выбор цвета для каждой пустой клетки; оценка спавна бонуса по форме. |
| `ScriptableObjects/TileSpawnRules/TileSpawnRule.cs` | Сравнение нормализованной формы с учётом поворотов против вручную заданного набора клеток. |
| `ScriptableObjects/TileSpawnRules/SpawnRules.cs` | Индексация ассетом правил спавна по размеру группы совпадения. |
| `ScriptableObjects/BlockerDestroyRules/BlockerDestroyRules.cs` | Ассет соответствия `BlockerType` → правило уничтожения. |
| `ScriptableObjects/BlockerDestroyRules/BlockerAdjacentMatchRule.cs` | Уничтожает блокеры, ортогонально смежные с совпавшей плиткой. |
| `ScriptableObjects/BlockerDestroyRules/BlockerBottomRowRule.cs` | Уничтожает блокеры в строке 0. |
| `ScriptableObjects/BlockerDestroyRules/BlockerIndestructibleRule.cs` | Правило уничтожения, не делающее ничего. |

#### Оркестрация

| Скрипт | Ответственность |
|---|---|
| `Components/FSM/FiniteStateMachine.cs` | Хранит текущий ассет `GameState`, blackboard и оценщики; резолвит и входит в следующие состояния. |
| `Components/FSM/BlackBoard.cs` | `FieldBlackboard` — все межсостоянийные данные одного хода/каскада. |
| `ScriptableObjects/States/GameState.cs` | Абстрактная база `ScriptableObject` с `Enter(FiniteStateMachine)`. |
| `ScriptableObjects/States/FieldStates.cs` | Описанная данными таблица переходов (`GameState` → `StateEvent` → `GameState`). |
| `ScriptableObjects/States/LoadingState.cs` | Начальное заполнение поля со сложностью 1000, затем размещение блокеров. |
| `ScriptableObjects/States/IdleState.cs` | Ожидание ввода; счётчик ходов; арбитраж конца уровня. |
| `ScriptableObjects/States/SwapState.cs` | Применяет обмен игрока; маршрутизирует на детонацию бонуса или на оценку. |
| `ScriptableObjects/States/SwapBackState.cs` | Отменяет невалидный обмен. |
| `ScriptableObjects/States/EvaluationState.cs` | Запускает `MatchEvaluator`, сопоставляет группы с позициями, оценивает спавн бонусов. |
| `ScriptableObjects/States/BonusState.cs` | Цепная транзитивная детонация бонусов через очередь и `AddBonusGroup`. |
| `ScriptableObjects/States/RemoveState.cs` | Проход по блокерам, очистка совпавших клеток, публикация `Animation` / `ShaderImpact` / `Score`. |
| `ScriptableObjects/States/FillUpState.cs` | Размещение бонусов, гравитация через `TwoPointers`, проход по блокерам после гравитации, заполнение дыр. |
| `ScriptableObjects/States/WaitingState.cs` | Мост от события шины `AnimationEnd` обратно в конечный автомат. |
| `ScriptableObjects/States/FinalState.cs` | Разрешение победы/поражения, конверсия оставшихся ходов, активация бонусов по всему полю. |

#### Представление, ввод, UI

| Скрипт | Ответственность |
|---|---|
| `Components/FieldView.cs` | Владелец `ObjectPool<Tile>` и визуального реестра `Guid → Tile`; отображение сетки в мировые координаты. |
| `Components/FieldViewController.cs` | Сопоставление снапшотов, очередь анимаций, построение последовательностей PrimeTween, выбор анимации на плитку. |
| ~~`Components/ObjectPool.cs`~~ | **Файл удалён в `954f091`** — был полностью закомментированным самодельным пулом, вытеснен `UnityEngine.Pool`. См. **A-2**. |
| `Components/DragManager.cs` | Обработка нажатия/перетаскивания/отпускания через Input System, фиксация по оси, отправка `SwapInfo`. |
| `Components/LevelController.cs` | Жизненный цикл уровня: инстанцирование `Field` + `FieldView`, инициализация UI, арбитраж условия победы и финальных панелей. |
| `Components/TaskListController.cs` | Создаёт UI на каждую цель, отслеживает уничтоженные виды плиток, предоставляет `AreAllGoalsCompleted()`. |
| `Components/GoalController.cs` | Виджет одной цели: текст счётчика, галочка выполнения. |
| `Components/StepsController.cs` / `StepsUnitController.cs` | Создание и обновление счётчика ходов. |
| `Components/WoolController.cs` | Текстура данных шейдера прогресса нитей; reservoir-выбор нити на каждую уничтоженную плитку; управление визуальной обратной связью по целям. |
| `Components/GameplaySFXManager.cs` | Воспроизведение `GameSound` → `AudioClip`. |
| `Components/BackgroundMusicController.cs` | Применяет флаг `Music` из `PlayerPrefs` к `AudioSource.mute` по шине `SettingChanged`. |
| `Components/ToggleManager.cs` | UI переключателя настроек; сохраняет в `PlayerPrefs` и вызывает `SettingChanged`. |
| `Components/MainMenuScene/*.cs` | Меню, список выбора уровня, точка входа в настройки. |
| `Components/WinPanelController.cs` / `LosePanelController.cs` / `EndOfLevelsController.cs` | UI конца уровня, возврат в меню с показом рекламы. |
| `Components/LoadingController.cs` | Загрузка сцены корутиной со слайдером прогресса. |

#### Ассеты данных и инструментарий

| Скрипт | Ответственность |
|---|---|
| `ScriptableObjects/Events/Events.cs` | Enum `GameEvent` и ассет-маппер `GameEvent → имя шины`. |
| `Bus/GameplayEventBus.cs` | Универсальная статическая шина publish/subscribe со строковыми ключами. |
| `ScriptableObjects/Levels/LevelSettings.cs` | Контент уровня: размер, ходы, цели, блокеры, фон. |
| `ScriptableObjects/Levels/Levels.cs` | Реестр уровней с ключом `levelNumber`. |
| `ScriptableObjects/Tiles/TileTypeData.cs` | Ассет резолва `TileKind → TileDataBase`. |
| `ScriptableObjects/Tiles/TileDataBase.cs` | Тройка цветов + спрайт для одного вида плитки. |
| `ScriptableObjects/Sounds/Sounds.cs` | Ассет резолва `GameSound → AudioClip`. |
| `ScriptableObjects/ScoreData.cs` / `Session/SessionData.cs` | Счёт и рекорд; id текущего уровня. |
| `Components/RegularType.cs` | `RegularType`, `BonusType`, `TileKindType`, struct `TileKind`, `IsAnchored`. |
| `Drawers/ReqAttribute.cs` + `MissingPropertyDrawer.cs` | Маркер `[Req]` + drawer инспектора, подсвечивающий незаполненные обязательные ссылки. |
| `Editor/TileGeometryEditor.cs` | Окно UI Toolkit для отрисовки `TileSpawnRuleBase.activeCells` по сетке. |
| `Shaders/Spark/Editor/CreateSweepGlowMaterial.cs` | Редакторная утилита генерации материала sweep-glow. |

---

## 8. Замечания аудита

Проверенные наблюдения по результатам чтения исходников. Это не список задач на исправление.

### 8.0 Статус на текущий момент

Пункты перепроверены по коду после коммита `954f091` («a1-a10 фиксы», 16 файлов,
+69/−122). Обозначения:

| Метка | Значение |
|---|---|
| ✅ | исправлено полностью |
| ◐ | исправлено частично, остаток замечания в силе |
| ❌ | не исправлено |

| Итог | Кол-во | Пункты |
|---|---|---|
| ✅ Исправлено | 5 | A-1, A-2, A-3, A-5, A-10 |
| ◐ Частично | 3 | A-6, A-8, A-9 |
| ❌ Не тронуто | 22 | A-4, A-7, A-11 … A-30 (кроме перечисленных выше) |
| 🆕 Новые | 2 | A-31, A-32 — внесены тем же коммитом `954f091` |

**Объём коммита не совпадает с его названием.** Коммит заявлен как «a1-a10», но:

- **A-4 не упомянут и не тронут.** `MatchEvaluator._queue` остаётся `static`
  (`MatchEvaluator.cs:16`), `new List<Guid>()` остаётся на строках 41 и 97.
- **A-8 закрыт наполовину.** `SwapState` переведён на кэш позиций
  (`SwapState.cs:27-28`), но `SwapBackState.cs:22-23` продолжает вызывать
  O(R·C)-скан `Field.GetTileAt(Guid)`. Более того, после правки у этих двух методов
  в `Field.cs:57,69` остался единственный вызывающий — `SwapBackState`, то есть
  `ToPositionChCache` используется в двух местах из трёх.
- **A-9 закрыт наполовину и добавил новый дефект** — см. A-31.

**Про нумерацию строк.** Коммит сдвинул строки в 16 файлах. Номера в этом разделе
приведены к текущему коду; ссылки `file:line` в разделах 1–7 могут быть смещены
на единицы строк.

---

**✅ A-1 — ИСПРАВЛЕНО в `954f091`.** Ранее: `GoalController.cs:4` содержал
`using UnityEditor;` без защиты в рантайм-сборке. Файл не имел `#if UNITY_EDITOR` и не
имел assembly definition, поэтому компилировался в `Assembly-CSharp` и ломал сборку
плеера. Сейчас директива удалена, в `using`-ах остались только `TMPro`, `UnityEngine`,
`UnityEngine.UI`.

**✅ A-2 — ИСПРАВЛЕНО в `954f091`.** `Components/ObjectPool.cs` был закомментирован
целиком (63 строки) — мёртвый файл без ссылок. Удалён вместе с `.meta`.
Действующий пулинг — `UnityEngine.Pool.ObjectPool<Tile>` в `FieldView.cs:22-28`.

**✅ A-3 — ИСПРАВЛЕНО в `954f091`.** Существовали два почти идентичных компонента
уведомлений: рабочий `SuccessNotificationController.cs` (с задержкой на PrimeTween,
на него ссылается `LevelController.cs:40`) и пустая заглушка
`SucessNotificationController.cs` с опечаткой в имени. Заглушка удалена вместе
с `.meta`; в проекте остался один файл.

**❌ A-4 — НЕ ИСПРАВЛЕНО.** `MatchEvaluator._queue` по-прежнему объявлен
`private static Queue<Vector2Int>` (`MatchEvaluator.cs:16`) и общий для всех
экземпляров, включая арендованный из `ArrayPool` массив `visited`, передаваемый в тот
же `AlgoritmContext`. При текущем однопоточном, однополевом использовании безопасно,
но тип не предоставляет никакой защиты от конкурентного или повторного входа.
Отдельно отметим, что `new List<Guid>()` аллоцируется на каждую стартовую клетку в
`Evaluate` (`MatchEvaluator.cs:41`) и на каждый вызов в `AddBonusGroup`
(`MatchEvaluator.cs:97`) — арендованный и возвращаемый `ListPool` в этом классе
не используется, хотя в проекте он применяется.

**✅ A-5 — ИСПРАВЛЕНО в `954f091`.** `AddBonusGroup` арендовал `bool[]` на строке 87 и
мог выйти на строке 90 (`if (snapshot[position.x, position.y] is null) return;`) до
`ArrayPool.Return` — буфер не возвращался в пул. Сейчас все три проверки
(`null`, `Regular`, `Blocker` — `MatchEvaluator.cs:86,89,90`) стоят **до** аренды на
строке 92, а `Return` на строке 107 гарантированно достигается.

**◐ A-6 — ИСПРАВЛЕНО ЧАСТИЧНО в `954f091`.** Аллокации на горячем пути устранены,
алгоритмическая сложность — нет.

*Что исправлено:*

- `System.Enum.GetValues(typeof(RegularType))` вынесен из `Evaluate` в конструктор и
  кэшируется в поле `_regularTypes` (`SpawnEvaluator.cs:16-20`). Раньше свежий массив
  аллоцировался на каждый вызов `Evaluate` — то есть на каждое заполнение поля.
- `ListPool<Guid>.Get()` и `ArrayPool<bool>.Shared.Rent(r * c)` вынесены из цикла по
  клеткам наружу, `Release`/`Return` добавлены в конец метода
  (`SpawnEvaluator.cs:30-31,72-73`). Раньше аренда и возврат выполнялись на каждую
  пустую клетку.
- `MatchEvaluator.EvaluateTile` больше не арендует `visited` сам — буфер передаётся
  параметром (`MatchEvaluator.cs:60`).

*Что осталось:*

- **O(клеток × 6) полных вызовов BFS на одно заполнение сохраняется** — внутренний
  цикл `SpawnEvaluator.cs:42-54` по-прежнему делает `values.Length` пробных вставок
  с полным BFS каждая. Устранены аллокации, но не лишняя работа.
- Появился побочный дефект двойной очистки — см. **A-32**.
- Мутация снапшота вызывающего на пробных вставках сохраняется — см. **A-16**.

**❌ A-7 — НЕ ИСПРАВЛЕНО.** `Field.ToSnapshot()` аллоцирует новый
`LogicalTile?[,]` на каждый вызов (`Field.cs:28-30`) и вызывается от двух до шести
раз за переход состояния в `LoadingState`, `EvaluationState`, `RemoveState`,
`FillUpState`, `SwapState`, `SwapBackState`, `BonusState` и `FinalState`. Снапшот —
это транспортный формат для сопоставления в представлении, поэтому данная аллокация
по построению находится на критическом пути. Коммит добавил в `SwapState.cs:41,63`
ещё два вызова `ToSnapshot` на каждый обмен (по одному на каждую ветку).

**◐ A-8 — ИСПРАВЛЕНО ЧАСТИЧНО в `954f091`.** `Field.GetTileAt(Guid)` и
`Field.TryGetPosition(Guid)` — линейные сканы O(R·C) (`Field.cs:57-67`,
`Field.cs:69-84`).

*Что исправлено:* в `SwapState` вызовы заменены на O(1)-чтение по позиции из кэша —
`positionsCache.TryGetValue` + `Field.GetTileAt(Vector2Int)`
(`SwapState.cs:18-19,27-28`); старые O(R·C)-вызовы закомментированы на строках 24-25.
Добавлен корректный `DictionaryPool<Guid, Vector2Int>.Release` в обеих ветках
(`SwapState.cs:39,61`).

*Что осталось:*

- **`SwapBackState.cs:22-23` по-прежнему вызывает `GetTileAt(Guid)` напрямую**, то
  есть O(R·C)-скан остался в коде — теперь это единственный вызывающий этих методов.
- `Field.cs:57` и `Field.cs:69` не удалены. `ToPositionChCache` теперь используется в
  двух местах из трёх, где нужен поиск позиции по `Id`.

**◐ A-9 — ИСПРАВЛЕНО ЧАСТИЧНО в `954f091`.** `BlockerAdjacentMatchRule.ShouldDestroy`
аллоцировал `new HashSet<Guid>()` на каждый вызов (`BlockerAdjacentMatchRule.cs:17`
в старой ревизии). Он вызывался по одному разу на каждый блокер в
`RemoveState.ProcessBlockers` и `FillUpState.ProcessSafeBlockers`, оба из которых
перебирают каждую клетку поля.

*Что исправлено:* сигнатура `BlockerDestroyRuleBase.ShouldDestroy` получила третий
параметр `HashSet<Guid> matchedIds`; набор строит вызывающий. `RemoveState`
запрашивает его из `HashSetPool<Guid>`, заполняет один раз перед обходом и
возвращает в пул (`RemoveState.cs:62-63,96`) — аллокация на каждый блокер устранена
полностью.

*Что осталось:* во втором вызывающем аренда выполнена, но результат не использован и
не возвращён — см. **A-31**.

**✅ A-10 — ИСПРАВЛЕНО в `954f091`.** `FiniteStateMachine.Switch` содержал
безусловный `Debug.Log` (`FiniteStateMachine.cs:47`), интерполирующий имена типов
обоих состояний при каждом переходе; он не был обёрнут в условную компиляцию и не
проверял `Debug.isDebugBuild`. Ещё два таких лога были в `WaitingState.cs:9,17`.
Все три закомментированы (`FiniteStateMachine.cs:48`, `WaitingState.cs:10,19`).
Закомментированы, а не удалены — при включённой отладке вернутся одним снятием
комментария; в релизе логов нет, но и кода нет, а `Debug.Log` на каждом переходе
в `Switch` в горячем пути стоило бы обернуть в `[Conditional]`.

**❌ A-11 — НЕ ИСПРАВЛЕНО.** `ObjectPool<Tile>` в `FieldView` создаётся с
`collectionCheck: true` (`FieldView.cs:27`), что включает валидацию дублирующихся
аллокаций на каждом `Get`/`Release`. Это рекомендуемое значение по умолчанию для
разработки, но измеримая цена в релизной сборке.

**❌ A-12 — НЕ ИСПРАВЛЕНО.** `FieldStates.GetTransition` возвращает `null` для
неконфигурированного перехода (`FieldStates.cs:80`), и `FiniteStateMachine.Switch`
молча ничего не делает (`FiniteStateMachine.cs:49-53`). Отсутствующая запись
перехода останавливает игровой цикл без какой-либо диагностики. Коммит убрал
единственное сообщение в этой точке (`Debug.Log`, A-10), то есть диагностика на
этом пути стала **ещё слабее**, а не лучше.

**❌ A-13 — НЕ ИСПРАВЛЕНО.** `FieldBlackboard.Reset()` — мёртвый код. Объявлен на
`BlackBoard.cs:55-70` и не имеет ни одной точки вызова во всём `Assets/Game`
(проверено поиском). Он обнулял бы `CurrentMatches` и `CurrentBonuses`; на практике
`EvaluationState.cs:14-18` очищает те же два списка на месте с null-проверкой, а
затем передаёт их в `MatchEvaluator.Evaluate` / `SpawnEvaluator.EvaluateBonusSpawn`
как out-параметры, так что ветка с обнулением никогда не выполняется.
`EnsureBlockerList()` (`BlackBoard.cs:72`) *при этом используется* — в
`RemoveState.cs:56` и `FillUpState.cs:100`.

**❌ A-14 — НЕ ИСПРАВЛЕНО.** `Field.IsInBounds` не используется, а аксессоры массива
не защищены. `GetTileAt(Vector2Int)` (`Field.cs:55`), `SetTileAt` (`Field.cs:87`) и
`ClearTileAt` (`Field.cs:89`) индексируют напрямую без проверки границ — все три
однострочные. `IsInBounds` (`Field.cs:53`) имеет ровно одно вхождение в проекте: своё
собственное объявление. `BFS.Run` и `TwoPointers.Run` выполняют собственные проверки;
слой состояний — нет.

**❌ A-15 — НЕ ИСПРАВЛЕНО.** Жизненный цикл слушателей в `IdleState` асимметричен.
`OnFieldEvent` регистрируется в шине `Input` в `Enter` (`IdleState.cs:29`) и снимается
при первом вводе (`IdleState.cs:71`); `OnFinalEvent` заново регистрирует
`OnFieldEvent` в своей ветке не-финала (`IdleState.cs:45`) и снимает `OnFinalEvent` в
начале (`IdleState.cs:40`). Асимметрия намеренная, но не документирована, к тому же
`OnFieldEvent` объявлен `public` в ассете ScriptableObject (`IdleState.cs:64`), а не
`private`.

**❌ A-16 — НЕ ИСПРАВЛЕНО.** `SpawnEvaluator.Evaluate` пишет пробные плитки в
снапшот вызывающего (`SpawnEvaluator.cs:63-67`), и `MatchEvaluator.EvaluateTile`
также пишет синтетическую клетку (`MatchEvaluator.cs:76`). Оба мутируют параметр,
документированный как входной. Кроме того, `EvaluateTile` создаёт `new Guid()` на
каждую пробу (`MatchEvaluator.cs:76`), который затем отбрасывается, а `SpawnEvaluator`
создаёт ещё один `Guid.NewGuid()` для уже принятого решения (`SpawnEvaluator.cs:65`).
Коммит, вынося циклы, это не затронул.

**❌ A-17 — НЕ ИСПРАВЛЕНО.** `MatchInfo.Positions` хранит значения `Guid`, а не
позиции (`MatchEvaluator.cs:9`), хотя поле называется `Positions`. Все потребители
(`RemoveState.cs:30`, `FieldViewController`) трактуют их как идентичности.
`SpawnEvaluator` строит параллельный `List<Vector2Int>` в `EvaluationState.cs:34-47`
для позиционного поиска, так что одновременно удерживаются оба представления
совпадения.

**❌ A-18 — НЕ ИСПРАВЛЕНО.** `FieldViewController.cs:254` содержит нерешённый TODO:
*«разделить както чтобы падало по 1 линии типа за раз»* (разбивать падение каскада по
линиям, а не запускать всю волну сразу). Подтверждено: `MatchField` обрабатывает
весь diff поля за один проход, а `PlayNext` строит одну сгруппированную
последовательность на всё.

**❌ A-19 — НЕ ИСПРАВЛЕНО.** `GameplayEventBus<T>` — статический словарь
(`GameplayEventBus.cs:4,6`) без очистки при перезагрузке домена. Обработчики снимаются
в `OnDisable` по всему проекту, но сам словарь переживает перезагрузку. При включённой
опции «Enter Play Mode Options / no domain reload» устаревшие обработчики могут
пережить перезагрузку сцены, если порядок `OnDisable` окажется неверным. `Trigger`
кэширует делегат до вызова (`GameplayEventBus.cs:32`), поэтому подписка/отписка *во
время* диспетчеризации не влияет на уже начатый вызов — слушатели, добавленные в
середине диспетчеризации, отложены до следующего срабатывания.

**❌ A-20 — НЕ ИСПРАВЛЕНО.** Повторный вход (`Reentrancy`) никак не ограничен.
`Trigger` вызывает синхронно, а обработчики часто сами вызывают `Switch`
(например, `IdleState.cs:50`) или снова `Trigger` (`LevelController.OnFieldSettled` →
`GameplayEventBus<bool>.Trigger(FieldSettled)` на `LevelController.cs:134,147,161`,
изнутри обработчика `FieldSettled`). Очереди диспетчеризации или защиты от
повторного входа нет.

**❌ A-21 — НЕ ИСПРАВЛЕНО.** Тестовое покрытие отсутствует. Нет тестовых сборок, нет
каталогов `Tests` и нет `asmdef` в `Assets/Game` (проверено: 0 и 0). Алгоритмы в
`BFS`, `TwoPointers`, `MatchEvaluator`, `SpawnEvaluator` и `Euclide` чистые и
непосредственно пригодны для юнит-тестирования, но ничем не проверяются. Именно
отсутствием тестов объясняется, что регрессия A-31 (см. ниже) не была поймана.

**❌ A-22 — НЕ ИСПРАВЛЕНО.** Ни одного assembly definition в `Assets/Game`. Весь
рантайм-код компилируется в дефолтную `Assembly-CSharp` без изоляции между
симуляцией, представлением и сторонним кодом. `Assets/Scripts` и `Assets/Resources`
пусты. Следствие: проверки границ (A-14) и мёртвый код (A-13, A-24) не отсекаются ни
компилятором, ни архитектурными границами.

**❌ A-23 — НЕ ИСПРАВЛЕНО, формулировка уточнена при повторной проверке.**
`ScoreManager.AddScore(int)` — пустой метод (`ScoreManager.cs:13-16`), тело состоит
только из фигурных скобок. `CalculateScore` (`ScoreManager.cs:18-26`) объявлен
`internal` и не имеет ни одной точки вызова. `ScoreData`
(`ScriptableObjects/ScoreData.cs`) мутируется только по этому мёртвому пути, поэтому
счёт не начисляется никогда.

*Уточнение к исходной формулировке.* Ранее здесь было написано, что «ни один компонент
не подписывается на шину `GameEvent.Score`». Это неверно: `TaskListController`
подписывается на неё дважды (`TaskListController.cs:22,31`). Но подписка нужна ему
для собственной логики прогресса целей, а не для начисления счёта, — обработчик не
обращается к `ScoreManager`. `ScoreManager` сам не подписан ни на одну шину, поэтому
`CalculateScore`, единственный код, пишущий в `ScoreData`, недостижим. Вывод замечания
не меняется, причина сформулирована была неточно.

**❌ A-24 — НЕ ИСПРАВЛЕНО.** `Components/TileTypeChances.cs` объявляет
`TileSpawnChance` в пространстве имён `Assets.Game.Components`, на которое не
ссылается ни один другой файл, а сам `TileSpawnChance` нигде не инстанцируется.
Мёртвый код; кроме того, пространство имён расходится с глобальным, используемым всеми
остальными типами проекта.

**❌ A-25 — НЕ ИСПРАВЛЕНО.** `WoolController.HandleMatchDestroyedVisual` вызывает
`_ropeDataTexture.Apply()` на каждом шаге твина (`WoolController.cs:103`) внутри
колбэка `Tween.Custom`, загружая на GPU всю текстуру `RGBAFloat` каждый кадр для
каждой активной нити, тогда как CPU-массив `_texturePixels` обновляется корректно.
`PrimeTween` используется по всему проекту для композиции `Sequence`/`Chain`/`Group` и
своего параметра отмены `target:` (`WoolController.cs:110`), но сам `WoolController` не
пулится так, как пулится поле, — его замыкание в `Tween.Custom` захватывает
`targetRopeIndex`.

**❌ A-26 — НЕ ИСПРАВЛЕНО.** Поток уровня завершается на `null`-уровне, а не явным
конечным состоянием. `LevelController.StartLevel` делает ранний выход и активирует
`EndOfLevelsController`, когда `Levels.GetLevelSettings(id)` возвращает `null`
(`LevelController.cs:79,87-91`); FSM при этом не вводится в работу.
`LevelsMenuController.RunInfiniteLevel()` передаёт `-1`
(`LevelsMenuController.cs:47-50`), что ведёт именно по этому пути.

**❌ A-27 — НЕ ИСПРАВЛЕНО.** `LevelController.OnFieldSettled` пишет
`Blackboard.IsWin` при каждой стабилизации (`LevelController.cs:144`) из кода слоя
UI, используя `MaxSteps - Step > 0` как предикат победы, дублирующий часть логики
`FinalState` (`FinalState.cs:20-30`). Решение о победе разделено между двумя слоями.

**❌ A-28 — НЕ ИСПРАВЛЕНО.** `LevelController.OpenSettingsMenu`
(`LevelController.cs:209-219`) проверяет `_confirmMenu` вместо `_settingsMenu` перед
инстанцированием (`LevelController.cs:211`). Условие скопировано из
`OpenConfirmMenu` (там же строка 199) и является дефектом копипасты: если
`_confirmMenu` не равен `null`, то `_settingsMenu` разыменовывается, оставаясь при этом
потенциально `null`. Ветка `else` при этом всё равно инстанцирует — то есть при уже
открытом меню подтверждения будет создан дубль.

**❌ A-29 — НЕ ИСПРАВЛЕНО.** `FinalState.ConvertRemainingStepsToBonuses` ничем не
ограничен сверху. Цикл (`FinalState.cs:46`) ограничен `totalCells` и
`spawnedCount < stepsLeft` (`FinalState.cs:61`), но если на поле меньше обычных плиток,
чем `stepsLeft`, бонусов создастся меньше, чем осталось ходов, тогда как
`Blackboard.Step` увеличивается только на число реально созданных
(`FinalState.cs:65`) — арифметика верна, но оставшиеся ходы молча отбрасываются, а не
выводятся наружу.

**❌ A-30 — НЕ ИСПРАВЛЕНО.** `AnimationData.Delay` записывается, но никогда не
читается. `MatchField` вычисляет `waveDelay = (pos.x * 0.12f) + (pos.y * 0.12f)` плюс
`Random.Range(0f, 0.16f)` и присваивает его в `Delay` как для действий `Move`, так и
для `Spawn` (`FieldViewController.cs:141-142,166-168,189-191`), но `PlayNext`
игнорирует поле и выводит собственный `rowDelay` из `item.To.x`
(`FieldViewController.cs:298,302`). Таким образом, диагональная волна и случайный
джиттер на каждую плитку вычисляются — включая `UnityEngine.Random.Range`, то есть с
побочным эффектом — один раз на анимируемую плитку и отбрасываются. Три
закомментированные формулы задержки на `FieldViewController.cs:140-141,166-167,189-190`
показывают, что тайминг подбирался итерациями; действующим является вовсе не это
значение.

---

### Новые замечания, внесённые коммитом `954f091`

**🆕 A-31 — `FillUpState.ProcessSafeBlockers` арендует `HashSet<Guid>` из пула и не
использует его.** `FillUpState.cs:105-106`:

```csharp
var emptyMatchedIds = HashSetPool<Guid>.Get();
emptyMatchedIds.Clear();
```

Далее метод выполняет обход поля и вызывает
`rule.ShouldDestroy(pos, snapshot, null)` (`FillUpState.cs:120`) — переданный
аргумент не имеет отношения к `emptyMatchedIds`. Переменная не читается нигде до конца
метода, и `HashSetPool<Guid>.Release(emptyMatchedIds)` отсутствует
(`RemoveState.cs:96` такой вызов содержит, `FillUpState.cs` — нет).

Два следствия: (1) `HashSetPool` теряет по одному элементу на каждый вызов
`FillUpState` — пул не возвращается в исходное состояние, что обесценивает пулинг на
этом пути; (2) имя `emptyMatchedIds` вводит в заблуждение: по замыслу (сравните с
`RemoveState.cs:62`) здесь должен был быть пустой общий набор, передаваемый в
`ShouldDestroy` третьим аргументом. Фактическое поведение при этом **не изменилось**:
`BlockerAdjacentMatchRule.cs:14` явно проверяет `matchedIds == null || matchedIds.Count
== 0` и возвращает `false`, поэтому правила смежности по-прежнему самопротивосточаттся,
а после гравитации применяются только позиционные правила — как описано в §4.7.
`NullReferenceException` не возникает. Дефект регрессионный по качеству кода,
но не функциональный.

**🆕 A-32 — Двойная очистка общего буфера `visited` на каждой пробной вставке.**
`SpawnEvaluator.Evaluate` очищает арендованный буфер целиком перед каждым вызовом
(`SpawnEvaluator.cs:46`, `Array.Clear(visited, 0, visited.Length)`), после чего
`MatchEvaluator.EvaluateTile` очищает его же повторно
(`MatchEvaluator.cs:65`, `Array.Clear(visited, 0, r * c)`). Внутренняя очистка
избыточна: внешняя уже очистила весь буфер, а именно это нужно для корректности — с
нять устаревшего хвоста, оставшегося от возможно большей предыдущей аренды. Строка
`//Array.Clear` была бы уместна, но `r * c`-вариант выполняет работу впустую
`6 × (число пустых клеток)` раз на одно заполнение. Побочно отмечено: `EvaluateTile`
больше не владеет буфером, поэтому очистка переместилась к вызывающему, но
контракт метода («очистить `visited` перед запуском») не отражён в его сигнатуре и не
задокументирован.
