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

public class HanRPGDataBase
{
    public SqliteConnection _connection;

    public static string DbPath = Path.Combine(Application.RootDirectory, "configs/HanSimpleRPG/HanRPGData.db");
    public class PlayerData
    {
        public int Level { get; set; }
        public int Experience { get; set; }
        public int SkillPoints { get; set; }
        public ulong SteamId { get; set; }
        public string PlayerNames { get; set; }
    }
    public class SkillData
    {
        public int Health { get; set; }
        public int ReHealth { get; set; }
        public int Damage { get; set; }
        public int HeadShot { get; set; }
        public int Speed { get; set; }
        public int Gravity { get; set; }
        public int Knock { get; set; }
        public int Mirror { get; set; }
        public int vampire { get; set; }

    }

    public HanRPGDataBase(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task CreateDb()
    {
        try
        {
            // 检查文件夹是否存在，不存在则创建
            string directoryPath = Path.GetDirectoryName(DbPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanRPGData] 文件夹 {directoryPath} 不存在，已创建.");
            }

            Console.WriteLine($"[HanRPGData] 正在尝试连接数据库: {DbPath}");
            // 打开数据库连接
            _connection = new SqliteConnection($"Data Source={DbPath}");
            await _connection.OpenAsync();  // 使用异步打开连接
            Console.WriteLine("[HanRPGData] 数据库连接成功");

            // 创建表（如果不存在）
            await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS PlayerData (steamid INTEGER PRIMARY KEY, Level INTEGER DEFAULT 0, Experience INTEGER DEFAULT 0, SkillPoints INTEGER DEFAULT 0, playername TEXT NOT NULL)");

            Console.WriteLine("[HanRPGData] 数据库表已创建或已存在。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanRPGData] 创建数据库或表时发生错误: {ex.Message}");
        }
    }

