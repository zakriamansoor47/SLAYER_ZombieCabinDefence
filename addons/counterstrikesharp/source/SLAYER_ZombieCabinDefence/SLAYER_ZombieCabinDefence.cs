using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using Serilog;

namespace ZombieCabinDefense;
// Used these to remove compile warnings
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
public class ZombieCabinDefenseConfig : BasePluginConfig
{
    [JsonPropertyName("ZCD_PluginEnabled")] public bool ZCD_PluginEnabled { get; set; } = true;
    [JsonPropertyName("ZCD_HudText")] public bool ZCD_HudText { get; set; } = true;
    [JsonPropertyName("ZCD_NoBlock")] public bool ZCD_NoBlock { get; set; } = true;
    [JsonPropertyName("ZCD_Freeze")] public float ZCD_Freeze { get; set; } = 10;
    [JsonPropertyName("ZCD_IncreaseHealth")] public int ZCD_IncreaseHealth { get; set; } = 1;
    [JsonPropertyName("ZCD_DecreaseHealth")] public int ZCD_DecreaseHealth { get; set; } = 2;
    [JsonPropertyName("ZCD_TimeBetweenNextWave")] public float ZCD_TimeBetweenNextWave { get; set; } = 30.0f;
    [JsonPropertyName("ZCD_ZombieSpawnMin")] public int ZCD_ZombieSpawnMin { get; set; } = 5; // Minimum Number of Zombies Spawn in First Wave of this Difficulty
    [JsonPropertyName("ZCD_ZombieSpawnMax")] public int ZCD_ZombieSpawnMax { get; set; } = 50;    // Maximum Number of Zombies Spawn till the Last wave of this Difficulty
    [JsonPropertyName("ZCD_ZombieIncreaseRate")] public int ZCD_ZombieIncreaseRate { get; set; } = 2;
    [JsonPropertyName("ZCD_HumanMax")] public int ZCD_HumanMax { get; set; } = 5;
    [JsonPropertyName("ZCD_KillZombies")] public int ZCD_KillZombies { get; set; } = 30;
    [JsonPropertyName("ZCD_IncrementHealthBoostBy")] public int ZCD_IncrementHealthBoostBy { get; set; } = 5;
    [JsonPropertyName("ZCD_IncrementZKillCountBy")] public int ZCD_IncrementZKillCountBy { get; set; } = 5;
    [JsonPropertyName("ZCD_AdminFlagToUseCMDs")] public string ZCD_AdminFlagToUseCMDs { get; set; } = "@css/root";
    [JsonPropertyName("ZCD_Waves")] public List<ZombieCabinDefenseSettings> ZCD_Waves { get; set; } = new List<ZombieCabinDefenseSettings>();
    [JsonPropertyName("ZCD_Zombies")] public List<ZombieCabinDefenseZombieSettings> ZCD_Zombies { get; set; } = new List<ZombieCabinDefenseZombieSettings>();
}
public class ZombieCabinDefenseSettings // Wave Properties
{
    [JsonPropertyName("WaveName")] public string WaveName { get; set; } = "Outbreak";   // Name of the Wave Difficulty
    [JsonPropertyName("ZWaves")] public int ZWaves { get; set; } = 15;  // How many Waves with these Settings/Difficulty
    [JsonPropertyName("ZKillCount")] public int ZKillCount { get; set; } = 15;  // Total number of Zombies need to kill to go to next wave
    [JsonPropertyName("ZHealthBoost")] public int ZHealthBoost { get; set; } = 0; // Health Boost of Zombies in this Wave Difficulty
    [JsonPropertyName("ZRespawnTime")] public float ZRespawnTime { get; set; } = 5.0f;  // After how many seconds the zombie respawn
}
public class ZombieCabinDefenseZombieSettings // Zombie Properties
{
    [JsonPropertyName("ZombieClassName")] public string ZombieClassName { get; set; } = "NormalZombie";
    [JsonPropertyName("ZombieModelPath")] public string ZombieModelPath { get; set; } = "";
    [JsonPropertyName("ZombieInWaves")] public string ZombieInWaves { get; set; } = "Outbreak";
    [JsonPropertyName("ZombieHealth")] public int ZombieHealth { get; set; } = 100;
    [JsonPropertyName("ZombieSpeed")] public float ZombieSpeed { get; set; } = 1.1f;
    [JsonPropertyName("ZombieGravity")] public float ZombieGravity { get; set; } = 0.9f;
    [JsonPropertyName("ZombieJump")] public float ZombieJump { get; set; } = 15.0f;
    [JsonPropertyName("ZombieFOV")] public int ZombieFOV { get; set; } = 110;
}
public class ZombieCabinDefense : BasePlugin, IPluginConfig<ZombieCabinDefenseConfig>
{
    public override string ModuleName => "Zombie Cabin Defense";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "SLAYER";
    public override string ModuleDescription => "Humans stick together to fight off zombie attacks on Cabin";
    public required ZombieCabinDefenseConfig Config {get; set;}
    public void OnConfigParsed(ZombieCabinDefenseConfig config)
    {
        Config = config;
    }
    public ZombieCabinDefenseSettings GetWaveByName(string modeName, StringComparer comparer)
    {
        return Config.ZCD_Waves.FirstOrDefault(mode => comparer.Equals(mode.WaveName, modeName));
    }
    public ZombieCabinDefenseSettings GetWaveByIndex(int wave)
    {
        return Config.ZCD_Waves.ElementAt(wave);
    }
    public ZombieCabinDefenseZombieSettings GetZombieClassByName(string ClassName, StringComparer comparer)
    {
        return Config.ZCD_Zombies.FirstOrDefault(mode => comparer.Equals(mode.ZombieClassName, ClassName));
    }
    public ZombieCabinDefenseZombieSettings GetZombieByIndex(int Zombie)
    {
        return Config.ZCD_Zombies.ElementAt(Zombie);
    }
    public int GetZombieClassIndexByName(string ClassName)
    {
        int ZombieClassIndex = 0;
        foreach(var index in Config.ZCD_Zombies)
        {
            if(index.ZombieClassName == ClassName)
            {
                return ZombieClassIndex;
            }
            ZombieClassIndex++;
        }
        return -1;
    }
    
