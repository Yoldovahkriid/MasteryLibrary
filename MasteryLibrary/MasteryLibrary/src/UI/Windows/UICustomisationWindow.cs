using ImGuiNET;
using MasteryLibrary.src.Config;
using System;
using System.Numerics;
using Vintagestory.API.Client;
using VSImGui.API;

namespace MasteryLibrary.src.UI.Windows
{
    public class UICustomizationWindow
    {
        private readonly ICoreClientAPI capi;
        private bool isOpen = false;

        private UIThemeConfig workingTheme = UIThemeConfig.CreateDefault();

        private int selectedCategory = 0;
        private readonly string[] categories =
        {
            "Backgrounds",
            "Borders",
            "Text",
            "Accent Colors",
            "Skill States",
            "Visual Settings",
            "Presets"
        };

        public bool IsOpen => isOpen;

        public UICustomizationWindow(ICoreClientAPI capi)
        {
            this.capi = capi;
        }

        public void Toggle() { if (isOpen) Close(); else Open(); }

        public void Open()
        {
            isOpen = true;
            workingTheme = CloneTheme(MasteryLibConfigClient.Loaded.Theme);
        }

        public void Close() => isOpen = false;

        public CallbackGUIStatus Draw(float dt)
        {
            if (!isOpen) return CallbackGUIStatus.Closed;

            ImGui.SetNextWindowSize(new Vector2(700f, 600f), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(120f, 120f), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("MasteryLib – UI Customization", ref isOpen, ImGuiWindowFlags.None))
                RenderContent();

            ImGui.End();
            return CallbackGUIStatus.GrabMouse;
        }

        private void RenderContent()
        {
            ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.6f, 1f),
                "Customize your MasteryLib UI appearance");
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginTable("MainLayout", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 150f);
                ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                RenderSidebar();

                ImGui.TableNextColumn();
                RenderCategoryContent();

