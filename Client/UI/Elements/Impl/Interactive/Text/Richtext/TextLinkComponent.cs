﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;

namespace Vintagestory.API.Client
{
    public class LinkTextComponent : RichTextComponent
    {
        Action<LinkTextComponent> onLinkClicked;

        public string Href;

        LoadedTexture normalText;
        LoadedTexture hoverText;

        /// <summary>
        /// A text component with an embedded link.
        /// </summary>
        /// <param name="displayText">The text of the Text.</param>
        /// <param name="url">The link in the text.</param>
        public LinkTextComponent(ICoreClientAPI api, string displayText, CairoFont font, Action<LinkTextComponent> onLinkClicked) : base(api, displayText, font)
        {
            this.onLinkClicked = onLinkClicked;
            MouseOverCursor = "linkselect";

            this.font = this.font.Clone().WithColor(GuiStyle.ActiveButtonTextColor);

            hoverText = new LoadedTexture(api);
            normalText = new LoadedTexture(api);
        }

        double leftMostX;
        double topMostY;


        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
        {
            leftMostX = 999999;
            topMostY = 999999;
            double rightMostX = 0;
            double bottomMostY = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                TextLine line = lines[i];

                leftMostX = Math.Min(leftMostX, line.Bounds.X);
                topMostY = Math.Min(topMostY, line.Bounds.Y);

                rightMostX = Math.Max(rightMostX, line.Bounds.X + line.Bounds.Width);
                bottomMostY = Math.Max(bottomMostY, line.Bounds.Y + line.Bounds.Height);
            }

            ImageSurface surface = new ImageSurface(Format.Argb32, (int)(rightMostX - leftMostX), (int)(bottomMostY - topMostY));
            Context ctx = new Context(surface);
            ctx.SetSourceRGBA(0, 0, 0, 0);
            ctx.Paint();

            ctx.Save();
            Matrix m = ctx.Matrix;
            m.Translate((int)-leftMostX, (int)-topMostY);
            ctx.Matrix = m;
            
            CairoFont normalFont = this.font;

            ComposeFor(ctx, surface);
            api.Gui.LoadOrUpdateCairoTexture(surface, false, ref normalText);

            ctx.Operator = Operator.Clear;
            ctx.SetSourceRGBA(0, 0, 0, 0);
            ctx.Paint();
            ctx.Operator = Operator.Over;

            this.font = this.font.Clone();
            this.font.Color[0] = Math.Min(1, this.font.Color[0] * 1.2);
            this.font.Color[1] = Math.Min(1, this.font.Color[1] * 1.2);
            this.font.Color[2] = Math.Min(1, this.font.Color[2] * 1.2);
            ComposeFor(ctx, surface);
            this.font = normalFont;

            ctx.Restore();
            
            api.Gui.LoadOrUpdateCairoTexture(surface, false, ref hoverText);
            
            surface.Dispose();
            ctx.Dispose();
        }

        void ComposeFor(Context ctx, ImageSurface surface)
        { 
            textUtil.DrawMultilineText(ctx, font, lines, EnumTextOrientation.Left);

            ctx.LineWidth = 1;
            ctx.SetSourceRGBA(font.Color);

            for (int i = 0; i < lines.Length; i++)
            {
                TextLine line = lines[i];
                ctx.MoveTo(line.Bounds.X + line.PaddingLeft, line.Bounds.Y + line.Bounds.AscentOrHeight + 2);
                ctx.LineTo(line.Bounds.X + line.PaddingLeft - line.PaddingRight + line.Bounds.Width, line.Bounds.Y + line.Bounds.AscentOrHeight + 2);
                ctx.Stroke();
            }
        }

        public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY)
        {
            base.RenderInteractiveElements(deltaTime, renderX, renderY);
            bool isHover = false;

            foreach (var val in BoundsPerLine)
            {
                if (val.PointInside(api.Input.MouseX - renderX, api.Input.MouseY - renderY))
                {
                    isHover = true;
                    break;
                }
            }

            api.Render.Render2DTexturePremultipliedAlpha(
                isHover ? hoverText.TextureId : normalText.TextureId, 
                (int)(renderX + leftMostX), 
                (int)(renderY + topMostY), 
                hoverText.Width, hoverText.Height, 50
            );
        }

        public override void OnMouseUp(MouseEvent args)
        {
            foreach (var val in BoundsPerLine)
            {
                if (val.PointInside(args.X, args.Y))
                {
                    args.Handled = true;
                    Trigger();                    
                }
            }
        }

        public LinkTextComponent SetHref(string href)
        {
            this.Href = href;
            return this;
        }

        public void Trigger()
        {
            if (onLinkClicked == null)
            {
                if (Href.StartsWith("hotkey://"))
                {
                    api.Input.GetHotKeyByCode(Href.Substring("hotkey://".Length))?.Handler?.Invoke(null);
                }
                else
                {
                    string[] parts = Href.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && api.LinkProtocols != null && api.LinkProtocols.ContainsKey(parts[0]))
                    {
                        api.LinkProtocols[parts[0]].Invoke(this);
                        return;
                    }

                    if (parts.Length > 0)
                    {
                        if (parts[0].StartsWith("http") || parts[0].StartsWith("https"))
                        {
                            api.Gui.OpenLink(Href);
                        }
                    }
                }
            }
            else
            {
                onLinkClicked.Invoke(this);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            hoverText?.Dispose();
            normalText?.Dispose();
        }
    }
}
