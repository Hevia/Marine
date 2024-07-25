using HenryMod.Survivors.Henry.SkillStates;
using MarineMod.Characters.Survivors.Marine.SkillStates;

namespace HenryMod.Survivors.Henry
{
    public static class HenryStates
    {
        public static void Init()
        {
            Modules.Content.AddEntityState(typeof(Rifle));

            Modules.Content.AddEntityState(typeof(Aim));

            Modules.Content.AddEntityState(typeof(Shoot));

            Modules.Content.AddEntityState(typeof(Roll));

            Modules.Content.AddEntityState(typeof(ShoulderBash));

            Modules.Content.AddEntityState(typeof(OrbitalStrike));

            Modules.Content.AddEntityState(typeof(CallAirstrikeBase));
        }
    }
}
