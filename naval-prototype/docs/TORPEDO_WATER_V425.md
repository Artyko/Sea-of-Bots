# v425 — torpedoes inside dry compartments

Latest log records two Dreadnought torpedo hits (651/701 m travel) but not intermediate compartment water states. Inspection confirmed that old movement forced the torpedo to ocean level minus depth regardless of interior flooding. Torpedoes now query actual interior water near hulls. With no water supporting the body they lose powered water movement, retain damped horizontal inertia and fall under gravity. Real obstacle contacts still detonate an armed warhead. A fully flooded compartment permits water travel; no ocean-level wake is emitted during dry motion.

Performance: one normal step and direct ocean query offshore; cheap swept proximity gating before interior queries; up to .05 s steps only near hulls. No per-frame collections were added. Existing collision buffers are reused. Added TorpedoMotion timing to battle performance telemetry to verify the actual cost; local tests cannot guarantee unchanged FPS.

Validation: build, TorpedoWaterChecks and TorpedoStateChecks. The exact runtime dry-hold crossing and frame cost require the next battle log.
