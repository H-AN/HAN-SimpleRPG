using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;
using System.Drawing;
using CounterStrikeSharp.API.Core.Capabilities;
using System;
using System.Data.SqlTypes;
using System.IO;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HanSimpleRPG;


public static class TakeDamageTest
{

    //private static HanSimpleRPGPlugin pluginInstance = new HanSimpleRPGPlugin();

    static public void setammo(this CBasePlayerWeapon? weapon, int clip, int reserve)
    {
        if(weapon == null)
        {
            return;
        }
        //CCSPlayerController? client = Utilities.GetPlayerFromSlot((int)weapon.OriginalOwnerXuidLow);

        weapon.Clip1 = clip;
        weapon.ReserveAmmo[0] = reserve;
        
        //weapon.AcceptInput("SetReserveAmmoAmount", null,null, $"{reserve}");
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        //Utilities.SetStateChanged(entity, "CBasePlayerWeapon", "m_iClip2");
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
    }
    public static void TakeDamage(this CCSPlayerController client, float damage, CCSPlayerController attacker) 
    {
        var size = Schema.GetClassSize("CTakeDamageInfo");
        var ptr = Marshal.AllocHGlobal(size);

        for (var i = 0; i < size; i++)
            Marshal.WriteByte(ptr, i, 0);

        var damageInfo = new CTakeDamageInfo(ptr);
        var attackerInfo = new CAttackerInfo(attacker);

        Marshal.StructureToPtr(attackerInfo, new IntPtr(ptr.ToInt64() + 0x80), false);

        Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hInflictor", attacker.Pawn.Raw);
        Schema.SetSchemaValue(damageInfo.Handle, "CTakeDamageInfo", "m_hAttacker", attacker.Pawn.Raw);

        damageInfo.Damage = damage;

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Invoke(client.Pawn.Value!, damageInfo);
        Marshal.FreeHGlobal(ptr);

    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CAttackerInfo
    {
        public CAttackerInfo(CEntityInstance attacker)
        {
            NeedInit = false;
            IsWorld = true;
            Attacker = attacker.EntityHandle.Raw;
            if (attacker.DesignerName != "cs_player_controller") return;

            var controller = attacker.As<CCSPlayerController>();
            IsWorld = false;
            IsPawn = true;
            AttackerUserId = (ushort)(controller.UserId ?? 0xFFFF);
            TeamNum = controller.TeamNum;
            TeamChecked = controller.TeamNum;
        }

        [FieldOffset(0x0)] public bool NeedInit = true;
        [FieldOffset(0x1)] public bool IsPawn = false;
        [FieldOffset(0x2)] public bool IsWorld = false;

        [FieldOffset(0x4)]
        public UInt32 Attacker;

        [FieldOffset(0x8)]
        public ushort AttackerUserId;

        [FieldOffset(0x0C)] public int TeamChecked = -1;
        [FieldOffset(0x10)] public int TeamNum = -1;
    }

    

    


    

}
