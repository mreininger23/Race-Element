﻿using RaceElement.Util.SystemExtensions;
using RaceElement.Data.ACC.Database.Telemetry;
using RaceElement.Data;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using static RaceElement.Data.ACC.Tracks.TrackData;
using ScottPlot.Plottable;

namespace RaceElement.Controls.Telemetry.RaceSessions.Plots;

internal class TyreTempsPlot
{
    private readonly TextBlock _textBlockMetrics;
    private readonly AbstractTrackData _trackData;

    public TyreTempsPlot(AbstractTrackData trackData, ref TextBlock textBlockMetrics)
    {
        _trackData = trackData;
        _textBlockMetrics = textBlockMetrics;
    }

    internal WpfPlot Create(Grid outerGrid, Dictionary<long, TelemetryPoint> dict)
    {
        WpfPlot wpfPlot = new();

        PlotUtil.SetDefaultWpfPlotConfiguration(ref wpfPlot);

        wpfPlot.Height = outerGrid.ActualHeight;
        wpfPlot.MaxHeight = outerGrid.MaxHeight;
        wpfPlot.MinHeight = outerGrid.MinHeight;
        outerGrid.SizeChanged += (se, ev) =>
        {
            wpfPlot.Height = outerGrid.ActualHeight;
            wpfPlot.MaxHeight = outerGrid.MaxHeight;
            wpfPlot.MinHeight = outerGrid.MinHeight;
        };

        Plot plot = wpfPlot.Plot;
        plot.Palette = PlotUtil.WheelPositionPallete;
        plot.Benchmark(false);

        double[][] tyreTemps = new double[4][];
        double minTemp = int.MaxValue;
        double maxTemp = int.MinValue;
        double[] splines = dict.Select(x => (double)x.Value.SplinePosition * _trackData.TrackLength).ToArray();

        if (splines.Length == 0)
            return wpfPlot;

        SignalPlotXY[] tyrePlots = new SignalPlotXY[4];
        string fourSpaces = "".FillEnd(4, ' ');
        for (int i = 0; i < 4; i++)
        {
            var temps = dict.Select(x => (double)x.Value.TyreData.TyreCoreTemperature[i]);

            _textBlockMetrics.Text += $"Av. {Enum.GetNames(typeof(SetupConverter.Wheel))[i]}: {temps.Average():F2}{fourSpaces}";
            tyreTemps[i] = temps.ToArray();

            minTemp.ClipMax(tyreTemps[i].Min());
            maxTemp.ClipMin(tyreTemps[i].Max());

            tyrePlots[i] = plot.AddSignalXY(splines, tyreTemps[i], label: Enum.GetNames(typeof(SetupConverter.Wheel))[i]);
        }

        double padding = 2;
        plot.SetAxisLimitsX(xMin: 0, xMax: _trackData.TrackLength);
        plot.SetAxisLimitsY(minTemp - padding, maxTemp + padding);
        plot.XAxis.SetBoundary(0, _trackData.TrackLength);
        plot.YAxis.SetBoundary(minTemp - padding, maxTemp + padding);
        plot.XLabel("Meters");
        plot.YLabel("Celsius");


        #region add markers

        MarkerPlot[] tyreMarkers = new MarkerPlot[4];
        for (int i = 0; i != tyreMarkers.Length; i++)
        {
            tyreMarkers[i] = wpfPlot.Plot.AddPoint(0, 0, color: tyrePlots[i].Color);
            PlotUtil.SetDefaultMarkerStyle(ref tyreMarkers[i]);
        }

        outerGrid.MouseMove += (s, e) =>
        {
            (double mouseCoordsX, _) = wpfPlot.GetMouseCoordinates();

            for (int i = 0; i != tyreMarkers.Length; i++)
            {
                (double x, double y, int index) = tyrePlots[i].GetPointNearestX(mouseCoordsX);
                tyreMarkers[i].SetPoint(x, y);
                tyreMarkers[i].IsVisible = true;
                tyrePlots[i].Label = $"{Enum.GetNames(typeof(SetupConverter.Wheel))[i]}: {tyreTemps[i][index]:F3}";

                PlotUtil.MarkerIndex = index;
            }

            wpfPlot.RenderRequest();
        };

        #endregion

        wpfPlot.AxesChanged += PlotUtil.WpfPlot_AxesChanged;
        if (PlotUtil.AxisLimitsCustom)
            plot.SetAxisLimits(xAxisIndex: 0, xMin: PlotUtil.AxisLimits.XMin, xMax: PlotUtil.AxisLimits.XMax);

        PlotUtil.SetDefaultPlotStyles(ref plot);
        PlotUtil.AddCorners(ref wpfPlot, _trackData);

        wpfPlot.RenderRequest();

        return wpfPlot;
    }
}
