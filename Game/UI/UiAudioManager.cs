// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Godot;

namespace Chromonia.UI;

public partial class UiAudioManager : Node
{
    [Export] private AudioStreamPlayer _hoverSfx = null!;
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
                button.FocusEntered += PlayHoverSound;
                button.MouseEntered += PlayHoverSound;
                button.Pressed += PlayClickSound;
                break;
            case Slider slider:
                slider.FocusEntered += PlayHoverSound;
                slider.MouseEntered += PlayHoverSound;
                slider.ValueChanged += PlaySliderClickSound;
                break;
        }
    }

    private void PlayHoverSound() => _hoverSfx.Play();
    private void PlayClickSound() => _clickSfx.Play();
    private void PlaySliderClickSound(double _) => _clickSfx.Play();
}