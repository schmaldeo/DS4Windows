using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vader4ProReader.Device;
using System.Buffers.Binary;

namespace DS4Windows.InputDevices
{
    internal class Vader4ProDevice : DS4Device
    {
        private bool timeStampInit = false;
        private uint timeStampPrevious = 0;
        private uint deltaTimeCurrent = 0;
        private bool outputDirty = false;
        private DS4HapticState previousHapticState = new DS4HapticState();

        private Vader4ProControllerOptions nativeOptionsStore;
        public Vader4ProControllerOptions NativeOptionsStore { get => nativeOptionsStore; }
        public Vader4ProDevice(HidDevice hidDevice, string disName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4, string macAddress = "") :
            base(hidDevice, disName, featureSet)
        {
            synced = true;
            if (macAddress != "")
            {
                Mac = macAddress;
            }
        }

        public override event ReportHandler<EventArgs> Report = null;
        public override event EventHandler BatteryChanged;
        public override event EventHandler ChargingChanged;
        public override void PostInit()
        {
            if (Mac == null || Mac == "" || Mac == BLANK_SERIAL) 
            {
                Mac = hDevice.GenerateFakeHwSerial();
            }
            deviceType = InputDeviceType.Vader4Pro;
            gyroMouseSensSettings = new GyroMouseSens();
            inputReport = new byte[hDevice.Capabilities.InputReportByteLength];
            warnInterval = WARN_INTERVAL_USB;
            conType = ConnectionType.USB;
            byte[] buf = new byte[32];
            buf[0] = 0x05;
            buf[1] = 0x10;
            buf[2] = 0x01;
            buf[3] = 0x01;
            buf[4] = 0x01;
            hDevice.WriteOutputReportViaInterrupt(buf, READ_STREAM_TIMEOUT);
        }

        public override bool DisconnectBT(bool callRemoval = false)
        {
            // Do Nothing
            return true;
        }

        public override bool DisconnectDongle(bool remove = false)
        {
            // Do Nothing
            return true;
        }

        public override bool DisconnectWireless(bool callRemoval = false)
        {
            return true;
        }

        public override bool IsAlive()
        {
            return synced;
        }

        public override void RefreshCalibration()
        {
            return;
        }

        public override void StartUpdate()
        {
            this.inputReportErrorCount = 0;

            if (ds4Input == null)
            {
                ds4Input = new Thread(ReadInput);
                ds4Input.Priority = ThreadPriority.AboveNormal;
                ds4Input.Name = "Vader 4 Pro Input thread: " + Mac;
                ds4Input.IsBackground = true;
                ds4Input.Start();
            }
            else
                Console.WriteLine("Thread already running for Vader 4 Pro: " + Mac);
        }

