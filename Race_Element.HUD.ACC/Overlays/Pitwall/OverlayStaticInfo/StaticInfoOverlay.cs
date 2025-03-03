﻿using RaceElement.HUD.Overlay.Internal;
using RaceElement.HUD.Overlay.OverlayUtil;
using RaceElement.HUD.Overlay.Util;
using RaceElement.Util;
using System;
using System.Drawing;
using System.Reflection;
using RaceElement.HUD.ACC.Overlays.OverlayDebugInfo;
using static RaceElement.HUD.ACC.Overlays.OverlayDebugInfo.DebugInfoHelper;
using RaceElement.HUD.Overlay.OverlayUtil.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Drawing2D;

namespace RaceElement.HUD.ACC.Overlays.OverlayStaticInfo;

[Overlay(Name = "Static Info", Version = 1.00,
    Description = "Shared Memory Static Page", OverlayType = OverlayType.Pitwall)]
internal sealed class StaticInfoOverlay : AbstractOverlay
{
    private readonly DebugConfig _config = new();

    private GraphicsGrid _graphicsGrid;
    private readonly List<string> fieldNames = new();

    public StaticInfoOverlay(Rectangle rectangle) : base(rectangle, "Static Info")
    {
        this.AllowReposition = false;
        this.RefreshRateHz = 5;
    }

    private void Instance_WidthChanged(object sender, bool e)
    {
        if (e)
            this.X = DebugInfoHelper.Instance.GetX(this);
    }

    public sealed override void BeforeStart()
    {
        Font font = FontUtil.FontSegoeMono(8 * Scale);
        float fontHeight = font.GetHeight(120);
        int columnHeight = (int)fontHeight - 1;

        int valueWidth = (int)Math.Ceiling(150 * Scale);

        FieldInfo[] fields = pageStatic.GetType().GetFields();

        foreach (FieldInfo member in fields)
        {
            bool isObsolete = false;
            foreach (CustomAttributeData cad in member.CustomAttributes)
                if (cad.AttributeType == typeof(ObsoleteAttribute)) isObsolete = true;

            if (!isObsolete && !member.Name.Equals("Buffer") && !member.Name.Equals("Size"))
                fieldNames.Add(member.Name);
        }
        int rows = fieldNames.Count;

        int maxNameLength = (int)Math.Ceiling(fieldNames.Max(x => x.Length) * font.SizeInPoints);

        _graphicsGrid = new GraphicsGrid(rows, 2);

        Color color = Color.FromArgb(230, Color.Black);
        using HatchBrush hatchBrush = new(HatchStyle.LightUpwardDiagonal, color, Color.FromArgb(color.A - 75, color));

        for (int row = 0; row < rows; row++)
        {
            DrawableTextCell headerCell = new(new RectangleF(0, columnHeight * row, maxNameLength, columnHeight), font);
            headerCell.CachedBackground.SetRenderer(g => g.FillRoundedRectangle(hatchBrush, new Rectangle(0, 0, (int)headerCell.Rectangle.Width, (int)headerCell.Rectangle.Height), (int)(3 * Scale)));
            headerCell.StringFormat.Alignment = StringAlignment.Near;
            headerCell.UpdateText(fieldNames[row]);
            _graphicsGrid.Grid[row][0] = headerCell;

            DrawableTextCell valueCell = new(new RectangleF(headerCell.Rectangle.Width, columnHeight * row, valueWidth, columnHeight), font);
            valueCell.CachedBackground.SetRenderer(g => g.FillRoundedRectangle(hatchBrush, new Rectangle(0, 0, (int)valueCell.Rectangle.Width, (int)valueCell.Rectangle.Height), (int)(3 * Scale)));
            valueCell.StringFormat.Alignment = StringAlignment.Far;

            _graphicsGrid.Grid[row][1] = valueCell;
        }

        Height = rows * columnHeight;
        Width = maxNameLength + valueWidth;

        if (this._config.Dock.Undock)
            this.AllowReposition = true;
        else
        {
            DebugInfoHelper.Instance.WidthChanged += Instance_WidthChanged;
            DebugInfoHelper.Instance.AddOverlay(this);
            this.X = DebugInfoHelper.Instance.GetX(this);
            this.Y = 0;
        }
    }

    public sealed override void BeforeStop()
    {
        if (!this._config.Dock.Undock)
        {
            DebugInfoHelper.Instance.RemoveOverlay(this);
            DebugInfoHelper.Instance.WidthChanged -= Instance_WidthChanged;
        }
        _graphicsGrid?.Dispose();
    }

    public sealed override void Render(Graphics g)
    {
        int rowCount = 0;
        foreach (var member in pageStatic.GetType().GetFields().Where(member => fieldNames.Contains(member.Name)))
        {
            var value = member.GetValue(pageStatic);
            value = ReflectionUtil.FieldTypeValue(member, value);
            DrawableTextCell cell = (DrawableTextCell)_graphicsGrid.Grid[rowCount][1];
            cell.UpdateText($"{value}");
            rowCount++;
        }

        _graphicsGrid.Draw(g);
    }

    public sealed override bool ShouldRender() => true;
}
