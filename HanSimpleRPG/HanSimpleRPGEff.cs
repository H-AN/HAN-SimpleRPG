using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

using System.Drawing;

namespace HanSimpleRPG;

public class Eff
{
    private readonly BasePlugin plugin;

    public Eff(BasePlugin plugin)
    {
        this.plugin = plugin;
    }

    public void CreateBeamBetweenPoints(Vector start, Vector end)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam");

        if (beam == null)
            return;

        beam.Render = Color.FromArgb(255, 0, 255, 0); // 不透明绿色
        beam.Width = 1.5f;
        beam.EndWidth = 1.5f;

        beam.Teleport(start, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;
        beam.DispatchSpawn();

        plugin.AddTimer(0.2f, () =>
        {
            if (beam.IsValid)
                beam.AcceptInput("Kill");
        });
    }
}