    public int gZombiesKilled = 0;
    public int gZombieToSpawn = 1;
    public int gCurrentWaveDifficulty = 0;
    public int gCurrentWave = 0;
    public int gIncrementedHealthBoost = 0;
    public int gIncrementedZKillCount = 0;
    public float gNextWaveTime = 0;
    public int[] gZombieID = new int[64];
    public int[] gPlayerZombieKilled = new int[64];
    public float[] gRespawnTime = new float[64];
    public bool[] IsPlayerZombie = new bool[64];
    public bool gShouldStartGame = false;
    // Timers
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_ZFreeze;
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_NextWave;
    public CounterStrikeSharp.API.Modules.Timers.Timer? t_CheckPlayerLocation;
    public CounterStrikeSharp.API.Modules.Timers.Timer[]? tRespawn = new CounterStrikeSharp.API.Modules.Timers.Timer[64];
    public override void Unload(bool hotReload)
    {
        ZCDEnd();
    }
    public override void Load(bool hotReload)
    {
        AddCommand("css_startzombies", "Sets the certain zombie wave", CMD_StartGame);
        AddCommand("css_restartzombies", "Sets the certain zombie wave", CMD_StartGame);
        
        if(Config.ZCD_PluginEnabled)
        {
            gCurrentWaveDifficulty = 0; gCurrentWave = 0;gIncrementedHealthBoost = 0;gIncrementedZKillCount = 0;
            gZombieToSpawn = (Config.ZCD_ZombieSpawnMin < 1) ? 1 : Config.ZCD_ZombieSpawnMin;
            Server.ExecuteCommand("bot_kick");
            Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
            Server.ExecuteCommand("mp_autoteambalance 0");
            Server.ExecuteCommand("mp_limitteams 0");
        }
        RegisterListener<Listeners.OnMapStart>((string mapName) =>
        {
            if(Config.ZCD_PluginEnabled)
            {
                gCurrentWaveDifficulty = 0; gCurrentWave = 0;gIncrementedHealthBoost = 0;gIncrementedZKillCount = 0 ;
                gZombieToSpawn = (Config.ZCD_ZombieSpawnMin < 1) ? 1 : Config.ZCD_ZombieSpawnMin;
                Server.ExecuteCommand("bot_kick");
                Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
                Server.ExecuteCommand("mp_autoteambalance 0");
                Server.ExecuteCommand("mp_limitteams 0");
            }
        });
        RegisterListener<Listeners.OnTick>(() =>
        {
            if(!Config.ZCD_PluginEnabled || !gShouldStartGame)return;
            
            foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !player.IsBot && player.TeamNum > 0))
            {
                if(Config.ZCD_HudText && gNextWaveTime <= 0 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE || player.TeamNum == 1)
                {
                    player.PrintToCenterHtml
                    (
                        $"<font color='red'>☣ </font> <font class='fontSize-m' color='red'>Zombie Cabin Defense</font><font color='red'> ☣</font><br>" +
                        $"<font color='green'>►</font> <font class='fontSize-m' color='gold'>Wave: {gCurrentWave+1}</font> <font color='green'>◄</font><br>"+
                        //$"<font color='green'>►</font> <font class='fontSize-m' color='red'>Zombies Left: {GetZombieToKill()}</font> <font color='green'>◄</font><br>" +
                        $"<font color='green'>►</font> <font class='fontSize-m' color='DodgerBlue'>Humans Left: {GetHumanCount(true)}</font> <font color='green'>◄</font>"
                    );
                }
                else if(t_NextWave != null && gNextWaveTime > 0)
                {
                    player.PrintToCenterHtml // Print next wave Message
                    (
                        $"<font color='red'>☣ </font> <font class='fontSize-m' color='red'>Zombie Cabin Defense</font><font color='red'> ☣</font><br>" +
                        $"<font color='green'>►</font> <font class='fontSize-m' color='gold'>Next Wave will Start in:</font> <font class='fontSize-m' color='red'>{gNextWaveTime}</font> <font color='green'>◄</font><br>" +
                        $"<font color='green'>►</font> <font class='fontSize-m' color='lime'>You can now buy</font> <font color='green'>◄</font>"
                    );
                }
            }
        });
        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            if(!Config.ZCD_PluginEnabled || !gShouldStartGame)return HookResult.Continue;
            ResetZombies();
            RemoveObjectives();
            Server.ExecuteCommand("mp_autoteambalance 0");
            Server.ExecuteCommand("mp_limitteams 0");
            Server.ExecuteCommand($"bot_knives_only"); // Bot Use Only Knife
            BeginWave(false);
            if(t_ZFreeze != null)t_ZFreeze?.Kill();
            if(t_NextWave != null)t_NextWave?.Kill();
            if(t_CheckPlayerLocation != null)t_CheckPlayerLocation?.Kill();
            return HookResult.Continue;
        });
        RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
        {
            if(!Config.ZCD_PluginEnabled || !gShouldStartGame)return HookResult.Continue;
            RemoveObjectives();
            if(t_ZFreeze != null)t_ZFreeze?.Kill();
            if(t_CheckPlayerLocation != null)t_CheckPlayerLocation?.Kill();
            t_CheckPlayerLocation = AddTimer(1.0f, ()=>CheckPlayerLocationTimer(), TimerFlags.REPEAT);
            if(Config.ZCD_Freeze > 0) // Freeze Zombies
            {
                FreezeZombies();
                foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !player.IsBot && player.TeamNum == 3))
                {
                    player.PrintToChat($" {ChatColors.Gold}[{ChatColors.DarkRed}★ {ChatColors.Lime}Zombie Cabin Defense {ChatColors.DarkRed}★{ChatColors.Gold}] {ChatColors.Green}Zombies are {ChatColors.DarkRed}Frozen {ChatColors.Green}for {ChatColors.Gold}{Config.ZCD_Freeze} {ChatColors.Green}seconds!");
                }
                t_ZFreeze = AddTimer(Config.ZCD_Freeze, UnFreezeZombies);
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if(!Config.ZCD_PluginEnabled || @event.Userid == null || !@event.Userid.IsValid)return HookResult.Continue;
            CCSPlayerController player = @event.Userid;
            if(tRespawn?[player.Slot] != null)tRespawn?[player.Slot]?.Kill();
            return HookResult.Continue;
        });
        AddCommandListener("jointeam", (player, commandInfo) =>     // Team Switch Control
        {
            if(Config.ZCD_PluginEnabled && player != null && player.IsValid && commandInfo.ArgByIndex(1) != "0")
            {
                if(commandInfo.ArgByIndex(1) == "1")return HookResult.Continue; // If anyone join to Spectator then he can freely join it.
                if(player.IsBot && commandInfo.ArgByIndex(1) == "3")return HookResult.Handled; // Bot (zombies) are not allowed to join CT
                if(!player.IsBot && commandInfo.ArgByIndex(1) == "2")return HookResult.Handled; // Players (Humans) are not allowed to Join T
                return HookResult.Continue;
            }
            return HookResult.Handled;
        });
        RegisterEventHandler<EventPlayerTeam>((@event, info) =>
        {
            if(!Config.ZCD_PluginEnabled || @event.Disconnect)return HookResult.Continue;
            if(@event.Userid == null || !@event.Userid.IsValid || @event.Userid.Connected != PlayerConnectedState.PlayerConnected || @event.Userid.IsHLTV)return HookResult.Continue;
            CCSPlayerController player = @event.Userid;
            IsPlayerZombie[player.Slot] = player.IsBot;
            if (@event.Team != 1 && @event.Oldteam == 0 || @event.Oldteam == 1)
            {
                AddTimer(0.1f,()=>AssignTeam(player, player.Pawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE));
            }
            return HookResult.Continue;
        }, HookMode.Post);
        
        RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            if(!Config.ZCD_PluginEnabled || !gShouldStartGame || @event.Userid == null || !@event.Userid.IsValid ||  @event.Userid.TeamNum < 2 || @event.Userid.IsHLTV)return HookResult.Continue;
            CCSPlayerController player = @event.Userid;
            gZombieID[player.Slot] = -1;
            gPlayerZombieKilled[player.Slot] = 0;
            IsPlayerZombie[player.Slot] = player.IsBot;;
            AddTimer(0.1f,()=>AssignTeam(player, player.Pawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE));
            if (IsPlayerZombie[player.Slot])
            {
                //InitClientDeathCount(index);
                var moneyServices = player.InGameMoneyServices;
                if (moneyServices != null)moneyServices.Account = 0;
                if (Config.ZCD_NoBlock) // No Block
                {
                    player.PlayerPawn.Value!.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                    player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                    Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                    Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
                }
                var zombies = GetZombiesToSpawn();
                int zombieid = 0;
                Random zombieRandom = new Random();
                if (zombies != null && zombies.Count() > 0)
                {
                    zombieid = zombieRandom.Next(0, zombies.Count());
                    Zombify(player, GetZombieClassIndexByName(zombies[zombieid]));
                }
                else
                {
                    Log.Error($"[SLAYER Zombie Cabin Defense] No Zombie Found!");
                }
                if(gIncrementedHealthBoost > 0) // Health Boost after highest difficulty reach
                {
                    player.PlayerPawn.Value!.Health += gIncrementedHealthBoost; // Set Health boost 
                }
                else player.PlayerPawn.Value!.Health += GetWaveByIndex(gCurrentWaveDifficulty).ZHealthBoost; // Set Health boost 
            }
            else
            {
                player.PlayerPawn.Value!.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            }
            if (tRespawn[player.Slot] != null)
            {
                tRespawn[player.Slot]?.Kill();
            }
            return HookResult.Continue;
        }, HookMode.Post);
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if(!Config.ZCD_PluginEnabled || !gShouldStartGame)return HookResult.Continue;
            if(@event.Userid == null || !@event.Userid.IsValid || @event.Attacker == null || !@event.Attacker.IsValid)return HookResult.Continue;
            CCSPlayerController player = @event.Userid;
            CCSPlayerController attacker = @event.Attacker;
            if (IsPlayerZombie[player.Slot])
            {
                gZombiesKilled++;
                gPlayerZombieKilled[attacker.Slot]++;
                StartZombieRespawnTimer(player);
                if (gZombiesKilled == GetWaveByIndex(gCurrentWaveDifficulty).ZKillCount && gIncrementedZKillCount <= 0)
                {
                    HumansWin();
                }
                else if(gIncrementedZKillCount > 0 && gZombiesKilled == gIncrementedZKillCount) // Kill Count After Highest Difficulty Reach
                {
                    HumansWin();
                }
                if(gPlayerZombieKilled[attacker.Slot] == Config.ZCD_KillZombies)
                {
                    gPlayerZombieKilled[attacker.Slot] = 0; // reset kills
                    attacker.GiveNamedItem("weapon_healthshot"); // Give Healthshot
                }
            }
            else
            {
                var moneyServices = player.InGameMoneyServices;
                if (moneyServices != null)moneyServices.Account = 0; // Setting Cash to 0
                Utilities.SetStateChanged(player, "CCSPlayerController_InGameMoneyServices", "m_iAccount"); // Updating Money
                if (GetHumanCount(true) <= 0) // When all humans die
                {
                    Server.PrintToChatAll($" {ChatColors.Gold}[{ChatColors.DarkRed}★ {ChatColors.Lime}Zombie Cabin Defense {ChatColors.DarkRed}★{ChatColors.Gold}] {ChatColors.DarkRed}Zombie Wins {ChatColors.Lime}- {ChatColors.Gold}Reached Wave {ChatColors.Lime}{gCurrentWave} {ChatColors.DarkRed}(Restarting Round)");
                    ZombiesWin();
                }
            }
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerJump>((@event, info) =>
        {
            if(@event.Userid == null || !@event.Userid.IsValid)return HookResult.Continue;
            CCSPlayerController player = @event.Userid;
            if(!Config.ZCD_PluginEnabled || !gShouldStartGame || !IsPlayerZombie[player.Slot])return HookResult.Continue;
            player.PlayerPawn.Value.AbsVelocity.Z = GetZombieByIndex(gZombieID[player.Slot]).ZombieJump;
            return HookResult.Continue;
        });
    }
    private void AssignTeam(CCSPlayerController? player, bool spawn)
    {
        if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsHLTV)return;
        
        if (IsPlayerZombie[player.Slot]) // If player is Zombie
        {
            if(player.TeamNum != 2) // And he is not in Terrorist then switch him to terrorist
            {
                if (player.PawnIsAlive)
                {
                    player.SwitchTeam(CsTeam.Terrorist);
                }
                else player.ChangeTeam(CsTeam.Terrorist);
                if (spawn)player.Respawn();
            }
        }
        else // If Human
        {
            if(GetHumanCount(false) <= Config.ZCD_HumanMax) // if number of CTs is less than max limit
            {
                if(player.TeamNum != 3) // And if he is not in CT then switch him to CT
                {
                    if (player.PawnIsAlive)
                    {
                        player.SwitchTeam(CsTeam.CounterTerrorist);
                    }
                    else player.ChangeTeam(CsTeam.CounterTerrorist);
                    if (spawn)player.Respawn();
                }
            }
            else // If CT Team reach the max limit then switch player to Spectator
            {
                player.ChangeTeam(CsTeam.Spectator);
                player.PrintToChat($" {ChatColors.Gold}[{ChatColors.DarkRed}★ {ChatColors.Lime}Zombie Cabin Defense {ChatColors.DarkRed}★{ChatColors.Gold}] {ChatColors.Blue}Counter Terrorist {ChatColors.Lime}team is {ChatColors.DarkRed}Full!");
            }
        }
    }
    private void StartZombieRespawnTimer(CCSPlayerController? player)
    {
        if(GetAliveZombieCount() < GetWaveByIndex(gCurrentWaveDifficulty).ZKillCount - gZombiesKilled)
        {
            if (tRespawn[player.Slot] != null)tRespawn[player.Slot].Kill();
            gRespawnTime[player.Slot] = GetWaveByIndex(gCurrentWaveDifficulty).ZRespawnTime;
            tRespawn[player.Slot] = AddTimer(1.0f, ()=>ZombieRespawn(player), TimerFlags.REPEAT);     
        }
    }
    private void CheckPlayerLocationTimer()
    {
        if(!Config.ZCD_PluginEnabled || !gShouldStartGame || GetHumanCount(true) <= 0)
        {
            t_CheckPlayerLocation.Kill();
            return;
        }
        // Get Alive Human Players
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !IsPlayerZombie[player.Slot] && player.TeamNum == 3 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            // is player in Cabin or not? By checking Buy Zone
            if(player.PlayerPawn.Value.InBuyZone) 
            {
                if(player.PlayerPawn.Value!.Health < 100)
                {
                    player.PlayerPawn.Value!.Health += Config.ZCD_IncreaseHealth;
                }
            }
            else
            {
                if(player.PlayerPawn.Value!.Health > 0)player.PlayerPawn.Value!.Health -= Config.ZCD_DecreaseHealth;
                else player.CommitSuicide(false, true);
            }
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth"); // Update Health
        }
    }
    public static CCSGameRules GetGameRules()
    {
        return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
    }
    public void ZombieRespawn(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsHLTV || player.TeamNum != 2 || gNextWaveTime > 0 || GetAliveZombieCount() >= GetWaveByIndex(gCurrentWaveDifficulty).ZKillCount - gZombiesKilled)
        {
            tRespawn[player.Slot].Kill();
            return;
        }

        gRespawnTime[player.Slot]--;

        if (gRespawnTime[player.Slot] > 0)return;
        RespawnClient(player);
        tRespawn[player.Slot].Kill();
        return;
    }
    
    private int GetZombieToKill()
    {
        return GetWaveByIndex(gCurrentWaveDifficulty).ZKillCount - gZombiesKilled;
    }
    private int GetAliveZombieCount()
    {
        int zombieleft = 0;
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 1 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if (IsPlayerZombie[player.Slot])zombieleft++;
        }
        return zombieleft;
    }
    private int GetHumanCount(bool alive)
    {
        int humansleft = 0;
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 1))
        {
            if (!IsPlayerZombie[player.Slot])
            {
                if(alive && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)humansleft++;
                else if(!alive)humansleft++;
            }
        }
        
        return humansleft;
    }
    private  void RemoveObjectives()
    {
        foreach (var entity in Utilities.GetAllEntities().Where(entity =>  entity != null && entity.IsValid))
        {
            if( entity.DesignerName == "func_bomb_target"  ||
                entity.DesignerName == "func_hostage_rescue" ||
                entity.DesignerName ==  "c4" ||
                entity.DesignerName ==  "hostage_entity")
            {
                entity.Remove();
            }
            
        }
    }
    private void FreezeZombies()
    {
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 1 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if (IsPlayerZombie[player.Slot])
            {
                player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NONE;
                Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0); // freeze
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                player.PlayerPawn.Value.TakesDamage = false;
            }
        }
    }
    private void UnFreezeZombies()
    {
        PrinttoChatCT($" {ChatColors.Gold}[{ChatColors.DarkRed}★ {ChatColors.Lime}Zombie Cabin Defense {ChatColors.DarkRed}★{ChatColors.Gold}] {ChatColors.Green}Zombies are {ChatColors.DarkRed}released {ChatColors.Green}Now!");
        
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 1 && player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE))
        {
            if (IsPlayerZombie[player.Slot])
            {
                player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                Schema.SetSchemaValue(player.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
                player.PlayerPawn.Value.TakesDamage = true;
            }
        }
    }
    public void BeginWave(bool respawn)
    {
        gZombiesKilled = 0;
        Server.ExecuteCommand("mp_buytime 0");  // By doing Buytime to 0 we can stop players from buying weapons during wave
        gZombieToSpawn = CalculateZombiesForWave(gCurrentWave);
        if(GetWaveByIndex(gCurrentWaveDifficulty).ZKillCount < gZombieToSpawn)gZombieToSpawn = GetWaveByIndex(gCurrentWaveDifficulty).ZKillCount;
        AddTimer(0.5f, ()=>
        {
            Server.ExecuteCommand($"bot_quota {gZombieToSpawn}");
        });
        if(respawn)
        {
            AddTimer(1.0f, ()=>
            {
                foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum == 2 && player.IsBot))
                {
                    IsPlayerZombie[player.Slot] = true;
                    RespawnClient(player);
                }
            });
        }
    }
    private void ZombiesWin()
    {
        gZombiesKilled = 0;
        gCurrentWave = 0;
        gCurrentWaveDifficulty = 0;
        gIncrementedZKillCount = 0;
        gIncrementedHealthBoost = 0;
        gShouldStartGame = false;
        if(t_CheckPlayerLocation != null)t_CheckPlayerLocation?.Kill();
        Server.ExecuteCommand("bot_kick");
        TerminateRound(3.0f, RoundEndReason.GameCommencing);
    }

    private void HumansWin()
    {
        gZombiesKilled = 0;
        gCurrentWave++;
        
        if(gCurrentWave == GetWaveByIndex(gCurrentWaveDifficulty).ZWaves && Config.ZCD_Waves.Count > gCurrentWaveDifficulty+1)
        {
            gCurrentWaveDifficulty++;
        }
        else if(Config.ZCD_Waves.Count == gCurrentWaveDifficulty+1 && gCurrentWave >= GetWaveByIndex(gCurrentWaveDifficulty).ZWaves) // Highest Difficulty reached
        {
            if(gIncrementedHealthBoost <= 0)gIncrementedHealthBoost = GetWaveByIndex(gCurrentWaveDifficulty).ZHealthBoost;
            else gIncrementedHealthBoost += Config.ZCD_IncrementHealthBoostBy;
            if(gIncrementedZKillCount <= 0)gIncrementedZKillCount = GetWaveByIndex(gCurrentWaveDifficulty).ZKillCount;
            else gIncrementedZKillCount += Config.ZCD_IncrementZKillCountBy;
        }
        RespawnDeadHumans();
        Server.ExecuteCommand("mp_buytime 99999");  // By doing this players can buy before next wave start
        if(t_NextWave != null)t_NextWave?.Kill();
        gNextWaveTime = Config.ZCD_TimeBetweenNextWave;
        t_NextWave = AddTimer(1.0f, ()=>
        {
            gNextWaveTime--;
            if(gNextWaveTime > 0)return;
            BeginWave(true); // Respawn Zombies
            t_NextWave?.Kill();
            return;
        }, TimerFlags.REPEAT);
    }
    private void Zombify(CCSPlayerController? player, int zombieid)
    {
        if (player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsHLTV || player.TeamNum < 2 || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)return;
        gZombieID[player.Slot] = zombieid;
        
        AddTimer(0.3f, ()=>
        {
            if(player.PlayerPawn.Value.WeaponServices!.MyWeapons.Count != 0)player.RemoveWeapons(); // Remove all weapons of Player (Zombie)
            var knife = player.GiveNamedItem("weapon_knife"); // Give him Knife Only
            foreach (var weapon in player.PlayerPawn.Value.WeaponServices!.MyWeapons.Where(weapon => weapon != null && weapon.IsValid && weapon.Value.IsValid))
            {
                if (weapon.Value.DesignerName.Contains("weapon_knife")) // Check Knife
                {
                    weapon.Value.RenderMode = RenderMode_t.kRenderTransAlpha; // Don't Render Knife
                    weapon.Value.Render = Color.FromArgb(0, 255, 255, 255);
                }
            }
            player.PlayerPawn.Value!.Health = GetZombieByIndex(zombieid).ZombieHealth; // Set Health
            player.PlayerPawn.Value!.VelocityModifier = GetZombieByIndex(zombieid).ZombieSpeed; // Set Speed
            player.PlayerPawn.Value!.GravityScale *= GetZombieByIndex(zombieid).ZombieGravity; // Set Gravity
            player.DesiredFOV = Convert.ToUInt32(GetZombieByIndex(zombieid).ZombieFOV); // Set FOV
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV"); // Update FOV
            if(GetZombieByIndex(zombieid).ZombieModelPath != "") // Set Skin if any Path Given
            {
                Server.PrecacheModel(GetZombieByIndex(zombieid).ZombieModelPath);
                player.PlayerPawn.Value.SetModel(GetZombieByIndex(zombieid).ZombieModelPath);
            }
        });
    }
    private void ResetZombies()
    {
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > 1))
        {
            IsPlayerZombie[player.Slot] = player.IsBot;
        }
    }
    private void ZCDEnd()
    {
        TerminateRound(3.0f, RoundEndReason.GameCommencing);
        Server.ExecuteCommand("bot_all_weapons");
        Server.ExecuteCommand("bot_kick");
        foreach (var player in Utilities.GetPlayers().Where(player => player == null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected))
        {
            if (tRespawn[player.Slot] != null)
            {
                tRespawn[player.Slot]?.Kill();
            }
        }
        if (t_CheckPlayerLocation != null)t_CheckPlayerLocation?.Kill();
        if (t_NextWave != null)t_NextWave?.Kill();
    }
    private void CMD_StartGame(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if(Config.ZCD_PluginEnabled == false)
        {
            player.PrintToChat($" {ChatColors.Gold}[{ChatColors.DarkRed}★ {ChatColors.Lime}Zombie Cabin Defense {ChatColors.DarkRed}★{ChatColors.Gold}] {ChatColors.DarkRed}Zombie Cabin Defense Plugin is Disabled!");
            return;
        }
        if(player != null && !AdminManager.PlayerHasPermissions(player, Config.ZCD_AdminFlagToUseCMDs))
        {
            player.PrintToChat($" {ChatColors.Gold}[{ChatColors.DarkRed}★ {ChatColors.Lime}Zombie Cabin Defense {ChatColors.DarkRed}★{ChatColors.Gold}] {ChatColors.DarkRed}You don't have permission to use this command!");
            return;
        }
        ZombiesWin(); // Reset Game
        gShouldStartGame = true;
    }
    public static bool CanTarget(CCSPlayerController controller, CCSPlayerController target)
	{
		if (target.IsBot) return true;
		return AdminManager.CanPlayerTarget(controller, target);
	}
    public void RespawnClient(CCSPlayerController client)
    {
        if (!client.IsValid || client.PawnIsAlive)
            return;

        var clientPawn = client.PlayerPawn.Value;
        CBasePlayerController_SetPawnFunc.Invoke(client, clientPawn, true, false);
        VirtualFunction.CreateVoid<CCSPlayerController>(client.Handle, GameData.GetOffset("CCSPlayerController_Respawn"))(client);
    }
    public static string RespawnWindowsSig = new(GameData.GetSignature("CBasePlayerController_SetPawn"));
    
    public static string RespawnLinuxSig = new(GameData.GetSignature("CBasePlayerController_SetPawn"));
    public static MemoryFunctionVoid<CCSPlayerController, CCSPlayerPawn, bool, bool> CBasePlayerController_SetPawnFunc = new(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? RespawnLinuxSig : RespawnWindowsSig);
    private string[] GetZombiesToSpawn()
    {
        List<string> VaildZombiesInWave = new List<string>();
        foreach(var zombie in Config.ZCD_Zombies) // Check Zombie from All zombies
        {
            string[] waves = zombie.ZombieInWaves.Split(","); // Split all waves name by ','
            if(zombie.ZombieInWaves != "" && waves != null && waves.Count() > 0) // Wave Name string is not empty
            {
                if(waves.Contains(GetWaveByIndex(gCurrentWaveDifficulty).WaveName)) // Check is Current Wave Difficulty is in Zombie Wave string
                {
                    // Save this zombie in variable VaildZombiesInWave
                    VaildZombiesInWave.Add(zombie.ZombieClassName);
                }
            }
            else if(zombie.ZombieInWaves == "" || zombie.ZombieInWaves == " ") VaildZombiesInWave.Add(zombie.ZombieClassName);
        }
        // return all zombies who are store in variable
        return VaildZombiesInWave.ToArray();
    }
    private void RespawnDeadHumans()
    {
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum == 3 && player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE))
        {
            RespawnClient(player);
        }
    }
    private void PrinttoChatCT(string message)
    {
        foreach (var player in Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !player.IsBot && player.TeamNum == 3))
        {
            player.PrintToChat(message);
        }
    }
    public int CalculateZombiesForWave(int currentWave)
    {
        if (currentWave <= 0) return Config.ZCD_ZombieSpawnMin; // Ensure there's at least the base number of zombies on wave 1

        int growthRate = Config.ZCD_ZombieIncreaseRate; // Adjusted rate
        int growthMultiplier = 1; // New multiplier to steepen the curve

        // Calculate the number of zombies using a logarithmic growth formula
        // This formula ensures that as the number of waves increases, the number of zombies grows but at a slowing rate
        int zombiesToSpawn = Config.ZCD_ZombieSpawnMin + (int)Math.Floor(Math.Log(currentWave + 1) * growthRate * growthMultiplier);

        // Ensure the number of zombies does not exceed the maximum cap
        zombiesToSpawn = Math.Min(zombiesToSpawn, Config.ZCD_ZombieSpawnMax);

        return zombiesToSpawn;
    }
    public Action<IntPtr, float, RoundEndReason, nint, uint> TerminateRoundWindows = TerminateRoundWindowsFunc.Invoke;
	public static MemoryFunctionVoid<nint, float, RoundEndReason, nint, uint> TerminateRoundWindowsFunc = new(GameData.GetSignature("CCSGameRules_TerminateRound"));
    // For linux users
    public static MemoryFunctionVoid<nint, RoundEndReason, nint, uint, float> TerminateRoundLinuxFunc = new("55 48 89 E5 41 57 41 56 41 55 41 54 49 89 FC 53 48 81 EC ? ? ? ? 48 8D 05 ? ? ? ? F3 0F 11 85");
    public Action<IntPtr, RoundEndReason, nint, uint, float> TerminateRoundLinux = TerminateRoundLinuxFunc.Invoke;
    private static readonly bool IsWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public void TerminateRound(float delay, RoundEndReason roundEndReason)
    {
        CCSGameRules gamerules = GetGameRules();
        if (IsWindowsPlatform)
            TerminateRoundWindows(gamerules.Handle, delay, roundEndReason, 0, 0);
        else
            TerminateRoundLinux(gamerules.Handle, roundEndReason, 0, 0, delay);
        
    }
}
    