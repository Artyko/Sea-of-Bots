## v139 — 2% ignition and cannon access

Ordinary hull-hit ignition is now 2%. Powder chances unchanged. Firefighters search inner approach corridors at local x=+/-1.35, +/-0.9 as well as +/-1.8, with sight, full-body clearance and supported-deck checks. This enables approaching burning cannon-area cells from behind the weapon instead of only alongside its carriage. Existing area dousing extinguishes active cannon fires in the same radius as hull fires.

## v138 — firefighting water sources and separate areas

Firefighters rank sea collection posts and wet, accessible lower-hold cells by summed world-space length of the shared deck/stair route from their current position. Intake support, body clearance and same-deck route segments are checked. Failed collection positions are excluded during job replans. Source selection runs only on dispatch/replanning; movement does not repeatedly scan the flood grid. Bilge water is removed at collection time and carried as fireWater mass; partially drained cells supply only their remainder, empty sources trigger reselection. Sea collection retains the full 1.8 m3 bucket. Fire claims now exclude targets within 2.2 world metres of another active firefighter target, so critical assistance handles another area. Normal one-firefighter / critical two-firefighter limits remain.

## v137 — stronger powder blast

Hull damage radius increased from 3.2 to 6 world metres and impact power from 2 to 3. Every ship within the blast receives the common voxel damage operation; nearby powder can enter its existing warning fuse chain. All robot teams, including the player, receive lethal damage within 5.5 m (formerly approximately 2.7 m), using the existing scripted-kill behaviour. Flash, flame expansion, fragment velocity and smoke size are enlarged, retaining fifty batched particles and the 512-particle shared cap. Ignition probabilities and ordinary cannonball holes are unchanged.

## v136 — native flame visuals

Inspected local Assembly-CSharp.dll using Mono.Cecil: MechBodyPart.UpdateMe emits native burning-voxel particles through Singleton<GlobalFireParticleSystem>.Instance.SpawnSingleBig(Vector3), which emits one particle into FireSystem without damage callbacks. Ship fires and lit powder now use the same API at the existing 4 Hz cadence; manager lookup is cached with five-second retries. Existing block flames remain a fallback when the native scene effect is unavailable. Custom smoke, wetting, ignition probabilities and damage remain unchanged. Native flame appearance requires a live game check.

## v134 — distant powder blast visibility

Powder explosions now stage a broad orange flash, radial flame burst, ballistic wood/ember fragments and a larger rising smoke cloud. Fifty batched particles per blast share the existing 512-particle capacity; a shadowless point light fades in 0.35 seconds and its helper is destroyed after 0.8 seconds. Smoke dissipates through the existing opacity palette. Damage radius, kill radius and powder probabilities are unchanged.

## v131 — incoming water and smoke outlets

Hull leaks, wet gunports and overtopped rim samples wet fires in a localized incoming stream corridor. This calls wetting only, without adding water a second time. Deck plank centres within 0.25 world metres below the walking surface count as surface fires, fixing phantom centre-hatch smoke. Real hold fire vents through the open stair hatch and grouped damaged weather-deck openings, cached by hull revision and tested for a clear exit. No hold fire means no vent smoke. A ship-wide round-robin budget emits two long-lived smoke puffs per second; nearby surface fires and deck holes are grouped. Existing smoke dissipates naturally after extinguishing. Build and pure behaviour checks pass; no live Unity visual test.

## v127 — ordinary impact ignition

Ordinary cannon impacts now use a 5% ignition roll, replacing v125's 1%. Dry surviving fuel is still required. Powder-barrel outcomes remain unchanged.

## v125 — ordinary impact ignition

Ordinary cannon impacts now have a 1% ignition roll (previously 0.1%). Ignition still requires a dry, flammable surviving cell at the impact rim. Powder barrel outcomes remain 40% fuse / 10% instant explosion / 50% neither.

## v124 — exposed deck powder supply

Moved the existing 1/2/3 barrels to the weather deck on sloop/brigantine/galleon. Supplies alternate sides of the central stairs at local (1.15,-2.3), (-1.15,2.1), with the galleon third at (1.15,-4.8) near the aft station. Their height follows the ship weather deck; no extra submerged storage remains. Automated layout checks cover central passage, mast foundations and gunner stations on every profile. Existing firefighting uses the same shared target positions and prioritization.

## v123 — wooden barrel appearance

Powder barrels now measure 1.25 m across and 1.8 m high (previously 1.0 x 1.6 m). Alternating warm wood staves, darker lid rims and two dark metal hoops on the exterior distinguish them from the hull. They retain the same 128 destructible voxels per barrel, hold locations and v122 ignition rules. Geometry checks cover passage clearance on every ship profile.

## v122 — firefighting assignment and direct hits

