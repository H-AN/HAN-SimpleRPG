using CounterStrikeSharp.API.Core;
using System.Text.Json;

public class HanSimpleRPGConfig
{
    public int FirstLevelExperience { get; set; }
    public int LevelNeedExperience { get; set; }
    public int LevelGiveSkillPoints { get; set; }
    public int KillGiveExperience { get; set; }
    public float HurtDamageExperiencePercen { get; set; }
    public bool UpgradeExperienceRetention { get; set; }


    public static string ConfigPath = Path.Combine(Application.RootDirectory, "configs/HanSimpleRPG/HanSimpleRPGConfig.json");

    public static HanSimpleRPGConfig Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                // 尝试反序列化配置
                var config = JsonSerializer.Deserialize<HanSimpleRPGConfig>(json);
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
        var defaultConfig = new HanSimpleRPGConfig
        {
            FirstLevelExperience = 250,
            LevelNeedExperience = 150,
            LevelGiveSkillPoints = 1,
            KillGiveExperience = 100,
            HurtDamageExperiencePercen = 0.1f,
            UpgradeExperienceRetention = false
            
        };
        Save(defaultConfig); // 保存默认配置
        return defaultConfig;
    }

    public static void Save(HanSimpleRPGConfig config)
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