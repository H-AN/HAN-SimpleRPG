using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

public class Knockback
{
    public static void KnockbackClient(CCSPlayerController? client, CCSPlayerController? attacker, float knockback) //, string weaponname
    {
        //if(weaponname.Contains("hegrenade"))
        //   return;

        if(client == null || attacker == null)
            return;

        // ent world also trigger hurt event
        if(attacker.DesignerName != "cs_player_controller")
            return;

        // knockback is for zombie only.
        if (attacker.TeamNum == 2)
            return;

        var clientPos = client.PlayerPawn.Value?.AbsOrigin;
        var attackerPos = attacker.PlayerPawn.Value?.AbsOrigin;

        if(clientPos == null || attackerPos == null)
            return;

        var direction = clientPos - attackerPos;
        var normalizedDir = NormalizeVector(direction);

        float weaponknockback = 1.0f;   

        var pushVelocity = normalizedDir * knockback * weaponknockback;//normalizedDir * dmgHealth * knockback * weaponknockback;
        client.PlayerPawn.Value?.AbsVelocity.Add(pushVelocity);
    }


    private static Vector NormalizeVector(Vector vector)
    {
        var x = vector.X;
        var y = vector.Y;
        var z = vector.Z;

        var magnitude = MathF.Sqrt(x * x + y * y + z * z);

        if (magnitude != 0.0)
        {
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
        }

        return new Vector(x, y, z);
    }



}