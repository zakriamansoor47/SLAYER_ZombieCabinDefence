# Accepting Paid Request! Discord: Slayer47#7002
# Donate: If you like my work, you can donate to me via [Steam Trade Offer](https://bit.ly/3qDpgPd)

## Description:


## Installation:
**1.** First download this map to your server: [Zombie Cabin Defence Map](https://steamcommunity.com/sharedfiles/filedetails/?id=3171695956) 

**2.** Then download this plugin and add it to your server (don't replace **cfg** folder).
- **addons**
- **script**

**3.** Change the Map **or** Restart the Server **or** Load the Plugin.

**4.** Now it's time to add some important ConVars to your **sever.cfg** file
```
mp_autoteambalance 0
mp_limitteams 0
mp_forcecamera 1 // Set this to 0 to allow players to spectate any team. Set this to 1 to allow players to only spectate their own teammp_ignore_round_win_conditions 1
mp_startmoney 0
ammo_grenade_limit_default 9999999
ammo_grenade_limit_flashbang 9999999
ammo_grenade_limit_total 9999999
mp_weapons_allow_typecount 999999
mp_solid_teammates 1

// Bot Stuff
bot_quota_mode "normal" // change it to 'fill' if zombies not spawning according to JSON file
bot_difficulty 3
bot_join_after_player true
bot_chatter off
bot_join_team t  // bots (Zombies) should always join the Terrorist team
mp_bot_ai_bt "scripts/ai/custom/bt_default.kv3" // Load custom AI script for BOTs which is provided with plugin. (Note: this AI script is specially designed for this cabin map only)

```


## Commands:

## Configuration:
