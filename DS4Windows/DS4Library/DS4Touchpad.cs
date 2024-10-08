﻿/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;

namespace DS4Windows
{
    public class TouchpadEventArgs : EventArgs
    {
        private readonly Touch[] touches = null;
        private readonly DateTime timeStamp;
        private readonly bool touchButtonPressed;
        private readonly bool touchActive;

        public Touch[] Touches => touches;

        public DateTime TimeStamp => timeStamp;

        public bool TouchButtonPressed => touchButtonPressed;

        public bool TouchActive => touchActive;

        public TouchpadEventArgs(DateTime utcTimestamp, bool tButtonDown, bool touchActive, Touch t0, Touch t1 = null)
        {
            if (t1 != null)
            {
                touches = new Touch[2];
                touches[0] = t0;
                touches[1] = t1;
            }
            else if (t0 != null)
            {
                touches = new Touch[1];
                touches[0] = t0;
            }

            touchButtonPressed = tButtonDown;
            this.touchActive = touchActive;
            timeStamp = utcTimestamp;
        }
    }

    public class Touch
    {
        private int deltaY;
        private byte touchID;
        private Touch previousTouch;
        private int hwX;
        private int hwY;
        private int deltaX;

        internal Touch(int X, int Y, byte tID, Touch prevTouch = null)
        {
            populate(X, Y, tID, prevTouch);
        }

        public int HwX { get => hwX; set => hwX = value; }
        public int HwY { get => hwY; set => hwY = value; }
        public int DeltaX { get => deltaX; set => deltaX = value; }
        public int DeltaY { get => deltaY; set => deltaY = value; }
        public byte TouchID { get => touchID; set => touchID = value; }
        public Touch PreviousTouch { get => previousTouch; set => previousTouch = value; }

        internal void populate(int X, int Y, byte tID, Touch prevTouch = null)
        {
            HwX = X;
            HwY = Y;
            TouchID = tID;
            PreviousTouch = prevTouch;
            if (PreviousTouch != null)
            {
                DeltaX = X - PreviousTouch.HwX;
                DeltaY = Y - PreviousTouch.HwY;
            }
        }
    }

    public class DS4Touchpad
    {
        public delegate void TouchHandler<TEventArgs>(DS4Touchpad sender, TEventArgs args);

        public event TouchHandler<TouchpadEventArgs> TouchesBegan = null; // finger one or two landed (or both, or one then two, or two then one; any touches[] count increase)
        public event TouchHandler<TouchpadEventArgs> TouchesMoved = null; // deltaX/deltaY are set because one or both fingers were already down on a prior sensor reading
        public event TouchHandler<TouchpadEventArgs> TouchesEnded = null; // all fingers lifted
        public event TouchHandler<TouchpadEventArgs> TouchButtonDown = null; // touchpad pushed down until the button clicks
        public event TouchHandler<TouchpadEventArgs> TouchButtonUp = null; // touchpad button released
        public event TouchHandler<EventArgs> TouchUnchanged = null; // no status change for the touchpad itself... but other sensors may have changed, or you may just want to do some processing
        public event TouchHandler<EventArgs> PreTouchProcess = null; // used to publish that a touch packet is about to be processed
        //public event EventHandler<EventArgs> PreTouchProcess = null; // used to publish that a touch packet is about to be processed

        public readonly static int DS4_TOUCHPAD_DATA_OFFSET = 35;
        public const int RESOLUTION_X_MAX = 1920;
        public const int RESOLUTION_Y_MAX = 942;
        public const int RES_HALFED_X = (int)(RESOLUTION_X_MAX * 0.5);
        public const int RES_HALFED_Y = (int)(RESOLUTION_Y_MAX * 0.5);

        internal int lastTouchPadX1, lastTouchPadY1,
            lastTouchPadX2, lastTouchPadY2; // tracks 0, 1 or 2 touches; we maintain touch 1 and 2 separately
        internal bool lastTouchPadIsDown;
        internal bool lastIsActive1, lastIsActive2;
        internal byte lastTouchID1, lastTouchID2;
        internal byte[] previousPacket = new byte[8];
        private Touch tPrev0, tPrev1, t0, t1;

        public DS4Touchpad()
        {
            tPrev0 = new Touch(0, 0, 0);
            tPrev1 = new Touch(0, 0, 0);
            t0 = new Touch(0, 0, 0);
            t1 = new Touch(0, 0, 0);
        }

        // We check everything other than the not bothering with not-very-useful TouchPacketCounter.
        private bool PacketChanged(byte[] data, int touchDataOffset, int touchPacketOffset)
        {
            bool changed = false;
            for (int i = 0, arLen = previousPacket.Length; !changed && i < arLen; i++)
            {
                //byte oldValue = previousPacket[i];
                //previousPacket[i] = data[i + TOUCHPAD_DATA_OFFSET + touchPacketOffset];
                if (previousPacket[i] != data[i + touchDataOffset + touchPacketOffset])
                    changed = true;
            }

            return changed;
        }

