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

public class RpgWasdMenu
{

    public CPointWorldText? RpgworldText { get; private set; }
    public static CHandle<CCSGOViewModel> GetOrCreateViewModels(CCSPlayerPawn playerPawn)
    {
        var handle = new CHandle<CCSGOViewModel>((IntPtr)(playerPawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
        if (!handle.IsValid)
        {
            CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
            viewmodel.DispatchSpawn();
            handle.Raw = viewmodel.EntityHandle.Raw;
            Utilities.SetStateChanged(playerPawn, "CCSPlayerPawnBase", "m_pViewModelServices");
        }
        return handle;
    }

    public static CPointWorldText CreateText(CCSPlayerController client, CHandle<CCSGOViewModel> handle, string Text, float shiftX = 0f, float shiftY = 0f,int size = 128,string FontName = "",Color? color = null)
    {
        var playerPawn = client.PlayerPawn.Value;
        CPointWorldText RpgworldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;

        RpgworldText.Entity!.Name = "Rpgsmenu";
        RpgworldText.OwnerEntity.Raw = client.Pawn.Raw;
        Schema.SetSchemaValue(RpgworldText.Handle, "CBaseEntity", "m_hOwnerEntity", client.Pawn.Raw);
        Marshal.WriteInt64(RpgworldText.Handle, 1392, 2417); // m_pfnTouch = 2417 (BounceTouch)

        RpgworldText.MessageText = Text;
        RpgworldText.Enabled = true;
        RpgworldText.FontSize = size;
        RpgworldText.Fullbright = true;
        RpgworldText.Color = color ?? Color.Aquamarine;
        RpgworldText.DrawBackground = true;
        RpgworldText.BackgroundBorderHeight = 0.0f;
        RpgworldText.BackgroundBorderWidth = 0.2f;
        RpgworldText.BackgroundMaterialName = "";
        RpgworldText.WorldUnitsPerPx = 0.01f;
        RpgworldText.FontName = FontName;
        RpgworldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
        RpgworldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
        RpgworldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;
        //RpgworldText.DepthOffset = Config.Offset_Z;
        QAngle eyeAngles = playerPawn.EyeAngles;
        Vector forward = new Vector(), right = new Vector() ,up = new Vector();
        NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);
        //Vector offset = CalculateHudOffset(forward, right, up);
        Vector offset = new();
        offset += forward * 7;
        offset += right * shiftX;
        offset += up * shiftY;

        QAngle angles = new QAngle()
        {
            Y = eyeAngles.Y + 270,
            Z = 90 - eyeAngles.X,
            X = 0
        };
        RpgworldText.DispatchSpawn();
        RpgworldText.Teleport(playerPawn.AbsOrigin! + offset + new Vector(0, 0, playerPawn.ViewOffset.Z), angles, null);
        RpgworldText.AcceptInput("ClearParent");
        RpgworldText.AcceptInput("SetParent", handle.Value, null, "!activator");
        
        
        return RpgworldText;
        
    }

    

  

    


}