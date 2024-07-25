using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarineMod.Characters.Survivors.Marine.SkillStates
{
    public static class SkillStore
    {
        public static SkillDef rifleSkill;
        public static SkillDef bashSkill;

        public static void updateRifleRef(SkillDef newSkill)
        {
            rifleSkill = newSkill;
        }

        public static void updateBashRef(SkillDef newSkill)
        {
            bashSkill = newSkill;
        }
    }
}
