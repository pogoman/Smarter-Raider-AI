# Smarter Raider AI
Tynan says Rimworld is a storyteller, not a skill test. This mod is for people who want it to be both.

This mod focuses on reworking and prioritising the Rader AI decision making tree to be more in line with what a player would do, all whilst attempting to maintain vanilla performance. This is done by leveraging the existing vanilla rimworld avoid grid used in smart raids and extending it to incorporate player pawns as well (not just turrets).

## Summary of main changes:
All enemy pawns will now avoid areas your turrets or drafted pawns have line of sight to (yes no more killboxes).
Note: If a turret or pawn is engaged in a firefight the avoid grid will be temporarily disabled for that unit, so positions can still be overrun if there are more raiders than defenders. Trap killboxes will still work in the early game but not if your pawns have overwatch.
Any raider with a weapon defined as breachable (set in mod options), will now attempt to breach first (regardless of whether it is a breach or normal raid). By default these are weapons including the keywords "stickbomb, concussion, frag, rocket, inferno, chargeblast, thermal, thump". i.e. a raider carrying a doomsday will fire it at your walls if there is no better way into your base.
Raiders will prioritise pillaging and destroying colony buildings and will take the safest path to do this. If there is no safe path and they have no breachers then they will sap/attack walls in the safest locations.

Due to all of the above it is much harder to 'Game' the AI. Strategic defence of the whole colony and not just one entrance is very important.

## Compatibilities:

Can add/remove from save without issue.

Compatible with combat extended.

Tentatively compatible with CAI (more testing required, may cause performance issues).

(This mod must be loaded after both of the above.)

Incompatible with Careful Raids.
