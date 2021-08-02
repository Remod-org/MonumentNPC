# MonumentNPC
Autokill NPCs spawned by Rust to protect monument puzzles.

The default configuration shown below will only kill NPCs spawned at the Airfield and Trainyard.  For convenience, a list of allMonuments is kept in the config for the admin to use later as these new NPCs get added to the game.

You can also set killAtAllMonuments to true to kill NPCs at the excavator and oil rigs, or anywhere else they may spawn.

This should not affect NPC plugins at the time of writing.

## Configuration
```json
{
  "debug": false,
  "killAtAllMonuments": false,
  "killMonuments": [
    "Airfield",
    "Trainyard"
  ],
  "allMonuments": [
    "Airfield",
    "Bandit Town",
    "Cave Small Easy",
    "Cave Small Hard",
    "Cave Small Medium",
    "Compound",
    "Entrance Bunker A",
    "Entrance Bunker B",
    "Entrance Bunker C",
    "Entrance Bunker D",
    "Excavator",
    "Fishing Village A",
    "Fishing Village B",
    "Fishing Village C",
    "Gas Station",
    "Harbor 2",
    "Junkyard",
    "Large Oilrig",
    "Launch Site",
    "Lighthouse",
    "Military Tunnel",
    "Mining Quarry A",
    "Mining Quarry B",
    "Mining Quarry C",
    "Powerplant",
    "Radtown Small 3",
    "Satellite Dish",
    "Small Oilrig",
    "Sphere Tank",
    "Stables A",
    "Stables B",
    "Supermarket",
    "Swamp A",
    "Swamp B",
    "Swamp C",
    "Warehouse",
    "Water Treatment Plant",
    "Water Well B",
    "Water Well C",
    "Water Well D",
    "Water Well E"
  ],
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 1
  }
}
```
