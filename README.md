![](logo.png)
# KindredLogistics for V Rising 1.0!
KindredLogistics is a server modification for V Rising that adds expansive features like stashing, crafting, pulling, searching for items, conveyor system for chain crafting, and auto stashing of servant inventories.

It is entirely server side, and you can double tap R with your inventory open to stash, or double click the sort button to stash. (Legacy .stash is also available) Contained within a territory!
You can pull items from your chests for crafting by right clicking the recipe in the crafting station!
Servants will autostash their inventories into chests or mission overflow chests (Label them "spoils").
Never lose where your stuff is again! Use .finditem to find where your items are stored!
Tired of running around from station to station to make something? No worries! Use the conveyor system to link chests and refining inventories for chain crafting! Label 


This was a collaborative effort from Odjit(Dj) and Zfolmt(Mitch). 

Feel free to reach out on discord to either (odjit) or (zfolmt) if you have any questions or need help with the mod.

[V Rising Modding Discord](https://vrisingmods.com/discord)

## Player Commands
### Enable/Disable
- `.logistics sortStash`
  - Turns on or off your personal ability to use .stash
  - Shortcut: *.l ss*
- `.logistics craftpull `
  - Turns on or off your personal ability to pull items from chests for crafting in stations.
  - Shortcut: *.l cr*
- `.logistics dontPullLast`
  - Turns on or off your personal ability to not pull the last item from a container for Logistics commands.
  - Shorcut: *.l dpl*
- `.logistics autoStashMissions`
  - Turns on or off your servants from autostashing into chests/Mission overflow chests.
  - Shortcut: *.l asm*
- `.logistics conveyor`
  - Turns on or off your personal ability to set up linked inventories for converying crafting materials.
  - Shortcut: *.l co*
- `.losgistics settings`
  - Shows the enabled/disabled status of the above systems.
  - Shortcut: *.l s*


### Stash Commands

- `.stash`
  - Will send all items besides the hotbar in your inventory to chests on the territory you are in. (Requires allyship or ownership)
  - Shortcut: Double Tap the "Sort" Button in your inventory, or while hovering over inventory open double tap R (default keybinding for sort.)
- `.pull (item) (quantity)`
  - will take items out of your chests to your inventory in the amount and kind specified
  - Example: *.pull plank 50*
- `.finditem (item)`
  - conducts a search for an item and returns the name of the chests that contain said item.
  - Shortcut: *.fi (item)*



### Admin Commands Commands
- `.logisticsglobal sortStash`
  - Turns on or off the availability to players for the ability to use .stash
  - Shortcut: *.lg ss*
- `.logisticsglobal craftpull `
  - Turns on or off the availability to players for the ability to pull items from chests for crafting in stations.
  - Shortcut: *.lg cr*
- `.logisticsglobal autoStashMissions`
  - Turns on or off the availability to players for servants from autostashing into chests/Mission overflow chests.
  - Shortcut: *.lg asm*
- `.logisticsglobal conveyor`
  - Turns on or off the availability to players for the ability to set up linked inventories for converying crafting materials.
  - Shortcut: *.lg co*
- `.losgisticsglobal settings`
  - Shows the enabled/disabled status of the above systems.
  - Shortcut: *.lg s*
 

