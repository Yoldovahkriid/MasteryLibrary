using ImGuiNET;
using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Config;
using MasteryLibrary.src.Core.Masteries.Data;
using MasteryLibrary.src.Core.Masteries.Instances;
using MasteryLibrary.src.Networking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using VSImGui.API;

namespace MasteryLibrary.src.UI.Windows
{
    public class MasteryGUIWindow
    {
        private readonly ICoreClientAPI capi;
        private readonly NetworkServiceClient network;

        private bool opened = false;

        private readonly Dictionary<string, int> textureCache = new();

        private string? draggedSkillCode = null;
        private int dragSourceSlot = -1;

        private UIThemeConfig Theme => MasteryLibConfigClient.Loaded.Theme;

        private Vector4 C_BgDeep => Theme.BackgroundDeep.ToVector4();
        private Vector4 C_BgMid => Theme.BackgroundMid.ToVector4();
        private Vector4 C_BgPanel => Theme.BackgroundPanel.ToVector4();
        private Vector4 C_BdDim => Theme.BorderDim.ToVector4();
        private Vector4 C_BdBrt => Theme.BorderBright.ToVector4();
        private Vector4 C_TxtPri => Theme.TextPrimary.ToVector4();
        private Vector4 C_TxtMut => Theme.TextMuted.ToVector4();
        private Vector4 C_TxtHnt => Theme.TextHint.ToVector4();
        private Vector4 C_Gold => Theme.Gold.ToVector4();
        private Vector4 C_GoldDim => Theme.GoldDim.ToVector4();
        private Vector4 C_GoldDp => Theme.GoldDeep.ToVector4();
        private Vector4 C_Green => Theme.Green.ToVector4();
        private Vector4 C_Red => Theme.Red.ToVector4();
        private Vector4 C_Uniq => Theme.UniqueAccent.ToVector4();
        private Vector4 C_UniqDim => Theme.UniqueAccentDim.ToVector4();
        private Vector4 C_SkLock => Theme.SkillLocked.ToVector4();
        private Vector4 C_SkAvail => Theme.SkillAvailable.ToVector4();
        private Vector4 C_SkUnlk => Theme.SkillUnlocked.ToVector4();
        private Vector4 C_SkUniq => Theme.SkillUnlockedUnique.ToVector4();

        public MasteryGUIWindow(ICoreClientAPI capi, NetworkServiceClient network)
        {
            this.capi = capi;
            this.network = network;
        }

        // VSImGui runs with ImGui multi-viewport enabled. A tooltip that doesn't fit inside the game window
        // gets promoted to its own OS window, which steals focus, and a fullscreen game window then
        // auto-minimises. Pinning the tooltip to the main viewport keeps it inside the game window.
        private static void BeginTooltipInMainViewport()
        {
            ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
            ImGui.BeginTooltip();

            // Tooltips auto-size to their widest non-wrapping line. With a short title, wrapped
            // description text ends up squeezed into a very narrow, very tall box. Give wrapped text
            // a fixed maximum width so every tooltip gets a consistent, readable width.
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * TooltipWrapEm);
        }

