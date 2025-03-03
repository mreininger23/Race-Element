﻿using RaceElement.Data.ACC.Database.Telemetry;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using static RaceElement.Data.ACC.Tracks.TrackData;
using System.Windows.Controls;
using ScottPlot.Drawing;
using ScottPlot.SnapLogic;
using System.Drawing;

namespace RaceElement.Controls.Telemetry.RaceSessions.Plots;

internal class TractionCirclePlot
{
    private readonly TextBlock _textBlockMetrics;
    private readonly AbstractTrackData _trackData;

    public TractionCirclePlot(AbstractTrackData trackData, ref TextBlock textBlockMetrics)
    {
        _trackData = trackData;
        _textBlockMetrics = textBlockMetrics;
    }

    internal WpfPlot Create(Grid outerGrid, Dictionary<long, TelemetryPoint> dict)
    {
        WpfPlot wpfPlot = new();

        PlotUtil.SetDefaultWpfPlotConfiguration(ref wpfPlot);

        wpfPlot.Configuration.Pan = false;
        wpfPlot.Configuration.Zoom = false;
        wpfPlot.Configuration.ScrollWheelZoom = false;
        wpfPlot.Configuration.LeftClickDragPan = false;

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
        plot.Benchmark(false);
        plot.Grid(true, color: Color.FromArgb(2, Color.White), lineStyle: LineStyle.Dot, onTop: true);
        plot.XAxis.TickMarkDirection(false);
        plot.YAxis.TickMarkDirection(false);
        plot.XAxis.TickLabelNotation(multiplier: true);


        if (dict.First().Value.PhysicsData.Acceleration == null)
            return wpfPlot;

        double[] lateralAcceleration;
        double[] longAcceleration;


        Dictionary<long, TelemetryPoint> filteredDict = dict;
        int filteredFirstIndex = 0;
        if (PlotUtil.AxisLimitsCustom)
        {
            filteredDict = dict.Where(x =>
            {
                double trackPosition = PlotUtil.trackData.TrackLength * x.Value.SplinePosition;
                return trackPosition < PlotUtil.AxisLimits.XMax && trackPosition > (PlotUtil.AxisLimits.XMin);
            }).ToDictionary(x => x.Key, x => x.Value);
            filteredFirstIndex = dict.ToList().IndexOf(filteredDict.First());

            PlotUtil.UpdateAxisLimits(wpfPlot, PlotUtil.AxisLimits);

            lateralAcceleration = filteredDict.Select(x => (double)x.Value.PhysicsData.Acceleration[0]).ToArray();
            longAcceleration = filteredDict.Select(x => (double)x.Value.PhysicsData.Acceleration[1]).ToArray();
        }
        else
        {
            lateralAcceleration = dict.Select(x => (double)x.Value.PhysicsData.Acceleration[0]).ToArray();
            longAcceleration = dict.Select(x => (double)x.Value.PhysicsData.Acceleration[1]).ToArray();
        }


        for (int i = 0; i < lateralAcceleration.Length; i++)
        {
            double x = lateralAcceleration[i];
            double y = longAcceleration[i];
            double colorFraction = Math.Sqrt(x * x + y * y) / 2.5;
            var color = Colormap.Jet.GetColor(colorFraction);

            plot.AddPoint(x, y, color, 3);
        }

        // add convex hull of points
        List<PointF> points = filteredDict.Select(x => new PointF(x.Value.PhysicsData.Acceleration[0], x.Value.PhysicsData.Acceleration[1])).ToList();
        List<PointF> convexPoints = ConvexHull.GetConvexHull(points);
        plot.AddPolygon(convexPoints.Select(x => (double)x.X).ToArray(), convexPoints.Select(x => (double)x.Y).ToArray(), fillColor: Color.FromArgb(12, Color.White));

        plot.AddHorizontalLine(0, Color.White);
        plot.AddVerticalLine(0, Color.White);

        var tractionMarker = wpfPlot.Plot.AddMarkerDraggable(lateralAcceleration[0], longAcceleration[0], size: 20, color: System.Drawing.Color.OrangeRed, shape: MarkerShape.openCircle);
        tractionMarker.MarkerLineWidth = 3;
        tractionMarker.IsVisible = false;
        tractionMarker.Dragged += (s, e) =>
        {
            var coords = wpfPlot.GetMouseCoordinates();

            int index = new Nearest2D(lateralAcceleration, longAcceleration).SnapIndex(new Coordinate(coords.x, coords.y));
            tractionMarker.SetPoint(lateralAcceleration[index], longAcceleration[index]);
            tractionMarker.IsVisible = true;

            PlotUtil.MarkerIndex = filteredFirstIndex + index;
            tractionMarker.Label = $"Lat: {lateralAcceleration[index]:F3}, Long: {longAcceleration[index]:F3}";
        };
        wpfPlot.MouseLeftButtonUp += (s, e) =>
        {
            var coords = wpfPlot.GetMouseCoordinates();

            int index = new Nearest2D(lateralAcceleration, longAcceleration).SnapIndex(new Coordinate(coords.x, coords.y));
            tractionMarker.SetPoint(lateralAcceleration[index], longAcceleration[index]);
            tractionMarker.IsVisible = true;

            PlotUtil.MarkerIndex = filteredFirstIndex + index;
            tractionMarker.Label = $"Lat: {lateralAcceleration[index]:F3}, Long: {longAcceleration[index]:F3}";
        };



        plot.XAxis.Label("Lateral Acceleration");
        plot.SetAxisLimitsX(-3, 3);
        plot.XAxis.LockLimits(true);

        plot.YAxis.Label("Longitudinal Acceleration");
        plot.SetAxisLimitsY(-2.5, 1.5);
        plot.YAxis.LockLimits(true);

        PlotUtil.SetDefaultPlotStyles(ref plot);

        wpfPlot.RenderRequest();

        return wpfPlot;
    }


    private class ConvexHull
    {
        private static float Cross(PointF O, PointF A, PointF B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }

        public static List<PointF> GetConvexHull(List<PointF> points)
        {
            if (points == null)
                return null;

            if (points.Count() <= 1)
                return points;

            int n = points.Count(), k = 0;
            List<PointF> H = new(new PointF[2 * n]);

            points.Sort((a, b) =>
                 a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

            // Build lower hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && Cross(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }

            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }

            return H.Take(k - 1).ToList();
        }
    }
}
