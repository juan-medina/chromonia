// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System;
using Godot;

namespace Chromonia.InputPrompts;

public enum InputDeviceType
{
    KeyboardAndMouse,
    Xbox,
    PlayStation,
    GenericGamepad
}

public partial class InputManager : Node
{
    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.KeyboardAndMouse;

    public event Action<InputDeviceType>? OnDeviceChanged;

    private ToastNotification.ToastNotification _toast = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _toast = GetNode<ToastNotification.ToastNotification>("/root/ToastNotification");

        Input.JoyConnectionChanged += OnJoyConnectionChanged;

        var connectedJoypads = Input.GetConnectedJoypads();
        if (connectedJoypads.Count > 0) CurrentDevice = DetectJoypadDevice(connectedJoypads[0]);

        Input.MouseMode = CurrentDevice == InputDeviceType.KeyboardAndMouse
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Hidden;
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        if (connected)
        {
            var newDevice = DetectJoypadDevice((int)device);
            if (newDevice == CurrentDevice) return;
            CurrentDevice = newDevice;
            Input.MouseMode = Input.MouseModeEnum.Hidden;
            _toast.ShowToast("Controller connected", Input.GetJoyName((int)device));
        }
        else
        {
            var joypads = Input.GetConnectedJoypads();
            if (joypads.Count != 0 || CurrentDevice == InputDeviceType.KeyboardAndMouse) return;
            CurrentDevice = InputDeviceType.KeyboardAndMouse;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _toast.ShowToast("Controller disconnected", "Switched to keyboard and mouse");
        }

        OnDeviceChanged?.Invoke(CurrentDevice);
    }

    public override void _Input(InputEvent @event)
    {
        InputDeviceType newDevice = CurrentDevice;

        switch (@event)
        {
            case InputEventKey or InputEventMouseButton:
            case InputEventMouseMotion mouseMotion when mouseMotion.Relative.Length() > 2.0f:
                newDevice = InputDeviceType.KeyboardAndMouse;
                break;
            case InputEventJoypadButton joypadButton:
                newDevice = DetectJoypadDevice(joypadButton.Device);
                break;
            case InputEventJoypadMotion joypadMotion when Mathf.Abs(joypadMotion.AxisValue) > 0.5f:
                newDevice = DetectJoypadDevice(joypadMotion.Device);
                break;
        }

        if (newDevice == CurrentDevice) return;
        CurrentDevice = newDevice;
        Input.MouseMode = newDevice == InputDeviceType.KeyboardAndMouse
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Hidden;
        OnDeviceChanged?.Invoke(CurrentDevice);
    }

    private static InputDeviceType DetectJoypadDevice(int deviceId)
    {
        string joyName = Input.GetJoyName(deviceId).ToLower();

        if (joyName.Contains("ps5") || joyName.Contains("ps4") || joyName.Contains("playstation") ||
            joyName.Contains("dualshock") || joyName.Contains("dualsense"))
            return InputDeviceType.PlayStation;

        if (joyName.Contains("xbox") || joyName.Contains("xinput"))
            return InputDeviceType.Xbox;

        return InputDeviceType.GenericGamepad;
    }

    public Texture2D? GetIconForAction(string actionName)
    {
        string path = string.Empty;
        const string basePath = "res://InputPrompts/kenney_input-prompts_1.5/";

        path = actionName switch
        {
            "ui_accept" => CurrentDevice switch
            {
                InputDeviceType.KeyboardAndMouse => basePath + "Keyboard & Mouse/Default/keyboard_enter.png",
                InputDeviceType.PlayStation => basePath + "PlayStation Series/Default/playstation_button_color_cross.png",
                InputDeviceType.Xbox => basePath + "Xbox Series/Default/xbox_button_color_a.png",
                _ => basePath + "Generic/Default/generic_button_circle_bottom.png"
            },
            "energy_cycle" => CurrentDevice switch
            {
                InputDeviceType.KeyboardAndMouse => basePath + "Keyboard & Mouse/Default/keyboard_space.png",
                InputDeviceType.PlayStation => basePath + "PlayStation Series/Default/playstation_button_color_cross.png",
                InputDeviceType.Xbox => basePath + "Xbox Series/Default/xbox_button_color_a.png",
                _ => basePath + "Generic/Default/generic_button_circle_bottom.png"
            },
            "ui_cancel" => CurrentDevice switch
            {
                InputDeviceType.KeyboardAndMouse => basePath + "Keyboard & Mouse/Default/keyboard_escape.png",
                InputDeviceType.PlayStation => basePath +
                                               "PlayStation Series/Default/playstation_button_color_circle.png",
                InputDeviceType.Xbox => basePath + "Xbox Series/Default/xbox_button_color_b.png",
                _ => basePath + "Generic/Default/generic_button_circle_right.png"
            },
            "pause_game" => CurrentDevice switch
            {
                InputDeviceType.KeyboardAndMouse => basePath + "Keyboard & Mouse/Default/keyboard_escape.png",
                InputDeviceType.PlayStation => basePath + "PlayStation Series/Default/playstation4_button_options.png",
                InputDeviceType.Xbox => basePath + "Xbox Series/Default/xbox_button_menu.png",
                _ => basePath + "Generic/Default/generic_button_start.png"
            },
            "Move" => CurrentDevice switch
            {
                InputDeviceType.KeyboardAndMouse => basePath + "Keyboard & Mouse/Default/keyboard_arrows.png",
                InputDeviceType.PlayStation => basePath + "PlayStation Series/Default/playstation_stick_l.png",
                InputDeviceType.Xbox => basePath + "Xbox Series/Default/xbox_stick_l.png",
                _ => basePath + "Generic/Default/generic_stick_l.png"
            },
            _ => path
        };

        if (string.IsNullOrEmpty(path))
            return null;

        if (ResourceLoader.Exists(path))
            return ResourceLoader.Load<Texture2D>(path);

        // Fallback for paths that might be slightly wrong (like missing _color_)
        path = path.Replace("_color", "");
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }
}