![](logo.png)
# KindredLogistics for V Rising 1.0!
KindredLogistics is a server modification for V Rising that adds expansive features like stashing, crafting, pulling, searching for items, conveyor system for chain crafting, and auto stashing of servant inventories.

It is entirely server side, and you can double tap R with your inventory open to stash, or double click the sort button to stash. (Legacy .stash is also available) Contained within a territory!
You can pull items from your chests for crafting by right clicking the recipe in the crafting station!
Servants will autostash their inventories into chests or mission overflow chests (Label them "spoils").
Never lose where your stuff is again! Use .finditem to find where your items are stored!
Tired of running around from station to station to make something? No worries! Use the conveyor system to link chests and refining inventories for chain crafting!


This was a collaborative effort from Odjit(Dj) and Zfolmt(Mitch). 

Feel free to reach out on discord to either (odjit) or (zfolmt) if you have any questions or need help with the mod.

[V Rising Modding Discord](https://vrisingmods.com/discord)

PLEASE READ THE FEATURE SUMMARY AT THE BOTTOM OF THIS PAGE FOR A QUICK OVERVIEW OF THE FEATURES!

## Player Commands
### Enable/Disable
- `.logistics sortstash`
  - Turns on or off your personal ability to use the sort button in your inventory to stash items.
  - Shortcut: *.l ss*
- `.logistics craftpull `
  - Turns on or off your personal ability to pull items from chests for crafting in stations.
  - Shortcut: *.l cr*
- `.logistics dontpulllast`
  - Turns on or off your personal ability to not pull the last item from a container for Logistics commands.
  - Shorcut: *.l dpl*
- `.logistics autostashmissions`
  - Turns on or off your servants from autostashing into chests/Mission overflow chests.
  - Shortcut: *.l asm*
- `.logistics conveyor`
  - Turns on or off your personal ability to set up linked inventories for converying crafting materials.
  - Shortcut: *.l co*
- `.logistics settings`
  - Shows the enabled/disabled status of the above systems.
  - Shortcut: *.l s*


### Stash Commands

- `.stash`
  - Will send all items besides the hotbar in your inventory to chests on the territory you are in. Requires allyship or ownership. Always enabled.
  - Shortcut: Double Tap the "Sort" Button in your inventory, or while hovering over inventory open double tap R (default keybinding for sort.)
- `.pull (item) (quantity)`
  - will take items out of your chests to your inventory in the amount and kind specified
  - Example: *.pull plank 50*
- `.finditem (item)`
  - conducts a search for an item and returns the name of the chests that contain said item. Also creates a beam to the chest you guide you there.
  - Shortcut: *.fi (item)*



### Admin Commands
- `.logisticsglobal sortstash`
  - Turns on or off the availability to players for the ability to use the sort button in their inventory to stash items.
  - Shortcut: *.lg ss*
- `.logisticsglobal craftpull `
  - Turns on or off the availability to players for the ability to pull items from chests for crafting in stations.
  - Shortcut: *.lg cr*
- `.logisticsglobal autostashmissions`
  - Turns on or off the availability to players for servants from autostashing into chests/Mission overflow chests.
  - Shortcut: *.lg asm*
- `.logisticsglobal conveyor`
  - Turns on or off the availability to players for the ability to set up linked inventories for converying crafting materials.
  - Shortcut: *.lg co*
- `.logisticsglobal settings`
  - Shows the enabled/disabled status of the above systems.
  - Shortcut: *.lg s*



 ### Feature Summary
 
**Stashing**
  - Colloquially known as Quickstash thanks to previous mods by iZastic and others, players are able to send all items in their inventory (items in hotbar will be ignored) to containers with matching items in their territory. This can be done via '.stash' or by double-clicking the sort button in your inventory after toggling on the feature with '.l ss'.
  - Exclude stashes by naming them and adding two single appostraphes to the end of the chest name. Ex: `IronOre''` will not be stashed to.
  - Players are also able to pull items to their inventory with '.pull (item) (quantity)' and search for items in stashes with '.finditem (item)'. The latter will guide you to stashes that contain the item you searched for.
  - Servants returning from missions are able to stash their inventories automatically if '.l asm' is toggled. Players are also able to set up a mission stash by naming it 'Spoils' which servants will stash to if no matching items are found in other stashes. Multiple mission stashes can be setup in advance incase they become full. Items will remain in the inventory of the servant otherwise.

**CraftPull**
  - Players are able to request items for recipes in crafting stations by right-clicking on the recipe to pull the ingredients if found and '.l cr' is toggled on. This works for all recipes except for jewel crafting at this time and can be done multiple times for larger batches of crafting. Players can toggle '.l dpl' on and off to prevent or allow the pulling of the last remaining quantity of an ingredient for a recipe.

**Conveyors**
  - Players can setup groups of refining stations that work in tandem to avoid excessive manual relocation of materials from station to station. Refining stations named 's0' will send required ingredients for recipes to other refining stations named 'r0', and multiple groups can be defined by using other numbers. For example, a sawmill and furnace both named 's0' will supply another sawmill named 'r0' with the ingredients needed for reinforced planks as the ingredients are produced. This can be used to create complex production chains to automate the refining of various materials and further controlled by toggling recipes on and off in the station menus.
  - Conveyor stations can be named S0R0 to both send and receive items. So for example you can have a chest with ore be "IronOreS1" sending out ore to a furnace named "IronFurnace R1S1". The Iron furnace will receive from the chest, and send out bars to the next station (in this case, a sawmill labelled "ReinforcedPlanksR1", or a chest labelled "IronIngotsR1" that has at least 1 ingot in it to store the output.

**Reminder**
  - If you have dontPullLast enabled, nothing, including crafting, will take the only version of an item from a chest. This is useful for keeping a chest with a single item in it for stashing purposes, but if you're crafting and you have exact mats stored you might want to toggle it off.
  - Find item beams will only be present for one beam per chest. So if two players are searching planks on the same plot, only one will keep the beam.
  - Check what you have enabled with `.l s` if you feel a system isn't working.
  - Stash will only send out items that exactly match that which you have stored. If it doesn't stash, you don't have a chest with that item in it, or the chest is full.
  - If a search query for an item returns multiple results, you can select it with :index.
  - Example: `.pull (item:1) (quantity)` to get the first result when more than one result is returned.
    
 

