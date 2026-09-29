using Cairo;
using MasteryLibrary.src.Config;
using MasteryLibrary.src.Networking.Client;
using MasteryLibrary.src.Networking.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace MasteryLibrary.src.UI.Components
{
    public class XpPopupGUI : IRenderer, IDisposable
    {
        public int MaxVisible { get; set; } = 5;
        private const int MAX_PENDING = 20;
        private const long LIFETIME_MS = 3500;
        private const long FADE_IN_MS = 200;
        private const long FADE_OUT_MS = 450;
        private const long PULSE_MS = 350;

        // Z layers inside the ortho pass (higher = on top)
        private const float Z_CARD = 50f;
        private const float Z_PULSE = 55f;
        private const float Z_ICON = 100f;

        private sealed class Popup
        {
            public int Id;
            public string Key = "";
            public XpPopUpPacket Packet = null!;
            public float Xp;
            public long SpawnMs;
            public long ExpireMs;
            public long PulseMs = -10000;
            public float CurrentY;
            public bool YInitialized;

            public string DisplayName = "";
            public DummySlot? IconSlot;        // Item / Block / Entity (spawn egg)
            public int IconTextureId;          // Image type
            public LoadedTexture CardTexture = null!;
            public bool Dirty = true;          // card texture needs (re)baking
        }

        private readonly ICoreClientAPI capi;
        private readonly NetworkServiceClient networkService;

        private readonly ConcurrentQueue<XpPopUpPacket> incoming = new();
        private readonly List<Popup> active = new();      // index 0 = oldest
        private readonly List<Popup> pending = new();
        private int nextId = 0;

        private LoadedTexture pulseTexture;               // shared bright border, drawn with alpha = pulse
        private float bakedScale = -1f;

        // ---- IRenderer ----
        public double RenderOrder => 0.95;
        public int RenderRange => 1;

        public XpPopupGUI(ICoreClientAPI capi, NetworkServiceClient networkService)
        {
            this.capi = capi;
            this.networkService = networkService;
            pulseTexture = new LoadedTexture(capi);

            networkService.XpGained += Enqueue;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "masterylib-xppopup");
        }

        public void Dispose()
        {
            networkService.XpGained -= Enqueue;
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);

            foreach (var p in active) p.CardTexture?.Dispose();
            foreach (var p in pending) p.CardTexture?.Dispose();
            active.Clear();
            pending.Clear();
            pulseTexture.Dispose();
        }

        private void Enqueue(XpPopUpPacket packet) => incoming.Enqueue(packet);


        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            long now = capi.World.ElapsedMilliseconds;

            DrainIncoming(now);
            RemoveExpired(now);
            PromotePending(now);

            if (active.Count == 0) return;

            float uiscale = RuntimeEnv.GUIScale;
            if (Math.Abs(uiscale - bakedScale) > 0.001f)
            {
                // GUI scale changed -> everything must be rebaked
                bakedScale = uiscale;
                BakePulseTexture();
                foreach (var p in active) p.Dirty = true;
            }

            float cardW = CardW;
            float cardH = CardH;
            float gap = (float)GuiElement.scaled(6);
            float margin = (float)GuiElement.scaled(24);
            float baseY = capi.Render.FrameHeight * 0.72f;      // bottom edge of the newest card
            float restX = capi.Render.FrameWidth - margin - cardW;

            for (int i = 0; i < active.Count; i++)
            {
                Popup p = active[i];

                if (p.Dirty) BakeCard(p);

                float targetY = baseY - (active.Count - i) * (cardH + gap);
                if (!p.YInitialized) { p.CurrentY = targetY; p.YInitialized = true; }
                p.CurrentY += (targetY - p.CurrentY) * Math.Min(1f, deltaTime * 12f);

                float fadeIn = GameMath.Clamp((now - p.SpawnMs) / (float)FADE_IN_MS, 0f, 1f);
                float fadeOut = GameMath.Clamp((p.ExpireMs - now) / (float)FADE_OUT_MS, 0f, 1f);
                float alpha = Math.Min(fadeIn, fadeOut);
                float easedIn = 1f - (float)Math.Pow(1f - fadeIn, 3);
                float slideX = (1f - easedIn) * (float)GuiElement.scaled(40);
                float pulse = GameMath.Clamp(1f - (now - p.PulseMs) / (float)PULSE_MS, 0f, 1f);

                DrawCard(p, restX + slideX, p.CurrentY, alpha, pulse);
            }
        }

        private void DrawCard(Popup p, float x, float y, float alpha, float pulse)
        {
            if (alpha <= 0.001f) return;

            // Card body
            capi.Render.Render2DTexture(p.CardTexture.TextureId, x, y, CardW, CardH, Z_CARD, new Vec4f(1, 1, 1, alpha));

            // Merge pulse (bright border overlay)
            if (pulse > 0.001f)
            {
                capi.Render.Render2DTexture(pulseTexture.TextureId, x, y, CardW, CardH, Z_PULSE, new Vec4f(1, 1, 1, alpha * pulse));
            }

            // Icon
            float pad = (float)GuiElement.scaled(8);
            float accent = (float)GuiElement.scaled(3);
            float iconSize = CardH - pad * 2f;
            float iconX = x + pad + accent;
            float iconY = y + pad;
            float inset = (float)GuiElement.scaled(3);

            if (p.IconTextureId != 0)
            {
                capi.Render.Render2DTexture(p.IconTextureId, iconX + inset, iconY + inset,
                    iconSize - inset * 2f, iconSize - inset * 2f, Z_ICON, new Vec4f(1, 1, 1, alpha));
            }
            else if (p.IconSlot != null)
            {
                // RenderItemstackToGui takes the CENTER of the item
                int color = ColorUtil.ToRgba((int)(alpha * 255), 255, 255, 255);
                capi.Render.RenderItemstackToGui(
                    p.IconSlot,
                    iconX + iconSize / 2.0,
                    iconY + iconSize / 2.0,
                    Z_ICON,
                    (iconSize - inset * 2f) * 0.6f,
                    color,
                    shading: true,
                    rotate: false,
                    showStackSize: false);
            }
        }

        private float CardW => (float)GuiElement.scaled(270);
        private float CardH => (float)GuiElement.scaled(58);


        private void DrainIncoming(long now)
        {
            while (incoming.TryDequeue(out XpPopUpPacket? packet))
            {
                string key = $"{packet.DisplayType}|{packet.Source}|{packet.DisplayText}";

                Popup? existing = active.Find(p => p.Key == key && now < p.ExpireMs - FADE_OUT_MS);
                if (existing != null)
                {
                    existing.Xp += packet.XpGained;
                    existing.ExpireMs = now + LIFETIME_MS;
                    existing.PulseMs = now;
                    existing.Dirty = true;
                    continue;
                }

                existing = pending.Find(p => p.Key == key);
                if (existing != null)
                {
                    existing.Xp += packet.XpGained;
                    existing.Dirty = true;
                    continue;
                }

                var popup = new Popup
                {
                    Id = nextId++,
                    Key = key,
                    Packet = packet,
                    Xp = packet.XpGained,
                    DisplayName = ResolveName(packet.DisplayText),
                    CardTexture = new LoadedTexture(capi)
                };
                ResolveIcon(popup);

                if (active.Count < MaxVisible)
                {
                    Activate(popup, now);
                }
                else
                {
                    if (pending.Count >= MAX_PENDING)
                    {
                        pending[0].CardTexture.Dispose();
                        pending.RemoveAt(0);
                    }
                    pending.Add(popup);
                }
            }
        }

        private void RemoveExpired(long now)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (now < active[i].ExpireMs) continue;
                active[i].CardTexture.Dispose();
                active.RemoveAt(i);
            }
        }

        private void PromotePending(long now)
        {
            while (pending.Count > 0 && active.Count < MaxVisible)
            {
                Popup next = pending[0];
                pending.RemoveAt(0);
                Activate(next, now);
            }
        }

        private void Activate(Popup popup, long now)
        {
            popup.SpawnMs = now;
            popup.ExpireMs = now + LIFETIME_MS;
            active.Add(popup);
        }


        private void ResolveIcon(Popup popup)
        {
            XpPopUpPacket packet = popup.Packet;

            if (packet.DisplayType == EnumXpImageSourceType.Image)
            {
                // Source is a texture asset, e.g. "masterylibrary:textures/icons/social.png"
                popup.IconTextureId = capi.Render.GetOrLoadTexture(packet.Source);
                return;
            }

            ItemStack? stack = ResolveStack(packet);
            if (stack != null)
            {
                popup.IconSlot = new DummySlot(stack);
            }
            // otherwise both are empty -> fallback glyph is baked into the card
        }

        private ItemStack? ResolveStack(XpPopUpPacket packet)
        {
            AssetLocation code = packet.Source;
            switch (packet.DisplayType)
            {
                case EnumXpImageSourceType.Item:
                    {
                        Item? item = capi.World.GetItem(code);
                        return item == null || item.IsMissing ? null : new ItemStack(item);
                    }
                case EnumXpImageSourceType.Block:
                    {
                        Block? block = capi.World.GetBlock(code);
                        return block == null || block.IsMissing || block.Id == 0 ? null : new ItemStack(block);
                    }
                case EnumXpImageSourceType.Entity:
                    {
                        Item? egg = capi.World.GetItem(new AssetLocation(code.Domain, "creature-" + code.Path));
                        return egg == null || egg.IsMissing ? null : new ItemStack(egg);
                    }
                default:
                    return null;
            }
        }

        private static double[] Rgba(System.Numerics.Vector4 v, double aMul = 1.0)
            => new[] { (double)v.X, (double)v.Y, (double)v.Z, v.W * aMul };

        private static void SetSource(Context ctx, double[] c) => ctx.SetSourceRGBA(c[0], c[1], c[2], c[3]);

        private static void RoundedRect(Context ctx, double x, double y, double w, double h, double r,
            bool roundLeft = true, bool roundRight = true)
        {
            double rl = roundLeft ? r : 0, rr = roundRight ? r : 0;
            ctx.NewPath();
            ctx.MoveTo(x + rl, y);
            ctx.LineTo(x + w - rr, y);
            if (rr > 0) ctx.Arc(x + w - rr, y + rr, rr, -Math.PI / 2, 0); else ctx.LineTo(x + w, y);
            ctx.LineTo(x + w, y + h - rr);
            if (rr > 0) ctx.Arc(x + w - rr, y + h - rr, rr, 0, Math.PI / 2); else ctx.LineTo(x + w, y + h);
            ctx.LineTo(x + rl, y + h);
            if (rl > 0) ctx.Arc(x + rl, y + h - rl, rl, Math.PI / 2, Math.PI); else ctx.LineTo(x, y + h);
            ctx.LineTo(x, y + rl);
            if (rl > 0) ctx.Arc(x + rl, y + rl, rl, Math.PI, 3 * Math.PI / 2); else ctx.LineTo(x, y);
            ctx.ClosePath();
        }

        private void BakePulseTexture()
        {
            var theme = MasteryLibConfigClient.Loaded.Theme;
            int w = (int)Math.Ceiling(CardW), h = (int)Math.Ceiling(CardH);
            double radius = GuiElement.scaled(4);
            double line = Math.Max(1.5, GuiElement.scaled(2));

            using var surface = new ImageSurface(Format.Argb32, w, h);
            using var ctx = new Context(surface);
            ctx.Operator = Operator.Clear; ctx.Paint(); ctx.Operator = Operator.Over;

            SetSource(ctx, Rgba(theme.BorderBright.ToVector4()));
            ctx.LineWidth = line;
            RoundedRect(ctx, line / 2, line / 2, w - line, h - line, radius);
            ctx.Stroke();

            capi.Gui.LoadOrUpdateCairoTexture(surface, true, ref pulseTexture);
        }

        private void BakeCard(Popup p)
        {
            p.Dirty = false;

            var theme = MasteryLibConfigClient.Loaded.Theme;
            var colBg = Rgba(theme.BackgroundDeep.ToVector4(), 0.92);
            var colPanel = Rgba(theme.BackgroundPanel.ToVector4());
            var colBorder = Rgba(theme.BorderDim.ToVector4());
            var colText = Rgba(theme.TextPrimary.ToVector4());
            var colGold = Rgba(theme.Gold.ToVector4());
            var colGoldDim = Rgba(theme.GoldDim.ToVector4());

            int w = (int)Math.Ceiling(CardW), h = (int)Math.Ceiling(CardH);
            double radius = GuiElement.scaled(4);
            double accentW = GuiElement.scaled(3);
            double pad = GuiElement.scaled(8);

            using var surface = new ImageSurface(Format.Argb32, w, h);
            using var ctx = new Context(surface);
            ctx.Operator = Operator.Clear; ctx.Paint(); ctx.Operator = Operator.Over;

            // Background
            SetSource(ctx, colBg);
            RoundedRect(ctx, 0, 0, w, h, radius);
            ctx.Fill();

            // Border
            SetSource(ctx, colBorder);
            ctx.LineWidth = 1;
            RoundedRect(ctx, 0.5, 0.5, w - 1, h - 1, radius);
            ctx.Stroke();

            // Gold accent bar on the left edge
            ctx.Save();
            RoundedRect(ctx, 0, 0, w, h, radius);
            ctx.Clip();
            SetSource(ctx, colGold);
            ctx.Rectangle(0, 0, accentW, h);
            ctx.Fill();
            ctx.Restore();

            // Icon slot
            double iconSize = h - pad * 2;
            double iconX = pad + accentW, iconY = pad;
            SetSource(ctx, colPanel);
            RoundedRect(ctx, iconX, iconY, iconSize, iconSize, GuiElement.scaled(2));
            ctx.Fill();
            SetSource(ctx, colGoldDim);
            ctx.LineWidth = 1;
            RoundedRect(ctx, iconX + 0.5, iconY + 0.5, iconSize - 1, iconSize - 1, GuiElement.scaled(2));
            ctx.Stroke();

            // Fallback glyph when there is no icon
            if (p.IconTextureId == 0 && p.IconSlot == null)
            {
                string glyph = string.IsNullOrEmpty(p.DisplayName) ? "?" : p.DisplayName.Substring(0, 1).ToUpperInvariant();
                CairoFont gf = CairoFont.WhiteMediumText().WithColor(colText);
                gf.SetupContext(ctx);
                TextExtents ge = ctx.TextExtents(glyph);
                ctx.MoveTo(iconX + iconSize / 2 - ge.Width / 2 - ge.XBearing,
                           iconY + iconSize / 2 - ge.Height / 2 - ge.YBearing);
                SetSource(ctx, colText);
                ctx.ShowText(glyph);
            }

            // Texts
            double textX = iconX + iconSize + pad;
            double textMaxW = w - textX - pad;

            CairoFont nameFont = CairoFont.WhiteSmallText().WithColor(colText);
            nameFont.SetupContext(ctx);
            string name = Truncate(ctx, p.DisplayName, textMaxW);
            SetSource(ctx, colText);
            ctx.MoveTo(textX, h * 0.44);
            ctx.ShowText(name);

            CairoFont xpFont = CairoFont.WhiteSmallText().WithColor(colGold);
            xpFont.SetupContext(ctx);
            string xpText = Lang.Get("masterylib:xppopup-gained", FormatXp(p.Xp));
            SetSource(ctx, colGold);
            ctx.MoveTo(textX, h * 0.78);
            ctx.ShowText(xpText);

            capi.Gui.LoadOrUpdateCairoTexture(surface, true, ref p.CardTexture);
        }

        private static string ResolveName(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return Lang.GetIfExists(text) ?? text;
        }

        private static string FormatXp(float xp) => xp.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static string Truncate(Context ctx, string text, double maxWidth)
        {
            if (ctx.TextExtents(text).Width <= maxWidth) return text;
            const string ellipsis = "...";
            int len = text.Length;
            while (len > 1 && ctx.TextExtents(text.Substring(0, len) + ellipsis).Width > maxWidth) len--;
            return text.Substring(0, len) + ellipsis;
        }
    }
}