// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Core;
using Godot;

namespace Chromonia.UI.Components;

public partial class InputPromptIcon : TextureRect
{
    [Export] public string ActionName { get; set; } = "ui_accept";

    private InputManager _inputManager = null!;

    public override void _Ready()
    {
        _inputManager = GetNode<InputManager>("/root/InputManager");

        if (!IsInstanceValid(_inputManager)) return;
        _inputManager.OnDeviceChanged += UpdateIcon;
        UpdateIcon(_inputManager.CurrentDevice);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_inputManager)) _inputManager.OnDeviceChanged -= UpdateIcon;
    }

    private void UpdateIcon(InputDeviceType deviceType) => Texture = _inputManager.GetIconForAction(ActionName);
}