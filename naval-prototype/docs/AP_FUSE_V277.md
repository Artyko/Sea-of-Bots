# v277 AP arming

Main battery AP (existing projectilePower > 2 classification) arms after perforating armour, or after a first metallic obstacle followed by another metallic obstacle / mechanism. A first metal or machinery contact alone does not arm main battery AP. Smaller AP arms after its first perforated metal, machinery or armour contact. Wood does not arm either class.

First contact is remembered across flight substeps. Coplanar neighbours and adjacent hydraulic shell seams do not count as a second metallic obstacle. Unarmed perforating contacts retain compact kinetic damage and continue the sorted sweep without a burst. Once armed, the next-obstacle / two-second behavior remains unchanged.

ShellFuseChecks covers both classes, wood, non-perforations, machinery and seam discrimination. Build passed; live battle behavior remains unverified.
