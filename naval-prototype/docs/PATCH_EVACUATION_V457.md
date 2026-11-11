# v457: committed patch evacuation

Log explicitly shows evacuation at air=22 while task=Repair 10%, repair_s=0: active completion exemption excluded internal barriers and walkway repairs. Extended active/pending patch classification to hull, barriers and walkway holes, excluding long mechanism jobs. Pending work requires boards only when the repair material policy requires them.

For accepted supplied repair approaches, allow <=8 s remaining travel when air covers travel+repair+0.5 s, independent of estimated escape path. Swimming-site eligibility uses the same rule. Stalled/waiting/supply-only jobs still cannot use this exemption. General 22-second evacuation and drowning remain. Already-started eligible patch remains committed even at zero air.

Build, HoldSafetyChecks, diff check passed. Remaining dry-route obstruction behaviour not certified fixed.
