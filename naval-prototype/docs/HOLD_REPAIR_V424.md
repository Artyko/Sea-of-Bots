# v424 — finish a patch before evacuation

Restored the user's requested exception to the 22-second air trigger. On the home ship, if it is not already lost and the currently active patch needs at most four more seconds, a sailor may finish before escaping when actual air exceeds remaining work plus max(8 s, estimated escape reserve minus 4 s). No delay applies to a task still being approached or an already lost ship. The ordinary evacuation threshold remains 22 seconds.

Validation: build and HoldSafetyChecks, including allowed completion, insufficient air, excessive remaining work and a long exit path.
