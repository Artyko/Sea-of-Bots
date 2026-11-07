# v423 — fixed evacuation reserve and Novik ramp access

Escaping flooded hold now begins only with the head submerged and actual air <=22 seconds, independent of estimated exit length or sunk flag. A near-complete patch can no longer delay the threshold. Surface air restoration and safe-resumption rules remain. Since breath capacity is 30 seconds, this leaves about eight seconds of submerged work before evacuation starts.

Latest log showed repeated Novik approach/return stalls at forecastle ramp points around (.36,.96,3.37) and lower access treads. The 25.5–29.5 m forecastle ramp now has explicit passage classification, including support height; destroyer ramp arrival accepts the recorded .34–.43 m longitudinal capsule offsets while rejecting below-tread shortcuts. Other hull classes keep their existing stair policy.

Validation: build, HoldSafetyChecks, DestroyerRampRegressionChecks and StairTrafficChecks. Remaining congestion and access near damaged equipment require gameplay confirmation.
