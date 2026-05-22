using System.Collections.Generic;
using System.Numerics;
using Vintagestory.API.Client;

namespace MasteryLibrary.src.Config
{
    public class ColorConfig
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public ColorConfig() { }
        public ColorConfig(float r, float g, float b, float a) { R = r; G = g; B = b; A = a; }

        public Vector4 ToVector4() => new Vector4(R, G, B, A);

        public static ColorConfig FromVector4(Vector4 v) => new ColorConfig(v.X, v.Y, v.Z, v.W);

        public ColorConfig Clone() => new ColorConfig(R, G, B, A);
    }

    public class UIThemeConfig
    {
        public string CurrentPreset { get; set; } = "Default";

        public ColorConfig BackgroundDeep { get; set; } = new ColorConfig(0.08f, 0.06f, 0.04f, 0.97f);
        public ColorConfig BackgroundMid { get; set; } = new ColorConfig(0.13f, 0.10f, 0.07f, 0.97f);
        public ColorConfig BackgroundPanel { get; set; } = new ColorConfig(0.10f, 0.08f, 0.05f, 0.95f);

        public ColorConfig BorderDim { get; set; } = new ColorConfig(0.35f, 0.26f, 0.12f, 0.60f);
        public ColorConfig BorderBright { get; set; } = new ColorConfig(0.72f, 0.56f, 0.22f, 0.90f);

        public ColorConfig TextPrimary { get; set; } = new ColorConfig(0.90f, 0.85f, 0.72f, 1.00f);
        public ColorConfig TextMuted { get; set; } = new ColorConfig(0.60f, 0.54f, 0.42f, 1.00f);
        public ColorConfig TextHint { get; set; } = new ColorConfig(0.40f, 0.36f, 0.28f, 1.00f);

        public ColorConfig Gold { get; set; } = new ColorConfig(0.91f, 0.79f, 0.37f, 1.00f);
        public ColorConfig GoldDim { get; set; } = new ColorConfig(0.65f, 0.50f, 0.18f, 1.00f);
        public ColorConfig GoldDeep { get; set; } = new ColorConfig(0.28f, 0.20f, 0.06f, 1.00f);

        public ColorConfig Green { get; set; } = new ColorConfig(0.44f, 0.82f, 0.30f, 1.00f);
        public ColorConfig GreenDim { get; set; } = new ColorConfig(0.24f, 0.48f, 0.16f, 1.00f);
        public ColorConfig Red { get; set; } = new ColorConfig(0.85f, 0.28f, 0.22f, 1.00f);

        public ColorConfig UniqueAccent { get; set; } = new ColorConfig(0.30f, 0.85f, 0.90f, 1.00f);
        public ColorConfig UniqueAccentDim { get; set; } = new ColorConfig(0.18f, 0.55f, 0.60f, 1.00f);
        public ColorConfig UniqueAccentDeep { get; set; } = new ColorConfig(0.06f, 0.22f, 0.25f, 1.00f);

        public ColorConfig SkillLocked { get; set; } = new ColorConfig(0.07f, 0.05f, 0.03f, 1.00f);
        public ColorConfig SkillAvailable { get; set; } = new ColorConfig(0.18f, 0.14f, 0.06f, 1.00f);
        public ColorConfig SkillUnlocked { get; set; } = new ColorConfig(0.22f, 0.17f, 0.06f, 1.00f);
        public ColorConfig SkillUnlockedUnique { get; set; } = new ColorConfig(0.06f, 0.18f, 0.22f, 1.00f);

        public float WindowRounding { get; set; } = 4f;
        public float FrameRounding { get; set; } = 3f;
        public float UIScale { get; set; } = 1.0f;
        public bool EnableBackgroundBlur { get; set; } = false;

        public static UIThemeConfig CreateDefault() => new UIThemeConfig { CurrentPreset = "Default" };

        public static List<string> GetPresetNames() =>
            new List<string> { "Default", "Dark", "Light", "HighContrast", "Transparent" };

        public static UIThemeConfig GetPreset(string name)
        {
            return name switch
            {
                "Dark" => new UIThemeConfig
                {
                    CurrentPreset = "Dark",
                    BackgroundDeep = new ColorConfig(0.04f, 0.04f, 0.04f, 0.98f),
                    BackgroundMid = new ColorConfig(0.08f, 0.08f, 0.08f, 0.98f),
                    BackgroundPanel = new ColorConfig(0.06f, 0.06f, 0.06f, 0.96f),
                    BorderDim = new ColorConfig(0.25f, 0.25f, 0.25f, 0.60f),
                    BorderBright = new ColorConfig(0.60f, 0.60f, 0.60f, 0.90f),
                    TextPrimary = new ColorConfig(0.92f, 0.92f, 0.92f, 1.00f),
                    TextMuted = new ColorConfig(0.58f, 0.58f, 0.58f, 1.00f),
                    TextHint = new ColorConfig(0.36f, 0.36f, 0.36f, 1.00f),
                    Gold = new ColorConfig(0.95f, 0.82f, 0.40f, 1.00f),
                    GoldDim = new ColorConfig(0.60f, 0.48f, 0.20f, 1.00f),
                    GoldDeep = new ColorConfig(0.22f, 0.16f, 0.04f, 1.00f),
                    Green = new ColorConfig(0.40f, 0.85f, 0.30f, 1.00f),
                    GreenDim = new ColorConfig(0.22f, 0.50f, 0.16f, 1.00f),
                    Red = new ColorConfig(0.88f, 0.30f, 0.24f, 1.00f),
                    UniqueAccent = new ColorConfig(0.28f, 0.88f, 0.92f, 1.00f),
                    UniqueAccentDim = new ColorConfig(0.16f, 0.52f, 0.58f, 1.00f),
                    UniqueAccentDeep = new ColorConfig(0.05f, 0.20f, 0.24f, 1.00f),
                    SkillLocked = new ColorConfig(0.06f, 0.06f, 0.06f, 1.00f),
                    SkillAvailable = new ColorConfig(0.14f, 0.14f, 0.06f, 1.00f),
                    SkillUnlocked = new ColorConfig(0.18f, 0.15f, 0.05f, 1.00f),
                    SkillUnlockedUnique = new ColorConfig(0.05f, 0.16f, 0.20f, 1.00f),
                },
                "Light" => new UIThemeConfig
                {
                    CurrentPreset = "Light",
                    BackgroundDeep = new ColorConfig(0.88f, 0.84f, 0.76f, 0.97f),
                    BackgroundMid = new ColorConfig(0.80f, 0.76f, 0.68f, 0.97f),
                    BackgroundPanel = new ColorConfig(0.84f, 0.80f, 0.72f, 0.95f),
                    BorderDim = new ColorConfig(0.55f, 0.48f, 0.36f, 0.60f),
                    BorderBright = new ColorConfig(0.40f, 0.30f, 0.12f, 0.90f),
                    TextPrimary = new ColorConfig(0.14f, 0.10f, 0.06f, 1.00f),
                    TextMuted = new ColorConfig(0.36f, 0.30f, 0.20f, 1.00f),
                    TextHint = new ColorConfig(0.52f, 0.46f, 0.34f, 1.00f),
                    Gold = new ColorConfig(0.60f, 0.42f, 0.06f, 1.00f),
                    GoldDim = new ColorConfig(0.76f, 0.60f, 0.22f, 1.00f),
                    GoldDeep = new ColorConfig(0.88f, 0.80f, 0.60f, 1.00f),
                    Green = new ColorConfig(0.20f, 0.58f, 0.12f, 1.00f),
                    GreenDim = new ColorConfig(0.30f, 0.70f, 0.20f, 1.00f),
                    Red = new ColorConfig(0.75f, 0.18f, 0.12f, 1.00f),
                    UniqueAccent = new ColorConfig(0.08f, 0.55f, 0.65f, 1.00f),
                    UniqueAccentDim = new ColorConfig(0.14f, 0.68f, 0.78f, 1.00f),
                    UniqueAccentDeep = new ColorConfig(0.72f, 0.88f, 0.92f, 1.00f),
                    SkillLocked = new ColorConfig(0.72f, 0.68f, 0.60f, 1.00f),
                    SkillAvailable = new ColorConfig(0.80f, 0.78f, 0.68f, 1.00f),
                    SkillUnlocked = new ColorConfig(0.82f, 0.78f, 0.58f, 1.00f),
                    SkillUnlockedUnique = new ColorConfig(0.68f, 0.84f, 0.88f, 1.00f),
                },
                "HighContrast" => new UIThemeConfig
                {
                    CurrentPreset = "HighContrast",
                    BackgroundDeep = new ColorConfig(0.00f, 0.00f, 0.00f, 1.00f),
                    BackgroundMid = new ColorConfig(0.10f, 0.10f, 0.10f, 1.00f),
                    BackgroundPanel = new ColorConfig(0.05f, 0.05f, 0.05f, 1.00f),
                    BorderDim = new ColorConfig(0.50f, 0.50f, 0.50f, 1.00f),
                    BorderBright = new ColorConfig(1.00f, 1.00f, 1.00f, 1.00f),
                    TextPrimary = new ColorConfig(1.00f, 1.00f, 1.00f, 1.00f),
                    TextMuted = new ColorConfig(0.80f, 0.80f, 0.80f, 1.00f),
                    TextHint = new ColorConfig(0.60f, 0.60f, 0.60f, 1.00f),
                    Gold = new ColorConfig(1.00f, 0.90f, 0.00f, 1.00f),
                    GoldDim = new ColorConfig(0.70f, 0.62f, 0.00f, 1.00f),
                    GoldDeep = new ColorConfig(0.30f, 0.26f, 0.00f, 1.00f),
                    Green = new ColorConfig(0.00f, 1.00f, 0.40f, 1.00f),
                    GreenDim = new ColorConfig(0.00f, 0.60f, 0.24f, 1.00f),
                    Red = new ColorConfig(1.00f, 0.18f, 0.12f, 1.00f),
                    UniqueAccent = new ColorConfig(0.00f, 1.00f, 1.00f, 1.00f),
                    UniqueAccentDim = new ColorConfig(0.00f, 0.60f, 0.60f, 1.00f),
                    UniqueAccentDeep = new ColorConfig(0.00f, 0.22f, 0.22f, 1.00f),
                    SkillLocked = new ColorConfig(0.08f, 0.08f, 0.08f, 1.00f),
                    SkillAvailable = new ColorConfig(0.18f, 0.18f, 0.00f, 1.00f),
                    SkillUnlocked = new ColorConfig(0.22f, 0.18f, 0.00f, 1.00f),
                    SkillUnlockedUnique = new ColorConfig(0.00f, 0.18f, 0.22f, 1.00f),
                },
                "Transparent" => new UIThemeConfig
                {
                    CurrentPreset = "Transparent",
                    BackgroundDeep = new ColorConfig(0.04f, 0.03f, 0.02f, 0.55f),
                    BackgroundMid = new ColorConfig(0.08f, 0.06f, 0.04f, 0.55f),
                    BackgroundPanel = new ColorConfig(0.06f, 0.04f, 0.03f, 0.50f),
                    BorderDim = new ColorConfig(0.40f, 0.30f, 0.14f, 0.40f),
                    BorderBright = new ColorConfig(0.72f, 0.56f, 0.22f, 0.70f),
                    TextPrimary = new ColorConfig(0.95f, 0.90f, 0.78f, 1.00f),
                    TextMuted = new ColorConfig(0.65f, 0.58f, 0.44f, 1.00f),
                    TextHint = new ColorConfig(0.45f, 0.40f, 0.30f, 1.00f),
                    Gold = new ColorConfig(0.91f, 0.79f, 0.37f, 1.00f),
                    GoldDim = new ColorConfig(0.65f, 0.50f, 0.18f, 1.00f),
                    GoldDeep = new ColorConfig(0.28f, 0.20f, 0.06f, 0.70f),
                    Green = new ColorConfig(0.44f, 0.82f, 0.30f, 1.00f),
                    GreenDim = new ColorConfig(0.24f, 0.48f, 0.16f, 1.00f),
                    Red = new ColorConfig(0.85f, 0.28f, 0.22f, 1.00f),
                    UniqueAccent = new ColorConfig(0.30f, 0.85f, 0.90f, 1.00f),
                    UniqueAccentDim = new ColorConfig(0.18f, 0.55f, 0.60f, 1.00f),
                    UniqueAccentDeep = new ColorConfig(0.06f, 0.22f, 0.25f, 0.60f),
                    SkillLocked = new ColorConfig(0.07f, 0.05f, 0.03f, 0.70f),
                    SkillAvailable = new ColorConfig(0.18f, 0.14f, 0.06f, 0.70f),
                    SkillUnlocked = new ColorConfig(0.22f, 0.17f, 0.06f, 0.70f),
                    SkillUnlockedUnique = new ColorConfig(0.06f, 0.18f, 0.22f, 0.70f),
                },
                _ => CreateDefault(),
            };
        }
    }

    public class MasteryLibConfigClient
    {
        private const string CONFIG_FILE = "masterylib_client.json";
        public static MasteryLibConfigClient Loaded { get; private set; } = new MasteryLibConfigClient();

        public UIThemeConfig Theme { get; set; } = UIThemeConfig.CreateDefault();

        public static MasteryLibConfigClient Load(ICoreClientAPI api)
        {
            try
            {
                var cfg = api.LoadModConfig<MasteryLibConfigClient>(CONFIG_FILE);
                if (cfg != null)
                {
                    Loaded = cfg;
                    api.Logger.Notification("[MasteryLib] Client config loaded.");
                }
                else
                {
                    Loaded = new MasteryLibConfigClient();
                    Save(api);
                    api.Logger.Notification("[MasteryLib] No client config found, created default.");
                }
            }
            catch (System.Exception ex)
            {
                api.Logger.Error($"[MasteryLib] Error loading client config: {ex.Message}");
                Loaded = new MasteryLibConfigClient();
            }

            return Loaded;
        }

        public static void Save(ICoreClientAPI api)
        {
            try
            {
                api.StoreModConfig(Loaded, CONFIG_FILE);
                api.Logger.Notification("[MasteryLib] Client config saved.");
            }
            catch (System.Exception ex)
            {
                api.Logger.Error($"[MasteryLib] Error saving client config: {ex.Message}");
            }
        }
    }
}