using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.Data.Sqlite;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Commands;
using System.Data.SqlTypes;
using CounterStrikeSharp.API.Modules.Admin;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static CounterStrikeSharp.API.Core.Listeners;
using VipCoreApi;
using static VipCoreApi.IVipCoreApi;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using CounterStrikeSharp.API.Modules.Events;

namespace HanSimpleRPG;

public class HanSimpleRPGPlugin : BasePlugin
{
    public override string ModuleName => "[华仔]RPG升级插件";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "By : 华仔H-AN";
    public override string ModuleDescription => "简单RPG升级,QQ群107866133";

    #region 声明VIPapi
    private IVipCoreApi? _api;
    private PluginCapability<IVipCoreApi> PluginCapability { get; } = new("vipcore:core");
    #endregion

    #region 声明timer
    private CounterStrikeSharp.API.Modules.Timers.Timer?[] g_hHudinfo { get; set; } = new CounterStrikeSharp.API.Modules.Timers.Timer[65];
    private CounterStrikeSharp.API.Modules.Timers.Timer?[] g_HumanRehealth { get; set; } = new CounterStrikeSharp.API.Modules.Timers.Timer[65];
    private CounterStrikeSharp.API.Modules.Timers.Timer?[] _timer { get; set; } = new CounterStrikeSharp.API.Modules.Timers.Timer[65];

    #endregion

    #region 声明布尔值
    private bool[] MenuOpen = new bool[65];
    private bool[] MenuCd = new bool[65];
    private bool[] showtop10 = new bool[65];

    #endregion

    #region 读取配置文件
    HanSimpleRPGConfig CFG = HanSimpleRPGConfig.Load();
    RpgHudConfig HudCFG = RpgHudConfig.Load();
    HanSimpleRPGSkillCFG SkillCFG = HanSimpleRPGSkillCFG.Load();

    #endregion

    #region 声明字符串
    public string WordText { get; set; }
    public string ShowDamageWordText { get; set; }
    public string RpgMenuWordText { get; set; }

    #endregion

    #region 构造函数中存储实例
    private static HanSimpleRPGPlugin _instance;
    public HanSimpleRPGPlugin()
    {
        _instance = this; // 在构造函数中存储实例
    }

    #endregion

    #region 字典

    private Dictionary<ulong, int> playerExperiences = new Dictionary<ulong, int>();
    private Dictionary<ulong, int> playerLevels = new Dictionary<ulong, int>();
    private Dictionary<ulong, int> playerSkillPoints = new Dictionary<ulong, int>();
    private Dictionary<ulong, Dictionary<string, int>> playerSkillLevels = new Dictionary<ulong, Dictionary<string, int>>();
    private Dictionary<int, CPointWorldText> PlayerHud = new Dictionary<int, CPointWorldText>();
    private Dictionary<int, CPointWorldText> ShowDamage = new Dictionary<int, CPointWorldText>();  
    private Dictionary<int, CPointWorldText> PlayerMenuEntities = new Dictionary<int, CPointWorldText>();
    private Dictionary<int, int> playerCurrentSelections = new Dictionary<int, int>();
    private Dictionary<string, int> WeaponOriginalAmmo = new Dictionary<string, int>(); // 存储武器的原始默认子弹数
    private static readonly Dictionary<string, string> SkillNameToChinese = new Dictionary<string, string>
    {
        { "Health", "暴饮暴食(生命值上限)" },
        { "ReHealth", "自我修复(生命值恢复)" },
        { "Damage", "破坏狂(伤害)" },
        { "HeadShot", "致命一击(爆头伤害)" },
        { "Speed", "心跳加速(移动速度)" },
        { "Gravity", "月球人(重力)" },
        { "Knock", "大口径子弹(击退)" },
        { "Mirror", "改装大师(弹匣扩容)" },
        { "Vampire", "嗜血狂热(吸血)" }
    };

    #endregion

    #region 数据库连接
    private SqliteConnection _connection;
    private HanRPGDataBase _database;

    #endregion

    #region 存储颜色
    public Color TextColor { get; set; } // 用于存储颜色

    #endregion

    private Eff? _eff;
    public override void Load(bool hotReload)
    {
        _connection = new SqliteConnection($"Data Source={HanRPGDataBase.DbPath}");
        _connection.Open();  // 打开连接
        // 将连接传递给 HanRPGDataBase 实例
        _database = new HanRPGDataBase(_connection);
        _database.CreateDb();  // 创建数据库
        _database.CreateSkillDb();
        

        EventInitialize();
        SkillEventInitialize();

        VirtualFunctions.GiveNamedItemFunc.Hook(OverrideGiveNamedItemPost, HookMode.Post);

        //VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(CBaseEntity_TakeDamageOldFunc, HookMode.Pre);

        AddCommand("css_rpg", "rpg", rpgmenu);
        AddCommand("css_adminrpg", "adminrpg", addrpg); 
        AddCommand("css_rpgtop10", "rpgtop10", showrpgtop);

        _eff = new Eff(this);

        //AddCommand("css_name", "name", NAME);
    }
    private void NAME(CCSPlayerController client, CommandInfo info)
    {
        if(!client.IsValid ||client == null)
            return;

        var activeWeapon = client.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

        string globalname = activeWeapon.Globalname;

        client.PrintToChat($"DesignerName 是 {activeWeapon.DesignerName} CustomName 是 {activeWeapon.AttributeManager.Item.CustomName} globalname 是 {globalname}");
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = PluginCapability.Get();
        if (_api == null) return;
    }

    #region 命令

    [GameEventHandler(HookMode.Post)]
    public HookResult OnChat(EventPlayerChat @event, GameEventInfo info)
    {
        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (@event.Text.Trim() == "rpg" || @event.Text.Trim() == "RPG")
        {
            rpgmenu(player, null!);
        }
        else if (@event.Text.StartsWith("rpgtop10") || @event.Text.Trim() == "RPGTOP10")
        {
            showrpgtop(player, null!);
        }
        return HookResult.Continue;
    }

    [RequiresPermissions(@"css/slay")]
    private void addrpg(CCSPlayerController? client, CommandInfo info)
    {
        if(client == null || !client.IsValid )
            return;

        ulong steamId = client.SteamID;
        int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;
        skillPoints += 10000;
        playerSkillPoints[steamId] = skillPoints;
        _database.SavePlayerSkillPoints(steamId, playerSkillPoints[steamId]);
        client.PrintToChat($"[华仔]管理员为自己增加1万技能点!!");

    }

    public void showrpgtop(CCSPlayerController? client, CommandInfo info)
    {
        if (client == null || !client.IsValid)
            return;

        if (!client.PawnIsAlive || client.TeamNum != 3) 
            return;

        showtop10[client.Slot] = true;
        AddTimer(10.0f, () => { showtop10[client.Slot] = false;});    
        
    }

