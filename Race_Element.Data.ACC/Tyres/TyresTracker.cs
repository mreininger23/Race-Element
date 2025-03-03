using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RaceElement.Util;
using static RaceElement.Data.SetupConverter;

namespace RaceElement.Data.ACC.Tyres;

/// <summary>
/// Tracks changes in tyre pressure like pressure loss but is aware of tyre sets.   
/// <br/><br/>
/// <i>Originally written by Mominon in abc2823be1740d7a8c911f34880bc7a01753fba2 -	Add TyresTracker to track pressure losses</i>
/// </summary>
public class TyresTracker
{
    public class TyresInfo
    {
        public float[] PressureLoss { get; internal set; }
    }

    private static TyresTracker _instance;

    public static TyresTracker Instance
    {
        get { return _instance ??= new TyresTracker(); }
    }

    public event EventHandler<TyresInfo> OnTyresInfoChanged;

    private bool _isTracking;
    private float[] _lastPressureReading;
    private float[] _lastPressureLosses = [0, 0, 0, 0];
    private int _lastTyreSetIndexValue;
    private bool _isInPitLane = false;

    private TyresTracker()
    {
        if (!_isTracking)
            Start();
    }

    public static TyresInfo GetPreviewTyresInfo()
    {
        return new TyresInfo
        {
            PressureLoss = [0.12f, 0.23f, 0.34f, 0.45f]
        };
    }

    private void Start()
    {
        if (_isTracking)
            return;

        _isTracking = true;
        new Thread(x =>
        {
            ResetPressureLosses();
            SendTyresInfoUpdate();

            while (_isTracking)
            {
                try
                {
                    Thread.Sleep(100);

                    var physicsPage = ACCSharedMemory.Instance.ReadPhysicsPageFile();
                    float[] currentReading = physicsPage.WheelPressure;

                    var graphicsPage = ACCSharedMemory.Instance.ReadGraphicsPageFile();
                    int currentTyreSetIndex = graphicsPage.currentTyreSet;
                    CheckTyreSetChange(currentTyreSetIndex);
                    CheckWetTyreSetChange(graphicsPage);

                    if (graphicsPage.IsSetupMenuVisible)
                    {
                        ResetPressureLosses();
                        SendTyresInfoUpdate();
                    }
                    else
                    {
                        _isInPitLane = graphicsPage.IsInPitLane;
                        if (!_isInPitLane)
                            UpdatePressureLosses(currentReading);
                    }

                    _lastPressureReading = currentReading;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"TyresTracker: {ex}");
                    LogWriter.WriteToLog($"TyresTracker: {ex}");
                }
            }

            _instance = null;
            _isTracking = false;
        }).Start();
    }

    internal void Stop()
    {
        _isTracking = false;
    }

    private void ResetPressureLosses()
    {
        _lastPressureLosses = [0, 0, 0, 0];
    }

    private void SendTyresInfoUpdate()
    {
        TyresInfo tyresInfo = new()
        {
            PressureLoss = _lastPressureLosses
        };

        // Debug.WriteLine($"TyresTracker: TyresInfo changed: | PressureLoss: [{string.Join(", ", tyresInfo.PressureLoss)}]");

        OnTyresInfoChanged?.Invoke(this, tyresInfo);
    }

    private void UpdatePressureLosses(IEnumerable<float> currentReading)
    {
        if (_lastPressureReading == null)
            return;

        float[] pressureVariation = _lastPressureReading
            .Zip(currentReading, (last, current) => current - last)
            .ToArray();

        if (pressureVariation.Sum() == 0)
            return;

        // Debug.WriteLine($"TyresTracker: Pressure variation: [{string.Join(", ", pressureVariation)}]");

        bool hasLostPressure = false;
        foreach (Wheel wheel in Enum.GetValues(typeof(Wheel)))
        {
            int wheelIndex = (int)wheel;
            if (!(pressureVariation[wheelIndex] <= -0.03f) || !(pressureVariation[wheelIndex] > -3f))
                continue;

            _lastPressureLosses[wheelIndex] += pressureVariation[wheelIndex] * -1;
            hasLostPressure = true;
        }

        if (hasLostPressure)
            SendTyresInfoUpdate();
    }

    private void CheckTyreSetChange(int currentTyreSetIndex)
    {
        var hasTyreSetChanged = _lastTyreSetIndexValue != currentTyreSetIndex;
        if (!hasTyreSetChanged)
            return;

        Debug.WriteLine($"TyresTracker: Tyre set has changed from [{_lastTyreSetIndexValue}] to [{currentTyreSetIndex}]");

        ResetPressureLosses();
        SendTyresInfoUpdate();
        _lastTyreSetIndexValue = currentTyreSetIndex;
    }

    private void CheckWetTyreSetChange(ACCSharedMemory.SPageFileGraphic graphicsPage)
    {
        bool hasLeftPitlane = _isInPitLane && !graphicsPage.IsInPitLane;

        if (graphicsPage.TyreCompound != "wet_compound" || !hasLeftPitlane)
            return;

        // Workaround as wet tyre sets have no index to check
        Debug.WriteLine("TyresTracker: Reset pressure loss as leaving pit lane with a wet set on is considered as a new set");
        ResetPressureLosses();
        SendTyresInfoUpdate();
    }
}
