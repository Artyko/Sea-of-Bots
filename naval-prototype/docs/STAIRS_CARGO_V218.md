# Stair routes and cargo v218

Evidence: latest Player.log runs v217 Brigantine/Brigantine. Two eight-second stalls at (0.34,-1.32,-1.36) and (0.36,-1.31,-1.29), both targeting (0.47,-1.29,-1.78); duties Going to planks and Returning.

Underflight recovery previously required a target on stairs. It now detects a sailor below a flight regardless of task/target, retaining physical body and segment clearance checks before sideways recovery. No teleport or collision disabling added.

Shared supply routes avoid flight undersides on maintenance floors. Visibility repair includes staircase corners, preventing crate detours from reentering the flight footprint. Cargo groups move clear of the stair zone, preserving stocks and pickup offsets; side-changing crossings remain inside hull length. Standby posts move to outer lanes clear of grouped cargo. All profiles and both teams share these layouts. Existing ships require a new spawn/battle for layout changes.

Validation: full mod build; 3514 directed task routes including floor-level stair exclusion (lower-flight top landing segments exempt), 656 cargo delivery routes and stock checks, ascent/descent capsule approximation, underflight tests including both exact v217 stalls and both galleon flights, eight opposite-direction stair routes. These are geometric/pure tests, not a native Unity playtest. Damage, moving ships and real capsule interaction still require a fresh battle log.
