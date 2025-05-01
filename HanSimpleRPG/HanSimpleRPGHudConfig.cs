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


public class RpgHudConfig
{

    public static string HudConfigPath = Path.Combine(Application.RootDirectory, "configs/HanSimpleRPG/HanRPGHudConfig.json");

    public float HudTextX { get; set; }
    public float HudTextY { get; set; }
    public int Hudsize { get; set; }
    public string HudFontName { get; set; }
    public string HudColor { get; set; }
    public bool HudEnble { get; set; }
    public static RpgHudConfig Load()
    {
        if (File.Exists(HudConfigPath))
        {
            try
            {
                string json = File.ReadAllText(HudConfigPath);
                // 尝试反序列化配置
                var config = JsonSerializer.Deserialize<RpgHudConfig>(json);
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
        var defaultConfig = new RpgHudConfig
        {
            HudTextX = 5f,
            HudTextY = 5f,
            Hudsize = 32,
            HudFontName = "Arial Black",
            HudColor = "#00FF00",
            HudEnble = true
            
        };
        Save(defaultConfig); // 保存默认配置
        return defaultConfig;
    }

    public static void Save(RpgHudConfig config)
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(HudConfigPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[HanSimpleRPGHUD] 文件夹 {directoryPath} 不存在，已创建.");
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(HudConfigPath, json);
            Console.WriteLine("[HanSimpleRPGHUD] 配置文件已保存。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HanSimpleRPGHUD] 无法写入配置文件: {ex.Message}");
            Console.WriteLine($"详细错误：{ex.StackTrace}");
        }
    }

    

  

    


}