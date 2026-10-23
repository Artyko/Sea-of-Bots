# Plank access v228

Latest v227 Brigantine battle contains 30 repair job assignments and zero Repair finished reports, plus repeated pump assignments, underflight recoveries and return stalls. Therefore completed hull repair is not demonstrated by this battle. No plank acquisition/failure messages existed, so it cannot establish which individual attempt failed at which supply stage.

Concrete source mismatch: reported actual sailor radius is 0.80 m. Old hold pickup is 1.30 m from crate centre; crate outer half width is about 0.675 m. Remaining clearance 0.625 m is less than the capsule radius, so ClearBody rejects the nominal pickup. Side alternatives only shifted Z and did not remove that overlap.

Hold pickup distance now 1.65 m from crate centre. Brigantine/Galleon approach outside the stock columns; Sloop approaches from the central aisle because its outer gap to ballast cannot fit the 1.6 m-wide capsule. No crate positions, resource counts or repair priorities changed. Route repair discards mandatory floor detour corners inside another obstacle/stair exclusion, preserving actual treads/top landings and final endpoints. Shared graph remains current-layout based.

Added Planks fetched / Plank trip failed messages with sailor ID, ship and count/stage; combined with Repair finished, the next log can distinguish supply access, transit and actual patching.

Validation: full build, 3514 task routes, 656 cargo routes/stock tests; new checks verify pickup-to-crate clearance for actual 0.8 m radius and space before ballast, plus underflight regressions. No native PhysX/repair gameplay test. A new battle is required for changed pickup locations and proof of successful repair.
