﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputDemoRecorder
{
    public class InputChannelPatches
    {
        static readonly MethodInfo axisValueGetter = AccessTools.PropertyGetter(typeof(AbstractCommands), "AxisValue");
        static readonly MethodInfo axisValueSetter = AccessTools.PropertySetter(typeof(AbstractCommands), "AxisValue");

        static readonly MethodInfo isActiveThisFrameSetter = AccessTools.PropertySetter(typeof(AbstractCommands), "IsActiveThisFrame");

        static readonly MethodInfo wasActiveLastFrameGetter = AccessTools.PropertyGetter(typeof(AbstractCommands), "WasActiveLastFrame");

        static readonly MethodInfo inputStartedTimeSetter = AccessTools.PropertySetter(typeof(AbstractCommands), "InputStartedTime");

        private static bool ChangeInputs = false;

        public delegate void InputData(InputConsts.InputCommandType commandType, ref Vector2 value);
        private static InputData InputChanger;

        public delegate void UpdateInputs();
        public static event UpdateInputs OnUpdateInputs;

        static public void DoPatches(Harmony harmonyInstance)
        {
            HarmonyMethod inputManagerUpdatePrefix = new HarmonyMethod(typeof(InputChannelPatches), nameof(InputChannelPatches.UpdateInputsPrefix));
            HarmonyMethod abstractCommandsUpdateTranspiler = new HarmonyMethod(typeof(InputChannelPatches), nameof(InputChannelPatches.AbstractCommandsUpdateTranspiler));

            harmonyInstance.Patch(typeof(InputManager).GetMethod(nameof(InputManager.Update)), prefix: inputManagerUpdatePrefix);
            harmonyInstance.Patch(typeof(AbstractCommands).GetMethod("Update"), transpiler: abstractCommandsUpdateTranspiler);
        }

        static void UpdateInputsPrefix()
        {
            OnUpdateInputs?.Invoke();
        }
        public static void SetInputValue(AbstractCommands __instance)
        {
            if (ChangeInputs)
            {
                Vector2 axisValue = (Vector2)axisValueGetter.Invoke(__instance, null);
                InputChanger?.Invoke(__instance.CommandType, ref axisValue);
                axisValueSetter.Invoke(__instance, new object[] { axisValue });

                float comparer = (__instance.ValueType == InputConsts.InputValueType.DOUBLE_AXIS) ? float.Epsilon : __instance.PressedThreshold;
                isActiveThisFrameSetter.Invoke(__instance, new object[] { axisValue.magnitude > comparer });
            }
        }
        public static void SetInputChanger(InputData inputChanger)
        {
            InputChanger = inputChanger;
        }
        public static void AllowChangeInputs(bool changeInputs = true)
        {
            ChangeInputs = changeInputs;
        }
        public static void ResetInputChannelEdited()
        {
            ChangeInputs = false;
            InputChanger = null;
        }

        static IEnumerable<CodeInstruction> AbstractCommandsUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            int index = -1;
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_0 && codes[i + 1].opcode == OpCodes.Call && codes[i + 2].opcode == OpCodes.Ldarg_0 && codes[i + 3].opcode == OpCodes.Callvirt)
                {
                    index = i + 4;
                    break;
                }
            }
            if (index > -1)
            {
                codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(index + 1, CodeInstruction.Call(typeof(InputChannelPatches), nameof(InputChannelPatches.SetInputValue), new Type[] { typeof(AbstractCommands) }));
            }
            return codes;
        }
    }
}

