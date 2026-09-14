using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Core.Masteries.Instances;
using MasteryLibrary.src.Config;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using VSImGui.API;
using MasteryLibrary.src.Networking.Client;

namespace MasteryLibrary.src.UI.Components
{
    public class AbilityHotbarGUI
    {
        private readonly ICoreClientAPI capi;
        private readonly NetworkServiceClient networkService;

        private readonly Dictionary<string, int> textureCache = new Dictionary<string, int>();

        private bool isVisible = true;

        private float currentOpacity = 1.0f;

        private long lastInteractionMs = 0;

        private const float FADE_DELAY_SECONDS = 30f;
        private const float FADE_DURATION_SECONDS = 1.5f;
        private const float MIN_OPACITY = 0.2f;

        public AbilityHotbarGUI(ICoreClientAPI capi, NetworkServiceClient networkService)
        {
            this.capi = capi;
            this.networkService = networkService;
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
            if (isVisible)
            {
                currentOpacity = 1.0f;
                lastInteractionMs = capi.World.ElapsedMilliseconds;
            }
        }

        public void Dispose()
        {
            foreach (int texId in textureCache.Values)
                capi.Render.GLDeleteTexture(texId);
            textureCache.Clear();
        }

        public CallbackGUIStatus Draw(float deltaSeconds)
        {
            if (!isVisible) return CallbackGUIStatus.DontGrabMouse;

            var behavior = capi.World.Player?.Entity?.GetBehavior<EntityBehaviorPlayerMasteries>();
            if (behavior == null) return CallbackGUIStatus.DontGrabMouse;

            DrawActiveSkillHotbar(behavior.PlayerMasteryData);
            return CallbackGUIStatus.DontGrabMouse;
        }

        private void MarkInteraction()
        {
            lastInteractionMs = capi.World.ElapsedMilliseconds;
            currentOpacity = 1.0f;
        }

        private bool UpdateFadeAnimation(PlayerMasteryData data, long currentMs)
        {
            bool hasCooldown = false;
            foreach (string skillCode in data.EquippedActiveSkills)
            {
                if (string.IsNullOrEmpty(skillCode)) continue;
                var sInst = data.GetSkillInstance(skillCode);
                if (sInst == null) continue;

                if (sInst.IsOnCooldown())
                {
                    hasCooldown = true;
                    break;
                }
            }

            if (hasCooldown)
            {
                currentOpacity = 1.0f;
                lastInteractionMs = currentMs;
                return true;
            }

            float timeSinceInteraction = (currentMs - lastInteractionMs) / 1000f;
            if (timeSinceInteraction > FADE_DELAY_SECONDS)
            {
                float fadeProgress = (timeSinceInteraction - FADE_DELAY_SECONDS) / FADE_DURATION_SECONDS;
                fadeProgress = Math.Clamp(fadeProgress, 0f, 1f);
                fadeProgress = 1f - (float)Math.Pow(1f - fadeProgress, 3); // cubic ease-out
                currentOpacity = 1.0f - fadeProgress * (1.0f - MIN_OPACITY);
            }
            else
            {
                currentOpacity = 1.0f;
            }

            return false;
        }

