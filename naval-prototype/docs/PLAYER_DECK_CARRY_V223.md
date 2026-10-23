# Player deck carry v223

User reports continuous slipping on Galleon after v222. Latest log confirms v222 Galleon/Galleon, but does not contain individual rider contact traces. Source inspection found two model-dependent assumptions: support casts used root height plus startup footOffset, and rigid carry rotated the root while the character controller/body remains upright. Root-to-foot offsets therefore rotate with the hull and can shift actual feet sideways during roll/pitch.

Common implementation for all ship profiles:
- Support origin uses actual enabled CharacterController bounds sole (bounds centre X/Z and min Y), not a startup root offset. World-space controller radius is retained. Sphere starts with its bottom 0.25 m above the sole and sweeps 0.5 m, so it seeks a real nearby surface and does not latch to a lower hold from another deck. Existing ship membership, normal threshold and exact fallback filtering remain.
- Capture local sole position plus current world root-minus-sole vector before each ship movement. Carry the sole through the ship transform and add the upright vector without rotating it. Own/enemy ships share NavalRiderCarry.Position. Real controller Move, walking, falling and jumping remain enabled.
- Pre-native-movement water-entry support uses the same helper and checks own/enemy ship. Existing swimming immersion policy is unchanged.

Validation: full build; actual shared carry helper exercised against mocked transform math for 8100 moving/rolling steps at ship scales 2/3/4, multiple deck and root heights. Foot position remains invariant relative to deck; root-rotation reference reproduces lateral displacement. This does not simulate native controller depenetration, input or PhysX, and is not proof of live gameplay success. Restart and a fresh Galleon battle are required to verify the reported slipping.