These rules supersede the historical probability and crew notes below, for every ship profile.

- A dry powder barrel hit by a normal cannonball uses one categorical roll: 10% immediate explosion, 40% ignition with the existing 12-second fuse, 50% neither. Wet powder stays protected. Debug ignition and blast-chain ignition retain a warning fuse.
- Immediate explosions are queued until LateUpdate, after hull damage finishes using shared collision buffers. A spent barrel cannot detonate twice.
- Normally only one sailor claims firefighting work aboard a ship. At most two may work when two powder barrels are lit, a barrel has less than five seconds of fuse remaining, or there are at least sixteen burning cells. Each claims a separate target; this does not override critical flood pumping or the final boarding attack.
- Burning powder is selected first. An ordinary-fire worker checks once per second for an unclaimed reachable barrel and switches targets, retaining a filled bucket.
- Water collection checks twelve positions along both weather-deck rails for body clearance. Blocked routes try another collection point, or another approach to the fire, with at most three replans and a bounded overall job duration. Falling switches to rescue instead of continuing the walking route.
- Successful bucket pouring, bilge water accounting, wetting, spread and burn lifetimes are unchanged.

Validation: full mod compilation and pure fire/powder, layout/smoke, crew priorities, skeleton crew and broadside checks. Route clearance and movement require an in-game check; no Unity play session was run.

## v121 — редкий пожар от ядра, крупные бочки в трюме и высокий дым

Шанс поджечь дерево обычным сухим попаданием снижен с 12% до 0,1% (один из тысячи бросков). Появление пробоины от обычного неогневого повреждения само по себе по-прежнему не зажигает пожар. Попадание в сухую пороховую бочку теперь даёт 85% на фитиль вместо 45%; предупреждение 12 с, тушение и намокание сохранены. Взрыв пороха и уже существующий огонь остаются отдельными источниками загорания.

Бочки перенесены на пол нижнего трюма. Общий профиль задаёт размеры в мировых метрах: высота 1,6 м, диаметр 1 м, 128 разрушаемых вокселей, суженные торцы, широкая середина, красноватое дерево и два тёмных обруча. Количество — 1/2/3 по кораблям. Центральная лестница и помпа свободны, между бочками и бортовыми балластными баками оставлен проход. Новый радиус обнаружения попадания соответствует увеличенному размеру бочки.

Появился высокий дымовой столб: частицы постепенно набирают высоту примерно до 28 м за 12 с, расширяются и переходят через восемь степеней прозрачности. Вверху дым светлее и разреженнее. Подпалубный пожар выводит основной наружный столб через центральный лестничный люк; низкий дым остаётся у огня. Не более четырёх наружных источников на корабль, существующий общий пул на 512 частиц сохранён. У взрыва тоже появляется долгий поднимающийся дым. Это косметическое приближение вентиляции, не симуляция газов.

Проверки: сборка и шесть наборов C#. PowderLayoutSmokeChecks проверяет границу 0,1%, форму 128 вокселей, размеры и проходы для трёх профилей, ступени прозрачности и подъём 28 м при разных шагах времени. Также FirePowderChecks (теперь 85%), CrewPrioritiesChecks, SkeletonCrewChecks, CannonImpactChecks и StrongerBroadsideChecks. Внешний вид, материал прозрачности на GPU и реальные проходы в Unity ещё требуют игровой проверки.

# v119 — порох, пожары и тушение

Первая общая реализация для шлюпа, бригантины и галеона, обеих команд. Полный список предложений хранится отдельно в [NAVAL_MECHANICS_IDEAS.md](NAVAL_MECHANICS_IDEAS.md).

## Бочки

На открытой палубе появляется 1 / 2 / 3 воксельных бочки соответственно. Они стоят сбоку от центральных проходов, а не на лестницах или у стволов пушек. Бочки входят в общие списки повреждений, структурных связей, коллайдеров и рендера до их построения.

Попадание рядом с порохом делает один случайный бросок на бочку: 45% на загорание. Не один бросок на каждый удаляемый воксель. Подводные ослабленные ядра не зажигают порох. Две секунды близкого огня также поджигают его. После загорания остаётся 12 с: видны искры, подпись с отсчётом и слышно пространственное шипение. Вода отменяет фитиль и делает порох мокрым до полного восстановления корабля. Погружение бочки в воду делает то же самое.

Взрыв повреждает реальную геометрию в радиусе 3,2 м, с усиленной мощностью против мачт. Проверяются оба корабля, поэтому близко стоящие суда могут пострадать вместе. Роботы в пределах примерно 2,7 м погибают независимо от команды. Соседний сухой порох получает собственный полный предупредительный фитиль, а не мгновенную рекурсивную детонацию. Сработавшая бочка повторно не взрывается. Если оставшаяся геометрия бочки отделилась, точка пороха следует за ней; если геометрия полностью разрушена, горящая пороховая смесь остаётся в последней точке до тушения/взрыва.

