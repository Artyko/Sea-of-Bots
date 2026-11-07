# Авианалёт на конвой

## Миссия

Отдельная карта паровой эпохи. Игрок защищает дредноут в сопровождении трёх броненосцев. Эсминцы тоже получают ПВО и могут использовать её на других картах. Воздушные волны включаются только маркером новой миссии.

- Три волны. Стартовый баланс — 2, 3 и 3 биплана. Каждый несёт четыре бомбы по 50 кг.
- Первая волна появляется после короткой паузы. Следующая начинается через 20 секунд после того, как все самолёты предыдущей сбиты или покинули район. Это стартовое значение для настройки.
- Победа, если дредноут пережил третью волну. Поражение, если дредноут затонул. Потери эскорта отражаются в итогах.
- Игрок управляет кораблём или занимает зенитную установку. Самолётами управляет ИИ.

## Полёт и атака

1. Звено появляется за границей видимого района и летит к конвою.
2. Каждый пилот выбирает корабль на плаву. Дредноут имеет повышенный приоритет, часть звена может атаковать эскорт.
3. Пилоты заходят по очереди с небольшим разносом. Прицел учитывает курс и скорость корабля и время падения бомбы. Самолёт сбрасывает бомбы короткой серией, затем выходит из пикирования.
4. Если цель затонула до сброса, пилот выбирает новую. Уже сброшенная бомба летит по своей траектории. Если доступных целей больше нет, самолёт уходит.
5. После атаки самолёт уходит за пределы района и удаляется. Сбитый самолёт исключается из состава волны. Для застрявшего ИИ нужен предельный срок вылета, после которого он уходит без сброса.

Бомба использует существующие проверки попаданий по вокселям. Попадание в корабль повреждает палубу и оборудование и может вызвать пожар. Попадание в воду даёт всплеск. Радиус повреждений, влияние брони и вероятность пожара подбираются по первому тесту миссии.

## Корабельная ПВО

Зенитные пулемёты доступны на эсминце, броненосце и дредноуте. Стартовый баланс — две, три и четыре установки соответственно. Точки установки определяются профилем корабля. Пулемёты не перекрывают лестницы, пути к постам и сектора поворота башен.

Матрос наводит пулемёт с упреждением и стреляет очередями по самолёту в пределах сектора. Палуба и надстройки блокируют огонь. Игрок занимает установку через E, наводится мышью и стреляет ЛКМ. Повреждённая установка теряет возможность стрелять по тем же правилам, что корабельные механизмы. Красные трассеры визуально ориентированы на Spidertron.

## Интерфейс и проверки

Плашка показывает номер волны, самолёты в воздухе и паузу до следующей волны. Итоги включают сбитые самолёты, сброшенные бомбы и попадания, точность ПВО, потери кораблей и матросов.

Проверить полную последовательность из трёх волн, выход всех самолётов из боя, смену цели при затоплении, победу и поражение, ручную и автоматическую ПВО на каждом классе. Замерять ИИ звена, бомбы, ПВО и трассеры отдельно. Трассеры переиспользовать через пул, поиск целей ПВО распределять между установками, проверку препятствия выполнять перед выстрелом.

## Реализация v403

- ПВО занимает посты при появлении самолётов в воздухе.
- На дредноуте передние установки находятся на боковых площадках капитанской надстройки, кормовые на отдельной поднятой площадке. Для кормовой площадки и поднятой башни X предусмотрены вертикальные лестницы. Игрок использует E возле конца лестницы.
- Ширина лестницы надстройки увеличена до 2,65 м, входа до 3,3 м. Башни X/Y сближены с 7,5 до 6,9 м между центрами с сохранением проходов у погребов.
- Бипланы идут на высоте около 180–220 м и переходят в пикирование. Радиус разброса точки бомбометания составляет 36 м.
- Крылья состоят из отдельных панелей. Попадание убирает соответствующую панель. Разность потерь слева и справа влияет на курс.
- Двигатель начинает дымить при попадании и выводится из строя после четырёх попаданий в его зону. Попадание в пилота лишает самолёт управления.
- Повреждённая геометрия самолёта собирается в общие меши по материалам. Перестройка происходит при разрушении секции.

## Изменение v405

Кормовые ПВО 3/4 перенесены к z=-3,06 модели в отдельные круглые гнёзда радиусом около 2 м на высоте 8,88 м над палубой. Гнёзда имеют независимые вертикальные лестницы, переход между ними осуществляется через основную палубу. Передние боковые площадки надстройки открыты сверху и снабжены опорами до палубы.

Камера ручного ПВО смотрит вдоль ствола, по выходе возвращается к исходному положению. Разброс плавно увеличивается от 1,8 до 4,8 градуса по мере роста дистанции. Компас над экраном следует за направлением камеры и содержит метку выхода конвоя.


### v406 — damage, crew access and player sights

- Aircraft connectivity is built once from part bounds. A destroyed attachment releases disconnected panels into combined, falling visual debris; debris is removed from aircraft hit tests immediately. No rigidbody per panel.
- AA opens fire immediately after acquiring aim; the native bullet interval still controls cadence.
- Bombs emit 24 visible fragments with bounded crew hits and hull obstruction, plus a six-second rising smoke plume.
- Aft AA ladder movement completes before generic deck recovery, preserving vertical endpoints. Working sailors can yield by job priority and stable ID while keeping their task. Repair routes finish at an unobstructed reachable edge instead of demanding arrival over a missing deck tile.
- Manual main-battery control excludes the operator from automatic entrance detection, supports mouse aiming and a barrel-aligned sight, and permits independent elevation when traverse is obstructed.
- Funnel mouths have black insets and raised destructible rims. The distant generic station prompt is hidden.
- Local verification: build, AA deck routing, air-raid release calculations, turret doors, deck-seam repair, stair traffic and crew movement. In-game battle verification remains with the player.


### v409 — air support for destroyer interception

The original seven-ship convoy mission retains both playable sides. A one-shot support call occurs when fewer than three destroyers survive or the flagship has 1000 m or less to the exit. It starts the existing three-wave raid (2/3/3 aircraft, four 50 kg bombs each) at 3000 m, always from northeast. Targets are limited to the convoy, independent of the player's side. The convoy's AA crew assignment activates on the existing airborne-threat policy. Losing the final destroyer does not end the mission until called air support has finished; flagship loss, exit and timeout still finish the mission and stop the raid.

Destroyers use separated flank/longitudinal approach offsets beyond 600 m. Every torpedo launch, including delayed second tubes, checks closest predicted approach against friendly ships over the torpedo lifetime using actual ship motion and a conservative full-length clearance. This prevents friendly launches rather than suppressing friendly damage.