                ImGui.EndTable();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            RenderActionButtons();
        }

        private void RenderSidebar()
        {
            ImGui.Text("Categories");
            ImGui.Spacing();
            for (int i = 0; i < categories.Length; i++)
                if (ImGui.Selectable(categories[i], selectedCategory == i))
                    selectedCategory = i;
        }

        private void RenderCategoryContent()
        {
            ImGui.BeginChild("CategoryContent", new Vector2(0f, -48f), false);
            switch (selectedCategory)
            {
                case 0: RenderBackgrounds(); break;
                case 1: RenderBorders(); break;
                case 2: RenderText(); break;
                case 3: RenderAccentColors(); break;
                case 4: RenderSkillStates(); break;
                case 5: RenderVisualSettings(); break;
                case 6: RenderPresets(); break;
            }
            ImGui.EndChild();
        }

        private void RenderBackgrounds()
        {
            Header("Background Colors");
            ColorEdit4("Deep Background", workingTheme.BackgroundDeep); Hint("Main window background");
            ColorEdit4("Mid Background", workingTheme.BackgroundMid); Hint("Tabs and sections");
            ColorEdit4("Panel Background", workingTheme.BackgroundPanel); Hint("Content areas");
        }

        private void RenderBorders()
        {
            Header("Border Colors");
            ColorEdit4("Dim Border", workingTheme.BorderDim); Hint("Normal elements");
            ColorEdit4("Bright Border", workingTheme.BorderBright); Hint("Highlights and active elements");
        }

        private void RenderText()
        {
            Header("Text Colors");
            ColorEdit4("Primary Text", workingTheme.TextPrimary); Hint("Main content text");
            ColorEdit4("Muted Text", workingTheme.TextMuted); Hint("Secondary information");
            ColorEdit4("Hint Text", workingTheme.TextHint); Hint("Tooltips and subtle info");
        }

        private void RenderAccentColors()
        {
            Header("Accent Colors");

            ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.4f, 1f), "Gold (Earned / Unlocked)");
            ColorEdit4("Gold", workingTheme.Gold);
            ColorEdit4("Gold Dim", workingTheme.GoldDim);
            ColorEdit4("Gold Deep", workingTheme.GoldDeep);
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.5f, 0.9f, 0.3f, 1f), "Green (Available)");
            ColorEdit4("Green", workingTheme.Green);
            ColorEdit4("Green Dim", workingTheme.GreenDim);
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.2f, 1f), "Red (Locked)");
            ColorEdit4("Red", workingTheme.Red);
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.9f, 1f), "Unique (Cyan / Teal)");
            ColorEdit4("Unique Accent", workingTheme.UniqueAccent);
            ColorEdit4("Unique Accent Dim", workingTheme.UniqueAccentDim);
            ColorEdit4("Unique Accent Deep", workingTheme.UniqueAccentDeep);
        }

        private void RenderSkillStates()
        {
            Header("Skill State Colors");
            ColorEdit4("Locked Skill", workingTheme.SkillLocked); Hint("Skills that cannot be unlocked yet");
            ColorEdit4("Available Skill", workingTheme.SkillAvailable); Hint("Skills ready to unlock");
            ColorEdit4("Unlocked Skill", workingTheme.SkillUnlocked); Hint("Skills already learned");
            ColorEdit4("Unlocked Unique Skill", workingTheme.SkillUnlockedUnique); Hint("Unique skills already learned");
        }

        private void RenderVisualSettings()
        {
            Header("Visual Settings");

            float wr = workingTheme.WindowRounding;
            if (ImGui.SliderFloat("Window Rounding", ref wr, 0f, 12f, "%.1f px"))
                workingTheme.WindowRounding = wr;
            ImGui.Spacing();

            float fr = workingTheme.FrameRounding;
            if (ImGui.SliderFloat("Frame Rounding", ref fr, 0f, 12f, "%.1f px"))
                workingTheme.FrameRounding = fr;
            ImGui.Spacing();

            float us = workingTheme.UIScale;
            if (ImGui.SliderFloat("UI Scale", ref us, 0.8f, 1.5f, "%.2f"))
                workingTheme.UIScale = Math.Clamp(us, 0.8f, 1.5f);
            Hint("Adjusts overall size of UI elements");
            ImGui.Spacing();

            bool blur = workingTheme.EnableBackgroundBlur;
            if (ImGui.Checkbox("Background Blur (if supported)", ref blur))
                workingTheme.EnableBackgroundBlur = blur;
        }

        private void RenderPresets()
        {
            Header("Theme Presets");
            ImGui.Text($"Current preset: {workingTheme.CurrentPreset}");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            foreach (var preset in UIThemeConfig.GetPresetNames())
            {
                if (ImGui.Button(preset, new Vector2(150f, 28f)))
                    workingTheme = CloneTheme(UIThemeConfig.GetPreset(preset));
                ImGui.SameLine();
                ImGui.TextDisabled(GetPresetDescription(preset));
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextWrapped("Selecting a preset replaces your color settings. " +
                              "You can further edit colors after applying.");
        }

        private void RenderActionButtons()
        {
            if (ImGui.Button("Apply", new Vector2(100f, 30f)))
            {
                ApplyTheme();
                capi.ShowChatMessage("[MasteryLib] UI theme applied.");
            }

            ImGui.SameLine();

            if (ImGui.Button("Apply & Close", new Vector2(120f, 30f)))
            {
                ApplyTheme();
                capi.ShowChatMessage("[MasteryLib] UI theme saved.");
                Close();
            }

            ImGui.SameLine();

            if (ImGui.Button("Reset to Default", new Vector2(130f, 30f)))
                workingTheme = CloneTheme(UIThemeConfig.CreateDefault());

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(100f, 30f)))
                Close();

            ImGui.SameLine();
            ImGui.TextDisabled("Changes are not saved until you click Apply");
        }

        private void ApplyTheme()
        {
            MasteryLibConfigClient.Loaded.Theme = workingTheme;
            MasteryLibConfigClient.Save(capi);
        }

        private void Header(string text)
        {
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.4f, 1f), text);
            ImGui.Spacing();
        }

        private static void Hint(string text)
        {
            ImGui.TextDisabled(text);
            ImGui.Spacing();
        }

        private void ColorEdit4(string label, ColorConfig color)
        {
            var vec = color.ToVector4();
            if (ImGui.ColorEdit4(label, ref vec, ImGuiColorEditFlags.AlphaPreviewHalf))
            {
                color.R = vec.X;
                color.G = vec.Y;
                color.B = vec.Z;
                color.A = vec.W;
            }
        }

        private static string GetPresetDescription(string preset) => preset switch
        {
            "Default" => "Medieval/parchment — warm amber tones",
            "Dark" => "High-contrast dark theme",
            "Light" => "Soft light theme",
            "HighContrast" => "Maximum contrast for accessibility",
            "Transparent" => "Semi-transparent minimal UI",
            _ => string.Empty
        };

        private static UIThemeConfig CloneTheme(UIThemeConfig source)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(source);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<UIThemeConfig>(json)!;
        }
    }
}