    public void rpgmenu(CCSPlayerController? client, CommandInfo info)
    {
        if (client == null || !client.IsValid)
            return;

        if (!client.PawnIsAlive || client.TeamNum != 3) 
            return;

        if (!CheckMenusAndHandle(client)) 
            return;

        ulong steamId = client.SteamID;

        // 每页显示的技能项数
        const int itemsPerPage = 5;

        // 获取玩家当前的页码，确保每个玩家的翻页状态是独立的
        if (!playerCurrentSelections.ContainsKey(client.Slot))
        {
            playerCurrentSelections[client.Slot] = 0; // 默认选择第一页的第一个选项
        }

       var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();
       SkillConfig healthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Health");
       SkillConfig rehealthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "ReHealth");
       SkillConfig damageSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Damage");
       SkillConfig headshotSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "HeadShot");
       SkillConfig speedSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Speed");
       SkillConfig gravitySkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Gravity");
       SkillConfig knokSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Knock");
       SkillConfig mirrorSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Mirror");
       SkillConfig vampireSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Vampire");

        //硬编码每个技能
        var skills = new[]
        {
            new { Name = "暴饮暴食(血量上限++)", 
                Level = skillLevels.ContainsKey("Health") ? skillLevels["Health"] : 0, 
                UpgradeCost = healthSkill.HealthFirstLevel + (skillLevels.ContainsKey("Health") ? skillLevels["Health"] : 0) * healthSkill.HealthEachLevel, 
                MaxLevel = healthSkill.HealthMaxLevel
                },

            new { Name = "自我修复(血量恢复++)", 
                Level = skillLevels.ContainsKey("ReHealth") ? skillLevels["ReHealth"] : 0, 
                UpgradeCost = rehealthSkill.ReHealthFirstLevel + (skillLevels.ContainsKey("ReHealth") ? skillLevels["ReHealth"] : 0) * rehealthSkill.ReHealthEachLevel, 
                MaxLevel = rehealthSkill.ReHealthMaxLevel
                },

            new { Name = "破坏狂(伤害增加++)", 
                Level = skillLevels.ContainsKey("Damage") ? skillLevels["Damage"] : 0, 
                UpgradeCost = damageSkill.DamageFirstLevel + (skillLevels.ContainsKey("Damage") ? skillLevels["Damage"] : 0) * damageSkill.DamageEachLevel, 
                MaxLevel = damageSkill.DamageMaxLevel
                },

            new { Name = "致命一击(爆头伤害++)", 
                Level = skillLevels.ContainsKey("HeadShot") ? skillLevels["HeadShot"] : 0, 
                UpgradeCost = headshotSkill.HeadShotFirstLevel + (skillLevels.ContainsKey("HeadShot") ? skillLevels["HeadShot"] : 0) * headshotSkill.HeadShotEachLevel,
                MaxLevel = headshotSkill.HeadShotMaxLevel
                 },

            new { Name = "心跳加速(移动速度++)", 
                Level = skillLevels.ContainsKey("Speed") ? skillLevels["Speed"] : 0, 
                UpgradeCost = speedSkill.SpeedFirstLevel + (skillLevels.ContainsKey("Speed") ? skillLevels["Speed"] : 0) * speedSkill.SpeedEachLevel, 
                MaxLevel = speedSkill.SpeedMaxLevel
                },

            new { Name = "月球人(重力降低)", 
                Level = skillLevels.ContainsKey("Gravity") ? skillLevels["Gravity"] : 0, 
                UpgradeCost = gravitySkill.GravityFirstLevel + (skillLevels.ContainsKey("Gravity") ? skillLevels["Gravity"] : 0) * gravitySkill.GravityEachLevel,
                MaxLevel =  gravitySkill.GravityMaxLevel
                },

            new { Name = "大口径子弹(击退++)", 
                Level = skillLevels.ContainsKey("Knock") ? skillLevels["Knock"] : 0, 
                UpgradeCost = knokSkill.KnockFirstLevel + (skillLevels.ContainsKey("Knock") ? skillLevels["Knock"] : 0) * knokSkill.KnockEachLevel, 
                MaxLevel = knokSkill.KnockMaxLevel
                },

            new { Name = "改装大师(弹匣扩容++)", 
                Level = skillLevels.ContainsKey("Mirror") ? skillLevels["Mirror"] : 0, 
                UpgradeCost = mirrorSkill.MirrorFirstLevel + (skillLevels.ContainsKey("Mirror") ? skillLevels["Mirror"] : 0) * mirrorSkill.MirrorEachLevel, 
                MaxLevel = mirrorSkill.MirrorMaxLevel
                },

            new { Name = "嗜血狂热(伤害吸血++)", 
                Level = skillLevels.ContainsKey("Vampire") ? skillLevels["Vampire"] : 0, 
                UpgradeCost = vampireSkill.VampireFirstLevel + (skillLevels.ContainsKey("Vampire") ? skillLevels["Vampire"] : 0) * vampireSkill.VampireEachLevel,
                MaxLevel = vampireSkill.VampireMaxLevel
                },
        };



        int totalSkills = skills.Length;

        // 计算当前页码和当前选项
        int currentPage = playerCurrentSelections[client.Slot] / itemsPerPage + 1;
        int currentSelection = playerCurrentSelections[client.Slot] % itemsPerPage;

        // 计算最大页码
        int maxPage = (int)Math.Ceiling((double)totalSkills / itemsPerPage);

        // 计算当前页要显示的技能项
        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Math.Min(startIndex + itemsPerPage, totalSkills);

        // 更新菜单文本
        WordText = $"[华仔]RPG技能升级菜单:\n按W/S向上下选择(第{currentPage}/{maxPage}页)\n";
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i == startIndex + currentSelection)  // 高亮当前选中的项
            {
                WordText += $"-> {skills[i].Name}: 等级:[{skills[i].Level}/{skills[i].MaxLevel}](升级消耗: {skills[i].UpgradeCost})\n";
            }
            else
            {
                WordText += $"{skills[i].Name}: 等级:[{skills[i].Level}/{skills[i].MaxLevel}](升级消耗: {skills[i].UpgradeCost})\n";
            }
        }

        WordText += "按E确认|按SHIFT关闭菜单";

        // 检查菜单是否已经打开，若没有则创建菜单实体
        var clientpawn = client.PlayerPawn.Value;
        if (clientpawn != null)
        {
            var handle = RpgWasdMenu.GetOrCreateViewModels(clientpawn);
            if (!PlayerMenuEntities.ContainsKey(client.Slot) || PlayerMenuEntities[client.Slot] == null)
            {
                PlayerMenuEntities[client.Slot] = RpgWasdMenu.CreateText(client, handle, WordText, 2.0f, 1.0f, 32, "Arial Bold", Color.Cyan);
                MenuOpen[client.Slot] = true;
                client.PrintToChat("[华仔]RPG技能升级菜单已开启");
            }
            else
            {
                CloseMenu(client);
            }
        }
    }

    #endregion



    #region Hook事件
    public void EventInitialize() 
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterListener<CheckTransmit>(OnTransmit);
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnected);
    }
    
    #region 玩家死亡事件
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var client = @event.Userid;
        var attacker = @event.Attacker;
        if (client == null || attacker == null)
            return HookResult.Continue;
        if(!client.IsValid || !attacker.IsValid)
            return HookResult.Continue;
        if(client.TeamNum == 3 || client == attacker)
            return HookResult.Continue;
        if(attacker.IsBot)
            return HookResult.Continue;

        if(attacker.TeamNum == 3)
        {
            AddExperience(attacker,CFG.KillGiveExperience);
        }
        if(!client.IsBot && client.TeamNum == 3)
        {
            g_HumanRehealth[client.Slot]?.Kill();
            g_HumanRehealth[client.Slot] = null;

            RemoveRPGText(client);
            RemoveShowdamage(client);
            Remove(client);
            MenuOpen[client.Slot] = false;
        }

        return HookResult.Continue;
    }
    #endregion

    #region 玩家连接事件
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if (client == null)
            return HookResult.Continue;
        if(!client.IsValid)
            return HookResult.Continue;

        ulong steamid = client.SteamID;
        string Names = client.PlayerName;
        _database.CreatePlayerDataRecord(steamid,Names);
        _database.CreateSkillDataRecord(steamid);
        GetPlayerData(client);
        //GetPlayerDataAsync(client);

        return HookResult.Continue;
    }
    #endregion

    #region 玩家复活事件
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if (client == null)
            return HookResult.Continue;

        if(!client.IsValid)
            return HookResult.Continue;

        // 延迟创建HUD文本
        if(!client.IsBot && client.TeamNum == 3)
        {
            AddTimer(1f, () => {ShowRPGText(client);});
            AddTimer(1f, () => {ShowdamageText(client);});
            //AddTimer(1f, () => {Server.PrintToChatAll($"复活玩家 {client.PlayerName}");}); 
        }
        
        return HookResult.Continue;
    }
    #endregion

    #region 回合结束事件
    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        List<CCSPlayerController> playerlist = Utilities.GetPlayers();
        foreach (var client in playerlist)
        {
            if(!client.IsValid ||client == null)
                return HookResult.Continue;

            g_HumanRehealth[client.Slot]?.Kill();
            g_HumanRehealth[client.Slot] = null;

            g_hHudinfo[client.Slot]?.Kill();
            g_hHudinfo[client.Slot] = null;

            RemoveRPGText(client);
            Remove(client);
            RemoveShowdamage(client);
            MenuOpen[client.Slot] = false;
        }
        return HookResult.Continue;
    }
    #endregion

    #region 屏蔽其他玩家Worldtext
    void OnTransmit(CCheckTransmitInfoList infoList)
    {
        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null || !player.IsValid || !player.Pawn.IsValid || player.Pawn.Value == null) continue;

            // 处理 PlayerHud 字典
            foreach (var item in PlayerHud)
            {
                int slot = item.Key; // 获取字典项的键
                CPointWorldText Entity = item.Value; // 获取字典项的值
                if (slot != player.Slot && Entity != null)
                {
                    info.TransmitEntities.Remove(Entity);
                }
            }
            foreach (var item in PlayerMenuEntities)
            {
                int slot = item.Key; // 获取字典项的键
                CPointWorldText Entity = item.Value; // 获取字典项的值
                if (slot != player.Slot && Entity != null)
                {
                    info.TransmitEntities.Remove(Entity);
                }
            }
            foreach (var item in ShowDamage)
            {
                int slot = item.Key; // 获取字典项的键
                CPointWorldText Entity = item.Value; // 获取字典项的值
                if (slot != player.Slot && Entity != null)
                {
                    info.TransmitEntities.Remove(Entity);
                }
            }

        }
    }
    #endregion

    #region 勾住ontick
    private void OnTick()
    {
        
        List<CCSPlayerController> playerlist = Utilities.GetPlayers();
        foreach (var client in playerlist)
        {
            if (client?.PlayerPawn?.Value?.MovementServices?.Buttons.ButtonStates.Length > 0 &&MenuOpen[client.Slot] == true)
            {
                var buttons = (PlayerButtons)client.PlayerPawn.Value.MovementServices.Buttons.ButtonStates[0];
                HandlePlayerInput(client, buttons);
            }
        }
    }
    #endregion

    #region 回合开始事件
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    { 
        List<CCSPlayerController> playerlist2 = Utilities.GetPlayers();
        foreach (var client in playerlist2)
        {
            if(!client.IsValid ||client == null)
                return HookResult.Continue;

            Remove(client);
            MenuOpen[client.Slot] = false; 
        }
        return HookResult.Continue;
    }
    #endregion

    #region 玩家断开连接事件
    private void OnClientDisconnected(int client)
    {
        var player = Utilities.GetPlayerFromSlot(client);

        if(player == null)
            return;

        if(!player.IsValid)
            return;

        g_HumanRehealth[player.Slot]?.Kill();
        g_HumanRehealth[player.Slot] = null;

        g_hHudinfo[player.Slot]?.Kill();
        g_hHudinfo[player.Slot] = null;

        
    }
    #endregion

    #endregion

    #region Hook技能事件
    public void SkillEventInitialize()
    {
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerHealthSpawn);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerReHealthSpawn);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerRpgHurt);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
    }

    #region 玩家技能事件复活最大血量设置
    public HookResult OnPlayerHealthSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if (client == null || !client.IsValid)
            return HookResult.Continue;

        var clientpawn = client.PlayerPawn.Value;

        if (clientpawn == null || !clientpawn.IsValid)
            return HookResult.Continue;

        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();
        int healthLevel = skillLevels.ContainsKey("Health") ? skillLevels["Health"] : 0;
        SkillConfig healthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Health");

        int SpeedLevel = skillLevels.ContainsKey("Speed") ? skillLevels["Speed"] : 0;
        SkillConfig speedSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Speed");

        int GravityLevel = skillLevels.ContainsKey("Gravity") ? skillLevels["Gravity"] : 0;
        SkillConfig gravitySkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Gravity");

        if(client.IsBot)
            return HookResult.Continue;
        if(client.TeamNum != 3)
            return HookResult.Continue;

        AddTimer(0.2f, () => 
        {  
            Server.NextWorldUpdate(() => 
            {
                if(healthLevel > 0 && healthSkill.HealthEnable)
                {
                    int Maxhealth = 100 + healthLevel * healthSkill.EachLevelAddHealth;
                    clientpawn.Health = Maxhealth;
                    Utilities.SetStateChanged(clientpawn, "CBaseEntity", "m_iHealth");
                }
                if(SpeedLevel > 0 && speedSkill.SpeedEnable)
                {
                    clientpawn.VelocityModifier = 1.0f + (SpeedLevel *(1.0f * speedSkill.SpeedEachLevelAdd));
                }
                if(GravityLevel > 0 && gravitySkill.GravityEnable)
                {
                    float RpgGravity = 1f - (GravityLevel * gravitySkill.GravityEachLevelAdd);
                    if(RpgGravity <= 0)
                    RpgGravity = 0.1f;
                    clientpawn.GravityScale = RpgGravity;
                }
            });
        }); 

        /*  
                
        if(healthLevel > 0 && healthSkill.HealthEnable)
        {
            int Maxhealth = 100 + healthLevel * healthSkill.EachLevelAddHealth;                 
            AddTimer(0.2f, () => 
            {
                Server.NextWorldUpdate(() => 
                {
                    clientpawn.Health = Maxhealth;
                    Utilities.SetStateChanged(clientpawn, "CBaseEntity", "m_iHealth");
                });
            });     
        }
        if(SpeedLevel > 0 && speedSkill.SpeedEnable)
        {
            AddTimer(0.2f, () => 
            {
                Server.NextWorldUpdate(() => 
                {
                    clientpawn.VelocityModifier = 1.0f + (SpeedLevel *(1.0f * speedSkill.SpeedEachLevelAdd));

                });
            });

        }
        if(GravityLevel > 0 && gravitySkill.GravityEnable)
        {
            AddTimer(0.2f, () => 
            {
                Server.NextWorldUpdate(() => 
                {
                    float RpgGravity = 1f - (GravityLevel * gravitySkill.GravityEachLevelAdd);
                    if(RpgGravity <= 0)
                    RpgGravity = 0.1f;
                    clientpawn.GravityScale = RpgGravity;

                });
            });

        }
        */

        return HookResult.Continue;
    }

    #endregion

    #region 玩家技能事件复活回复血量事件
    public HookResult OnPlayerReHealthSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var client = @event.Userid;

        if (client == null || !client.IsValid)
            return HookResult.Continue;

        var clientpawn = client.PlayerPawn.Value;
        if (clientpawn == null || !clientpawn.IsValid)
            return HookResult.Continue;

        if(client.IsBot || client.TeamNum != 3)
            return HookResult.Continue;
            
        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();
        int RehealthLevel = skillLevels.ContainsKey("ReHealth") ? skillLevels["ReHealth"] : 0;   
        SkillConfig rehealthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "ReHealth");
        
        if(RehealthLevel <= 0)
            return HookResult.Continue;
        if(!rehealthSkill.ReEnable)
            return HookResult.Continue;

        
        int LevelRehealth = RehealthLevel * rehealthSkill.ReHealthEachLevelHealth;
        float ReTime = rehealthSkill.ReEachLevelFirstInterval -  RehealthLevel * rehealthSkill.ReHealthEachLevelInterval;
        if(ReTime <= 0)
           ReTime = 0.5f;

        AddTimer(0.2f, () => 
        {  
            Server.NextWorldUpdate(() => 
            {
                g_HumanRehealth[client.Slot]?.Kill();
                g_HumanRehealth[client.Slot] = null;
                g_HumanRehealth[client.Slot] = AddTimer(ReTime, () => {RpgRehealth(client,LevelRehealth);},TimerFlags.REPEAT|TimerFlags.STOP_ON_MAPCHANGE);

            });
        });
        
        return HookResult.Continue;    
    }
    #endregion

    #region 玩家技能事件伤害事件
    private HookResult OnPlayerRpgHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if (client == null ||!client.IsValid)
            return HookResult.Continue;

        var attacker = @event.Attacker;
        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        if (client.TeamNum == 3 || client == attacker || attacker.TeamNum == 2 || attacker.IsBot)
            return HookResult.Continue;

        var clientpawn = client.PlayerPawn.Value;
        if (clientpawn == null || !clientpawn.IsValid)
            return HookResult.Continue;

        var attackerpawn = attacker.PlayerPawn.Value;
        if (attackerpawn == null || !attackerpawn.IsValid)
            return HookResult.Continue;

        var dmgHealth = @event.DmgHealth;
        var hitgroup = @event.Hitgroup;

        ulong attackersteamId = attacker.SteamID;
        ulong clientsteamId = client.SteamID;

        // 如果是 bot 或者没有有效 SteamID，跳过
        //bool isBot = attackersteamId == 0 || clientsteamId == 0; // 可能是 bot 或没有 SteamID

        

        var attackerskillLevels = playerSkillLevels.ContainsKey(attackersteamId) ? playerSkillLevels[attackersteamId] : new Dictionary<string, int>();

        var clientskillLevels = playerSkillLevels.ContainsKey(clientsteamId) ? playerSkillLevels[clientsteamId] : new Dictionary<string, int>();

        int DamageLevel = attackerskillLevels.ContainsKey("Damage") ? attackerskillLevels["Damage"] : 0;
        SkillConfig damageSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Damage");

        int HeadShotLevel = attackerskillLevels.ContainsKey("HeadShot") ? attackerskillLevels["HeadShot"] : 0;
        SkillConfig headshotSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "HeadShot");

        

        int healthLevel = attackerskillLevels.ContainsKey("Health") ? attackerskillLevels["Health"] : 0;
        SkillConfig healthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Health");
        int Maxhealth = 100 + healthLevel * healthSkill.EachLevelAddHealth;



        int KnockLevel = attackerskillLevels.ContainsKey("Knock") ? attackerskillLevels["Knock"] : 0;
        SkillConfig knokSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Knock");

        int vampireLevel = attackerskillLevels.ContainsKey("Vampire") ? attackerskillLevels["Vampire"] : 0;
        SkillConfig vampireSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Vampire");

        float AddDamage = dmgHealth + (dmgHealth * (DamageLevel * damageSkill.DamageEachLevelAdd));

        float AddHeadDamage = dmgHealth + (dmgHealth * (HeadShotLevel * headshotSkill.HeadShotEachLevelAdd));

        float KnockBack = KnockLevel * knokSkill.KnockEachLevelAdd;

        

        if (attacker.TeamNum == 3 && !attacker.IsBot)
        {
            float OriginaExperience = dmgHealth * CFG.HurtDamageExperiencePercen;
            AddExperience(attacker, OriginaExperience);
            //attacker.PrintToChat($"玩家{attacker.PlayerName} 增加基础经验值{OriginaExperience}");

            var activeWeapon = attacker.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
            if (activeWeapon == null)
                return HookResult.Continue;
            
            if(activeWeapon.DesignerName == "weapon_knife")
            {
                if(activeWeapon.AttributeManager.Item.CustomName == "鬼丸国纲"||activeWeapon.AttributeManager.Item.CustomName == "科林手锯"||activeWeapon.AttributeManager.Item.CustomName == "流萤长剑"||activeWeapon.AttributeManager.Item.CustomName == "流星锤")
                {
                    AddDamage *= 10.0f;
                    AddHeadDamage *= 10.0f;
                    KnockBack *= 10.0f;
                }
            }
            
            if (client.TeamNum == 2)
            {
                if (vampireLevel > 0 && vampireSkill.VampireEnable)
                {
                    if (attackerpawn.Health < Maxhealth)
                    {
                        attackerpawn.Health += vampireLevel * vampireSkill.VampireEachLevelAdd;
                        Utilities.SetStateChanged(attackerpawn, "CBaseEntity", "m_iHealth");
                        var attackerOrigin = attacker.PlayerPawn.Value?.AbsOrigin;
                        var clientOrigin = client.PlayerPawn.Value?.AbsOrigin;
                        if (attackerOrigin != null && clientOrigin != null)
                        {
                            Vector adjustedClientOrigin = new Vector(clientOrigin.X, clientOrigin.Y, clientOrigin.Z + 65f);

                            _eff?.CreateBeamBetweenPoints(attackerOrigin, adjustedClientOrigin);
                        }
                    }

                }

            }
            float finalBonusDamage = 0f;
            if (DamageLevel > 0 && damageSkill.DamageEnable)
            {
                finalBonusDamage += AddDamage;
            }
            if(hitgroup == 1 && HeadShotLevel > 0 && headshotSkill.HeadShotEnable)
            {
                finalBonusDamage += (AddDamage + AddHeadDamage);
            }
            client.TakeDamage(finalBonusDamage, attacker);

            float hurtExperience = finalBonusDamage * CFG.HurtDamageExperiencePercen;
            AddExperience(attacker, hurtExperience);
            //attacker.PrintToChat($"玩家{attacker.PlayerName} 增加额外伤害经验值{hurtExperience}");

            if (ShowDamage[attacker.Slot] != null)
            {
                float Show = dmgHealth + finalBonusDamage;

                ShowDamageWordText = $"{(int)Show}";
                //ShowDamageWordText = $"{(int)dmgHealth} + {(int)finalBonusDamage}";
                ShowDamage[attacker.Slot].AcceptInput("SetMessage", ShowDamage[attacker.Slot], ShowDamage[attacker.Slot], $"{ShowDamageWordText}");
                AddTimer(0.2f, () =>
                {
                    if (ShowDamage[attacker.Slot] != null)
                    {
                        ShowDamageWordText = "";
                        ShowDamage[attacker.Slot].AcceptInput("SetMessage", ShowDamage[attacker.Slot], ShowDamage[attacker.Slot], $"{ShowDamageWordText}");
                    }
                });
            }

            //attacker.PrintToChat($"原始伤害: {dmgHealth}, 附加: {finalBonusDamage}, 总伤害: {dmgHealth + finalBonusDamage}");

            if (KnockLevel > 0 && knokSkill.KnockEnable)
            {
                Knockback.KnockbackClient(client, attacker, KnockBack);
            }

        }

        return HookResult.Continue;


    }
    #endregion

    #region 玩家技能开火事件

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if (client == null || !client.IsValid)
            return HookResult.Continue;

        var clientpawn = client.PlayerPawn.Value;

        if (clientpawn == null || !clientpawn.IsValid)
            return HookResult.Continue;

        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();
       
        int SpeedLevel = skillLevels.ContainsKey("Speed") ? skillLevels["Speed"] : 0;
        SkillConfig speedSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Speed");

        int GravityLevel = skillLevels.ContainsKey("Gravity") ? skillLevels["Gravity"] : 0;
        SkillConfig gravitySkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Gravity");
        if(SpeedLevel > 0 && speedSkill.SpeedEnable)
        {   
            if(clientpawn.VelocityModifier != 1.0f + (SpeedLevel *(1.0f * speedSkill.SpeedEachLevelAdd)))
            {
                Server.NextWorldUpdate(() => 
                {
                    clientpawn.VelocityModifier = 1.0f + (SpeedLevel *(1.0f * speedSkill.SpeedEachLevelAdd));

                });
            }
        }
        if(GravityLevel > 0 && gravitySkill.GravityEnable)
        {
            if(clientpawn.GravityScale != 1f - (GravityLevel * gravitySkill.GravityEachLevelAdd))
            {
                Server.NextWorldUpdate(() => 
                {
                    float RpgGravity = 1f - (GravityLevel * gravitySkill.GravityEachLevelAdd);
                    if(RpgGravity <= 0)
                    RpgGravity = 0.1f;
                    clientpawn.GravityScale = RpgGravity;

                });
            }
        }

        SetReserveAmmo(client);
        
        return HookResult.Continue;
    }
    #endregion
    
    #endregion



    #region 子弹扩容技能
    public List<string> IgnoredItems = [
        "weapon_decoy",
        "weapon_flashbang",
        "weapon_smokegrenade",
        "weapon_hegrenade",
        "weapon_molotov",
        "weapon_incgrenade",
        "weapon_healthshot",
        "weapon_tagrenade",
        "weapon_breachcharge",
        "weapon_diversion",
        "weapon_firebomb",
        "weapon_frag",
        "weapon_snowball",
        "weapon_tablet",
        "weapon_bumpmine",
        "weapon_shield",
        "weapon_c4",
        "weapon_knife"
    ];

    private Dictionary<string, int> WeaponAmmoMap = new Dictionary<string, int>
    {
        { "weapon_deagle", 7 }, 
        { "weapon_elite", 30 }, 
        { "weapon_fiveseven", 20 }, 
        { "weapon_glock", 20 }, 
        { "weapon_ak47", 30 }, 
        { "weapon_aug", 30 }, 
        { "weapon_awp", 25 }, 
        { "weapon_famas", 25 }, 
        { "weapon_g3sg1", 20 }, 
        { "weapon_galilar", 35 }, 
        { "weapon_m249", 100 },  
        { "weapon_m4a1", 30 }, 
        { "weapon_mac10", 30 }, 
        { "weapon_p90", 50 }, 
        { "weapon_mp5sd", 30 }, 
        { "weapon_ump45", 25 }, 
        { "weapon_xm1014", 7 }, 
        { "weapon_bizon", 64 }, 
        { "weapon_mag7", 5 }, 
        { "weapon_negev", 150 }, 
        { "weapon_sawedoff", 7 }, 
        { "weapon_tec9", 18 }, 
        { "weapon_taser", 1 }, 
        { "weapon_hkp2000", 13 }, 
        { "weapon_mp7", 30 }, 
        { "weapon_mp9", 30 }, 
        { "weapon_nova", 8 }, 
        { "weapon_p250", 13 }, 
        { "weapon_scar20", 20 }, 
        { "weapon_sg556", 30 }, 
        { "weapon_ssg08", 10 }, 
        { "weapon_m4a1_silencer", 20 }, 
        { "weapon_usp_silencer", 12 }, 
        { "weapon_cz75a", 12 }, 
        { "weapon_revolver", 8 },  
    };

    // 钩子函数，处理玩家获取武器时的弹药设置
    private HookResult OverrideGiveNamedItemPost(DynamicHook h)
    {
        string weapon = h.GetParam<string>(1);
        
        // 确保武器有效且不在忽略列表
        if (string.IsNullOrEmpty(weapon) || !WeaponAmmoMap.ContainsKey(weapon) || IgnoredItems.Contains(weapon))
            return HookResult.Continue;

        CCSPlayerController? player = GetPlayerFromItemServices(h.GetParam<CCSPlayer_ItemServices>(0));
        CBasePlayerWeapon item = h.GetReturn<CBasePlayerWeapon>();
        
        // 确保玩家和武器有效
        if (player == null || !player.IsValid || !item.IsValid)
            return HookResult.Continue;

        if (player.IsBot || player.TeamNum == 2) // 跳过机器人或恐怖分子
            return HookResult.Continue;

        string globalname = item.Globalname;
        string customname = item.AttributeManager.Item.CustomName;

       ulong steamId = player.SteamID;

        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();

        int mirrorLevel = skillLevels.ContainsKey("Mirror") ? skillLevels["Mirror"] : 0;

        if(mirrorLevel <= 0)
            return HookResult.Continue;

        SkillConfig mirrorSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Mirror");

        if(!mirrorSkill.MirrorEnable)
            return HookResult.Continue;

        int baseAmmo = WeaponAmmoMap[weapon];
        int ammoPerLevel = mirrorSkill.MirrorEachLevelAdd; // 每级增加的子弹数
        // 计算最终弹药数：基础弹药 + 技能等级提升的子弹数
        int totalAmmo = baseAmmo + (mirrorLevel * ammoPerLevel);
        // 设置武器的弹药数据
        if (item.VData != null && string.IsNullOrEmpty(globalname) && string.IsNullOrEmpty(customname))
        {
            //item.VData.MaxClip1 = totalAmmo;
            //item.VData.DefaultClip1 = totalAmmo;
            item.VData.MaxClip1 = totalAmmo;
            item.Clip1 = totalAmmo;
            Utilities.SetStateChanged(item, "CBasePlayerWeapon", "m_iClip1");
        }

        // 更新玩家弹药
        //Server.NextWorldUpdate(() => TakeDamageTest.setammo(item, totalAmmo, 1000));

        return HookResult.Continue;
    }

    public void SetReserveAmmo(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return;

        var playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null || !playerPawn.IsValid)
            return;

        var weaponServices = new CCSPlayer_WeaponServices(playerPawn.WeaponServices.Handle);
        if(weaponServices == null)
            return;

        foreach (var item in weaponServices.MyWeapons)
        {
            var weaponItem = item.Get();
            if (weaponItem == null)
            {
                continue;
            }
            if(weaponItem.ReserveAmmo[0] < 1000)
            {
                weaponItem.ReserveAmmo[0] = 1000;
                Utilities.SetStateChanged(weaponItem, "CBasePlayerWeapon", "m_pReserveAmmo");
            }

        }
        
    }

    public static CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
    {
        if (itemServices?.Pawn?.Value is CBasePlayerPawn pawn && pawn.IsValid && pawn.Controller?.IsValid == true && pawn.Controller.Value != null)
        {
            var player = new CCSPlayerController(pawn.Controller.Value.Handle);
            if (player.IsValid && !player.IsBot && !player.IsHLTV && player.Connected == PlayerConnectedState.PlayerConnected)
            {
                return player;
            }
        }

        return null;
    }
    #endregion
    

    
    

    
    #region rpg设置血量函数
    private void RpgRehealth(CCSPlayerController client,int LevelRehealth)
    {
        if (client == null || !client.IsValid || !client.PawnIsAlive)
            return;

        var clientpawn = client.PlayerPawn.Value;

        if (clientpawn == null || !clientpawn.IsValid)
            return;

        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();
        int healthLevel = skillLevels.ContainsKey("Health") ? skillLevels["Health"] : 0;
        SkillConfig healthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Health");
        int Maxhealth = 100 + healthLevel * healthSkill.EachLevelAddHealth;
        int PlayerHealth = clientpawn.Health;

        if (PlayerHealth >= Maxhealth)
            return;

        Server.NextWorldUpdate(() => 
        {
            clientpawn.Health = Math.Min(PlayerHealth + LevelRehealth, Maxhealth);
            Utilities.SetStateChanged(clientpawn, "CBaseEntity", "m_iHealth");
        });

    }
    #endregion

    #region 从数据库获取玩家数据
    
    private void GetPlayerData(CCSPlayerController client)
    {
        ulong steamId = client.SteamID;

        // 获取玩家的基础数据
        int level = _database.GetPlayerLevelFromDb(steamId);
        int experience = _database.GetPlayerExperienceFromDb(steamId);
        int skillPoints = _database.GetPlayerSkillPointsFromDb(steamId);

        string names = _database.GetPlayerNames(steamId);

        // 将基础数据存储到字典
        playerLevels[steamId] = level;
        playerExperiences[steamId] = experience;
        playerSkillPoints[steamId] = skillPoints;

        //playerNames[steamId] = names;

        // 获取玩家的技能数据
        var skillLevels = new Dictionary<string, int>
        {
            { "Health", _database.GetHealthLevelFromDb(steamId) },
            { "ReHealth", _database.GetReHealthLevelFromDb(steamId) },
            { "Damage", _database.GetDamageLevelFromDb(steamId) },
            { "HeadShot", _database.GetHeadShotLevelFromDb(steamId) },
            { "Speed", _database.GetSpeedLevelFromDb(steamId) },
            { "Gravity", _database.GetGravityLevelFromDb(steamId) },
            { "Knock", _database.GetKnockLevelFromDb(steamId) },
            { "Mirror", _database.GetMirrorLevelFromDb(steamId) },
            { "Vampire", _database.GetvampireLevelFromDb(steamId) }
        };

        // 将技能数据存储到字典
        playerSkillLevels[steamId] = skillLevels;

        // 启动定时器来保存玩家数据（包括基础数据和技能数据）
        _timer[client.Slot] = AddTimer(60.0f, () => SavePlayerDataForAllPlayers(), TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }
    /*
    
    private async Task GetPlayerDataAsync(CCSPlayerController client)
    {
        ulong steamId = client.SteamID;

        // 获取玩家的基础数据
        int level = await _database.GetPlayerLevelFromDbAsync(steamId);
        int experience = await _database.GetPlayerExperienceFromDbAsync(steamId);
        int skillPoints = await _database.GetPlayerSkillPointsFromDbAsync(steamId);

        // 将基础数据存储到字典
        playerLevels[steamId] = level;
        playerExperiences[steamId] = experience;
        playerSkillPoints[steamId] = skillPoints;

        // 获取玩家的技能数据
        var skillLevels = new Dictionary<string, int>
    {
        { "Health", await _database.GetHealthLevelFromDbAsync(steamId) },
        { "ReHealth", await _database.GetReHealthLevelFromDbAsync(steamId) },
        { "Damage", await _database.GetDamageLevelFromDbAsync(steamId) },
        { "HeadShot", await _database.GetHeadShotLevelFromDbAsync(steamId) },
        { "Speed", await _database.GetSpeedLevelFromDbAsync(steamId) },
        { "Gravity", await _database.GetGravityLevelFromDbAsync(steamId) },
        { "Knock", await _database.GetKnockLevelFromDbAsync(steamId) },
        { "Mirror", await _database.GetMirrorLevelFromDbAsync(steamId) },
        { "Vampire", await _database.GetvampireLevelFromDbAsync(steamId) }
    };

        // 将技能数据存储到字典
        playerSkillLevels[steamId] = skillLevels;

        // 启动定时器来保存玩家数据（包括基础数据和技能数据）
        _timer[client.Slot] = AddTimer(3.0f, () => SavePlayerDataForAllPlayersAsync(), TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }
    */
    #endregion

    #region 遍历所有玩家并保存他们的数据
    // 遍历所有玩家并保存他们的数据
    
    public void SavePlayerDataForAllPlayers()
    {
        // 获取所有玩家
        List<CCSPlayerController> playerlist = Utilities.GetPlayers();

        foreach (var client in playerlist)
        {
            if (client != null)
            {
                ulong steamId = client.SteamID;
                if (steamId > 0)
                {
                    // 从字典中获取玩家的基础数据
                    int experience = playerExperiences.ContainsKey(steamId) ? playerExperiences[steamId] : 0;
                    int level = playerLevels.ContainsKey(steamId) ? playerLevels[steamId] : 0;
                    int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;
                    //string Playname =  playerNames.ContainsKey(steamId) ? playerNames[steamId] : "无";

                    // 从字典中获取玩家的技能数据
                    var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();

                    // 保存玩家的基础数据和技能数据到数据库
                    SavePlayerData(steamId, skillLevels);
                }
            }
        }
    }

    public void SavePlayerData(ulong steamId, Dictionary<string, int> skillLevels)
    {
        // 保存玩家的基础数据到数据库
        _database.SavePlayerExperience(steamId, playerExperiences[steamId]);
        _database.SavePlayerlevel(steamId, playerLevels[steamId]);
        _database.SavePlayerSkillPoints(steamId, playerSkillPoints[steamId]);
        //_database.SavePlayername(steamId, playerNames[steamId]);

        // 保存玩家的技能数据到数据库
        if (skillLevels.ContainsKey("Health"))
            _database.SavePlayerHealth(steamId, skillLevels["Health"]);
        if (skillLevels.ContainsKey("ReHealth"))
            _database.SavePlayerReHealth(steamId, skillLevels["ReHealth"]);
        if (skillLevels.ContainsKey("Damage"))
            _database.SavePlayerDamage(steamId, skillLevels["Damage"]);
        if (skillLevels.ContainsKey("HeadShot"))
            _database.SavePlayerHeadShot(steamId, skillLevels["HeadShot"]);
        if (skillLevels.ContainsKey("Speed"))
            _database.SavePlayerSpeed(steamId, skillLevels["Speed"]);
        if (skillLevels.ContainsKey("Gravity"))
            _database.SavePlayerGravity(steamId, skillLevels["Gravity"]);
        if (skillLevels.ContainsKey("Knock"))
            _database.SavePlayerKnock(steamId, skillLevels["Knock"]);
        if (skillLevels.ContainsKey("Mirror"))
            _database.SavePlayerMirror(steamId, skillLevels["Mirror"]);
        if (skillLevels.ContainsKey("Vampire"))
            _database.SavePlayervampire(steamId, skillLevels["Vampire"]);
    }

    public void SavePlayerSkill(ulong steamId, Dictionary<string, int> skillLevels)
    {
        // 保存玩家的技能数据到数据库
        if (skillLevels.ContainsKey("Health"))
            _database.SavePlayerHealth(steamId, skillLevels["Health"]);
        if (skillLevels.ContainsKey("ReHealth"))
            _database.SavePlayerReHealth(steamId, skillLevels["ReHealth"]);
        if (skillLevels.ContainsKey("Damage"))
            _database.SavePlayerDamage(steamId, skillLevels["Damage"]);
        if (skillLevels.ContainsKey("HeadShot"))
            _database.SavePlayerHeadShot(steamId, skillLevels["HeadShot"]);
        if (skillLevels.ContainsKey("Speed"))
            _database.SavePlayerSpeed(steamId, skillLevels["Speed"]);
        if (skillLevels.ContainsKey("Gravity"))
            _database.SavePlayerGravity(steamId, skillLevels["Gravity"]);
        if (skillLevels.ContainsKey("Knock"))
            _database.SavePlayerKnock(steamId, skillLevels["Knock"]);
        if (skillLevels.ContainsKey("Mirror"))
            _database.SavePlayerMirror(steamId, skillLevels["Mirror"]);
        if (skillLevels.ContainsKey("Vampire"))
            _database.SavePlayervampire(steamId, skillLevels["Vampire"]);

    }
    /*
    
    public async Task SavePlayerDataForAllPlayersAsync()
    {
        List<CCSPlayerController> playerlist = Utilities.GetPlayers();

        foreach (var client in playerlist)
        {
            if (client != null)
            {
                ulong steamId = client.SteamID;
                if (steamId > 0)
                {
                    // 从字典中获取玩家的基础数据
                    int experience = playerExperiences.ContainsKey(steamId) ? playerExperiences[steamId] : 0;
                    int level = playerLevels.ContainsKey(steamId) ? playerLevels[steamId] : 0;
                    int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;

                    // 从字典中获取玩家的技能数据
                    var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();

                    // 保存玩家的基础数据和技能数据到数据库
                    await SavePlayerDataAsync(steamId, skillLevels);
                }
            }
        }
    }

    public async Task SavePlayerDataAsync(ulong steamId, Dictionary<string, int> skillLevels)
    {
        // 保存玩家的基础数据到数据库
        await _database.SavePlayerExperienceAsync(steamId, playerExperiences[steamId]);
        await _database.SavePlayerlevelAsync(steamId, playerLevels[steamId]);
        await _database.SavePlayerSkillPointsAsync(steamId, playerSkillPoints[steamId]);

        // 保存玩家的技能数据到数据库
        if (skillLevels.ContainsKey("Health"))
            await _database.SavePlayerHealthAsync(steamId, skillLevels["Health"]);
        if (skillLevels.ContainsKey("ReHealth"))
            await _database.SavePlayerReHealthAsync(steamId, skillLevels["ReHealth"]);
        if (skillLevels.ContainsKey("Damage"))
            await _database.SavePlayerDamageAsync(steamId, skillLevels["Damage"]);
        if (skillLevels.ContainsKey("HeadShot"))
            await _database.SavePlayerHeadShotAsync(steamId, skillLevels["HeadShot"]);
        if (skillLevels.ContainsKey("Speed"))
            await _database.SavePlayerSpeedAsync(steamId, skillLevels["Speed"]);
        if (skillLevels.ContainsKey("Gravity"))
            await _database.SavePlayerGravityAsync(steamId, skillLevels["Gravity"]);
        if (skillLevels.ContainsKey("Knock"))
            await _database.SavePlayerKnockAsync(steamId, skillLevels["Knock"]);
        if (skillLevels.ContainsKey("Mirror"))
            await _database.SavePlayerMirrorAsync(steamId, skillLevels["Mirror"]);
        if (skillLevels.ContainsKey("Vampire"))
            await _database.SavePlayervampireAsync(steamId, skillLevels["Vampire"]);
    }

    public async Task SavePlayerSkillAsync(ulong steamId, Dictionary<string, int> skillLevels)
    {
        // 保存玩家的技能数据到数据库
        if (skillLevels.ContainsKey("Health"))
            await _database.SavePlayerHealthAsync(steamId, skillLevels["Health"]);
        if (skillLevels.ContainsKey("ReHealth"))
            await _database.SavePlayerReHealthAsync(steamId, skillLevels["ReHealth"]);
        if (skillLevels.ContainsKey("Damage"))
            await _database.SavePlayerDamageAsync(steamId, skillLevels["Damage"]);
        if (skillLevels.ContainsKey("HeadShot"))
            await _database.SavePlayerHeadShotAsync(steamId, skillLevels["HeadShot"]);
        if (skillLevels.ContainsKey("Speed"))
            await _database.SavePlayerSpeedAsync(steamId, skillLevels["Speed"]);
        if (skillLevels.ContainsKey("Gravity"))
            await _database.SavePlayerGravityAsync(steamId, skillLevels["Gravity"]);
        if (skillLevels.ContainsKey("Knock"))
            await _database.SavePlayerKnockAsync(steamId, skillLevels["Knock"]);
        if (skillLevels.ContainsKey("Mirror"))
            await _database.SavePlayerMirrorAsync(steamId, skillLevels["Mirror"]);
        if (skillLevels.ContainsKey("Vampire"))
            await _database.SavePlayervampireAsync(steamId, skillLevels["Vampire"]);
    }
    */
    #endregion

    #region 计算玩家升级所需经验
    // 计算玩家升级所需经验（根据等级公式）
    private int GetExperienceForNextLevel(int level)
    {
        // 根据当前等级计算所需经验，假设每升一级需要 LevelNeedExperience 点经验
        return CFG.FirstLevelExperience + (level * CFG.LevelNeedExperience);
    }
    #endregion

    #region 增加经验值

    /*
    public void AddExperience(CCSPlayerController client, float Experience)
    {
        
        ulong steamId = client.SteamID;
        bool isBot = steamId == 0; // 可能是 bot 或没有 SteamID

        if(!isBot)
        {
            float AddExperience = Experience;
            if (_api != null && _api.IsClientVip(client))
            {
                AddExperience *= 2.0f;
            }
            int currentExp = playerExperiences.ContainsKey(steamId) ? playerExperiences[steamId] : 0;
            currentExp += (int)AddExperience; // 将伤害经验添加到总经验
            playerExperiences[steamId] = currentExp;
            int level = playerLevels.ContainsKey(steamId) ? playerLevels[steamId] : 0;
            int requiredExp = GetExperienceForNextLevel(level);

            //client.PrintToChat($"[华仔]RPG获取经验值{Experience}");
            if (currentExp >= requiredExp)
            {
                // 计算升级
                int levelUpExp = currentExp - requiredExp;
                level++;
                int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;
                skillPoints += CFG.LevelGiveSkillPoints; // 升级给予技能点

                playerLevels[steamId] = level;
                playerSkillPoints[steamId] = skillPoints;
                if(CFG.UpgradeExperienceRetention)
                {
                    playerExperiences[steamId] = levelUpExp;
                }
                else
                {
                    playerExperiences[steamId] = 0;
                }        
                client.ExecuteClientCommand("play sounds/buttons/blip2.vsnd_c"); // 等级升级音效

            }

        }

    }
    */

    public void AddExperience(CCSPlayerController client, float experience)
    {
        // 参数验证
        if (client == null || experience <= 0)
            return;

        ulong steamId = client.SteamID;
        if (steamId == 0) // 跳过 bot
            return;

        // 计算最终经验值
        float finalExperience = experience;

        // VIP 检查（只有在 API 可用且是 VIP 时才加倍）
        if (_api?.IsClientVip(client) == true)
        {
            finalExperience *= 2.0f;
        }

        // 获取当前经验（如果不存在则默认为0）
        playerExperiences.TryGetValue(steamId, out int currentExp);

        // 添加经验（四舍五入）
        currentExp += (int)Math.Round(finalExperience);
        playerExperiences[steamId] = currentExp;

        // 获取当前等级（如果不存在则默认为0）
        playerLevels.TryGetValue(steamId, out int level);

        int requiredExp = GetExperienceForNextLevel(level);

        if (currentExp >= requiredExp)
        {
            // 计算升级后的剩余经验
            int levelUpExp = currentExp - requiredExp;
            level++;

            // 获取当前技能点（如果不存在则默认为0）
            playerSkillPoints.TryGetValue(steamId, out int skillPoints);
            skillPoints += CFG.LevelGiveSkillPoints;

            // 更新玩家数据
            playerLevels[steamId] = level;
            playerSkillPoints[steamId] = skillPoints;
            playerExperiences[steamId] = CFG.UpgradeExperienceRetention ? levelUpExp : 0;

            // 播放升级音效
            client.ExecuteClientCommand("play sounds/buttons/blip2.vsnd_c");
        }
        //client.PrintToChat($"玩家{client.PlayerName} 初始经验值传入 {experience }获得最终 {finalExperience}经验值");
    }
    #endregion


    #region 显示伤害
    public void ShowdamageText(CCSPlayerController client)
    {
        if (client == null || !client.IsValid)
            return;

        if (!client.PawnIsAlive || client.TeamNum != 3)
            return;

        var clientpawn = client.PlayerPawn.Value;

        if (clientpawn == null || !clientpawn.IsValid)
            return;

        RemoveShowdamage(client);
        // 确保字典内没有重复的槽位
        if (ShowDamage[client.Slot] == null)
        {
                
            ShowDamageWordText = "";
            var handle = showdamageHud.GetOrCreateViewModels(clientpawn);
            if(handle != null)
            {
                ShowDamage[client.Slot] = showdamageHud.CreateText(client, handle, ShowDamageWordText, -0.25f, 1.0f, 48, "Arial Bold", Color.Red); 
            }
        }      
    }

    public void RemoveShowdamage(CCSPlayerController client)
    {
        if (ShowDamage.ContainsKey(client.Slot) && ShowDamage[client.Slot]?.IsValid == true)
        {
            ShowDamage[client.Slot].Remove();
        }
        ShowDamage[client.Slot] = null;
    }
    #endregion

    #region 显示RPG信息
    public void UpdataHud(CCSPlayerController client)
    {
        if (client == null || !client.IsValid )
            return;

        if(!client.PawnIsAlive || client.TeamNum != 3)
            return;

        if(PlayerHud[client.Slot] == null)
            return;

        //Server.PrintToChatAll($"玩家 {client.PlayerName} 进入 UpdataHud");

        ulong steamId = client.SteamID;
        int experience = playerExperiences.ContainsKey(steamId) ? playerExperiences[steamId] : 0;
        int level = playerLevels.ContainsKey(steamId) ? playerLevels[steamId] : 0;
        int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;
        int NextLevelExperience = GetExperienceForNextLevel(level);
        int Rank = _database.GetPlayerRank(steamId);

        var speed = client.PlayerPawn.Value?.AbsVelocity?.Length2D() ?? 0;
        var speedtext = Math.Round(speed).ToString();

        if(!showtop10[client.Slot])
        {
            string vipTag = ""; // VIP 标签
            if (_api != null && _api.IsClientVip(client))
            {
                vipTag = "[VIP]";
            }
            // 动态生成信息
            WordText = $"[华仔]RPG信息:\n玩家: {vipTag}{client.PlayerName} 等级: {level}\n排名: 第{Rank}名 RPG点数: {skillPoints}\n当前经验值: {experience}/{NextLevelExperience}\n当前移动速度: {speedtext}";

            if (!string.IsNullOrEmpty(vipTag))
            {
                var activeWeapon = client.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
                if(activeWeapon != null)
                {
                    if(activeWeapon.DesignerName == "weapon_knife")
                    {
                        if(activeWeapon.AttributeManager.Item.CustomName == "鬼丸国纲"||activeWeapon.AttributeManager.Item.CustomName == "科林手锯"||activeWeapon.AttributeManager.Item.CustomName == "流萤长剑"||activeWeapon.AttributeManager.Item.CustomName == "流星锤")
                        {
                            WordText += "\n[VIP]特殊刀具 - 伤害10倍";
                        }
                        else
                        {
                            WordText += "\n[VIP]双倍经验特权 - 生效中";
                        }
                    }
                    else
                    {
                        WordText += "\n[VIP]双倍经验特权 - 生效中";
                    }
                }
            }
            
        }
        else
        {
            var top10Players = _database.GetTop10PlayerNames();
            WordText = "[华仔]RPGTop10信息:\n";
            foreach (var player in top10Players)
            {
                WordText += $"排名: {player.Rank}, 玩家名: {player.PlayerName}, 等级: {player.Level}\n"; // 添加等级信息
            }

            
        }
        PlayerHud[client.Slot].AcceptInput("SetMessage", PlayerHud[client.Slot], PlayerHud[client.Slot], $"{WordText}");
            
        
    }

    public void ShowRPGText(CCSPlayerController client)
    {
        if (client == null || !client.IsValid )
            return;

        if(!client.PawnIsAlive || client.TeamNum != 3)
            return;

        var clientpawn = client.PlayerPawn.Value;

        if (clientpawn == null || !clientpawn.IsValid )
            return;

        RemoveRPGText(client);

        //Server.PrintToChatAll($"玩家 {client.PlayerName} 进入 ShowRPGText");
        
        if (string.IsNullOrEmpty(HudCFG.HudColor))
        {
            TextColor = Color.DarkOrange; // 如果 HudColor 为空，则使用默认颜色
        }
        else
        {
            try
            {
                TextColor = ColorTranslator.FromHtml(HudCFG.HudColor); // 使用 HTML 颜色代码
            }
            catch (Exception)
            {
                TextColor = Color.DarkOrange; // 如果解析失败，使用默认颜色
            }
        }
        if(HudCFG.HudEnble)
        {
            
            if(clientpawn != null)
            {
                //Server.PrintToChatAll($"玩家 {client.PlayerName} clientpawn != null");

                ulong steamId = client.SteamID;
                int experience = playerExperiences.ContainsKey(steamId) ? playerExperiences[steamId] : 0;
                int level = playerLevels.ContainsKey(steamId) ? playerLevels[steamId] : 0;
                int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;
                int NextLevelExperience = GetExperienceForNextLevel(level);
                int Rank = _database.GetPlayerRank(steamId);
                
                var speed = client.PlayerPawn.Value?.AbsVelocity?.Length2D() ?? 0;
                var speedtext = Math.Round(speed).ToString();

                if (PlayerHud[client.Slot] == null)
                {
                    //Server.PrintToChatAll($"玩家 {client.PlayerName} PlayerHud[client.Slot] == null");
                    
                    string vipTag = ""; // VIP 标签
                    if (_api != null && _api.IsClientVip(client))
                    {
                        vipTag = "[VIP]";
                    }
                    // 动态生成信息
                    WordText = $"[华仔]RPG信息:\n玩家: {vipTag}{client.PlayerName} 等级: {level}\n排名: 第{Rank}名 RPG点数: {skillPoints}\n当前经验值: {experience}/{NextLevelExperience}\n当前移动速度: {speedtext}";

                    if (!string.IsNullOrEmpty(vipTag))
                    {
                        var activeWeapon = client.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
                        if(activeWeapon != null)
                        {
                            if(activeWeapon.DesignerName == "weapon_knife")
                            {
                                if(activeWeapon.AttributeManager.Item.CustomName == "鬼丸国纲"||activeWeapon.AttributeManager.Item.CustomName == "科林手锯"||activeWeapon.AttributeManager.Item.CustomName == "流萤长剑"||activeWeapon.AttributeManager.Item.CustomName == "流星锤")
                                {
                                    WordText += "\n[VIP]特殊刀具 - 伤害10倍";
                                }
                                else
                                {
                                    WordText += "\n[VIP]双倍经验特权 - 生效中";
                                }
                            }
                            else
                            {
                                WordText += "\n[VIP]双倍经验特权 - 生效中";
                            }
                        }
                    }
                    var handle = RpgHud.GetOrCreateViewModels(clientpawn);
                    if(handle != null )
                    {
                        //Server.PrintToChatAll($"玩家 {client.PlayerName} handle != null");

                        PlayerHud[client.Slot] = RpgHud.CreateText(client, handle, WordText, HudCFG.HudTextX, HudCFG.HudTextY, HudCFG.Hudsize, HudCFG.HudFontName, TextColor);

                        g_hHudinfo[client.Slot]?.Kill();
                        g_hHudinfo[client.Slot] = null;
                        g_hHudinfo[client.Slot] = AddTimer(0.1f, () => {UpdataHud(client);},TimerFlags.REPEAT|TimerFlags.STOP_ON_MAPCHANGE); 
                    }

                }


            }
                 

        }
        


    }



    

    public void RemoveRPGText(CCSPlayerController client)
    {
        if (PlayerHud.ContainsKey(client.Slot) && PlayerHud[client.Slot]?.IsValid == true)
        {
            PlayerHud[client.Slot].Remove();
        }

        // 确保字典中该玩家的菜单实体被清除
        PlayerHud[client.Slot] = null;
    }

    #endregion

    

    #region RPG菜单

    private void HandlePlayerInput(CCSPlayerController client, PlayerButtons buttons)
    {
        // 检测按键：W、A、S、D、E、Shift
        if ((buttons & PlayerButtons.Forward) == PlayerButtons.Forward && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} W");
            ScrollSkillMenu(client, -1); // W 向上滚动
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        if ((buttons & PlayerButtons.Back) == PlayerButtons.Back && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} S");
            ScrollSkillMenu(client, 1); // S 向下滚动
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        if ((buttons & PlayerButtons.Use) == PlayerButtons.Use && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} E");
            SelectSkill(client); // 选择当前项目
            /*
            Server.NextWorldUpdate(async () =>
            {
                try
                {
                    await SelectSkillAsync(client); // 选择当前项目
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RPGDatabase] 保存 Skill 时发生错误: {ex.Message}");
                }

            });
            */
            //CloseMenu(client); // 关闭菜单
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        if ((buttons & PlayerButtons.Speed) == PlayerButtons.Speed && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} Shift");
            CloseMenu(client); // 关闭菜单
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        
    }
    private void ScrollSkillMenu(CCSPlayerController client, int direction)
    {
        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();

        // 获取所有技能项
        List<SkillConfig> skillList = SkillCFG.Skills.ToList();  // 动态获取技能列表
        int totalSkills = skillList.Count;  // 获取技能的总数
        const int itemsPerPage = 5;  // 每页显示的技能项数

        // 计算最大页码
        int maxPage = (int)Math.Ceiling((double)totalSkills / itemsPerPage);

        // 获取当前页码和当前选项
        if (!playerCurrentSelections.ContainsKey(client.Slot))
        {
            playerCurrentSelections[client.Slot] = 0; // 默认选择第一页的第一个选项
        }

        int globalIndex = playerCurrentSelections[client.Slot];  // 获取全局索引
        int currentPage = globalIndex / itemsPerPage + 1;  // 当前页码
        int currentSelection = globalIndex % itemsPerPage;  // 当前选中的项

        // 根据滚动方向调整选项
        if (direction == 1) // 向下滚动（选择下一项）
        {
            //if (currentSelection < itemsPerPage - 1)
            if (currentSelection < itemsPerPage - 1 && (currentPage - 1) * itemsPerPage + currentSelection + 1 < totalSkills)
            {
                currentSelection++;
            }
            else if (currentPage < maxPage)
            {
                currentPage++;
                currentSelection = 0;
            }
            else
            {
                // 如果已经是最后一项，保持在当前项
                currentSelection = Math.Min(currentSelection, totalSkills - (currentPage - 1) * itemsPerPage);
            }
        }
        else if (direction == -1) // 向上滚动（选择上一项）
        {
            if (currentSelection > 0)
            {
                currentSelection--;
            }
            else if (currentPage > 1)
            {
                currentPage--;
                currentSelection = itemsPerPage - 1;
            }
        }

        // 重新计算全局索引
        globalIndex = (currentPage - 1) * itemsPerPage + currentSelection;
        playerCurrentSelections[client.Slot] = globalIndex;  // 更新玩家当前选择的全局索引

        // 确保选项在有效范围内
        if (globalIndex >= totalSkills)
        {
            globalIndex = totalSkills - 1;
        }

        // 更新技能菜单显示
        UpdateSkillMenu(client, currentPage, currentSelection, maxPage);
        client.ExecuteClientCommand("play Ui/buttonclick.vsnd_c");
    }




    private void UpdateSkillMenu(CCSPlayerController client, int currentPage, int currentSelection, int maxPage)
    {
        // 每页显示的技能项数
        const int itemsPerPage = 5;

        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();

        SkillConfig healthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Health");
        SkillConfig rehealthSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "ReHealth");
        SkillConfig damageSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Damage");
        SkillConfig headshotSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "HeadShot");
        SkillConfig speedSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Speed");
        SkillConfig gravitySkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Gravity");
        SkillConfig knokSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Knock");
        SkillConfig mirrorSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Mirror");
        SkillConfig vampireSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Vampire");
        // 硬编码每个技能
        var skills = new[]
        {
            new { Name = "暴饮暴食(血量上限++)", Level = skillLevels.ContainsKey("Health") ? skillLevels["Health"] : 0, UpgradeCost = healthSkill.HealthFirstLevel + (skillLevels.ContainsKey("Health") ? skillLevels["Health"] : 0) * healthSkill.HealthEachLevel, MaxLevel = healthSkill.HealthMaxLevel },
            new { Name = "自我修复(血量恢复++)", Level = skillLevels.ContainsKey("ReHealth") ? skillLevels["ReHealth"] : 0, UpgradeCost = rehealthSkill.ReHealthFirstLevel + (skillLevels.ContainsKey("ReHealth") ? skillLevels["ReHealth"] : 0) * rehealthSkill.ReHealthEachLevel, MaxLevel = rehealthSkill.ReHealthMaxLevel },
            new { Name = "破坏狂(伤害增加++)", Level = skillLevels.ContainsKey("Damage") ? skillLevels["Damage"] : 0, UpgradeCost = damageSkill.DamageFirstLevel + (skillLevels.ContainsKey("Damage") ? skillLevels["Damage"] : 0) * damageSkill.DamageEachLevel, MaxLevel = damageSkill.DamageMaxLevel },
            new { Name = "致命一击(爆头伤害++)", Level = skillLevels.ContainsKey("HeadShot") ? skillLevels["HeadShot"] : 0, UpgradeCost = headshotSkill.HeadShotFirstLevel + (skillLevels.ContainsKey("HeadShot") ? skillLevels["HeadShot"] : 0) * headshotSkill.HeadShotEachLevel, MaxLevel = headshotSkill.HeadShotMaxLevel },
            new { Name = "心跳加速(移动速度++)", Level = skillLevels.ContainsKey("Speed") ? skillLevels["Speed"] : 0, UpgradeCost = speedSkill.SpeedFirstLevel + (skillLevels.ContainsKey("Speed") ? skillLevels["Speed"] : 0) * speedSkill.SpeedEachLevel, MaxLevel = speedSkill.SpeedMaxLevel },
            new { Name = "月球人(重力降低)", Level = skillLevels.ContainsKey("Gravity") ? skillLevels["Gravity"] : 0, UpgradeCost = gravitySkill.GravityFirstLevel + (skillLevels.ContainsKey("Gravity") ? skillLevels["Gravity"] : 0) * gravitySkill.GravityEachLevel, MaxLevel = gravitySkill.GravityMaxLevel },
            new { Name = "大口径子弹(击退++)", Level = skillLevels.ContainsKey("Knock") ? skillLevels["Knock"] : 0, UpgradeCost = knokSkill.KnockFirstLevel + (skillLevels.ContainsKey("Knock") ? skillLevels["Knock"] : 0) * knokSkill.KnockEachLevel, MaxLevel = knokSkill.KnockMaxLevel },
            new { Name = "改装大师(弹匣扩容++)", Level = skillLevels.ContainsKey("Mirror") ? skillLevels["Mirror"] : 0, UpgradeCost = mirrorSkill.MirrorFirstLevel + (skillLevels.ContainsKey("Mirror") ? skillLevels["Mirror"] : 0) * mirrorSkill.MirrorEachLevel, MaxLevel = mirrorSkill.MirrorMaxLevel },
            new { Name = "嗜血狂热(伤害吸血++)", Level = skillLevels.ContainsKey("Vampire") ? skillLevels["Vampire"] : 0, UpgradeCost = vampireSkill.VampireFirstLevel + (skillLevels.ContainsKey("Vampire") ? skillLevels["Vampire"] : 0) * vampireSkill.VampireEachLevel, MaxLevel = vampireSkill.VampireMaxLevel },
        };

        int totalSkills = skills.Length;

        // 计算当前页需要显示的技能项
        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Math.Min(startIndex + itemsPerPage, totalSkills);

        // 更新菜单文本
        WordText = $"[华仔]RPG技能升级菜单:\n按W/S向上下选择(第{currentPage}/{maxPage}页)\n";
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i == startIndex + currentSelection)  // 高亮当前选中的项
            {
                WordText += $"-> {skills[i].Name}: 等级:[{skills[i].Level}/{skills[i].MaxLevel}](升级消耗: {skills[i].UpgradeCost})\n";
            }
            else
            {
                WordText += $"{skills[i].Name}: 等级:[{skills[i].Level}/{skills[i].MaxLevel}](升级消耗: {skills[i].UpgradeCost})\n";
            }
        }

        WordText += "按E确认|按SHIFT关闭菜单";


        PlayerMenuEntities[client.Slot].AcceptInput("SetMessage", PlayerMenuEntities[client.Slot], PlayerMenuEntities[client.Slot], $"{WordText}");

        
    }
    /*
    private async Task SelectSkillAsync(CCSPlayerController client)
    {
        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();

        // 获取当前的全局索引
        int globalIndex = playerCurrentSelections[client.Slot];

        // 每页显示的技能项数
        const int itemsPerPage = 5;

        // 计算当前页和当前选择
        int currentPage = globalIndex / itemsPerPage + 1;  // 当前页码
        int currentSelection = globalIndex % itemsPerPage;  // 当前页的选择项

        // 获取当前页的技能列表
        List<SkillConfig> skillList = SkillCFG.Skills.ToList();
        SkillConfig selectedSkill = skillList.Skip((currentPage - 1) * itemsPerPage).Take(itemsPerPage).ElementAtOrDefault(currentSelection);

        if (selectedSkill == null)
        {
            client.PrintToChat("[华仔] 选择的技能不存在");
            return;
        }

        // 判断技能当前等级
        int currentLevel = skillLevels.ContainsKey(selectedSkill.SkillName) ? skillLevels[selectedSkill.SkillName] : 0;
        int maxLevel = selectedSkill.SkillName switch
        {
            "Health" => selectedSkill.HealthMaxLevel,
            "ReHealth" => selectedSkill.ReHealthMaxLevel,
            "Damage" => selectedSkill.DamageMaxLevel,
            "HeadShot" => selectedSkill.HeadShotMaxLevel,
            "Speed" => selectedSkill.SpeedMaxLevel,
            "Gravity" => selectedSkill.GravityMaxLevel,
            "Knock" => selectedSkill.KnockMaxLevel,
            "Mirror" => selectedSkill.MirrorMaxLevel,
            "Vampire" => selectedSkill.VampireMaxLevel,
            _ => 0
        };

        string chineseSkillName = SkillNameToChinese.ContainsKey(selectedSkill.SkillName)
                                ? SkillNameToChinese[selectedSkill.SkillName]
                                : selectedSkill.SkillName; // 如果没有中文名称，默认使用英文

        if (currentLevel >= maxLevel)
        {
            client.PrintToChat($"[华仔] {chineseSkillName} 已经达到最大等级");
            client.ExecuteClientCommand("play sounds/buttons/button8.vsnd_c"); // 技能点不足音效
            return;
        }

        // 获取升级所需的消耗
        int upgradeCost = GetUpgradeCost(selectedSkill, currentLevel);
        int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;

        // 判断玩家是否有足够的资源进行升级
        if (skillPoints < upgradeCost)
        {
            client.PrintToChat($"[华仔] {chineseSkillName} 升级所需 {upgradeCost} 技能点，当前技能点不足");
            client.ExecuteClientCommand("play sounds/buttons/button8.vsnd_c"); // 技能点不足音效
            return;
        }

        // 扣除技能点
        skillPoints -= upgradeCost;
        playerSkillPoints[steamId] = skillPoints;
        await _database.SavePlayerSkillPointsAsync(steamId, playerSkillPoints[steamId]); // 异步保存

        // 升级技能
        skillLevels[selectedSkill.SkillName] = currentLevel + 1;

        // 更新玩家技能数据
        playerSkillLevels[steamId] = skillLevels;

        await SavePlayerSkillAsync(steamId, skillLevels); // 异步保存

        // 输出升级信息
        client.PrintToChat($"[华仔] {chineseSkillName} 已升级到 {skillLevels[selectedSkill.SkillName]} 级");

        // 更新菜单，刷新技能信息
        UpdateSkillMenu(client, currentPage, currentSelection, (int)Math.Ceiling((double)SkillCFG.Skills.Count() / itemsPerPage));

        // 播放成功的音效或特效（根据游戏需求）
        client.ExecuteClientCommand("play sounds/buttons/button9.vsnd_c");
    }
    */





    private void SelectSkill(CCSPlayerController client)
    {
        ulong steamId = client.SteamID;
        var skillLevels = playerSkillLevels.ContainsKey(steamId) ? playerSkillLevels[steamId] : new Dictionary<string, int>();

        // 获取当前的全局索引
        int globalIndex = playerCurrentSelections[client.Slot];

        // 每页显示的技能项数
        const int itemsPerPage = 5;

        // 计算当前页和当前选择
        int currentPage = globalIndex / itemsPerPage + 1;  // 当前页码
        int currentSelection = globalIndex % itemsPerPage;  // 当前页的选择项

        // 获取当前页的技能列表
        List<SkillConfig> skillList = SkillCFG.Skills.ToList();
        SkillConfig selectedSkill = skillList.Skip((currentPage - 1) * itemsPerPage).Take(itemsPerPage).ElementAtOrDefault(currentSelection);

        if (selectedSkill == null)
        {
            client.PrintToChat("[华仔] 选择的技能不存在");
            return;
        }

        // 判断技能当前等级
        int currentLevel = skillLevels.ContainsKey(selectedSkill.SkillName) ? skillLevels[selectedSkill.SkillName] : 0;
        int maxLevel = selectedSkill.SkillName switch
        {
            "Health" => selectedSkill.HealthMaxLevel,
            "ReHealth" => selectedSkill.ReHealthMaxLevel,
            "Damage" => selectedSkill.DamageMaxLevel,
            "HeadShot" => selectedSkill.HeadShotMaxLevel,
            "Speed" => selectedSkill.SpeedMaxLevel,
            "Gravity" => selectedSkill.GravityMaxLevel,
            "Knock" => selectedSkill.KnockMaxLevel,
            "Mirror" => selectedSkill.MirrorMaxLevel,
            "Vampire" => selectedSkill.VampireMaxLevel,
            _ => 0
        };

        string chineseSkillName = SkillNameToChinese.ContainsKey(selectedSkill.SkillName) 
                                ? SkillNameToChinese[selectedSkill.SkillName] 
                                : selectedSkill.SkillName; // 如果没有中文名称，默认使用英文

        if (currentLevel >= maxLevel)
        {
            client.PrintToChat($"[华仔] {chineseSkillName} 已经达到最大等级");
            client.ExecuteClientCommand("play sounds/buttons/button8.vsnd_c"); // 技能点不足音效
            return;
        }

        // 获取升级所需的消耗
        int upgradeCost = GetUpgradeCost(selectedSkill, currentLevel);
        int skillPoints = playerSkillPoints.ContainsKey(steamId) ? playerSkillPoints[steamId] : 0;

        // 判断玩家是否有足够的资源进行升级
        if (skillPoints < upgradeCost)
        {
            client.PrintToChat($"[华仔] {chineseSkillName} 升级所需 {upgradeCost} 技能点，当前技能点不足");
            client.ExecuteClientCommand("play sounds/buttons/button8.vsnd_c"); // 技能点不足音效
            return;
        }

        // 扣除技能点
        skillPoints -= upgradeCost;
        playerSkillPoints[steamId] = skillPoints;
        _database.SavePlayerSkillPoints(steamId, playerSkillPoints[steamId]);

        // 升级技能
        skillLevels[selectedSkill.SkillName] = currentLevel + 1;

        // 更新玩家技能数据
        playerSkillLevels[steamId] = skillLevels;

        SavePlayerSkill(steamId, skillLevels);


        // 输出升级信息
        client.PrintToChat($"[华仔] {chineseSkillName} 已升级到 {skillLevels[selectedSkill.SkillName]} 级");

        // 更新菜单，刷新技能信息
        UpdateSkillMenu(client, currentPage, currentSelection, (int)Math.Ceiling((double)SkillCFG.Skills.Count() / itemsPerPage));

        // 播放成功的音效或特效（根据游戏需求）
        client.ExecuteClientCommand("play sounds/buttons/button9.vsnd_c");
    }
    
    #endregion


    /*
        private SkillConfig GetSkillFromSelection(int selectionIndex)
        {
            // 获取选中的技能配置
            switch (selectionIndex)
            {
                case 0: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Health");
                case 1: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "ReHealth");
                case 2: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Damage");
                case 3: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "HeadShot");
                case 4: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Speed");
                case 5: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Gravity");
                case 6: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Knock");
                case 7: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Mirror");
                case 8: return SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Vampire");
                default: return null;
            }
        }
    */

    private int GetUpgradeCost(SkillConfig skillConfig, int currentLevel)
    {
        // 根据当前等级和技能的升阶规则计算升级所需的资源
        switch (skillConfig.SkillName)
        {
            case "Health":
                return skillConfig.HealthFirstLevel + currentLevel * skillConfig.HealthEachLevel;
            case "ReHealth":
                return skillConfig.ReHealthFirstLevel + currentLevel * skillConfig.ReHealthEachLevel;
            case "Damage":
                return skillConfig.DamageFirstLevel + currentLevel * skillConfig.DamageEachLevel;
            case "HeadShot":
                return skillConfig.HeadShotFirstLevel + currentLevel * skillConfig.HeadShotEachLevel;
            case "Speed":
                return skillConfig.SpeedFirstLevel + currentLevel * skillConfig.SpeedEachLevel;
            case "Gravity":
                return skillConfig.GravityFirstLevel + currentLevel * skillConfig.GravityEachLevel;
            case "Knock":
                return skillConfig.KnockFirstLevel + currentLevel * skillConfig.KnockEachLevel;
            case "Mirror":
                return skillConfig.MirrorFirstLevel + currentLevel * skillConfig.MirrorEachLevel;
            case "Vampire":
                return skillConfig.VampireFirstLevel + currentLevel * skillConfig.VampireEachLevel;
            default:
                return 0;
        }
    }

    


    private void CloseMenu(CCSPlayerController client)
    {
        // 关闭菜单的逻辑，可能是清除屏幕显示
        Remove(client);
        client.PrintToChat("[华仔]RPG菜单已关闭");
        client.ExecuteClientCommand("play Ui/buttonclick.vsnd_c");
        MenuOpen[client.Slot] = false;
    }

    public void Remove(CCSPlayerController client)
    {
        if (PlayerMenuEntities.ContainsKey(client.Slot) && PlayerMenuEntities[client.Slot]?.IsValid == true)
        {
            PlayerMenuEntities[client.Slot].Remove();
        }

        // 确保字典中该玩家的菜单实体被清除
        PlayerMenuEntities[client.Slot] = null;
    }

    public bool CheckMenusAndHandle(CCSPlayerController client)
    {
        // 获取所有 "point_worldtext" 实体
        var entities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("point_worldtext");

        foreach (var entity in entities)
        {
            if (entity != null)
            {
                // 如果该实体属于玩家
                if (entity.OwnerEntity.Raw == client.Pawn.Raw)
                {
                    // 如果实体的名字不是 "modelsmenu" 且当前菜单不是模型菜单
                    if (entity.Entity!.Name != "Rpgsmenu")
                    {
                        // 提示玩家当前有其他菜单打开
                        client.PrintToChat("你现在正在打开别的菜单，请先关闭后再打开RPG菜单。");
                        return false;  // 返回 false 表示菜单不能正常打开
                    }
                }
            }
        }
        // 如果没有检测到其他菜单，正常打开菜单
        return true;  // 返回 true 表示可以正常打开菜单
    }

    /*

    HookResult CBaseEntity_TakeDamageOldFunc(DynamicHook hook)
    {
        var victim = hook.GetParam<CEntityInstance>(0);
        if (victim == null)
            return HookResult.Continue;

        var damageinfo = hook.GetParam<CTakeDamageInfo>(1);
        if (damageinfo == null)
            return HookResult.Continue;

        var attacker = damageinfo.Attacker?.Value;
        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        var AttackerPawn = attacker.As<CCSPlayerPawn>();
        if (AttackerPawn == null || !AttackerPawn.IsValid)
            return HookResult.Continue;

        var VictimPawn = victim.As<CCSPlayerPawn>();
        if (VictimPawn == null || !VictimPawn.IsValid)
            return HookResult.Continue;

        var AttackerController = AttackerPawn.OriginalController.Value;
        if (AttackerController == null || !AttackerController.IsValid)
            return HookResult.Continue;

        var VictimController = VictimPawn.OriginalController.Value;
        if (VictimController == null || !VictimController.IsValid)
            return HookResult.Continue;

        if(AttackerController.IsBot)
            return HookResult.Continue;

        if (AttackerController.TeamNum != 3)
            return HookResult.Continue;

        if(VictimPawn.TakesDamage == false)
            return HookResult.Continue;

        ulong attackersteamId = AttackerController.SteamID;

        var attackerskillLevels = playerSkillLevels.ContainsKey(attackersteamId) ? playerSkillLevels[attackersteamId] : new Dictionary<string, int>();

        int DamageLevel = attackerskillLevels.ContainsKey("Damage") ? attackerskillLevels["Damage"] : 0;
        SkillConfig damageSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "Damage");

        int HeadShotLevel = attackerskillLevels.ContainsKey("HeadShot") ? attackerskillLevels["HeadShot"] : 0;
        SkillConfig headshotSkill = SkillCFG.Skills.FirstOrDefault(s => s.SkillName == "HeadShot");

        
        float AddDamage;
        float AddHeadDamage;  
        AddDamage = damageinfo.Damage + (damageinfo.Damage * (DamageLevel * damageSkill.DamageEachLevelAdd));
        AddHeadDamage = damageinfo.Damage + (damageinfo.Damage * (HeadShotLevel * headshotSkill.HeadShotEachLevelAdd));
        
        var activeWeapon = AttackerPawn.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon != null)
        {
            if (activeWeapon.DesignerName == "weapon_knife")
            {
                if (activeWeapon.AttributeManager.Item.CustomName == "鬼丸国纲" || activeWeapon.AttributeManager.Item.CustomName == "科林手锯" || activeWeapon.AttributeManager.Item.CustomName == "流萤长剑" || activeWeapon.AttributeManager.Item.CustomName == "流星锤")
                {
                    AddDamage *= 5.0f;
                    AddHeadDamage *= 5.0f;
                }
            }
        }
        
        if (damageinfo.GetHitGroup() == HitGroup_t.HITGROUP_HEAD)
        {
            if (HeadShotLevel > 0 && headshotSkill != null && headshotSkill.HeadShotEnable)
            {
                damageinfo.Damage += AddDamage + AddHeadDamage;
            }
        }
        else
        {
            if (DamageLevel > 0 && damageSkill != null && damageSkill.DamageEnable)
            {
                damageinfo.Damage += AddDamage;
            }
        }
        
        float hurtExperience = damageinfo.Damage * CFG.HurtDamageExperiencePercen;
        AddExperience(AttackerController, hurtExperience);
        
        if (ShowDamage[AttackerController.Slot] != null)
        {
            float Show = damageinfo.Damage;
            ShowDamageWordText = $"{(int)Show}";
            ShowDamage[AttackerController.Slot].AcceptInput("SetMessage", ShowDamage[AttackerController.Slot], ShowDamage[AttackerController.Slot], $"{ShowDamageWordText}");
            AddTimer(0.2f, () =>
            {
                if (ShowDamage[AttackerController.Slot] != null)
                {
                    ShowDamageWordText = "";
                    ShowDamage[AttackerController.Slot].AcceptInput("SetMessage", ShowDamage[AttackerController.Slot], ShowDamage[AttackerController.Slot], $"{ShowDamageWordText}");
                }

            });
        }

        //AttackerController.PrintToChat($"玩家{AttackerController.PlayerName} 攻击了 {VictimController.PlayerName} 造成 {damageinfo.Damage} 伤害\n伤害等级 {DamageLevel}爆头等级{HeadShotLevel} 伤害加成 {AddDamage} 爆头加成{AddHeadDamage}" +
        //    $"\n 总加成{AddDamage + AddHeadDamage} 获得经验值 {hurtExperience}");




        return HookResult.Continue;


    }
    */





}
