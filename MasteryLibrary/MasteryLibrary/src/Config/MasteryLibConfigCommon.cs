using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace MasteryLibrary.src.Config
{
    [ProtoContract]
    public class MasteryLibConfigCommon
    {
        private const string CONFIGFILENAME = "masterylibconfigcommon.json";
        public static MasteryLibConfigCommon Loaded = GetDefaultConfig();

        [ProtoMember(1)]
        public int MaxPlayerLevel { get; set; } = 100;

        [ProtoMember(2)]
        public int MasteryPointsPerLevel { get; set; } = 2;

        [ProtoMember(3)]
        public int MaxLearnableMasteries { get; set; } = 2;

        [ProtoMember(4)]
        public int MaxEquippedActiveSkills { get; set; } = 8;

        [ProtoMember(5)]
        public int MasteryPointsCostIncreasePerTier { get; set; } = 1;

        [ProtoMember(6)]
        public int MasteryPointsCostIncreaseIntervall { get; set; } = 10;

        [ProtoMember(7)]
        public float GlobalExperienceMultiplier { get; set; } = 1f;

        [ProtoMember(8, OverwriteList = true)]
        public double[] ExperienceFunctionParameters { get; set; } = new double[] { 0.05, 2, 50, 100, 500 };

        public static MasteryLibConfigCommon GetDefaultConfig()
        {
            return new MasteryLibConfigCommon();
        }

        public static MasteryLibConfigCommon Load(ICoreAPI api)
        {
            try
            {
                var cfg = api.LoadModConfig<MasteryLibConfigCommon>(CONFIGFILENAME);
                if (cfg != null)
                {
                    Loaded = cfg;
                    api.Logger.Notification("[MasteryLib] Common config loaded.");
                }
                else
                {
                    Loaded = new MasteryLibConfigCommon();
                    Save(api);
                    api.Logger.Notification("[MasteryLib] No common config found, created default.");
                }
            }
            catch (System.Exception ex)
            {
                api.Logger.Error($"[MasteryLib] Error loading common config: {ex.Message}");
                Loaded = new MasteryLibConfigCommon();
            }

            return Loaded;
        }

        public static void Save(ICoreAPI api)
        {
            try
            {
                api.StoreModConfig(Loaded, CONFIGFILENAME);
                api.Logger.Notification("[MasteryLib] Common config saved.");
            }
            catch (System.Exception ex)
            {
                api.Logger.Error($"[MasteryLib] Error saving common config: {ex.Message}");
            }
        }
    }
}