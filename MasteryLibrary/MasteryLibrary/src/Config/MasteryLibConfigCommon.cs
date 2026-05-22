using System;
using System.Collections.Generic;
using System.Text;

namespace MasteryLibrary.src.Config
{
    public class MasteryLibConfigCommon
    {
        public static MasteryLibConfigCommon Loaded = GetDefaultConfig();
        public int MaxPlayerLevel { get; set; } = 100;
        public int MasteryPointsPerLevel { get; set; } = 2;
        public int MaxLearnableMasteries { get; set; } = 2;
        public int MaxEquippedActiveSkills { get; set; } = 8;
        public int MasteryPointsCostIncreasePerTier { get; set; } = 1;
        public int MasteryPointsCostIncreaseIntervall { get; set; } = 10;
        public float GlobalExperienceMultiplier { get; set; } = 1f;
        public double[] ExperienceFunctionParameters { get; set; } = new double[] { 0.05, 2, 50, 100, 500 };

        public static MasteryLibConfigCommon GetDefaultConfig()
        {
            return new MasteryLibConfigCommon();
        }
    }
}
