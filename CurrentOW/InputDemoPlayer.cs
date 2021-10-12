﻿using UnityEngine;
using System.Collections.Generic;

namespace InputDemoRecorder
{
    public static class InputDemoPlayer
    {
        private static float currentInputTime;
        private static float startPlaybackTime;
        private static InputsCurveRecorder InputsCurve;

        public static float GetCurrentInputTime() => currentInputTime;

        static InputDemoPlayer()
        {
            InputChannelPatches.OnUpdateInputs += InputChannelPatches_OnUpdateInputs;
        }
        public static void StartPlayback(InputsCurveRecorder demoFile)
        {
            InputChannelPatches.SetInputChanger(ReturnInputCommandValue);
            InputsCurve = demoFile;
            startPlaybackTime = Time.unscaledTime;
        }
        public static void StopPlayback()
        {
            InputChannelPatches.ResetInputChannelEdited();
        }

        private static void InputChannelPatches_OnUpdateInputs()
        {
            currentInputTime = Time.unscaledTime - startPlaybackTime;
        }
        
        private static void ReturnInputCommandValue(InputConsts.InputCommandType commandType, ref Vector2 axisValue)
        {

            if (InputsCurve.InputCurves.TryGetValue(commandType, out var curves))
            {
                var input = new Vector2(curves[0].Evaluate(currentInputTime), curves[1].Evaluate(currentInputTime));

                if (commandType != InputConsts.InputCommandType.PAUSE)
                    axisValue = input;
                else if (input.magnitude > float.Epsilon)
                    axisValue = input;
            }
        }
    }
}
