# v415 — helm access and ammunition runners

The v414 battle repeatedly reported the Dreadnought hand at (.45,1.99,3.29) being sent toward weather-deck points instead of finishing the bridge ascent. Bridge flight detection now accepts the measured positive controller support offset. A worker already on the bridge flight and going to an upper post continues along the remaining ascent instead of descending and climbing again.

A separate Novik bug kept dynamically trained gun posts at the old hardcoded 2.3 m service distance, overriding the shared 3.6 m layout. Dynamic and initial posts now agree.

Ready-use crate pickup positions no longer alias the gunner seat: they are derived from the actual crate position with a 1.6 m inboard offset. Runners have eight approach directions at 1.35 m around that pickup point. They can handle ammunition within 1.35 m horizontally and .65 m vertically when supported and with a clear hand-to-transfer-point path, ignoring friendly body shapes but retaining walls. They keep the existing gun reservation, one-second handling animation and counted ammunition transfer. Hull survival priorities are unchanged.

Validation: bridge regression recreates the logged captain pose and rejects downward detours; CargoAccountingChecks and AmmoPriorityChecks cover resource accounting and maintenance priorities. The older CargoChecks currently fails because it assumes one 20-round unit per rack, while racks were previously grouped in pairs; cargo layout/resource budgets were not changed in v415. Actual gameplay access still needs confirmation by the next battle log.
