## Llama Studio v1.4.3 — Benchmark Center

### RU

**Новое**

- **Benchmark Center** — полная переработка проверки скорости на странице
  «Мониторинг»: вместо одиночного теста — центр тестов llama.cpp
  (r101–r107, r112):
  - тесты: **Быстрый** (большая карточка Ответ/Промпт tok/s сверху),
    **Prefill** (эффективный prefill по точкам контекста), **Depth**
    (TG по глубине контекста), **ENGINE** (llama-bench PP/TG), **Full**;
  - пресеты (Quick/Medium), warm-up, «свои точки», безопасный максимум
    контекста из профиля;
  - живые таблицы и графики (полосы с подписанными значениями, hover),
    столбцы мощности GPU (В/карта) и VRAM-пик, MTP acceptance;
  - история прогонов (JSON): выбор, экспорт/импорт, удаление;
  - **сравнение 2–4 историй** в слотах A–D: графики накладываются полосами
    в цветах слотов (легенда «цвет = история»), таблица PP/TG с Δ% и ×N
    относительно истории A (r112);
  - выбор теста — выпадающий список, под ним — динамическое описание
    «Как это работает — <тест>» (r112);
  - меню: «Мониторинг» переименован в **«Бенчмарк Центр»** (r112);
  - ENGINE: авто-стоп/авто-возврат llama-server, автоопределение
    capabilities, точный парсер вывода llama-bench (схемы b11042+ и
    старые), сырой вывод сохраняется в
    `~/.config/LlamaStudio/benchmarks/raw/` для диагностики (r108–r111);
  - тест переживает навигацию по вкладкам (баннер «идёт тест»), пер-точечный
    таймаут, защита от падения сервера во время sweep (r103, r105);
   - **полная локализация (RU/EN)**: весь Бенчмарк Центр переведён — подписи,
     тултипы, «Как это работает» (4 теста), заголовки таблицы, статусы,
     ошибки, прогресс и диалоги; переключение языка приложения в реальном
     времени перестраивает панель **без потери состояния** (выбранный тест/
     пресет/точки/история/слоты A–D) (r114).
   - **Benchmark Center в Web**: весь центр проверки скорости теперь доступен
     и во встроенном веб-интерфейсе (страница переименована «Мониторинг» →
     «Бенчмарк Центр»): выбор теста/пресета, «свои точки», запуск/остановка,
     живая таблица и графики PP/TG, история (экспорт/импорт/удаление),
     сравнение 2–4 историй в слотах A–D (Δ%/×N к базе) — RU/EN, общее
     состояние с десктопным центром (r116).

**Исправления**

- Выбор GPU-карт при старте: профиль с 2 картами мог показывать все карты
  и запускать сервер на них (гонка первого скана GPU); выбор теперь строго
  следует профилю (r99).
- Зависание оболочки при скачивании моделей и проверке контрольных сумм
  (длительная работа вынесена из UI-потока) (r100).