        private unsafe void ReadInput()
        {
            unchecked
            {
                Debouncer = SetupDebouncer();
                firstActive = DateTime.UtcNow;
                NativeMethods.HidD_SetNumInputBuffers(hDevice.SafeReadHandle.DangerousGetHandle(), 3);
                Queue<long> latencyQueue = new Queue<long>(21); // Set capacity at max + 1 to avoid any resizing
                int tempLatencyCount = 0;
                long oldtime = 0;
                string currerror = string.Empty;
                long curtime = 0;
                long testelapsed = 0;
                timeoutEvent = false;
                ds4InactiveFrame = true;
                idleInput = true;
                bool syncWriteReport = conType != ConnectionType.BT;

                bool tempCharging = charging;
                uint tempStamp = 0;
                double elapsedDeltaTime = 0.0;
                uint tempDelta = 0;
                long latencySum = 0;

                sixAxis.ResetContinuousCalibration();
                standbySw.Start();

                while (!exitInputThread)
                {
                    oldCharging = charging;
                    currerror = string.Empty;

                    if (tempLatencyCount >= 20)
                    {
                        latencySum -= latencyQueue.Dequeue();
                        tempLatencyCount--;
                    }

                    latencySum += this.lastTimeElapsed;
                    latencyQueue.Enqueue(this.lastTimeElapsed);
                    tempLatencyCount++;

                    //Latency = latencyQueue.Average();
                    Latency = latencySum / (double)tempLatencyCount;

                    readWaitEv.Set();

                    HidDevice.ReadStatus res = hDevice.ReadFile(inputReport);
                    if (res != HidDevice.ReadStatus.Success)
                    {

                        exitInputThread = true;
                        readWaitEv.Reset();
                        StopOutputUpdate();
                        isDisconnecting = true;
                        RunRemoval();
                        timeoutExecuted = true;
                        continue;
                    }
                    readWaitEv.Wait();
                    readWaitEv.Reset();

                    curtime = Stopwatch.GetTimestamp();
                    testelapsed = curtime - oldtime;
                    lastTimeElapsedDouble = testelapsed * (1.0 / Stopwatch.Frequency) * 1000.0;
                    lastTimeElapsed = (long)lastTimeElapsedDouble;
                    oldtime = curtime;

                    utcNow = DateTime.UtcNow; // timestamp with UTC in case system time zone changes

                    cState.PacketCounter = pState.PacketCounter + 1;
                    cState.ReportTimeStamp = utcNow;
                    byte[] buf = new byte[32];
                    int copyLen = Math.Min(inputReport.Length, 32); // safe length
                    Array.Copy(inputReport, buf, copyLen);         // copies only what exists
                    if (buf[1] == 0xFE)
                    {
                        var report = new Vader4ProReport(buf);
                        // Dpad
                        cState.DpadUp = report.IsDPadUpPressed;
                        cState.DpadRight = report.IsDPadRightPressed;
                        cState.DpadDown = report.IsDPadDownPressed;
                        cState.DpadLeft = report.IsDPadLeftPressed;
                        // Left Stick
                        cState.LX = report.LS_X;
                        cState.LY = report.LS_Y;
                        cState.L3 = report.IsLSPressed;
                        // Right Stick
                        cState.RX = report.RS_X;
                        cState.RY = report.RS_Y;
                        cState.R3 = report.IsRSPressed;
                        // Share/Options
                        cState.Share = report.IsSelectPressed;
                        cState.Options = report.IsStartPressed;
                        // Left Bumper / Right Bumper
                        cState.L1 = report.IsLBPressed;
                        cState.R1 = report.IsRBPressed;
                        // Left Trigger
                        cState.L2 = report.LT;
                        cState.L2Btn = cState.L2 > 0;
                        cState.L2Raw = cState.L2;
                        // Right Trigger
                        cState.R2 = report.RT;
                        cState.R2Btn = cState.R2 > 0;
                        cState.R2Raw = cState.R2;
                        // Face Buttons
                        cState.Cross = report.IsAPressed;
                        cState.Circle = report.IsBPressed;
                        cState.Square = report.IsXPressed;
                        cState.Triangle = report.IsYPressed;
                        // PlayStation Button
                        cState.PS = report.IsHOMEPressed;
                        // Paddles
                        cState.SideL = report.IsM4Pressed;
                        cState.SideR = report.IsM3Pressed;
                        cState.BLP = report.IsM2Pressed;
                        cState.BRP = report.IsM1Pressed;
                        // C,Z buttons
                        cState.FnL = report.IsCPressed;
                        cState.FnR = report.IsZPressed;
                        // FN button
                        cState.Capture = report.IsFNPressed;
                        // Gyro / Accelerometer
                        short yaw = (short)-report.YawCalibrated;
                        short pitch = (short)-report.PitchCalibrated;
                        short roll = (short)(report.RollCalibrated);
                        short ax = (short)-(report.AccelXCalibrated);
                        short ay = (short)(report.AccelYCalibrated);
                        short az = (short)(report.AccelZCalibrated);
                        if (synced)
                        {
                            sixAxis.handleSixaxisVals(yaw, pitch, roll, ax, ay, az, cState, elapsedDeltaTime);
                        }
                    }

                    cState.Battery = 99;


                    if (timeStampInit == false)
                    {
                        timeStampInit = true;
                        deltaTimeCurrent = tempStamp * 1u / 3u;
                    }
                    else if (timeStampPrevious > tempStamp)
                    {
                        tempDelta = uint.MaxValue - timeStampPrevious + tempStamp + 1u;
                        deltaTimeCurrent = tempDelta * 1u / 3u;
                    }
                    else
                    {
                        tempDelta = tempStamp - timeStampPrevious;
                        deltaTimeCurrent = tempDelta * 1u / 3u;
                    }


                    // Make sure timestamps don't match
                    if (deltaTimeCurrent != 0)
                    {
                        elapsedDeltaTime = 0.000001 * deltaTimeCurrent; // Convert from microseconds to seconds
                        cState.totalMicroSec = pState.totalMicroSec + deltaTimeCurrent;
                    }
                    else
                    {
                        // Duplicate timestamp. Use system clock for elapsed time instead
                        elapsedDeltaTime = lastTimeElapsedDouble * .001;
                        cState.totalMicroSec = pState.totalMicroSec + (uint)(elapsedDeltaTime * 1000000);
                    }

                    cState.elapsedTime = elapsedDeltaTime;
                    cState.ds4Timestamp = (ushort)((tempStamp / 16) % ushort.MaxValue);
                    timeStampPrevious = tempStamp;


                    if (idleTimeout == 0)
                    {
                        lastActive = utcNow;
                    }
                    else
                    {
                        idleInput = isDS4Idle();
                        if (!idleInput)
                        {
                            lastActive = utcNow;
                        }
                    }

                    if (fireReport)
                    {
                        Report?.Invoke(this, EventArgs.Empty);
                    }

                    PrepareOutReport();
                    if (outputDirty)
                    {
                        WriteReport();
                        previousHapticState = currentHap;
                    }

                    currentHap.dirty = false;
                    outputDirty = false;


                    if (!string.IsNullOrEmpty(currerror))
                        error = currerror;
                    else if (!string.IsNullOrEmpty(error))
                        error = string.Empty;

                    cState.CopyTo(pState);

                    if (hasInputEvts)
                    {
                        lock (eventQueueLock)
                        {
                            Action tempAct = null;
                            for (int actInd = 0, actLen = eventQueue.Count; actInd < actLen; actInd++)
                            {
                                tempAct = eventQueue.Dequeue();
                                tempAct.Invoke();
                            }

                            hasInputEvts = false;
                        }
                    }
                }

            }
            timeoutExecuted = true;
        }