## Огонь

Горючесть задаётся по зарегистрированным материалам дерева и ткани, а не по цвету. Механизмы пушек и помпы не считаются горючим деревом. От обычного сухого попадания с вероятностью 12% загорается сохранившийся край повреждения. Огонь может распространяться на близкие целые воксели. Для проверки можно вручную поджечь целую доску, парус или бочку клавишей Y.

Дерево прогорает за 26 с суммарного горения, ткань за 10 с. Сначала появляется обугливание, затем воксель удаляется обычным SetMissing: остаётся настоящая дыра, пересчитываются связи и поддержка. Тушение не восстанавливает уже сгоревшее дерево. Мокрый воксель защищён от повторного загорания 18 с. Подводный огонь гаснет. Долгое нахождение робота непосредственно в пламени (около 6 с) приводит к гибели; это отдельный таймер воздействия, а не постепенное изменение штатных HP частей робота.

## Игрок

- **B держать 1,2 с** возле борта открытой палубы либо доступной воды — набрать ведро до 1,8 м³. У борта это условный набор из моря через борт; полноценной симуляции верёвки нет.
- **V** — вылить воду на выбранный огонь/фитиль в пределах 8 м от камеры. При отсутствии выбранного очага можно вылить воду в направлении взгляда, в том числе за борт.
- **Y** — тестовый поджог выбранного горючего вокселя или фитиля бочки, также в пределах 8 м. Мокрый порох повторно не загорается.
- **Shift+R** — прежнее полное восстановление: прекращает пожары, сбрасывает бочки и восстанавливает повреждённую геометрию.

Есть ведро, видимая вода и анимация выливания с блочными брызгами. Вода из трюма действительно забирается из сетки. Переносимый объём игрока и матросов учитывается в массе/крене. Вода, вылитая внутри закрытых палуб, возвращается в сетку; вода с открытой палубы условно стекает за борт. Это упрощение стока, отдельной жидкости на открытой палубе пока нет.

## Матросы

Свободный матрос резервирует доступный очаг, набирает воду у борта, следует общими маршрутами и тушит его. Подбираются точки с проверкой тела и прямой видимости. Сам горящий воксель в конце луча не считается препятствием, но стены по дороге считаются. Есть ограничитель задачи 35 с и пауза после блокировки пути; за один поиск проверяется не более четырёх очагов, выбор циклически смещается.

Обычный пожар не отнимает единственного ремонтника у активных течей. Горящий порох или пять очагов становятся срочной задачей: можно снять последнего канонира. При отсутствии других свободных рук рулевой может временно помочь. Обычная откачка может быть прервана ради срочного пожара, критическая откачка — нет. Уже принятая заплата не бросается ради огня. После тушения матрос снова ищет работу или возвращается на боевой пост. Последняя атака без пушек сохраняет прежний приоритет абордажа.

## Ограничения первого этапа

Пушки пока не расходуют запас бочек: отдельного боезапаса и связи мокрого пороха с невозможностью выстрела нет. Перенос/выбрасывание целых бочек игроком, огонь на отделённых деревянных обломках, снижение парусов ради тушения и распространение по ветру оставлены на следующий этап. Матросы не получают способность доставать до верхушки мачты: недоступный с палуб огонь может уничтожить верхние паруса. Внешний вид, реальные проходы и баланс пожаров требуют проверки в Unity.

## Производительность и проверки

На корабле не более 32 активных горящих вокселей. Логика выполняется с шагом 0,25 с, распространение раз в секунду. Полная сетка топлива индексируется один раз при построении, каждый кадр весь корпус не обходится. Эффекты используют существующий ограниченный пакетный рендер дыма; новый ParticleSystem/свет на каждый очаг не создаётся. Для баррелей используются два общих звуковых клипа на корабль (фитиль и взрыв) и несколько источников для фитилей.

FirePowderChecks проверяет выгорание дерева/ткани, мокрые состояния, границу вероятности, предупреждение, последний момент тушения, намокание, единственность взрыва, тепловое/цепное загорание, выбор пожарного и сохранение воды в ведре/трюме. Дополнительно выполняются проверки экипажа, сокращённого экипажа, попаданий, движения ядра в воде, остойчивости, рендера и объединённых коллайдеров. Это проверки C# и сборки с DLL игры; живой бой, GPU/PhysX и FPS не измерены.

В определении полного погружения галеона дополнительно учтено смещение верхней палубы: здоровый сухой трюм не должен считаться открытым морем только потому, что средняя палуба находится у ватерлинии.