        private void DrawActiveSkillHotbar(PlayerMasteryData data)
        {
            if (data?.EquippedActiveSkills == null) return;

            long currentMs = capi.World.ElapsedMilliseconds;
            UpdateFadeAnimation(data, currentMs);

            // Theme colors (read fresh each frame)
            var theme = MasteryLibConfigClient.Loaded.Theme;
            Vector4 colBgDeep = ApplyOpacity(theme.BackgroundDeep.ToVector4());
            Vector4 colBgPanel = ApplyOpacity(theme.BackgroundPanel.ToVector4());
            Vector4 colBorderDim = ApplyOpacity(theme.BorderDim.ToVector4());
            Vector4 colBorderBright = ApplyOpacity(theme.BorderBright.ToVector4());
            Vector4 colTextPrimary = ApplyOpacity(theme.TextPrimary.ToVector4());
            Vector4 colTextMuted = ApplyOpacity(theme.TextMuted.ToVector4());
            Vector4 colTextHint = ApplyOpacity(theme.TextHint.ToVector4());
            Vector4 colGold = ApplyOpacity(theme.Gold.ToVector4());
            Vector4 colGoldDim = ApplyOpacity(theme.GoldDim.ToVector4());
            Vector4 colGreen = ApplyOpacity(theme.Green.ToVector4());
            Vector4 colCooldownSweep = ApplyOpacity(new Vector4(0f, 0f, 0f, 0.80f));

            // Window setup
            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoBackground;

            Size2i screenSize = capi.Forms.GetScreenSize();
            float uiscale = ClientSettings.GUIScale;
            float iconSize = 52f * uiscale;
            float slotPad = 6f * uiscale;
            float slotSize = iconSize + slotPad * 2f;
            float gap = 8f * uiscale;
            float outerPad = 12f * uiscale;

            int slotCount = MasteryLibConfigCommon.Loaded.MaxEquippedActiveSkills;
            float totalW = slotCount * slotSize + (slotCount - 1) * gap + outerPad * 2f;
            float totalH = slotSize + outerPad * 2f;

            ImGui.SetNextWindowPos(
                new Vector2(screenSize.Width / 2f, screenSize.Height - 180f * uiscale),
                ImGuiCond.Always,
                new Vector2(0.5f, 1.0f));

            if (!ImGui.Begin("ActiveSkillHUD##hud", flags))
            {
                ImGui.End();
                return;
            }

            var drawList = ImGui.GetWindowDrawList();
            Vector2 winPos = ImGui.GetWindowPos();

            // Background panel
            Vector2 panelMin = winPos;
            Vector2 panelMax = winPos + new Vector2(totalW, totalH);

            drawList.AddRectFilled(panelMin, panelMax,
                ImGui.ColorConvertFloat4ToU32(colBgDeep), 4f);
            drawList.AddRect(panelMin, panelMax,
                ImGui.ColorConvertFloat4ToU32(colBorderDim), 4f, ImDrawFlags.None, 1f);
            DrawCornerBrackets(drawList, panelMin, panelMax, colGoldDim, 10f);

            // Slots
            for (int i = 0; i < slotCount; i++)
            {
                float slotX = winPos.X + outerPad + i * (slotSize + gap);
                float slotY = winPos.Y + outerPad;
                Vector2 slotMin = new Vector2(slotX, slotY);
                Vector2 slotMax = slotMin + new Vector2(slotSize, slotSize);

                string? skillCode = i < data.EquippedActiveSkills.Length
                    ? data.EquippedActiveSkills[i]
                    : null;

                // Empty slot
                if (string.IsNullOrEmpty(skillCode))
                {
                    drawList.AddRectFilled(slotMin, slotMax,
                        ImGui.ColorConvertFloat4ToU32(colBgPanel), 2f);
                    drawList.AddRect(slotMin, slotMax,
                        ImGui.ColorConvertFloat4ToU32(colBorderDim), 2f);

                    drawList.AddText(slotMin + new Vector2(4f, 4f),
                        ImGui.ColorConvertFloat4ToU32(colTextMuted), GetHotkeyLabel(i));

                    string dash = "-";
                    Vector2 dashSz = ImGui.CalcTextSize(dash);
                    drawList.AddText(
                        slotMin + (new Vector2(slotSize, slotSize) - dashSz) * 0.5f,
                        ImGui.ColorConvertFloat4ToU32(colTextHint), dash);

                    ImGui.SetCursorScreenPos(slotMin);
                    if (ImGui.InvisibleButton($"##emptyslot_{i}", new Vector2(slotSize, slotSize)))
                    {
                        // Unused may open the EquipSkill Tab in the future
                    }

                    if (ImGui.IsItemHovered()) MarkInteraction();

                    if (i < slotCount - 1) ImGui.SameLine(0f, gap);
                    continue;
                }

                var sInst = data.GetSkillInstance(skillCode);
                if (sInst?.Skill == null)
                {
                    ImGui.SetCursorScreenPos(slotMin);
                    ImGui.InvisibleButton($"##nullslot_{i}", new Vector2(slotSize, slotSize));
                    if (i < slotCount - 1) ImGui.SameLine(0f, gap);
                    continue;
                }

                // Populated slot

                long cooldownMs = (long)(sInst.Skill.Cooldown * 1000f);
                long remainingMs = sInst.GetRemainingCooldownMs();
                long timeSinceUsed = cooldownMs - remainingMs;
                bool isOnCooldown = sInst.IsOnCooldown();

                Vector4 slotBgColor = isOnCooldown
                    ? new Vector4(colBgPanel.X * 0.7f, colBgPanel.Y * 0.7f, colBgPanel.Z * 0.7f, colBgPanel.W)
                    : colBgPanel;

                drawList.AddRectFilled(slotMin, slotMax,
                    ImGui.ColorConvertFloat4ToU32(slotBgColor), 2f);
                drawList.AddRect(slotMin, slotMax,
                    ImGui.ColorConvertFloat4ToU32(colBorderDim), 2f);

                // Hotkey label
                drawList.AddText(slotMin + new Vector2(4f, 4f),
                    ImGui.ColorConvertFloat4ToU32(colTextMuted), GetHotkeyLabel(i));

                // Icon
                Vector2 iconPos = slotMin + new Vector2(slotPad, slotPad);
                ImGui.SetCursorScreenPos(iconPos);

                int texId = GetOrLoadTexture(sInst.Skill.IconPath);
                if (texId != 0)
                {
                    ImGui.Image((IntPtr)texId, new Vector2(iconSize, iconSize));
                }
                else
                {
                    // Text fallback when no icon is available
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0)));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.08f)));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.16f)));
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(colTextMuted));
                    ImGui.Button(sInst.Skill.GetDisplayName(sInst.Level), new Vector2(iconSize, iconSize));
                    ImGui.PopStyleColor(4);
                }

                // Invisible button overlay for click-to-use
                ImGui.SetCursorScreenPos(slotMin);
                bool clicked = ImGui.InvisibleButton($"##skillslot_{i}", new Vector2(slotSize, slotSize));
                bool hovered = ImGui.IsItemHovered();

                if (hovered)
                {
                    MarkInteraction();
                    drawList.AddRect(slotMin + new Vector2(1f, 1f), slotMax - new Vector2(1f, 1f),
                        ImGui.ColorConvertFloat4ToU32(colBorderBright), 2f, ImDrawFlags.None, 2f);
                }

                if (clicked)
                {
                    MarkInteraction();
                    ExecuteSkillSlot(i);
                }

                // Cooldown sweep overlay
                if (isOnCooldown)
                {
                    float ratioRemaining = 1f - (float)timeSinceUsed / cooldownMs;
                    Vector2 center = iconPos + new Vector2(iconSize * 0.5f, iconSize * 0.5f);
                    float radius = iconSize * 0.5f;
                    float startAngle = -(float)Math.PI / 2f;
                    float endAngle = startAngle + ratioRemaining * (float)Math.PI * 2f;

                    drawList.PathLineTo(center);
                    drawList.PathArcTo(center, radius, startAngle, endAngle, 40);
                    drawList.PathFillConvex(ImGui.ColorConvertFloat4ToU32(colCooldownSweep));

                    drawList.PathArcTo(center, radius - 1f, startAngle, endAngle, 40);
                    drawList.PathStroke(ImGui.ColorConvertFloat4ToU32(colGold), ImDrawFlags.None, 1.5f);

                    // Countdown text centred on the icon
                    string cdText = GetRemainingCooldownText(cooldownMs, timeSinceUsed);
                    Vector2 textSz = ImGui.CalcTextSize(cdText);
                    Vector2 textPos = center - textSz * 0.5f;
                    drawList.AddText(textPos + new Vector2(1f, 1f),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 1f)), cdText);
                    drawList.AddText(textPos,
                        ImGui.ColorConvertFloat4ToU32(colGold), cdText);
                }
                else if (cooldownMs > 0)
                {
                    drawList.AddRect(slotMin + new Vector2(2f, 2f), slotMax - new Vector2(2f, 2f),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(colGold.X, colGold.Y, colGold.Z, 0.30f)),
                        2f, ImDrawFlags.None, 1f);
                }

                // Tooltip
                if (hovered)
                {
                    ImGui.PushStyleColor(ImGuiCol.PopupBg, ImGui.ColorConvertFloat4ToU32(colBgPanel));
                    ImGui.PushStyleColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(colBorderDim));
                    ImGui.BeginTooltip();

                    ImGui.TextColored(colGold, sInst.Skill.GetDisplayName(sInst.Level));
                    ImGui.TextColored(colTextPrimary, sInst.Skill.GetDescription(sInst.Level));
                    ImGui.Spacing();
                    ImGui.TextColored(colTextMuted, Lang.Get("masterylibrary:hotbar-tooltip-cooldown", sInst.Skill.Cooldown));

                    if (sInst.Skill.Duration > 0)
                        ImGui.TextColored(colGreen, Lang.Get("masterylibrary:hotbar-tooltip-duration", sInst.Skill.Duration));

                    ImGui.Spacing();
                    ImGui.TextColored(colTextHint, Lang.Get("masterylibrary:hotbar-tooltip-click-hint"));

                    ImGui.EndTooltip();
                    ImGui.PopStyleColor(2);
                }

                if (i < slotCount - 1) ImGui.SameLine(0f, gap);
            }

            // Reserve window space so ImGui's auto-resize covers the full panel
            ImGui.SetCursorScreenPos(winPos);
            ImGui.Dummy(new Vector2(totalW, totalH));

            ImGui.End();
        }

        private Vector4 ApplyOpacity(Vector4 color)
        {
            color.W *= currentOpacity;
            return color;
        }

        private int GetOrLoadTexture(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return 0;
            if (textureCache.TryGetValue(assetPath, out int texId)) return texId;

            int newTexId = capi.Render.GetOrLoadTexture(new AssetLocation(assetPath));
            textureCache[assetPath] = newTexId;
            return newTexId;
        }

        private string GetHotkeyLabel(int slotIndex) => slotIndex switch
        {
            _ => (slotIndex + 1).ToString()
        };

        private void ExecuteSkillSlot(int slotIndex)
        {
            networkService.RequestSkillActivation(slotIndex);
        }

        private void DrawCornerBrackets(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 color, float len)
        {
            uint c = ImGui.ColorConvertFloat4ToU32(color);
            const float t = 1.2f;
            // Top-left
            dl.AddLine(min, min + new Vector2(len, 0), c, t);
            dl.AddLine(min, min + new Vector2(0, len), c, t);
            // Top-right
            dl.AddLine(new Vector2(max.X, min.Y), new Vector2(max.X - len, min.Y), c, t);
            dl.AddLine(new Vector2(max.X, min.Y), new Vector2(max.X, min.Y + len), c, t);
            // Bottom-left
            dl.AddLine(new Vector2(min.X, max.Y), new Vector2(min.X + len, max.Y), c, t);
            dl.AddLine(new Vector2(min.X, max.Y), new Vector2(min.X, max.Y - len), c, t);
            // Bottom-right
            dl.AddLine(max, max - new Vector2(len, 0), c, t);
            dl.AddLine(max, max - new Vector2(0, len), c, t);
        }

        private string GetRemainingCooldownText(long cooldownMs, long timeSinceUsed)
        {
            long remainingMs = cooldownMs - timeSinceUsed;

            TimeSpan t = TimeSpan.FromMilliseconds(remainingMs);

            if (t.TotalHours >= 1)
            {
                return $"{Math.Floor(t.TotalHours)}h";
            }

            if (t.TotalMinutes >= 1)
            {
                if (t.TotalMinutes < 5) return $"{Math.Floor(t.TotalMinutes)}m {t.Seconds}s";
                return $"{Math.Floor(t.TotalMinutes)}m";
            }

            return $"{t.Seconds}s";
        }
    }
}