        private static void EndTooltipWrapped()
        {
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        // Max tooltip text width in font-height units (~400px at a 16px font). Tweak to taste.
        private const float TooltipWrapEm = 25f;
        public void Toggle()
        {
            opened = !opened;
            if (!opened)
            {
                draggedSkillCode = null;
                dragSourceSlot = -1;
            }
        }
        public bool IsOpen => opened;

        public void Dispose()
        {
            foreach (var id in textureCache.Values)
                capi.Render.GLDeleteTexture(id);
            textureCache.Clear();
        }

        public CallbackGUIStatus Draw(float dt)
        {
            if (!opened) return CallbackGUIStatus.Closed;

            Size2i screen = capi.Forms.GetScreenSize();
            ImGui.SetNextWindowPos(
                new Vector2(screen.Width * 0.5f, screen.Height * 0.5f),
                ImGuiCond.Always,
                new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(
                new Vector2(screen.Width * 0.64f, screen.Height * 0.74f),
                ImGuiCond.FirstUseEver);

            PushTheme();
            bool keepOpen = true;
            ImGui.Begin(Lang.Get("masterylibrary:window-title") + "##masterywin",
                        ref keepOpen, ImGuiWindowFlags.NoCollapse);
            if (!keepOpen)
            {
                opened = false;
                draggedSkillCode = null;
                dragSourceSlot = -1;
                ImGui.End();
                PopTheme();
                return CallbackGUIStatus.Closed;
            }

            var behavior = capi.World.Player?.Entity?.GetBehavior<EntityBehaviorPlayerMasteries>();
            if (behavior == null)
            {
                ImGui.TextColored(C_Red, Lang.Get("masterylibrary:error-no-mastery-data"));
            }
            else
            {
                var data = behavior.PlayerMasteryData;
                var masteryDefs = capi.ModLoader.GetModSystem<MasteryLibraryAPI>().MasteryDefinitions;

                DrawCharacterOverview(data);
                ImGui.Spacing();
                DrawMasteryTabs(data, masteryDefs);
            }

            ImGui.End();
            PopTheme();
            return CallbackGUIStatus.GrabMouse;
        }

        private void PushTheme()
        {
            float s = Theme.UIScale;
            ImGui.SetWindowFontScale(s);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, C_BgDeep);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, C_BgPanel);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, C_BgDeep);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, C_BgMid);
            ImGui.PushStyleColor(ImGuiCol.Border, C_BdDim);
            ImGui.PushStyleColor(ImGuiCol.Tab, C_BgDeep);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, C_GoldDp);
            ImGui.PushStyleColor(ImGuiCol.TabActive, C_BgMid);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, C_BgDeep);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, C_BgMid);
            ImGui.PushStyleColor(ImGuiCol.Header, C_GoldDp);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, C_GoldDim);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, C_Gold);
            ImGui.PushStyleColor(ImGuiCol.Button, C_GoldDp);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, C_GoldDim);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, C_Gold);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, C_BgPanel);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, C_GoldDp);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, C_GoldDim);
            ImGui.PushStyleColor(ImGuiCol.Separator, C_BdDim);
            ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, C_BdBrt);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, C_BgPanel);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, C_GoldDp);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, C_GoldDim);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, C_Gold);
            ImGui.PushStyleColor(ImGuiCol.PopupBg, C_BgMid);
            ImGui.PushStyleColor(ImGuiCol.Text, C_TxtPri);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14f * s, 12f * s));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f * s, 5f * s));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10f * s, 8f * s));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, Theme.WindowRounding * s);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, Theme.FrameRounding * s);
            ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, Theme.FrameRounding * s);
        }

        private void PopTheme()
        {
            ImGui.PopStyleVar(6);
            ImGui.PopStyleColor(27);
            ImGui.SetWindowFontScale(1.0f);
        }

        private void DrawCharacterOverview(PlayerMasteryData data)
        {
            double requiredXp = data.GetExperienceForNextLevel();
            float xpFrac = (float)Math.Min(data.Experience / Math.Max(requiredXp, 1.0), 1.0);

            var dl = ImGui.GetWindowDrawList();
            Vector2 cursor = ImGui.GetCursorScreenPos();
            float panelW = ImGui.GetContentRegionAvail().X;
            float panelH = 62f;
            float pad = 12f;

            // Panel
            dl.AddRectFilled(cursor, cursor + new Vector2(panelW, panelH),
                ImGui.ColorConvertFloat4ToU32(C_BgPanel), 3f);
            dl.AddRect(cursor, cursor + new Vector2(panelW, panelH),
                ImGui.ColorConvertFloat4ToU32(C_BdDim), 3f, ImDrawFlags.None, 1f);
            DrawCornerBrackets(dl, cursor, cursor + new Vector2(panelW, panelH), C_GoldDim, 8f);

            float fs = ImGui.GetFontSize();
            float smallH = fs;
            float bigH = fs * 1.4f;
            float rowH = smallH + bigH + 2f;
            float rowTopY = cursor.Y + (panelH - rowH) * 0.5f - 4f;

            // Level badge (left)
            string lvlLbl = Lang.Get("masterylibrary:overview-level-label");
            string lvlNum = $"{data.CharacterLevel}";
            dl.AddText(cursor + new Vector2(pad, rowTopY - cursor.Y),
                ImGui.ColorConvertFloat4ToU32(C_TxtMut), lvlLbl);
            ImGui.SetWindowFontScale(1.4f);
            dl.AddText(ImGui.GetFont(), fs * 1.4f,
                cursor + new Vector2(pad, rowTopY - cursor.Y + smallH + 2f),
                ImGui.ColorConvertFloat4ToU32(C_Gold), lvlNum);
            ImGui.SetWindowFontScale(1.0f);
            float lvlW = ImGui.CalcTextSize(lvlNum).X * 1.4f;
            float leftEdge = pad + Math.Max(ImGui.CalcTextSize(lvlLbl).X, lvlW) + pad;

            // Mastery points badge (right)
            string ptLbl = Lang.Get("masterylibrary:overview-mastery-points-label");
            string ptNum = $"{data.MasteryPoints}";
            Vector2 ptLblSz = ImGui.CalcTextSize(ptLbl);
            ImGui.SetWindowFontScale(1.4f);
            float ptNumW = ImGui.CalcTextSize(ptNum).X * 1.4f;
            ImGui.SetWindowFontScale(1.0f);
            float rightBadgeW = Math.Max(ptLblSz.X, ptNumW);
            float rightEdge = panelW - rightBadgeW - pad;
            dl.AddText(cursor + new Vector2(rightEdge + (rightBadgeW - ptLblSz.X) * 0.5f,
                        rowTopY - cursor.Y),
                ImGui.ColorConvertFloat4ToU32(C_TxtMut), ptLbl);
            ImGui.SetWindowFontScale(1.4f);
            dl.AddText(ImGui.GetFont(), fs * 1.4f,
                cursor + new Vector2(rightEdge + (rightBadgeW - ptNumW) * 0.5f,
                        rowTopY - cursor.Y + smallH + 2f),
                ImGui.ColorConvertFloat4ToU32(data.MasteryPoints > 0 ? C_Green : C_TxtMut), ptNum);
            ImGui.SetWindowFontScale(1.0f);

            // XP bar (centre)
            float barMargin = 16f;
            float barX = cursor.X + leftEdge + barMargin;
            float barR = cursor.X + rightEdge - barMargin;
            float barW = Math.Max(1f, barR - barX);
            float barY = cursor.Y + rowTopY - cursor.Y + (rowH - 8f) * 0.5f;
            float barH2 = 8f;

            dl.AddRectFilled(new Vector2(barX, barY), new Vector2(barR, barY + barH2),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.06f, 0.04f, 0.02f, 1f)), 2f);
            dl.AddRect(new Vector2(barX, barY), new Vector2(barR, barY + barH2),
                ImGui.ColorConvertFloat4ToU32(C_BdDim), 2f, ImDrawFlags.None, 0.5f);
            float fillW = barW * xpFrac;
            if (fillW > 4f)
            {
                dl.AddRectFilled(new Vector2(barX + 1f, barY + 1f),
                    new Vector2(barX + fillW - 1f, barY + barH2 - 1f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.48f, 0.28f, 0.06f, 1f)), 1f);
                dl.AddRectFilled(new Vector2(barX + 1f, barY + 1f),
                    new Vector2(barX + fillW * 0.6f, barY + barH2 * 0.5f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.78f, 0.56f, 0.14f, 0.5f)), 1f);
                dl.AddRectFilled(new Vector2(barX + fillW - 2f, barY + 1f),
                    new Vector2(barX + fillW - 1f, barY + barH2 - 1f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.88f, 0.50f, 0.9f)), 0f);
            }
            string xpTxt = Lang.Get("masterylibrary:overview-xp-format",
                data.Experience.ToString("F0"), requiredXp.ToString("F0"));
            Vector2 xpTxtSz = ImGui.CalcTextSize(xpTxt);
            dl.AddText(new Vector2(barX + (barW - xpTxtSz.X) * 0.5f, barY + barH2 + 3f),
                ImGui.ColorConvertFloat4ToU32(C_TxtMut), xpTxt);

            ImGui.Dummy(new Vector2(panelW, panelH + 4f));
        }

        private void DrawMasteryTabs(PlayerMasteryData data, MasteryDefinitions defs)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, C_Gold);
            bool open = ImGui.BeginTabBar("MasteryTabs");
            ImGui.PopStyleColor();
            if (!open) return;

            // Active Skills / Equip tab
            if (ImGui.BeginTabItem(Lang.Get("masterylibrary:tab-active-skills")))
            {
                DrawEquipTab(data);
                ImGui.EndTabItem();
            }

            // One tab per mastery
            foreach (var kv in defs.Masteries)
            {
                Mastery mastery = kv.Value;
                int tabLevel = data.LearntMasteries.TryGetValue(mastery.Code, out var tabInst) ? tabInst.Level : 0;
                if (ImGui.BeginTabItem($"  {mastery.GetDisplayName(tabLevel)}  ##tab_{mastery.Code}"))
                {
                    DrawMasteryTabContent(data, mastery);
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        private void DrawEquipTab(PlayerMasteryData data)
        {
            // Collect all unlocked Active skills
            var unlockedActives = new List<SkillInstance>();
            foreach (var mi in data.LearntMasteries.Values)
                foreach (var si in mi.UnlockedSkills.Values)
                    if (si.Skill.SkillType == EnumSkillType.Active)
                        unlockedActives.Add(si);

            int slotCount = data.EquippedActiveSkills.Length;

            ImGui.Spacing();
            ImGui.TextColored(C_TxtMut, Lang.Get("masterylibrary:equip-hint"));
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float availW = ImGui.GetContentRegionAvail().X;
            float availH = ImGui.GetContentRegionAvail().Y;
            float leftW = availW * 0.40f;
            float rightW = availW - leftW - ImGui.GetStyle().ItemSpacing.X;

            // Left panel: scrollable list of unlocked active skills
            ImGui.BeginChild("##equipLeft", new Vector2(leftW, availH - 4f), true);
            ImGui.TextColored(C_Gold, Lang.Get("masterylibrary:equip-panel-unlocked"));
            ImGui.Separator();
            ImGui.Spacing();

            foreach (var si in unlockedActives)
            {
                bool equippedAnywhere = data.EquippedActiveSkills.Contains(si.Skill.Code);
                DrawEquipListEntry(si, equippedAnywhere);
            }

            if (unlockedActives.Count == 0)
                ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:equip-no-actives"));

            ImGui.EndChild();

            // Right panel: equipment slots
            ImGui.SameLine();
            ImGui.BeginChild("##equipRight", new Vector2(rightW, availH - 4f), true);
            ImGui.TextColored(C_Gold, Lang.Get("masterylibrary:equip-panel-equipped"));
            ImGui.Separator();
            ImGui.Spacing();

            float slotSize = 56f;
            float slotPad = 10f;
            // Measure width now that we are inside the child — padding already accounted for.
            float innerW = ImGui.GetContentRegionAvail().X;
            int slotsPerRow = Math.Max(1, (int)((innerW + slotPad) / (slotSize + slotPad)));

            for (int i = 0; i < slotCount; i++)
            {
                if (i % slotsPerRow != 0)
                    ImGui.SameLine(0f, slotPad);

                DrawEquipSlot(data, i, slotSize, unlockedActives);
            }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:equip-drag-hint"));
            ImGui.EndChild();

            // Cancel drag on escape
            if (draggedSkillCode != null && ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                draggedSkillCode = null;
                dragSourceSlot = -1;
            }
        }

        // List entry (left panel)

        private void DrawEquipListEntry(SkillInstance si, bool alreadyEquipped)
        {
            var dl = ImGui.GetWindowDrawList();
            bool isDragging = draggedSkillCode == si.Skill.Code && dragSourceSlot == -1;

            Vector2 pos = ImGui.GetCursorScreenPos();
            float entH = 40f;
            float entW = ImGui.GetContentRegionAvail().X;
            float iconS = 30f;
            float innerPad = 6f;

            // Background
            uint bgCol = isDragging
                ? ImGui.ColorConvertFloat4ToU32(C_GoldDp)
                : alreadyEquipped
                    ? ImGui.ColorConvertFloat4ToU32(C_SkUnlk)
                    : ImGui.ColorConvertFloat4ToU32(C_SkAvail);

            dl.AddRectFilled(pos, pos + new Vector2(entW, entH), bgCol, 3f);
            dl.AddRect(pos, pos + new Vector2(entW, entH),
                ImGui.ColorConvertFloat4ToU32(alreadyEquipped ? C_Gold : C_BdDim),
                3f, ImDrawFlags.None, alreadyEquipped ? 1.2f : 0.7f);

            // Icon
            int texId = GetOrLoadTexture(si.Skill.IconPath);
            Vector2 iconPos = pos + new Vector2(innerPad, (entH - iconS) * 0.5f);
            if (texId != 0)
                dl.AddImage((IntPtr)texId, iconPos, iconPos + new Vector2(iconS, iconS));
            else
            {
                dl.AddRectFilled(iconPos, iconPos + new Vector2(iconS, iconS),
                    ImGui.ColorConvertFloat4ToU32(C_BgPanel), 2f);
                dl.AddRect(iconPos, iconPos + new Vector2(iconS, iconS),
                    ImGui.ColorConvertFloat4ToU32(C_BdDim), 2f);
            }

            // Name + type label
            float textX = pos.X + innerPad + iconS + innerPad;
            dl.AddText(new Vector2(textX, pos.Y + 6f),
                ImGui.ColorConvertFloat4ToU32(alreadyEquipped ? C_Gold : C_TxtPri),
                si.Skill.GetDisplayName(si.Level));
            dl.AddText(new Vector2(textX, pos.Y + 22f),
                ImGui.ColorConvertFloat4ToU32(C_TxtHnt),
                alreadyEquipped
                    ? Lang.Get("masterylibrary:equip-status-equipped")
                    : Lang.Get("masterylibrary:equip-status-available"));

            // Invisible button to handle drag
            ImGui.SetCursorScreenPos(pos);
            ImGui.InvisibleButton($"##listEntry_{si.Skill.Code}", new Vector2(entW, entH));

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4f))
            {
                draggedSkillCode = si.Skill.Code;
                dragSourceSlot = -1;
            }

            // Drag preview tooltip
            if (isDragging)
            {
                ImGui.SetNextWindowBgAlpha(0.80f);
                BeginTooltipInMainViewport();
                ImGui.TextColored(C_Gold, si.Skill.GetDisplayName(si.Level));
                ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:equip-drag-release-hint"));
                EndTooltipWrapped();
            }

            // Tooltip on hover (not dragging)
            if (!isDragging && ImGui.IsItemHovered())
                DrawSkillTooltip(si);

            ImGui.Spacing();
        }

        // Equipment slot (right panel)

        private void DrawEquipSlot(PlayerMasteryData data, int slotIndex, float slotSize,
                                    List<SkillInstance> unlockedActives)
        {
            var dl = ImGui.GetWindowDrawList();
            string? equippedCode = (slotIndex < data.EquippedActiveSkills.Length)
                ? data.EquippedActiveSkills[slotIndex]
                : null;
            bool filled = !string.IsNullOrEmpty(equippedCode);
            bool isDropTarget = draggedSkillCode != null;

            // BeginGroup so the entire slot (icon + label) is treated as one layout unit.
            ImGui.BeginGroup();

            Vector2 slotPos = ImGui.GetCursorScreenPos();
            Vector2 slotMax = slotPos + new Vector2(slotSize, slotSize);

            // Slot background
            Vector4 bgVec = isDropTarget ? C_GoldDp : (filled ? C_SkUnlk : C_SkLock);
            dl.AddRectFilled(slotPos, slotMax, ImGui.ColorConvertFloat4ToU32(bgVec), 4f);

            // Icon
            if (filled)
            {
                SkillInstance? si = unlockedActives.FirstOrDefault(s => s.Skill.Code == equippedCode);
                if (si != null)
                {
                    int texId = GetOrLoadTexture(si.Skill.IconPath);
                    if (texId != 0)
                    {
                        float ipad = 4f;
                        dl.AddImage((IntPtr)texId,
                            slotPos + new Vector2(ipad, ipad),
                            slotMax - new Vector2(ipad, ipad));
                    }
                }
            }

            // Border + brackets
            dl.AddRect(slotPos, slotMax,
                ImGui.ColorConvertFloat4ToU32(filled ? C_BdBrt : C_BdDim),
                4f, ImDrawFlags.None, filled ? 1.4f : 0.8f);
            DrawCornerBrackets(dl, slotPos, slotMax, filled ? C_Gold : C_GoldDim, 6f);

            // Slot index number
            string numLabel = $"{slotIndex + 1}";
            dl.AddText(slotPos + new Vector2(4f, 3f),
                ImGui.ColorConvertFloat4ToU32(filled ? C_Gold : C_TxtHnt), numLabel);

            // Invisible hit area — positioned at the slot's top-left, exactly slotSize square.
            ImGui.SetCursorScreenPos(slotPos);
            ImGui.InvisibleButton($"##slot_{slotIndex}", new Vector2(slotSize, slotSize));

            // Drop: release drag over this slot
            if (ImGui.IsItemHovered() && draggedSkillCode != null && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (dragSourceSlot >= 0 && dragSourceSlot != slotIndex)
                {
                    network.RequestSkillEquip(dragSourceSlot, equippedCode ?? string.Empty);
                    network.RequestSkillEquip(slotIndex, draggedSkillCode);
                }
                else if (dragSourceSlot == -1)
                {
                    network.RequestSkillEquip(slotIndex, draggedSkillCode);
                }

                draggedSkillCode = null;
                dragSourceSlot = -1;
            }

            // Start drag from an occupied slot
            if (filled && ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4f))
            {
                draggedSkillCode = equippedCode;
                dragSourceSlot = slotIndex;
            }

            // Right-click to unequip
            if (filled && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                network.RequestSkillEquip(slotIndex, string.Empty);

            // Hover tooltip
            if (ImGui.IsItemHovered() && draggedSkillCode == null)
            {
                if (filled)
                {
                    SkillInstance? si = unlockedActives.FirstOrDefault(s => s.Skill.Code == equippedCode);
                    if (si != null) DrawSkillTooltip(si);
                    else
                    {
                        BeginTooltipInMainViewport();
                        ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:equip-slot-unknown"));
                        EndTooltipWrapped();
                    }
                }
                else
                {
                    BeginTooltipInMainViewport();
                    ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:equip-slot-empty"));
                    EndTooltipWrapped();
                }
            }

            // Label centred below the icon
            SkillInstance? filledSi = filled
                ? unlockedActives.FirstOrDefault(s => s.Skill.Code == equippedCode)
                : null;
            string label = filledSi != null
                ? filledSi.Skill.GetDisplayName(filledSi.Level)
                : filled ? equippedCode!
                : $"— {slotIndex + 1} —";
            // Clamp label to slotSize width with an ellipsis
            float maxLabelW = slotSize;
            string displayLabel = label;
            while (displayLabel.Length > 1 && ImGui.CalcTextSize(displayLabel).X > maxLabelW)
                displayLabel = displayLabel[..^1];
            if (displayLabel.Length < label.Length) displayLabel += "…";

            float lw = ImGui.CalcTextSize(displayLabel).X;
            float indent = Math.Max(0f, (slotSize - lw) * 0.5f);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + indent);
            ImGui.TextColored(filled ? C_Gold : C_TxtHnt, displayLabel);

            ImGui.EndGroup();
        }

        private void DrawMasteryTabContent(PlayerMasteryData data, Mastery mastery)
        {
            data.LearntMasteries.TryGetValue(mastery.Code, out MasteryInstance? mInst);

            if (ImGui.BeginTable($"MastTab_{mastery.Code}", 3, ImGuiTableFlags.BordersInnerV))
            {
                // Progress Bar (Left)
                ImGui.TableSetupColumn("Progress", ImGuiTableColumnFlags.WidthFixed, 80f);
                // Skill Tree (Middle)
                ImGui.TableSetupColumn("Tree", ImGuiTableColumnFlags.WidthStretch);
                // Mastery Info (Right)
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthFixed, 180f);

                ImGui.TableNextRow(ImGuiTableRowFlags.None, ImGui.GetContentRegionAvail().Y);

                ImGui.TableNextColumn();
                DrawMasteryProgress(mastery, mInst, data);

                ImGui.TableNextColumn();
                DrawSkillTreeColumn(data, mastery, mInst);

                ImGui.TableNextColumn();
                DrawMasteryDetails(mastery, mInst);

                ImGui.EndTable();
            }
        }

        // Left column:  pip rack + rank-up

        private void DrawMasteryProgress(Mastery mastery, MasteryInstance? mInst, PlayerMasteryData data)
        {
            int curLevel = mInst?.Level ?? 0;
            int maxLevel = mastery.MaxLevel;
            var dl = ImGui.GetWindowDrawList();
            float colW = ImGui.GetContentRegionAvail().X;
            float availH = ImGui.GetContentRegionAvail().Y;

            // Dimensions
            float pipW = 26f;
            float pipH = 10f;
            float pipGap = 4f;
            float rackH = (pipH + pipGap) * maxLevel;

            // Calculate Total Content Height for Centering
            string rankLbl = Lang.Get("masterylibrary:progress-rank-label");
            Vector2 rLblSz = ImGui.CalcTextSize(rankLbl);
            string cntTxt = Lang.Get("masterylibrary:progress-level-format", curLevel, maxLevel);
            Vector2 cntSz = ImGui.CalcTextSize(cntTxt);
            float btnH = ImGui.GetFrameHeight();

            float totalH = rLblSz.Y + 6f + rackH + 5f + cntSz.Y + 12f + btnH;

            // Vertical Offset
            float startY = ImGui.GetCursorScreenPos().Y + Math.Max(0f, (availH - totalH) * 0.5f);
            float startX = ImGui.GetCursorScreenPos().X + (colW - pipW) * 0.5f;

            // Draw RANK label
            dl.AddText(new Vector2(startX + (pipW - rLblSz.X) * 0.5f, startY),
                ImGui.ColorConvertFloat4ToU32(C_TxtHnt), rankLbl);
            startY += rLblSz.Y + 6f;

            // Draw Pip Rack
            for (int pip = maxLevel; pip >= 1; pip--)
            {
                float pipY = startY + (maxLevel - pip) * (pipH + pipGap);
                var pipMin = new Vector2(startX, pipY);
                var pipMax = new Vector2(startX + pipW, pipY + pipH);
                bool filled = pip <= curLevel;

                uint fillC = (pip == curLevel)
                    ? ImGui.ColorConvertFloat4ToU32(C_Gold)
                    : (filled ? ImGui.ColorConvertFloat4ToU32(C_GoldDim) : ImGui.ColorConvertFloat4ToU32(C_BgDeep));

                dl.AddRectFilled(pipMin, pipMax, fillC, 2f);
                dl.AddRect(pipMin, pipMax, ImGui.ColorConvertFloat4ToU32(filled ? C_BdBrt : C_BdDim), 2f);
            }

            // Level counter
            float counterY = startY + rackH + 5f;
            dl.AddText(new Vector2(startX + (pipW - cntSz.X) * 0.5f, counterY),
                ImGui.ColorConvertFloat4ToU32(C_Gold), cntTxt);

            // Rank-up button
            float btnY = counterY + cntSz.Y + 12f;
            float btnW = Math.Min(colW - 8f, 70f);
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X + (colW - btnW) * 0.5f, btnY));

            bool canAfford = data.MasteryPoints > 0 && curLevel < maxLevel;
            if (ImGui.Button($"UP##{mastery.Code}", new Vector2(btnW, btnH)) && canAfford)
                network.RequestMasteryUpgrade(mastery.Code);
        }

        // Right column: skill tree

        private void DrawSkillTreeColumn(PlayerMasteryData data, Mastery mastery, MasteryInstance? mInst)
        {
            int curMasteryLevel = mInst?.Level ?? 0;

            // Group by required mastery level, highest first
            var grouped = mastery.Skills
                .GroupBy(kv => kv.Value.RequiredMasteryLevel)
                .OrderByDescending(g => g.Key)
                .ToList();

            // Pre-resolve visual columns (shift collisions right)
            var visualColumns = new Dictionary<string, int>();
            int maxCol = 0;
            foreach (var group in grouped)
            {
                var used = new HashSet<int>();
                foreach (var kv in group)
                {
                    int col = kv.Value.Column;
                    while (used.Contains(col)) col++;
                    used.Add(col);
                    visualColumns[kv.Key] = col;
                    if (col > maxCol) maxCol = col;
                }
            }

            float iconSize = 96f;
            float itemSpacX = ImGui.GetStyle().ItemSpacing.X;
            bool firstGroup = true;

            //   Channel 0 — lines (rendered first, i.e. behind everything)
            //   Channel 1 — skill buttons (rendered on top)

            var dl = ImGui.GetWindowDrawList();
            dl.ChannelsSplit(2);

            // Channel 1: render skill buttons and record icon centres
            dl.ChannelsSetCurrent(1);

            var skillIconCentres = new Dictionary<string, Vector2>();

            foreach (var group in grouped)
            {
                if (!firstGroup) { ImGui.Spacing(); DrawTierConnector(); ImGui.Spacing(); }
                firstGroup = false;

                DrawSectionDivider(Lang.Get("masterylibrary:skilltree-tier-header", group.Key));
                ImGui.Spacing();

                float availW = ImGui.GetContentRegionAvail().X;
                float totalRowW = (maxCol + 1) * iconSize + maxCol * itemSpacX;
                float rowStartX = ImGui.GetCursorPosX() + Math.Max(0f, (availW - totalRowW) * 0.5f);
                float rowStartY = ImGui.GetCursorPosY();
                float maxBottomY = rowStartY;

                foreach (var kv in group)
                {
                    Skill skill = kv.Value;
                    int col = visualColumns[kv.Key];
                    ImGui.SetCursorPos(new Vector2(rowStartX + col * (iconSize + itemSpacX), rowStartY));

                    // Record the exact screen-space position before the button is drawn.
                    Vector2 iconScreenPos = ImGui.GetCursorScreenPos();
                    skillIconCentres[skill.Code] = iconScreenPos + new Vector2(iconSize * 0.5f, iconSize * 0.5f);

                    DrawSkillButton(data, mastery, mInst, skill, iconSize, curMasteryLevel);
                    if (ImGui.GetCursorPosY() > maxBottomY) maxBottomY = ImGui.GetCursorPosY();
                }

                ImGui.SetCursorPosY(maxBottomY);
                ImGui.Spacing();
            }

            // Channel 0: draw prerequisite lines behind the buttons
            dl.ChannelsSetCurrent(0);
            DrawPrerequisiteLines(mastery, mInst, skillIconCentres, iconSize);

            dl.ChannelsMerge();
        }

        private void DrawPrerequisiteLines(
            Mastery mastery,
            MasteryInstance? mInst,
            Dictionary<string, Vector2> centres,
            float iconSize)
        {
            var dl = ImGui.GetWindowDrawList();
            float halfIcon = iconSize * 0.5f;

            foreach (var kv in mastery.Skills)
            {
                Skill skill = kv.Value;
                if (skill.LevelRequirements == null || skill.LevelRequirements.Count == 0)
                    continue;

                if (!centres.TryGetValue(skill.Code, out Vector2 skillCentre))
                    continue;

                foreach (var levelEntry in skill.LevelRequirements)
                {
                    foreach (var prereq in levelEntry.Value)
                    {
                        if (!centres.TryGetValue(prereq.SkillCode, out Vector2 prereqCentre))
                            continue;

                        bool prereqUnlocked = mInst != null &&
                            mInst.UnlockedSkills.TryGetValue(prereq.SkillCode, out var psi) &&
                            psi.Level >= prereq.MinimumLevel;

                        uint lineColor = ImGui.ColorConvertFloat4ToU32(
                            prereqUnlocked ? C_GoldDim : new Vector4(0.35f, 0.30f, 0.20f, 0.55f));
                        const float lineThickness = 1.5f;

                        // Draw an orthogonal elbow: vertical segment from the prerequisite's
                        // bottom edge to the midpoint between the two icons, then horizontal
                        // to align with the dependent skill, then vertical down to its top edge.
                        Vector2 from = prereqCentre + new Vector2(0f, halfIcon);   // bottom of prereq
                        Vector2 to = skillCentre - new Vector2(0f, halfIcon);   // top of skill

                        float midY = (from.Y + to.Y) * 0.5f;
                        Vector2 elbow1 = new Vector2(from.X, midY);
                        Vector2 elbow2 = new Vector2(to.X, midY);

                        dl.AddLine(from, elbow1, lineColor, lineThickness);
                        dl.AddLine(elbow1, elbow2, lineColor, lineThickness);
                        dl.AddLine(elbow2, to, lineColor, lineThickness);

                        // Small arrowhead pointing down into the dependent skill.
                        float arrowHalfW = 4f;
                        float arrowLen = 6f;
                        dl.AddTriangleFilled(
                            to,
                            to + new Vector2(-arrowHalfW, -arrowLen),
                            to + new Vector2(arrowHalfW, -arrowLen),
                            lineColor);
                    }
                }
            }
        }


        private void DrawMasteryDetails(Mastery mastery, MasteryInstance? mInst)
        {
            float colW = ImGui.GetContentRegionAvail().X;
            var dl = ImGui.GetWindowDrawList();

            // Icon
            float iconSize = Math.Min(colW - 16f, 80f);
            Vector2 iconPos = ImGui.GetCursorScreenPos() + new Vector2((colW - iconSize) * 0.5f, 10f);
            int texId = GetOrLoadTexture(mastery.IconPath);

            if (texId != 0) dl.AddImage((IntPtr)texId, iconPos, iconPos + new Vector2(iconSize, iconSize));
            dl.AddRect(iconPos, iconPos + new Vector2(iconSize, iconSize), ImGui.ColorConvertFloat4ToU32(C_BdDim), 4f);

            ImGui.Dummy(new Vector2(colW, iconSize + 20f));

            // Name
            int detailLevel = mInst?.Level ?? 0;
            string name = mastery.GetDisplayName(detailLevel);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (colW - ImGui.CalcTextSize(name).X) * 0.5f);
            ImGui.TextColored(C_Gold, name);

            ImGui.Separator();

            // Current description (hidden at level 0 — next-level preview covers it)
            if (detailLevel > 0)
            {
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + colW);
                ImGui.TextColored(C_TxtMut, mastery.GetDescription(detailLevel));
                ImGui.PopTextWrapPos();
            }

            // Next-level description (only when not at max)
            int nextMasteryLevel = detailLevel + 1;
            if (nextMasteryLevel <= mastery.MaxLevel)
            {
                string nextDesc = mastery.GetDescription(nextMasteryLevel);
                if (!string.IsNullOrEmpty(nextDesc) && (detailLevel == 0 || nextDesc != mastery.GetDescription(detailLevel)))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:details-next-level-label"));
                    ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + colW);
                    ImGui.TextColored(C_GoldDim, nextDesc);
                    ImGui.PopTextWrapPos();
                }
            }
        }
        // Single skill icon button

        private void DrawSkillButton(PlayerMasteryData data, Mastery mastery,
            MasteryInstance? mInst, Skill skill, float iconSize, int curMasteryLevel)
        {
            ImGui.BeginGroup();
            int curSkillLevel = 0;
            if (mInst != null && mInst.UnlockedSkills.TryGetValue(skill.Code, out var sInst))
            {
                curSkillLevel = sInst.Level;
            }
            bool isUnlocked = curSkillLevel > 0;
            bool isUnique = skill.IsUnique;
            bool meetsMastery = curMasteryLevel >= skill.RequiredMasteryLevel;
            bool meetsPrereqs = true;
            var missingPrereqs = new List<string>();

            foreach (var kv in skill.LevelRequirements)
            {
                foreach (var req in kv.Value)
                {
                    if (mInst == null || !mInst.UnlockedSkills.ContainsKey(req.SkillCode))
                    {
                        meetsPrereqs = false;
                        missingPrereqs.Add(req.SkillCode);
                    }
                }
            }

            bool isLocked = !isUnlocked && (!meetsMastery || !meetsPrereqs);
            bool isMaxed = isUnique || curSkillLevel >= skill.MaxLevel;

            Vector4 tint = (isUnique && isUnlocked)
                ? new Vector4(0.70f, 0.95f, 1.00f, 1f)
                : isLocked ? new Vector4(0.25f, 0.20f, 0.12f, 1f)
                : isUnlocked ? new Vector4(1.00f, 0.90f, 0.65f, 1f)
                             : new Vector4(0.75f, 0.65f, 0.45f, 1f);

            uint bgCol = ImGui.ColorConvertFloat4ToU32(
                isUnique && isUnlocked ? C_SkUniq
                : isLocked ? C_SkLock
                : isUnlocked ? C_SkUnlk
                                       : C_SkAvail);

            var dl = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 posMax = pos + new Vector2(iconSize, iconSize);

            dl.AddRectFilled(pos, posMax, bgCol, 3f);

            Vector4 brdVec = (isUnique && isUnlocked) ? C_Uniq
                           : isUnlocked ? C_BdBrt
                                                      : C_BdDim;
            dl.AddRect(pos, posMax, ImGui.ColorConvertFloat4ToU32(brdVec),
                3f, ImDrawFlags.None, isUnlocked ? 1.5f : 0.8f);

            Vector4 brkVec = (isUnique && isUnlocked) ? C_UniqDim
                           : isUnlocked ? C_Gold
                           : isLocked ? C_TxtHnt
                                                      : C_GoldDim;
            DrawCornerBrackets(dl, pos, posMax, brkVec, 7f);

            if (isUnique && isUnlocked)
            {
                string badge = "★";
                Vector2 badgeSz = ImGui.CalcTextSize(badge);
                dl.AddText(pos + new Vector2(iconSize - badgeSz.X - 3f, 3f),
                    ImGui.ColorConvertFloat4ToU32(C_Uniq), badge);
            }

            if (!isUnique && skill.MaxLevel > 1)
                DrawSkillRankPips(dl, pos, iconSize, curSkillLevel, skill.MaxLevel);

            bool clicked = false;
            int texId = GetOrLoadTexture(skill.IconPath);

            if (texId != 0)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 1, 1, 0.08f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 1, 1, 0.16f));
                clicked = ImGui.ImageButton($"##{skill.Code}", (IntPtr)texId,
                    new Vector2(iconSize, iconSize),
                    Vector2.Zero, Vector2.One, new Vector4(0, 0, 0, 0), tint);
                ImGui.PopStyleColor(3);
                ImGui.PopStyleVar();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 1, 1, 0.08f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 1, 1, 0.16f));
                ImGui.PushStyleColor(ImGuiCol.Text, tint);
                clicked = ImGui.Button($"{skill.GetDisplayName(curSkillLevel)}##{skill.Code}",
                    new Vector2(iconSize, iconSize));
                ImGui.PopStyleColor(4);
            }

            if (clicked && !isLocked && !isMaxed)
                network.RequestSkillUpgrade(mastery.Code, skill.Code);

            if (ImGui.IsItemHovered())
            {
                BeginTooltipInMainViewport();
                ImGui.TextColored(isUnique ? C_Uniq : C_Gold, skill.GetDisplayName(curSkillLevel));
                if (isUnique) ImGui.TextColored(C_UniqDim, Lang.Get("masterylibrary:skill-tooltip-unique-tag"));
                string skillDesc = skill.GetDescription(curSkillLevel);
                if (curSkillLevel > 0 && !string.IsNullOrEmpty(skillDesc)) ImGui.TextWrapped(skillDesc);

                // Next-level preview
                int nextSkillLevel = curSkillLevel + 1;
                if (!isUnique && nextSkillLevel <= skill.MaxLevel)
                {
                    string nextName = skill.GetDisplayName(nextSkillLevel);
                    string nextDesc = skill.GetDescription(nextSkillLevel);
                    bool nameChanged = nextName != skill.GetDisplayName(curSkillLevel);
                    bool descChanged = !string.IsNullOrEmpty(nextDesc) && (curSkillLevel == 0 || nextDesc != skillDesc);
                    if (nameChanged || descChanged)
                    {
                        ImGui.Spacing();
                        ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:details-next-level-label"));
                        if (nameChanged) ImGui.TextColored(C_GoldDim, nextName);
                        if (descChanged) ImGui.TextWrapped(nextDesc);
                    }
                }

                ImGui.Spacing();

                if (isUnique)
                    ImGui.TextColored(C_UniqDim, Lang.Get("masterylibrary:skill-tooltip-unique-no-upgrade"));
                else if (isMaxed)
                    ImGui.TextColored(C_GoldDim, Lang.Get("masterylibrary:skill-tooltip-mastered"));
                else if (!meetsMastery)
                    ImGui.TextColored(C_Red, Lang.Get("masterylibrary:skill-tooltip-locked-rank", skill.RequiredMasteryLevel));
                else
                    ImGui.TextColored(C_TxtMut, Lang.Get("masterylibrary:skill-tooltip-required-rank", skill.RequiredMasteryLevel));

                EndTooltipWrapped();
            }

            // Level label under icon
            string lvlTxt = isUnique
                ? Lang.Get("masterylibrary:skill-unique-label")
                : Lang.Get("masterylibrary:skill-level-format", curSkillLevel, skill.MaxLevel);
            float lvlTxtW = ImGui.CalcTextSize(lvlTxt).X;
            float indent = (iconSize - lvlTxtW) * 0.5f;
            if (indent > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + indent);
            ImGui.TextColored(
                isUnique ? C_Uniq : isUnlocked ? C_Gold : C_TxtHnt,
                lvlTxt);

            ImGui.EndGroup();
        }

        private void DrawSkillTooltip(SkillInstance si)
        {
            BeginTooltipInMainViewport();
            ImGui.TextColored(si.Skill.IsUnique ? C_Uniq : C_Gold, si.Skill.GetDisplayName(si.Level));
            string d = si.Skill.GetDescription(si.Level);
            if (si.Level > 0 && !string.IsNullOrEmpty(d)) ImGui.TextWrapped(d);

            // Next-level preview
            int nextLevel = si.Level + 1;
            if (!si.Skill.IsUnique && nextLevel <= si.Skill.MaxLevel)
            {
                string nextName = si.Skill.GetDisplayName(nextLevel);
                string nextDesc = si.Skill.GetDescription(nextLevel);
                bool nameChanged = nextName != si.Skill.GetDisplayName(si.Level);
                bool descChanged = !string.IsNullOrEmpty(nextDesc) && (si.Level == 0 || nextDesc != d);
                if (nameChanged || descChanged)
                {
                    ImGui.Spacing();
                    ImGui.TextColored(C_TxtHnt, Lang.Get("masterylibrary:details-next-level-label"));
                    if (nameChanged) ImGui.TextColored(C_GoldDim, nextName);
                    if (descChanged) ImGui.TextWrapped(nextDesc);
                }
            }

            ImGui.Spacing();
            ImGui.TextColored(C_TxtMut,
                si.Skill.SkillType == EnumSkillType.Active
                    ? Lang.Get("masterylibrary:skill-type-active")
                    : Lang.Get("masterylibrary:skill-type-passive"));
            if (si.Skill.Cooldown > 0)
                ImGui.TextColored(C_TxtHnt,
                    Lang.Get("masterylibrary:skill-cooldown", si.Skill.Cooldown));
            EndTooltipWrapped();
        }

        private void DrawCornerBrackets(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 color, float len)
        {
            uint c = ImGui.ColorConvertFloat4ToU32(color);
            float t = 1.2f;
            dl.AddLine(min, min + new Vector2(len, 0), c, t);
            dl.AddLine(min, min + new Vector2(0, len), c, t);
            dl.AddLine(new Vector2(max.X, min.Y), new Vector2(max.X - len, min.Y), c, t);
            dl.AddLine(new Vector2(max.X, min.Y), new Vector2(max.X, min.Y + len), c, t);
            dl.AddLine(new Vector2(min.X, max.Y), new Vector2(min.X + len, max.Y), c, t);
            dl.AddLine(new Vector2(min.X, max.Y), new Vector2(min.X, max.Y - len), c, t);
            dl.AddLine(max, max - new Vector2(len, 0), c, t);
            dl.AddLine(max, max - new Vector2(0, len), c, t);
        }

        private void DrawSkillRankPips(ImDrawListPtr dl, Vector2 iconPos, float iconSize, int current, int max)
        {
            int displayMax = Math.Min(max, 10);
            float pipW = (iconSize - 8f) / displayMax - 2f;
            float pipH = 4f;
            float startX = iconPos.X + 4f;
            float pipY = iconPos.Y + iconSize - pipH - 3f;

            for (int p = 0; p < displayMax; p++)
            {
                float px = startX + p * (pipW + 2f);
                dl.AddRectFilled(
                    new Vector2(px, pipY),
                    new Vector2(px + pipW, pipY + pipH),
                    ImGui.ColorConvertFloat4ToU32(p < current ? C_Gold : C_BgPanel), 1f);
            }
        }

        private void DrawSectionDivider(string label)
        {
            var dl = ImGui.GetWindowDrawList();
            float availW = ImGui.GetContentRegionAvail().X;
            Vector2 cur = ImGui.GetCursorScreenPos();
            Vector2 tsz = ImGui.CalcTextSize(label);
            float lineY = cur.Y + tsz.Y * 0.5f;
            float pad = 10f;
            float textX = cur.X + (availW - tsz.X) * 0.5f;

            dl.AddLine(new Vector2(cur.X, lineY), new Vector2(textX - pad, lineY),
                ImGui.ColorConvertFloat4ToU32(C_BdDim), 0.8f);
            dl.AddText(new Vector2(textX, cur.Y),
                ImGui.ColorConvertFloat4ToU32(C_TxtHnt), label);
            dl.AddLine(new Vector2(textX + tsz.X + pad, lineY), new Vector2(cur.X + availW, lineY),
                ImGui.ColorConvertFloat4ToU32(C_BdDim), 0.8f);

            ImGui.Dummy(new Vector2(availW, tsz.Y + 2f));
        }

        private void DrawTierConnector()
        {
            var dl = ImGui.GetWindowDrawList();
            float availW = ImGui.GetContentRegionAvail().X;
            Vector2 cur = ImGui.GetCursorScreenPos();
            float cx = cur.X + availW * 0.5f;
            float cy = cur.Y + 6f;
            uint lc = ImGui.ColorConvertFloat4ToU32(C_BdDim);
            uint dc = ImGui.ColorConvertFloat4ToU32(C_GoldDim);
            dl.AddLine(new Vector2(cx, cy - 6f), new Vector2(cx, cy + 6f), lc, 0.8f);
            dl.AddCircleFilled(new Vector2(cx, cy), 3f, dc);
            ImGui.Dummy(new Vector2(availW, 12f));
        }

        private int GetOrLoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            if (textureCache.TryGetValue(path, out int id)) return id;
            int newId = capi.Render.GetOrLoadTexture(new AssetLocation(path));
            textureCache[path] = newId;
            return newId;
        }
    }
}