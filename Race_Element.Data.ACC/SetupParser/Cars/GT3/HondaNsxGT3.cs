﻿using System;
using System.Collections.Generic;
using static RaceElement.Data.ConversionFactory;
using static RaceElement.Data.SetupConverter;

namespace RaceElement.Data.Cars.GT3;

internal class HondaNsxGT3 : ICarSetupConversion
{
    public CarModels CarModel => CarModels.Honda_NSX_GT3_2017;

    CarClasses ICarSetupConversion.CarClass => CarClasses.GT3;
    public DryTyreCompounds DryTyreCompound => DryTyreCompounds.DHF2023;

    AbstractTyresSetup ICarSetupConversion.TyresSetup => new TyreSetup();
    private class TyreSetup : AbstractTyresSetup
    {
        public override double Camber(Wheel wheel, List<int> rawValue)
        {
            switch (GetPosition(wheel))
            {
                case Position.Front: return Math.Round(-4 + 0.1 * rawValue[(int)wheel], 2);
                case Position.Rear: return Math.Round(-3.5 + 0.1 * rawValue[(int)wheel], 2);
                default: return -1;
            }
        }

        private readonly double[] casters = [ 8.8, 9.0, 9.2, 9.4, 9.6, 9.8, 10.0, 10.2, 10.4, 10.6,
                10.8, 10.9, 11.1, 11.3, 11.5, 11.7, 11.9, 12.1, 12.3, 12.4, 12.6, 12.8, 13.0, 13.2,
                13.4, 13.6, 13.8, 13.9, 14.1, 14.3, 14.5, 14.7, 14.9, 15.0, 15.2 ];
        public override double Caster(int rawValue)
        {
            return Math.Round(casters[rawValue], 2);
        }

        public override double Toe(Wheel wheel, List<int> rawValue)
        {
            return Math.Round(-0.4 + 0.01 * rawValue[(int)wheel], 2);
        }
    }

    IMechanicalSetup ICarSetupConversion.MechanicalSetup => new MechSetup();
    private class MechSetup : IMechanicalSetup
    {
        public int AntiRollBarFront(int rawValue)
        {
            return rawValue;
        }

        public int AntiRollBarRear(int rawValue)
        {
            return rawValue;
        }

        public double BrakeBias(int rawValue)
        {
            return Math.Round(50 + 0.2 * rawValue, 2);
        }

        public int BrakePower(int rawValue)
        {
            return 80 + rawValue;
        }

        public int BumpstopRange(List<int> rawValue, Wheel wheel)
        {
            return rawValue[(int)wheel];
        }

        public int BumpstopRate(List<int> rawValue, Wheel wheel)
        {
            return 300 + 100 * rawValue[(int)wheel];
        }

        public int PreloadDifferential(int rawValue)
        {
            return 20 + rawValue * 10;
        }

        public double SteeringRatio(int rawValue)
        {
            return Math.Round(10d + rawValue, 2);
        }

        private readonly int[] fronts = [ 115000, 124000, 133000, 142000, 151000,
            160000, 169000, 178000, 187000, 196000 ];
        private readonly int[] rears = [ 115000, 124000, 133000, 142000, 151000,
            160000, 169000, 178000, 187000, 196000, 205000 ];
        public int WheelRate(List<int> rawValue, Wheel wheel)
        {
            switch (GetPosition(wheel))
            {
                case Position.Front: return fronts[rawValue[(int)wheel]];
                case Position.Rear: return rears[rawValue[(int)wheel]];
                default: return -1;
            }
        }
    }

    IDamperSetup ICarSetupConversion.DamperSetup => DefaultDamperSetup;

    IAeroBalance ICarSetupConversion.AeroBalance => new AeroSetup();
    private class AeroSetup : IAeroBalance
    {
        public int BrakeDucts(int rawValue)
        {
            return rawValue;
        }

        public int RearWing(int rawValue)
        {
            return rawValue;
        }

        public int RideHeight(List<int> rawValue, Position position)
        {
            switch (position)
            {
                case Position.Front: return 54 + rawValue[0];
                case Position.Rear: return 54 + rawValue[2];
                default: return -1;
            }
        }

        public int Splitter(int rawValue)
        {
            return rawValue;
        }
    }
}
