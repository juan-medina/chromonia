// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.Theme;

public partial class AudioTheme : Node
{
    [Export] private AudioStreamPlayer _focusSfx = null!;
    [Export] private AudioStreamPlayer _clickSfx = null!;

    public void ConnectMenuSounds(Node rootNode)
    {
        // Recursively find all controls and connect sounds
        foreach (Node child in rootNode.GetChildren())
        {
            if (child is Control control) ConnectSounds(control);
            ConnectMenuSounds(child);
        }
    }

    private void ConnectSounds(Control control)
    {
        // Only connect to interactive elements
        switch (control)
        {
            case BaseButton button:
                button.FocusEntered += PlayFocusSound;
                button.Pressed += PlayClickSound;
                break;
            case Slider slider:
                slider.FocusEntered += PlayFocusSound;
                slider.ValueChanged += PlaySliderClickSound;
                break;
        }
    }

    private void PlayFocusSound() => _focusSfx.Play();
    private void PlayClickSound() => _clickSfx.Play();
    private void PlaySliderClickSound(double _) => _clickSfx.Play();
}