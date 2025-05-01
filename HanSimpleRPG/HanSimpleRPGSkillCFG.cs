using CounterStrikeSharp.API.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;
using System.IO;


public class SkillConfig
{
    public string SkillName { get; set; }
     // 血量上限配置
    public int HealthFirstLevel { get; set; }
    public int HealthEachLevel { get; set; }
    public int EachLevelAddHealth { get; set; }
    public bool HealthEnable { get; set; }
    public int HealthMaxLevel { get; set; }
    // 自我修复配置
    public int ReHealthFirstLevel { get; set; }
    public int ReHealthEachLevel { get; set; }
    public int ReHealthEachLevelHealth { get; set; }
    public float ReEachLevelFirstInterval { get; set; }
    public float ReHealthEachLevelInterval { get; set; }
    public bool ReEnable { get; set; }

    public int ReHealthMaxLevel { get; set; }

    // 伤害增加配置
    public int DamageFirstLevel { get; set; }
    public int DamageEachLevel { get; set; }
    public float DamageEachLevelAdd { get; set; }
    public bool DamageEnable { get; set; }

    public int DamageMaxLevel { get; set; }

    // 爆头伤害配置
    public int HeadShotFirstLevel { get; set; }
    public int HeadShotEachLevel { get; set; }
    public float HeadShotEachLevelAdd { get; set; }
    public bool HeadShotEnable { get; set; }

    public int HeadShotMaxLevel { get; set; }

    // 移动速度配置
    public int SpeedFirstLevel { get; set; }
    public int SpeedEachLevel { get; set; }
    public float SpeedEachLevelAdd { get; set; }
    public bool SpeedEnable { get; set; }

    public int SpeedMaxLevel { get; set; }

    // 重力配置
    public int GravityFirstLevel { get; set; }
    public int GravityEachLevel { get; set; }
    public float GravityEachLevelAdd { get; set; }
    public bool GravityEnable { get; set; }

    public int GravityMaxLevel { get; set; }

    // 击退配置
    public int KnockFirstLevel { get; set; }
    public int KnockEachLevel { get; set; }
    public float KnockEachLevelAdd { get; set; }
    public bool KnockEnable { get; set; }

    public int KnockMaxLevel { get; set; }

    // 反伤甲配置
    public int MirrorFirstLevel { get; set; }
    public int MirrorEachLevel { get; set; }
    public int MirrorEachLevelAdd { get; set; }
    public bool MirrorEnable { get; set; }
    public int MirrorMaxLevel { get; set; }

    // 吸血配置
    public int VampireFirstLevel { get; set; }
    public int VampireEachLevel { get; set; }
    public int VampireEachLevelAdd { get; set; }
    public bool VampireEnable { get; set; }
    public int VampireMaxLevel { get; set; }

}
public sealed class HanSimpleRPGSkillCFG
{
    
    public static string ConfigPath = Path.Combine(Application.RootDirectory, "configs/HanSimpleRPG/HanSimpleRPGSkillCFG.json");

    public List<SkillConfig> Skills { get; set; }

    

    public static HanSimpleRPGSkillCFG Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                // 尝试反序列化配置
                var config = JsonSerializer.Deserialize<HanSimpleRPGSkillCFG>(json);
                if (config != null)
                {
                    return config; // 成功反序列化，返回配置
                }
                else
                {
                    Console.WriteLine("读取配置文件失败，使用默认配置。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取配置文件时发生错误: {ex.Message}，使用默认配置。");
            }
        }
        // 如果配置文件不存在或读取失败，则创建默认配置并保存
        var defaultConfig = new HanSimpleRPGSkillCFG
        {
            Skills = new List<SkillConfig>
            {
                new SkillConfig
                {
                    SkillName = "Health",
                    HealthFirstLevel  = 50,
                    HealthEachLevel  = 50,
                    EachLevelAddHealth  = 20,
                    HealthEnable = true,
                    HealthMaxLevel = 100,
                },
                new SkillConfig
                {
                    SkillName = "ReHealth",
                    ReHealthFirstLevel  = 50,
                    ReHealthEachLevel = 50,
                    ReHealthEachLevelHealth = 1,
                    ReEachLevelFirstInterval = 1.0f,
                    ReHealthEachLevelInterval = 0.1f,
                    ReEnable = true,
                    ReHealthMaxLevel = 100,
                },
                new SkillConfig
                {
                    SkillName = "Damage",
                    DamageFirstLevel  = 120,
                    DamageEachLevel = 100,
                    DamageEachLevelAdd  = 0.03f,
                    DamageEnable  = true,
                    DamageMaxLevel = 100,
                    
                },
                new SkillConfig
                {
                    SkillName = "HeadShot",
                    HeadShotFirstLevel = 130,
                    HeadShotEachLevel  = 110,
                    HeadShotEachLevelAdd  = 0.05f,
                    HeadShotEnable  = true,
                    HeadShotMaxLevel = 100,
                    
                },
                new SkillConfig
                {
                    SkillName = "Speed",
                    SpeedFirstLevel = 140,
                    SpeedEachLevel = 130,
                    SpeedEachLevelAdd = 0.02f,
                    SpeedEnable = true,
                    SpeedMaxLevel = 100,
                    
                },
                new SkillConfig
                {
                    SkillName = "Gravity",
                    GravityFirstLevel = 160,
                    GravityEachLevel = 150,
                    GravityEachLevelAdd = 0.02f,
                    GravityEnable = true,
                    GravityMaxLevel = 100,
                    
                },
                new SkillConfig
                {
                    SkillName = "Knock",
                    KnockFirstLevel = 180,
                    KnockEachLevel = 160,
                    KnockEachLevelAdd = 0.05f,
                    KnockEnable = true,
                    KnockMaxLevel = 100,
                    
                },
                new SkillConfig
                {
                    SkillName = "Mirror",
                    MirrorFirstLevel  = 250,
                    MirrorEachLevel  = 230,
                    MirrorEachLevelAdd  = 1,
                    MirrorEnable  = true,
                    MirrorMaxLevel = 100,
                    
                },
                new SkillConfig
                {
                    SkillName = "Vampire",
                    VampireFirstLevel  = 250,
                    VampireEachLevel = 220,
                    VampireEachLevelAdd = 1,
                    VampireEnable  = true,
                    VampireMaxLevel = 100,
                    
                },

            }
                     
        };
        Save(defaultConfig); // 保存默认配置
        return defaultConfig;
    }

    public static void Save(HanSimpleRPGSkillCFG config)
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanSimpleRPGConfig] 文件夹 {directoryPath} 不存在，已创建.");
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            Console.WriteLine("[HanSimpleRPGConfig] 配置文件已保存。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanSimpleRPGConfig] 无法写入配置文件: {ex.Message}");
            Console.WriteLine($"详细错误：{ex.StackTrace}");
        }
    }

}