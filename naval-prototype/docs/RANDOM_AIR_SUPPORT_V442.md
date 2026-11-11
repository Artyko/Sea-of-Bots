# v442: random convoy air support

At battle start choose uniformly from six outcomes: no aviation, 2, 3, 4, 5 or 6 waves. Selection persists for that battle and is recorded in logs. Existing random arrival timer and 3 km fixed approach retained. First wave has five aircraft, subsequent waves six. Separate air-raid mission retains its default three waves. Runtime wave limit and aircraft generation use the same selected count. Build and RandomAirSupportChecks passed; diff check clean.