        private void PrepareOutReport()
        {
            MergeStates();
            bool change = false;
            bool rumbleSet = currentHap.IsRumbleSet();
            if (currentHap.dirty || !previousHapticState.Equals(currentHap))
            {
                change = true;
            }
            if (change)
            {
                outputDirty = true;
                if (rumbleSet)
                {
                    standbySw.Restart();
                }
                else
                {
                    standbySw.Reset();
                }
            }
            else if (rumbleSet && standbySw.ElapsedMilliseconds >= 4000L)
            {
                outputDirty = true;
                standbySw.Restart();
            }
        }
        private void WriteReport()
        {
            SetLed(currentHap.lightbarState.LightBarColor.red, currentHap.lightbarState.LightBarColor.green, currentHap.lightbarState.LightBarColor.blue);
            SetRumble(currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow, currentHap.rumbleState.RumbleMotorStrengthRightLightFast);
        }
        private void SetRumble(byte leftMain, byte rightMain, byte leftTrigger = 0, byte rightTrigger = 0)
        {
            byte[] buf = new byte[32];
            buf[0] = 0x05;
            buf[1] = 0x0F;
            buf[2] = leftMain;
            buf[3] = rightMain;
            buf[4] = leftTrigger;
            buf[5] = rightTrigger;
            hDevice.WriteOutputReportViaInterrupt(buf, READ_STREAM_TIMEOUT);
        }

        private void SetLed(byte r, byte g, byte b)
        {
            byte[] buf = new byte[32];
            buf[0] = 0x05;
            buf[1] = 0xE0;
            buf[2] = r;
            buf[3] = g;
            buf[4] = b;
            hDevice.WriteOutputReportViaInterrupt(buf, READ_STREAM_TIMEOUT);
        }

        protected override void StopOutputUpdate()
        {
            byte[] buf = new byte[32];
            buf[0] = 0x05;
            buf[1] = 0x10;
            buf[2] = 0x01;
            buf[3] = 0x01;
            buf[4] = 0x01;
            hDevice.WriteOutputReportViaInterrupt(buf, READ_STREAM_TIMEOUT);
            SetLed(0, 0, 0);
            setRumble(0, 0);
        }

    }
}