- Цвета Benchmark Center: кислотно-зелёный/фиолетовый на чёрном → палитра
  темы (мягкий зелёный #10B981, синий #3B82F6); починена битая привязка
  полосы прогресса; авто-раскрытие панели при старте; тултипы во всех полях
  (r102).
- Уход со страницы «Мониторинг» во время теста больше не обнуляет страницу:
  тест и результаты сохраняются (Benchmark Center — единый для всех вкладок)
  (r103).
- Тест не запускается без запущенного llama-server — понятное сообщение
  (кроме ENGINE, который сам управляет сервером) (r103).
- ENGINE: пустые результаты и 2.4 часа GPU (двойной prefill `-d`, 6
  перезагрузок модели вместо 1) — исправлено; JSON-разбор из смешанного
  stdout; поддержка реальной JSON-схемы llama-bench b11042+ — ENGINE теперь
  показывает настоящие цифры PP/TG (r108–r111).
- Кнопки сервера при загрузке модели: «Запустить» заблокирован, «Остановить»
  доступен (была гонка, заставлявшая нажимать «Запустить» повторно); защита
  от двойного запуска (r111).
- Графики Benchmark Center перерисовываются вместе с таблицей при входе на
  страницу (было: после теста/смены вкладки графики «нет данных», пока не
  перевыберут историю) (r113).
- Бенчмарк Центр: смена языка приложения (RU↔EN) больше не ломает панель —
  исправлено исключение «The Control already has a parent» при повторном
  переключении (r115).
- Web, «Бенчмарк Центр»: секция поднята под карточку скорости
  «Измерить скорость» (была ниже GPU-графиков) — порядок секций теперь как в
  десктопном приложении (r117).
- Web, «Бенчмарк Центр»: починен рендеринг графиков PP/TG — столбцы могли
  уходить за пределы поля и налезать друг на друга (максимум шкалы считался
  из пустого поля, координаты столбцов не учитывали номер точки); теперь
  столбцы стоят по своим точкам, с подписями и значениями (r118).
- Web: исправлено **зависание страницы** при переключении веб-интерфейса на
  английский: бесконечный цикл перевода на пункте «Engine — llama-bench
  (PP/TG)» — observer-переводчик вечно переписывал тот же текст (значение
  перевода совпадало с оригиналом); теперь одинаковое значение не
  переписывается, самоциклы удалены из словаря (r118).
- Web: добавлен недостающий перевод кнопки «Экспорт» → «Export» (r118).

**Сборки v1.4.3**

- Linux: self-contained, linux-x64 (папка ~270 файлов, `LlamaStudio`).
- Windows: self-contained single-file `LlamaStudio.exe` (win-x64).
- Обе ОС — один номер, сборки идут параллельно с каждого релиза (релиз
  r113; r103–r112 публиковались только для Linux, Windows-сборка r113
  включает все эти изменения).

### EN

**New**

- **Benchmark Center** — a full rework of the speed testing on the
  Monitoring page (r101–r107, r112):
  - tests: **Quick** (big Answer/Prompt tok/s card on top), **Prefill**
    (effective prefill across context sizes), **Depth** (TG vs context
    depth), **ENGINE** (llama-bench PP/TG), **Full**;
  - presets (Quick/Medium), warm-up, custom points, safe context maximum
    from the profile;
  - live tables and bar charts (labeled values, hover), per-GPU power (W)
    and VRAM peak columns, MTP acceptance;
  - run history (JSON): pick, export/import, delete;
  - **compare 2–4 histories** in slots A–D: charts overlay per-history
    bars in slot colors (legend "color = history"), PP/TG table with Δ%
    and ×N vs history A (r112);
  - test selection is a dropdown with a dynamic "How it works — <test>"
    description under it (r112);
  - menu: Monitoring renamed to **Benchmark Center** (r112);
  - ENGINE: auto stop/restore of llama-server, capability auto-detection,
    exact llama-bench output parsing (b11042+ and legacy schemas), raw
    output saved to `~/.config/LlamaStudio/benchmarks/raw/` for
    diagnostics (r108–r111);
  - a running test survives tab navigation (banner), per-point timeouts,
    protection from a dead server mid-sweep (r103, r105);
   - **full localization (RU/EN)**: the whole Benchmark Center is translated
     — labels, tooltips, "How it works" (4 tests), table headers, statuses,
     errors, progress and dialogs; switching the app language in real time
     rebuilds the panel **without losing state** (selected test/preset/points/
     history/A–D slots) (r114);
   - **Benchmark Center in the web UI**: the full speed-testing center is now
     available in the built-in web interface (page renamed "Monitoring" →
     "Benchmark Center"): test/preset selection, custom points, start/stop,
     live table and PP/TG charts, history (export/import/delete), compare of
     2–4 histories in A–D slots (Δ%/×N vs the base) — RU/EN, shared state
     with the desktop center (r116).

**Fixes**

- GPU card selection at startup: a 2-card profile could show all cards and
  start the server on them (first GPU scan race); selection now strictly
  follows the profile (r99).
- Shell freeze during model downloads / checksum verification (long work
  moved off the UI thread) (r100).
- Benchmark Center colors: acid green/violet on black → theme palette
  (soft green #10B981, blue #3B82F6); fixed the broken progress binding;
  panel auto-opens at start; tooltips on every field (r102).
- Leaving the Monitoring page mid-test no longer resets the page: the test
  and its results are kept (Benchmark Center is shared across tabs) (r103).
- Tests do not start without a running llama-server — a clear message
  (except ENGINE, which manages the server itself) (r103).
- ENGINE: empty results and 2.4 h of GPU (double prefill `-d`, 6 model
  reloads instead of 1) — fixed; JSON extraction from mixed stdout; real
  llama-bench b11042+ schema support — ENGINE now shows true PP/TG numbers
  (r108–r111).
- Server buttons while a model is loading: Start is locked, Stop is
  available (was a race that made users press Start repeatedly); double
  start is now guarded (r111).
- Benchmark Center charts are redrawn together with the table on page
  entry (was: "no data" after a run / tab switch until a history was
  re-picked) (r113).

- Benchmark Center: switching the app language (RU↔EN) no longer breaks the
  panel — fixed the "The Control already has a parent" exception on the
  second switch (r115).
- Web "Benchmark Center": the section now sits right under the speed card
  ("Measure speed") instead of below the GPU charts — section order now
  matches the desktop app (r117).
- Web "Benchmark Center": fixed PP/TG chart rendering — bars could be
  drawn off-canvas and stacked on each other (the scale max was computed
  from an empty field and bar x-coordinates ignored the point index); bars
  now sit at their points with labels and values (r118).
- Web: fixed a **page freeze** when switching the web UI to English: an
  infinite translation loop on the "Engine — llama-bench (PP/TG)" entry —
  the i18n observer kept rewriting the same text (the translation value
  was identical to the original); identical values are no longer rewritten
  and the self-cycles are removed from the dictionary (r118).
- Web: added the missing "Экспорт" → "Export" translation (r118).
**v1.4.3 builds**

- Linux: self-contained, linux-x64 (folder, ~270 files, `LlamaStudio`).
- Windows: self-contained single-file `LlamaStudio.exe` (win-x64).
- Both OS, one release number, built in parallel from every release
  (release r113; r103–r112 were Linux-only, the r113 Windows build
  contains all of those changes).