    public async Task CreateSkillDb()
    {
        try
        {
            // 检查文件夹是否存在，不存在则创建
            string directoryPath = Path.GetDirectoryName(DbPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanRPGData] 文件夹 {directoryPath} 不存在，已创建.");
            }

            Console.WriteLine($"[HanRPGData] 正在尝试连接数据库: {DbPath}");
            // 打开数据库连接
            _connection = new SqliteConnection($"Data Source={DbPath}");
            await _connection.OpenAsync();  // 使用异步打开连接
            Console.WriteLine("[HanRPGData] 数据库连接成功");

            // 创建表（如果不存在
            await _connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS SkillData (steamid INTEGER PRIMARY KEY, Health INTEGER DEFAULT 0, ReHealth INTEGER DEFAULT 0, Damage INTEGER DEFAULT 0, HeadShot INTEGER DEFAULT 0, Speed INTEGER DEFAULT 0, Gravity INTEGER DEFAULT 0, Knock INTEGER DEFAULT 0, Mirror INTEGER DEFAULT 0, vampire INTEGER DEFAULT 0)");

            Console.WriteLine("[HanRPGData] 数据库表已创建或已存在。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanRPGData] 创建数据库或表时发生错误: {ex.Message}");
        }
    }

    // 关闭数据库连接
    public void DatabaseOnUnload()
    {
        _connection?.Close();
        Console.WriteLine("[HanRPGData] 数据库连接已关闭。");
    }

    public void CreateSkillDataRecord(ulong steamId)
    {
        try
        {
            // 查询时直接使用 ulong 类型的 steamid
            var existingPlayer = _connection.QuerySingleOrDefault<SkillData>(
                "SELECT * FROM SkillData WHERE steamid = @SteamId", 
                new { SteamId = steamId });

            if (existingPlayer == null)
            {
                // 插入数据时，使用 ulong 类型的 steamid
                _connection.Execute(
                    "INSERT INTO SkillData (steamid, Health, ReHealth, Damage, HeadShot, Speed, Gravity, Knock, Mirror, vampire) VALUES (@SteamId, @HealtH, @ReHealtH, @DamagE, @HeadShoT, @SpeeD, @GravitY, @KnocK, @MirroR, @vampirE)",
                    new { SteamId = steamId, HealtH = 0, ReHealtH = 0, DamagE = 0, HeadShoT = 0, SpeeD = 0, GravitY = 0, KnocK = 0, MirroR = 0, vampirE = 0}); // 默认 0
                Console.WriteLine($"为新玩家 {steamId} 创建了RPG记录，默认0");

                // 确认数据是否已成功插入
                var newPlayer = _connection.QuerySingleOrDefault<SkillData>(
                    "SELECT * FROM SkillData WHERE steamid = @SteamId", 
                    new { SteamId = steamId });

                if (newPlayer == null)
                {
                    Console.WriteLine($"[RPGDatabase] 新玩家 {steamId} 的数据未能正确插入。");
                }
                else
                {
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 的数据已成功插入。");
                }
            }
            else
            {
                Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 已存在，跳过创建。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 创建新玩家记录时发生错误: {ex.Message}");
        }
    }

    public void CreatePlayerDataRecord(ulong steamId, string Names)
    {
        try
        {
            // 查询时直接使用 ulong 类型的 steamid
            var existingPlayer = _connection.QuerySingleOrDefault<PlayerData>(
                "SELECT * FROM PlayerData WHERE steamid = @SteamId", 
                new { SteamId = steamId });

            if (existingPlayer == null)
            {
                // 插入数据时，使用 ulong 类型的 steamid
                _connection.Execute(
                    "INSERT INTO PlayerData (steamid, Level, Experience, SkillPoints, playername) VALUES (@SteamId, @LeveL, @ExperiencE, @SkillPointS, @PLAYERNAME)",
                    new { SteamId = steamId, LeveL = 0, ExperiencE = 0, SkillPointS = 0, PLAYERNAME = Names}); // 默认 0
                Console.WriteLine($"为新玩家 {steamId} 创建了RPG记录，默认0");

                // 确认数据是否已成功插入
                var newPlayer = _connection.QuerySingleOrDefault<PlayerData>(
                    "SELECT * FROM PlayerData WHERE steamid = @SteamId", 
                    new { SteamId = steamId });

                if (newPlayer == null)
                {
                    Console.WriteLine($"[RPGDatabase] 新玩家 {steamId} 的数据未能正确插入。");
                }
                else
                {
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 的数据已成功插入。");
                }
            }
            else
            {
                if (existingPlayer.PlayerNames != Names)
                {
                    _connection.Execute(
                    "UPDATE PlayerData SET playername = @PlayerName WHERE steamid = @SteamId",
                        new { SteamId = steamId, PlayerName = Names });
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 的名字已更改为 {Names}");

                }
                else
                {
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 已存在，跳过创建。");
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 创建新玩家记录时发生错误: {ex.Message}");
        }
    }
    

    public int GetHealthLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT Health FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Health ?? 0; // 如果没有记录，返回默认 0
    }
    public int GetReHealthLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT ReHealth FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.ReHealth ?? 0; // 如果没有记录，返回默认 0
    }
    public int GetDamageLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT Damage FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Damage ?? 0; // 如果没有记录，返回默认 0
    }
    public int GetHeadShotLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT HeadShot FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.HeadShot ?? 0; // 如果没有记录，返回默认 0
    }
    public int GetSpeedLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT Speed FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Speed ?? 0; // 如果没有记录，返回默认 0
    }
    public int GetGravityLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT Gravity FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Gravity ?? 0; // 如果没有记录，返回默认 0
    }

    public int GetKnockLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT Knock FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Knock ?? 0; // 如果没有记录，返回默认 0
    }

    public int GetMirrorLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT Mirror FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Mirror ?? 0; // 如果没有记录，返回默认 0
    }
    public int GetvampireLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<SkillData>(
            "SELECT vampire FROM SkillData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.vampire ?? 0; // 如果没有记录，返回默认 0
    }
    
    
    

    public int GetPlayerLevelFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<PlayerData>(
            "SELECT Level FROM PlayerData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Level ?? 0; // 如果没有记录，返回默认 0
    }
    
    public int GetPlayerExperienceFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<PlayerData>(
            "SELECT Experience FROM PlayerData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.Experience ?? 0; // 如果没有记录，返回默认 0
    }

    public int GetPlayerSkillPointsFromDb(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<PlayerData>(
            "SELECT SkillPoints FROM PlayerData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.SkillPoints ?? 0; // 如果没有记录，返回默认 0
    }

    public string GetPlayerNames(ulong steamId)
    { 
        var player = _connection?.QuerySingleOrDefault<PlayerData>(
            "SELECT playername FROM PlayerData WHERE steamid = @SteamId",
            new { SteamId = steamId });   
        return player?.PlayerNames ?? "无"; // 如果没有记录，返回默认 0
    }

    

    public void SavePlayerHealth(ulong steamId, int HEALTH)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET Health = @HealtH WHERE steamid = @SteamId",
                new { HealtH = HEALTH, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Health 时发生错误: {ex.Message}");
        }
    }

    public void SavePlayerReHealth(ulong steamId, int REHEALTH)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET ReHealth = @ReHealtH WHERE steamid = @SteamId",
                new { ReHealtH = REHEALTH, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 ReHealth 时发生错误: {ex.Message}");
        }
    }
    public void SavePlayerDamage(ulong steamId, int DAMAGE)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET Damage = @DamagE WHERE steamid = @SteamId",
                new { DamagE = DAMAGE, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Health 时发生错误: {ex.Message}");
        }
    }
    public void SavePlayerHeadShot(ulong steamId, int HEADSHOT)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET HeadShot = @HeadShoT WHERE steamid = @SteamId",
                new { HeadShoT = HEADSHOT, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 HeadShot 时发生错误: {ex.Message}");
        }
    }
    public void SavePlayerSpeed(ulong steamId, int SPEED)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET Speed = @SpeeD WHERE steamid = @SteamId",
                new { SpeeD = SPEED, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Speed 时发生错误: {ex.Message}");
        }
    }
    public void SavePlayerGravity(ulong steamId, int GRAVITY)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET Gravity = @GravitY WHERE steamid = @SteamId",
                new { GravitY = GRAVITY, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Gravity 时发生错误: {ex.Message}");
        }
    }
    public void SavePlayerKnock(ulong steamId, int KONCK)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET Knock = @KnocK WHERE steamid = @SteamId",
                new { KnocK = KONCK, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Knock 时发生错误: {ex.Message}");
        }
    }
    public void SavePlayerMirror(ulong steamId, int MIRROR)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET Mirror = @MirroR WHERE steamid = @SteamId",
                new { MirroR = MIRROR, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Mirror 时发生错误: {ex.Message}");
        }
    }
    public void SavePlayervampire(ulong steamId, int VAMPIRE)
    {
        try
        {
            _connection.Execute(
                "UPDATE SkillData SET vampire = @vampirE WHERE steamid = @SteamId",
                new { vampirE = VAMPIRE, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 vampire 时发生错误: {ex.Message}");
        }
    }


    public void SavePlayerExperience(ulong steamId, int experience)
    {
        try
        {
            _connection.Execute(
                "UPDATE PlayerData SET Experience = @EXPerience WHERE steamid = @SteamId",
                new { EXPerience = experience, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Experience 时发生错误: {ex.Message}");
        }
    }

    public void SavePlayerlevel(ulong steamId, int level)
    {
        try
        {
            _connection.Execute(
                "UPDATE PlayerData SET Level = @leveL WHERE steamid = @SteamId",
                new { leveL = level, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Level 时发生错误: {ex.Message}");
        }
    }

    public void SavePlayerSkillPoints(ulong steamId, int skillPoints)
    {
        try
        {
            _connection.Execute(
                "UPDATE PlayerData SET SkillPoints = @skillPointS WHERE steamid = @SteamId",
                new { skillPointS = skillPoints, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 SkillPoints 时发生错误: {ex.Message}");
        }
    }

    public void SavePlayername(ulong steamId, string Name)
    {
        try
        {
            _connection.Execute(
                "UPDATE PlayerData SET playername = @PLAYERNAME WHERE steamid = @SteamId",
                new { PLAYERNAME = Name, SteamId = steamId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 玩家名字 时发生错误: {ex.Message}");
        }
    }

    public int GetPlayerRank(ulong steamId)
    {
        // 查询所有玩家的 steamId 和 level
        var players = _connection?.Query<PlayerData>(
        "SELECT steamid, level FROM PlayerData ORDER BY level DESC").ToList();

        if (players == null || !players.Any())
        {
            return -1; // 如果没有玩家数据，返回-1表示无法计算排名
        }

        // 查找当前玩家的排名
        var playerRank = players.FindIndex(player => player.SteamId == steamId) + 1;  // 排名从1开始

        return playerRank;
    }

    public List<(int Rank, string PlayerName, int Level)> GetTop10PlayerNames()
    {
        // 查询所有玩家的 steamid、名字和等级，并按等级降序排序
        var players = _connection?.Query<PlayerData>(
            "SELECT steamid, playername AS PlayerNames, level FROM PlayerData ORDER BY level DESC LIMIT 10").ToList();

        if (players == null || !players.Any())
        {
            return new List<(int, string, int)>(); // 如果没有玩家数据，返回空列表
        }

        // 生成包含排名、名字和等级的列表
        var top10Players = players.Select((player, index) => 
            (Rank: index + 1, PlayerName: player.PlayerNames, Level: player.Level)).ToList();

        return top10Players;
    }

    #region 异步使用查询

    public async Task<int> GetPlayerLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            // 记录日志，告知开发者连接为空，但不让服务器崩溃
            Console.WriteLine("Database connection is not initialized. Returning default level.");
            // 返回默认值，而不是抛出异常
            return 0; // 你可以根据需要返回其他默认值
        }
        try
        {
            var player = await _connection.QuerySingleOrDefaultAsync<PlayerData>(
            "SELECT Level FROM PlayerData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

            return player?.Level ?? 0; // 如果没有记录，返回默认 0
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while retrieving player level for steamId {steamId}: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetPlayerExperienceFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            // 处理数据库连接为空的情况
            Console.WriteLine("Database connection is not initialized. Returning default experience.");
            return 0; // 默认返回经验为 0
        }

        try
        {
            var player = await _connection.QuerySingleOrDefaultAsync<PlayerData>(
                "SELECT Experience FROM PlayerData WHERE steamid = @SteamId",
                new
                {
                    SteamId = steamId
                });

            return player?.Experience ?? 0; // 如果没有记录，返回默认 0
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while retrieving player experience for steamId {steamId}: {ex.Message}");
            return 0; // 发生异常时返回默认经验值
        }
    }

    public async Task<int> GetPlayerSkillPointsFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            // 处理数据库连接为空的情况
            Console.WriteLine("Database connection is not initialized. Returning default skill points.");
            return 0; // 默认返回技能点为 0
        }

        try
        {
            var player = await _connection.QuerySingleOrDefaultAsync<PlayerData>(
                "SELECT SkillPoints FROM PlayerData WHERE steamid = @SteamId",
                new
                {
                    SteamId = steamId
                });

            return player?.SkillPoints ?? 0; // 如果没有记录，返回默认 0
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while retrieving player skill points for steamId {steamId}: {ex.Message}");
            return 0; // 发生异常时返回默认技能点
        }
    }

    public async Task<string> GetPlayerNamesAsync(ulong steamId)
    {
        if (_connection == null)
        {
            // 处理数据库连接为空的情况
            Console.WriteLine("Database connection is not initialized. Returning default name.");
            return "无"; // 默认返回玩家名称为 "无"
        }

        try
        {
            var player = await _connection.QuerySingleOrDefaultAsync<PlayerData>(
                "SELECT playername FROM PlayerData WHERE steamid = @SteamId",
                new
                {
                    SteamId = steamId
                });

            return player?.PlayerNames ?? "无"; // 如果没有记录，返回默认名称 "无"
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred while retrieving player name for steamId {steamId}: {ex.Message}");
            return "无"; // 发生异常时返回默认名称 "无"
        }
    }

    public async Task SavePlayerHealthAsync(ulong steamId, int HEALTH)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET Health = @HealtH WHERE steamid = @SteamId",
                new
                {
                    HealtH = HEALTH,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Health 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerReHealthAsync(ulong steamId, int REHEALTH)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET ReHealth = @ReHealtH WHERE steamid = @SteamId",
                new
                {
                    ReHealtH = REHEALTH,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 ReHealth 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerDamageAsync(ulong steamId, int DAMAGE)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET Damage = @DamagE WHERE steamid = @SteamId",
                new
                {
                    DamagE = DAMAGE,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Damage 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerHeadShotAsync(ulong steamId, int HEADSHOT)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET HeadShot = @HeadShoT WHERE steamid = @SteamId",
                new
                {
                    HeadShoT = HEADSHOT,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 HeadShot 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerSpeedAsync(ulong steamId, int SPEED)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET Speed = @SpeeD WHERE steamid = @SteamId",
                new
                {
                    SpeeD = SPEED,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Speed 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerGravityAsync(ulong steamId, int GRAVITY)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET Gravity = @GravitY WHERE steamid = @SteamId",
                new
                {
                    GravitY = GRAVITY,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Gravity 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerKnockAsync(ulong steamId, int KONCK)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET Knock = @KnocK WHERE steamid = @SteamId",
                new
                {
                    KnocK = KONCK,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Knock 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerMirrorAsync(ulong steamId, int MIRROR)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET Mirror = @MirroR WHERE steamid = @SteamId",
                new
                {
                    MirroR = MIRROR,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Mirror 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayervampireAsync(ulong steamId, int VAMPIRE)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE SkillData SET vampire = @vampirE WHERE steamid = @SteamId",
                new
                {
                    vampirE = VAMPIRE,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 vampire 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerExperienceAsync(ulong steamId, int experience)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE PlayerData SET Experience = @EXPerience WHERE steamid = @SteamId",
                new
                {
                    EXPerience = experience,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Experience 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerlevelAsync(ulong steamId, int level)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE PlayerData SET Level = @leveL WHERE steamid = @SteamId",
                new
                {
                    leveL = level,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 Level 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayerSkillPointsAsync(ulong steamId, int skillPoints)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE PlayerData SET SkillPoints = @skillPointS WHERE steamid = @SteamId",
                new
                {
                    skillPointS = skillPoints,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 SkillPoints 时发生错误: {ex.Message}");
        }
    }

    public async Task SavePlayernameAsync(ulong steamId, string Name)
    {
        try
        {
            await _connection.ExecuteAsync(
                "UPDATE PlayerData SET playername = @PLAYERNAME WHERE steamid = @SteamId",
                new
                {
                    PLAYERNAME = Name,
                    SteamId = steamId
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 保存 玩家名字 时发生错误: {ex.Message}");
        }
    }

    public async Task<int> GetHealthLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default Health for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT Health FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.Health ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetReHealthLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default ReHealth for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT ReHealth FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.ReHealth ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetDamageLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default Damage for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT Damage FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.Damage ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetHeadShotLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default HeadShot for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT HeadShot FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.HeadShot ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetSpeedLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default Speed for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT Speed FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.Speed ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetGravityLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default Gravity for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT Gravity FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.Gravity ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetKnockLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default Knock for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT Knock FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.Knock ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetMirrorLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default Mirror for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT Mirror FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.Mirror ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task<int> GetvampireLevelFromDbAsync(ulong steamId)
    {
        if (_connection == null)
        {
            Console.WriteLine($"[RPGDatabase] Database connection is not initialized. Returning default Vampire for SteamID {steamId}");
            return 0; // 返回默认值
        }

        var player = await _connection.QuerySingleOrDefaultAsync<SkillData>(
            "SELECT vampire FROM SkillData WHERE steamid = @SteamId",
            new
            {
                SteamId = steamId
            });

        return player?.vampire ?? 0; // 如果没有记录，返回默认 0
    }

    public async Task CreateSkillDataRecordAsync(ulong steamId)
    {
        try
        {
            // 查询时直接使用 ulong 类型的 steamid
            var existingPlayer = await _connection.QuerySingleOrDefaultAsync<SkillData>(
                "SELECT * FROM SkillData WHERE steamid = @SteamId",
                new
                {
                    SteamId = steamId
                });

            if (existingPlayer == null)
            {
                // 插入数据时，使用 ulong 类型的 steamid
                await _connection.ExecuteAsync(
                    "INSERT INTO SkillData (steamid, Health, ReHealth, Damage, HeadShot, Speed, Gravity, Knock, Mirror, vampire) VALUES (@SteamId, @HealtH, @ReHealtH, @DamagE, @HeadShoT, @SpeeD, @GravitY, @KnocK, @MirroR, @vampirE)",
                    new
                    {
                        SteamId = steamId,
                        HealtH = 0,
                        ReHealtH = 0,
                        DamagE = 0,
                        HeadShoT = 0,
                        SpeeD = 0,
                        GravitY = 0,
                        KnocK = 0,
                        MirroR = 0,
                        vampirE = 0
                    }); // 默认 0
                Console.WriteLine($"为新玩家 {steamId} 创建了RPG记录，默认0");

                // 确认数据是否已成功插入
                var newPlayer = await _connection.QuerySingleOrDefaultAsync<SkillData>(
                    "SELECT * FROM SkillData WHERE steamid = @SteamId",
                    new
                    {
                        SteamId = steamId
                    });

                if (newPlayer == null)
                {
                    Console.WriteLine($"[RPGDatabase] 新玩家 {steamId} 的数据未能正确插入。");
                }
                else
                {
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 的数据已成功插入。");
                }
            }
            else
            {
                Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 已存在，跳过创建。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 创建新玩家记录时发生错误: {ex.Message}");
        }
    }

    public async Task CreatePlayerDataRecordAsync(ulong steamId, string Names)
    {
        try
        {
            // 查询时直接使用 ulong 类型的 steamid
            var existingPlayer = await _connection.QuerySingleOrDefaultAsync<PlayerData>(
                "SELECT * FROM PlayerData WHERE steamid = @SteamId",
                new
                {
                    SteamId = steamId
                });

            if (existingPlayer == null)
            {
                // 插入数据时，使用 ulong 类型的 steamid
                await _connection.ExecuteAsync(
                    "INSERT INTO PlayerData (steamid, Level, Experience, SkillPoints, playername) VALUES (@SteamId, @LeveL, @ExperiencE, @SkillPointS, @PLAYERNAME)",
                    new
                    {
                        SteamId = steamId,
                        LeveL = 0,
                        ExperiencE = 0,
                        SkillPointS = 0,
                        PLAYERNAME = Names
                    }); // 默认 0
                Console.WriteLine($"为新玩家 {steamId} 创建了RPG记录，默认0");

                // 确认数据是否已成功插入
                var newPlayer = await _connection.QuerySingleOrDefaultAsync<PlayerData>(
                    "SELECT * FROM PlayerData WHERE steamid = @SteamId",
                    new
                    {
                        SteamId = steamId
                    });

                if (newPlayer == null)
                {
                    Console.WriteLine($"[RPGDatabase] 新玩家 {steamId} 的数据未能正确插入。");
                }
                else
                {
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 的数据已成功插入。");
                }
            }
            else
            {
                if (existingPlayer.PlayerNames != Names)
                {
                    await _connection.ExecuteAsync(
                    "UPDATE PlayerData SET playername = @PlayerName WHERE steamid = @SteamId",
                        new
                        {
                            SteamId = steamId,
                            PlayerName = Names
                        });
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 的名字已更改为 {Names}");
                }
                else
                {
                    Console.WriteLine($"[RPGDatabase] 玩家 {steamId} 已存在，跳过创建。");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RPGDatabase] 创建新玩家记录时发生错误: {ex.Message}");
        }
    }






    #endregion






}