        public void handleTouchpad(byte[] data, DS4State sensors, int touchDataOffset, int touchPacketOffset = 0)
        {
            PreTouchProcess?.Invoke(this, EventArgs.Empty);

            bool touchPadIsDown = sensors.TouchButton;
            if (!PacketChanged(data, touchDataOffset, touchPacketOffset) && touchPadIsDown == lastTouchPadIsDown)
            {
                if (TouchUnchanged != null)
                    TouchUnchanged(this, EventArgs.Empty);
                return;
            }

            Array.Copy(data, touchDataOffset + touchPacketOffset, previousPacket, 0, 8);
            byte touchID1 = (byte)(data[0 + touchDataOffset + touchPacketOffset] & 0x7F);
            byte touchID2 = (byte)(data[4 + touchDataOffset + touchPacketOffset] & 0x7F);
            int currentX1 = ((data[2 + touchDataOffset + touchPacketOffset] & 0x0F) << 8) | data[1 + touchDataOffset + touchPacketOffset];
            int currentY1 = (data[3 + touchDataOffset + touchPacketOffset] << 4) | ((data[2 + touchDataOffset + touchPacketOffset] & 0xF0) >> 4);
            int currentX2 = ((data[6 + touchDataOffset + touchPacketOffset] & 0x0F) << 8) | data[5 + touchDataOffset + touchPacketOffset];
            int currentY2 = (data[7 + touchDataOffset + touchPacketOffset] << 4) | ((data[6 + touchDataOffset + touchPacketOffset] & 0xF0) >> 4);

            TouchpadEventArgs args;
            if (sensors.Touch1 || sensors.Touch2)
            {
                if ((sensors.Touch1 && !lastIsActive1) || (sensors.Touch2 && !lastIsActive2))
                {
                    if (TouchesBegan != null)
                    {
                        if (sensors.Touch1 && sensors.Touch2)
                        {
                            t0.populate(currentX1, currentY1, touchID1); t1.populate(currentX2, currentY2, touchID2);
                            args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0, t1);
                        }
                        else if (sensors.Touch1)
                        {
                            t0.populate(currentX1, currentY1, touchID1);
                            args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                        }
                        else
                        {
                            t0.populate(currentX2, currentY2, touchID2);
                            args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                        }

                        TouchesBegan(this, args);
                    }
                }
                else if (sensors.Touch1 == lastIsActive1 && sensors.Touch2 == lastIsActive2 && TouchesMoved != null)
                {
                    Touch currentT0, currentT1;

                    if (sensors.Touch1 && sensors.Touch2)
                    {
                        tPrev0.populate(lastTouchPadX1, lastTouchPadY1, lastTouchID1);
                        t0.populate(currentX1, currentY1, touchID1, tPrev0);
                        currentT0 = t0;

                        tPrev1.populate(lastTouchPadX2, lastTouchPadY2, lastTouchID2);
                        t1.populate(currentX2, currentY2, touchID2, tPrev1);
                        currentT1 = t1;
                    }
                    else if (sensors.Touch1)
                    {
                        tPrev0.populate(lastTouchPadX1, lastTouchPadY1, lastTouchID1);
                        t0.populate(currentX1, currentY1, touchID1, tPrev0);
                        currentT0 = t0;
                        currentT1 = null;
                    }
                    else
                    {
                        tPrev0.populate(lastTouchPadX2, lastTouchPadY2, lastTouchID2);
                        t0.populate(currentX2, currentY2, touchID2, tPrev0);
                        currentT0 = t0;
                        currentT1 = null;
                    }

                    args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, currentT0, currentT1);

                    TouchesMoved(this, args);
                }

                if (!lastTouchPadIsDown && touchPadIsDown && TouchButtonDown != null)
                {
                    if (sensors.Touch1 && sensors.Touch2)
                    {
                        t0.populate(currentX1, currentY1, touchID1);
                        t1.populate(currentX2, currentY2, touchID2);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0, t1);
                    }
                    else if (sensors.Touch1)
                    {
                        t0.populate(currentX1, currentY1, touchID1);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                    }
                    else
                    {
                        t0.populate(currentX2, currentY2, touchID2);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                    }

                    TouchButtonDown(this, args);
                }
                else if (lastTouchPadIsDown && !touchPadIsDown && TouchButtonUp != null)
                {
                    if (sensors.Touch1 && sensors.Touch2)
                    {
                        t0.populate(currentX1, currentY1, touchID1);
                        t1.populate(currentX2, currentY2, touchID2);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0, t1);
                    }
                    else if (sensors.Touch1)
                    {
                        t0.populate(currentX1, currentY1, touchID1);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                    }
                    else
                    {
                        t0.populate(currentX2, currentY2, touchID2);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                    }

                    TouchButtonUp(this, args);
                }

                if (sensors.Touch1)
                {
                    lastTouchPadX1 = currentX1;
                    lastTouchPadY1 = currentY1;
                }
                if (sensors.Touch2)
                {
                    lastTouchPadX2 = currentX2;
                    lastTouchPadY2 = currentY2;
                }

                lastTouchPadIsDown = touchPadIsDown;
            }
            else
            {
                if (touchPadIsDown && !lastTouchPadIsDown)
                {
                    if (TouchButtonDown != null)
                        TouchButtonDown(this, new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, null, null));
                }
                else if (!touchPadIsDown && lastTouchPadIsDown)
                {
                    if (TouchButtonUp != null)
                        TouchButtonUp(this, new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, null, null));
                }

                if ((lastIsActive1 || lastIsActive2) && TouchesEnded != null)
                {
                    if (lastIsActive1 && lastIsActive2)
                    {
                        t0.populate(lastTouchPadX1, lastTouchPadY1, touchID1);
                        t1.populate(lastTouchPadX2, lastTouchPadY2, touchID2);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0, t1);
                    }
                    else if (lastIsActive1)
                    {
                        t0.populate(lastTouchPadX1, lastTouchPadY1, touchID1);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                    }
                    else
                    {
                        t0.populate(lastTouchPadX2, lastTouchPadY2, touchID2);
                        args = new TouchpadEventArgs(sensors.ReportTimeStamp, sensors.TouchButton, sensors.Touch1Finger, t0);
                    }

                    TouchesEnded(this, args);
                }
            }

            lastIsActive1 = sensors.Touch1;
            lastIsActive2 = sensors.Touch2;
            lastTouchID1 = touchID1;
            lastTouchID2 = touchID2;
            lastTouchPadIsDown = touchPadIsDown;
        }
    }
}
