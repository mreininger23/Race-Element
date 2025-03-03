﻿using RaceElement.HUD.Overlay.Configuration;

namespace RaceElement.HUD.ACC.Overlays.Driving.OverlayLapDeltaTrace;

internal sealed class LapDeltaTraceConfiguration : OverlayConfiguration
{
    [ConfigGrouping("Chart", "Customize the charts refresh rate, data points or change the max amount of delta time.")]
    public ChartGrouping Chart { get; init; } = new ChartGrouping();
    public class ChartGrouping
    {
        [ToolTip("The amount of datapoints shown, this changes the width of the overlay.")]
        [IntRange(50, 800, 10)]
        public int Width { get; init; } = 300;

        [ToolTip("The amount of datapoints shown, this changes the width of the overlay.")]
        [IntRange(80, 250, 10)]
        public int Height { get; init; } = 120;

        [ToolTip("Set the thickness of the lines in the chart.")]
        [IntRange(1, 4, 1)]
        public int LineThickness { get; init; } = 1;

        [ToolTip("Sets the maximum amount of delta displayed.")]
        [FloatRange(0.5f, 3f, 0.5f, 1)]
        public float MaxDelta { get; init; } = 1f;

        [ToolTip("Sets the data collection rate, this does affect cpu usage at higher values.")]
        [IntRange(5, 20, 1)]
        public int Herz { get; init; } = 5;

        [ToolTip("Show horizontal grid lines.")]
        public bool GridLines { get; init; } = true;

        [ToolTip("Show the lap delta trace when spectating other cars.")]
        public bool Spectator { get; init; } = true;

        [ToolTip("Hide the Lap Delta Trace HUD during a Race session.")]
        public bool HideForRace { get; init; } = false;
    }

    [ConfigGrouping("Preview", "Customize the data for the preview image.")]
    public PreviewGrouping Preview { get; init; } = new PreviewGrouping();
    public class PreviewGrouping
    {
        [ToolTip("Data seed for the generated data in the preview image above.")]
        [IntRange(1, 500, 1)]
        public int Seed { get; init; } = 122;
    }

    public LapDeltaTraceConfiguration()
    {
        this.AllowRescale = true;
    }
}
