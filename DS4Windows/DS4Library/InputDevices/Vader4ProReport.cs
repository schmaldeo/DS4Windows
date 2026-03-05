using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vader4ProReader.Device
{
    public readonly struct Vader4ProReport
    {
        [Flags]
        enum ButtonCollection0 : byte
        {
            C = 1,
            Z = 2,
            M1 = 4,
            M2 = 8,
            M3 = 16,
            M4 = 32
        }

        [Flags]
        enum ButtonCollection1 : byte
        {
            FN = 1,
            HOME = 8,
        }

        [Flags]
        enum ButtonCollection2 : byte
        {
            DPadUp = 1,
            DPadRight = 2,
            DPadDown = 4,
            DPadLeft = 8,
            A = 16,
            B = 32,
            Select = 64,
            X = 128
        }
        [Flags]
        enum ButtonCollection3 : byte
        {
            Y = 1,
            Start = 2,
            LB = 4,
            RB = 8,
            LT = 16,
            RT = 32,
            LS = 64,
            RS = 128
        }

        /*
         * byte offset | description
         * ------------|----------------
         * 0           | Report ID
         * 1           | unknown (0xFE)
         * 2           | unknown (0x66)
         * 3           | Air Mouse active flag(0x80)
         * 4           | legacy yaw (low 8 bits)
         * 5           | legacy pitch (low 4 bits) << 4 | legacy yaw (high 4 bits)
         * 6           | legacy pitch (high 8 bits)
         * 7           | buttons0
         * 8           | buttons1
         * 9           | buttons2
         * 10          | buttons3
         * 11-12       | AccelXRaw
         * 13-14       | AccelZRaw
         * 15-16       | AccelYRaw
         * 17          | LS_X
         * 18,20       | YawRaw (-512~512)
         * 19          | LS_Y
         * 21          | RS_X
         * 22          | RS_Y
         * 23          | LT
         * 24          | RT
         * 25          | unknown (0x00)
         * 26-27       | PitchRaw (-512~512)
         * 28          | unknown (0x00)
         * 29-30       | RollRaw
         * 31          | unknown (0x00)
         */
        private readonly Memory<byte> rawReport;

        public Vader4ProReport(Memory<byte> rawReport)
        {
            if (rawReport.Length != 32)
                throw new ArgumentException("Invalid report length", nameof(rawReport));
            this.rawReport = rawReport;
        }

        private ButtonCollection0 buttons0 => (ButtonCollection0)rawReport.Span[7];
        private ButtonCollection1 buttons1 => (ButtonCollection1)rawReport.Span[8];
        private ButtonCollection2 buttons2 => (ButtonCollection2)rawReport.Span[9];
        private ButtonCollection3 buttons3 => (ButtonCollection3)rawReport.Span[10];

        public bool IsCPressed => buttons0.HasFlag(ButtonCollection0.C);
        public bool IsZPressed => buttons0.HasFlag(ButtonCollection0.Z);
        public bool IsM1Pressed => buttons0.HasFlag(ButtonCollection0.M1);
        public bool IsM2Pressed => buttons0.HasFlag(ButtonCollection0.M2);
        public bool IsM3Pressed => buttons0.HasFlag(ButtonCollection0.M3);
        public bool IsM4Pressed => buttons0.HasFlag(ButtonCollection0.M4);

        public bool IsFNPressed => buttons1.HasFlag(ButtonCollection1.FN);
        public bool IsHOMEPressed => buttons1.HasFlag(ButtonCollection1.HOME);

        public bool IsDPadUpPressed => buttons2.HasFlag(ButtonCollection2.DPadUp);
        public bool IsDPadRightPressed => buttons2.HasFlag(ButtonCollection2.DPadRight);
        public bool IsDPadDownPressed => buttons2.HasFlag(ButtonCollection2.DPadDown);
        public bool IsDPadLeftPressed => buttons2.HasFlag(ButtonCollection2.DPadLeft);
        public bool IsAPressed => buttons2.HasFlag(ButtonCollection2.A);
        public bool IsBPressed => buttons2.HasFlag(ButtonCollection2.B);
        public bool IsSelectPressed => buttons2.HasFlag(ButtonCollection2.Select);
        public bool IsXPressed => buttons2.HasFlag(ButtonCollection2.X);

        public bool IsYPressed => buttons3.HasFlag(ButtonCollection3.Y);
        public bool IsStartPressed => buttons3.HasFlag(ButtonCollection3.Start);
        public bool IsLBPressed => buttons3.HasFlag(ButtonCollection3.LB);
        public bool IsRBPressed => buttons3.HasFlag(ButtonCollection3.RB);
        public bool IsLTPressed => buttons3.HasFlag(ButtonCollection3.LT);
        public bool IsRTPressed => buttons3.HasFlag(ButtonCollection3.RT);
        public bool IsLSPressed => buttons3.HasFlag(ButtonCollection3.LS);
        public bool IsRSPressed => buttons3.HasFlag(ButtonCollection3.RS);

        public byte LS_X => rawReport.Span[17];
        public byte LS_Y => rawReport.Span[19];

        public byte RS_X => rawReport.Span[21];
        public byte RS_Y => rawReport.Span[22];

        public byte LT => rawReport.Span[23];
        public byte RT => rawReport.Span[24];

        public short YawRaw => BitConverter.ToInt16([rawReport.Span[18], rawReport.Span[20]]);
        public short PitchRaw => BitConverter.ToInt16(rawReport.Span[26..28]);
        public short RollRaw => BitConverter.ToInt16(rawReport.Span[29..31]);

        // LSM6DS* gyroscope max range: +/- 2000 dps

        public float YawCalibrated => YawRaw << 6;
        public float PitchCalibrated => PitchRaw << 6;
        public float RollCalibrated => RollRaw;

        public short AccelXRaw => BitConverter.ToInt16(rawReport.Span[11..13]);
        public short AccelYRaw => BitConverter.ToInt16(rawReport.Span[15..17]);
        public short AccelZRaw => BitConverter.ToInt16(rawReport.Span[13..15]);


        public short AccelXCalibrated => (short)(AccelXRaw << 5);
        public short AccelYCalibrated => (short)(AccelYRaw << 5);
        public short AccelZCalibrated => (short)(AccelZRaw << 5);

        public bool IsAirMouseActive => (rawReport.Span[3] & 128) != 0;




        // X/Y/Z axis automatically calibrated to 0/256/0 when the device is steady
        // statistical analysis shows that Z axis is offset by approx. 32, assume (256*scale)^2 + (32*scale)^2 = g^2
        // this may vary between devices and firmware versions
        // alternatively, do optimization with (ScaleX*(X+OffsetX))^2 + (ScaleY*(Y+OffsetY))^2 + (ScaleZ*(Z+OffsetZ))^2 = g^2
        // with steady readings in different orientations

        // scale = 9.80665 / sqrt(256^2 + 32^2)
        //const float accelScale = 0.03801141343622691f;
    }
}
