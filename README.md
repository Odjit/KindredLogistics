![](logo.png)
# KindredLogistics for V Rising 1.0!
KindredLogistics is a server modification for V Rising that adds expansive features like stashing, crafting, pulling, searching for items, conveyor system for chain crafting, and auto stashing of servant inventories.

- It is entirely server side, and you can double tap R with your inventory open to stash, or double click the sort button to stash. (Legacy .stash is also available) Contained within a territory!
- You can pull items from your chests for crafting by right clicking the recipe in the crafting station!
- Servants will autostash their inventories into chests or mission overflow chests (Label them "spoils").
- Never lose where your stuff is again! Use .finditem to find where your items are stored!
- Tired of running around from station to station to make something? No worries! Use the conveyor system to link chests and refining inventories for chain crafting!

This was a collaborative effort from Odjit (Dj) and Zfolmt (Mitch). 

Feel free to reach out on discord to either Odjit or Zfolmt if you have any questions or need help with the mod.

[V Rising Modding Discord](https://vrisingmods.com/discord) | [How to Install Mods on BepInEx](https://wiki.vrisingmods.com/user/Mod_Install.html) | [How to Use Mods In-Game](https://wiki.vrisingmods.com/user/Using_Server_Mods.html)


[![logwouldyou](https://github.com/user-attachments/assets/4412fd55-cf6d-488b-9e40-77fba9f83afa)](https://github.com/Odjit/KindredLogistics/wiki)
Check out the details on the WIKI by clicking above!

# Commands Overview

## Player Commands

| Command                          | Description                                                                                   | Shortcut     |
|----------------------------------|-----------------------------------------------------------------------------------------------|--------------|
| .logistics sortstash             | Turns on or off your personal ability to use the sort button in your inventory to stash items. | .l ss        |
| .logistics craftpull             | Turns on or off your personal ability to pull items from chests for crafting in stations.      | .l cr        |
| .logistics dontpulllast          | Turns on or off your personal ability to not pull the last item from a container.              | .l dpl       |
| .logistics autostashmissions     | Turns on or off your servants from autostashing into chests/Mission overflow chests.         | .l asm       |
| .logistics conveyor               | Turns on or off your personal ability to set up linked inventories for crafting materials.    | .l co        |
| .logistics silentstash            | Turns on or off the ability to have stash not report where you stash items.                   | .l ssh       |
| .logistics silentpull             | Turns on or off the ability to have pull not report where you pull items from.                | .l sp        |
| .logistics settings               | Shows the enabled/disabled status of the above systems.                                       | .l s         |

## Stash Commands

| Command                          | Description                                                                                   | Example                |
|----------------------------------|-----------------------------------------------------------------------------------------------|------------------------|
| .stash                           | Sends all items besides the hotbar in your inventory to chests in your territory.           |                        |
| .pull (item) (quantity)         | Takes specified items out of your chests to your inventory.                                  | .pull plank 50         |
| .finditem (item)                | Searches for an item and returns the names of the chests that contain it.                   | .fi "Blood Essence"        |

## Admin Commands

| Command                          | Description                                                                                   | Shortcut     |
|----------------------------------|-----------------------------------------------------------------------------------------------|--------------|
| .logisticsglobal sortstash       | Turns on or off the ability for players to use the sort button in their inventory.           | .lg ss       |
| .logisticsglobal craftpull       | Turns on or off the ability for players to pull items from chests for crafting.              | .lg cr       |
| .logisticsglobal autostashmissions | Turns on or off the ability for servants to autostash into chests.                         | .lg asm      |
| .logisticsglobal conveyor         | Turns on or off the ability for players to set up linked inventories for crafting materials. | .lg co       |
| .logisticsglobal settings         | Shows the enabled/disabled status of the logistics systems.                                  | .lg s        |




 

This mod is licensed under the AGPL-3.0 license.
