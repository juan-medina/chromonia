// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Scripts;
using Godot;

namespace Chromonia.Scenes;

public partial class SharedProgressBar : Control
{
    [Export] public float GoalPercentage { get; set; } = 0.35f;

    private Panel _bgPanel = null!;
    private Panel _redFill = null!;
    private Panel _blueFill = null!;
    private float _targetRed;
    private float _targetBlue;

    public override void _Ready()
    {
        foreach (Node child in GetChildren()) child.QueueFree();

        // Background Panel (with perfect soft drop shadow)
        _bgPanel = new Panel();
        _bgPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.2f), CornerRadiusTopLeft = 20, CornerRadiusTopRight = 20,
            CornerRadiusBottomLeft = 20, CornerRadiusBottomRight = 20
        };
        bgStyle.ShadowColor = new Color(0, 0, 0, 0.5f);
        bgStyle.ShadowSize = 15;
        _bgPanel.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(_bgPanel);

        // Red Panel
        _redFill = new Panel();
        _redFill.SetAnchorsPreset(LayoutPreset.FullRect);
        _redFill.AnchorRight = 0f;
        var redStyle = new StyleBoxFlat
            { BgColor = Colors.White, CornerRadiusTopLeft = 20, CornerRadiusBottomLeft = 20 };
        _redFill.AddThemeStyleboxOverride("panel", redStyle);
        _redFill.Modulate = Energy.A.Fill;
        AddChild(_redFill);

        // Blue Panel
        _blueFill = new Panel();
        _blueFill.SetAnchorsPreset(LayoutPreset.FullRect);
        _blueFill.AnchorLeft = 1f;
        var blueStyle = new StyleBoxFlat
            { BgColor = Colors.White, CornerRadiusTopRight = 20, CornerRadiusBottomRight = 20 };
        _blueFill.AddThemeStyleboxOverride("panel", blueStyle);
        _blueFill.Modulate = Energy.B.Fill;
        AddChild(_blueFill);

        CreateMarkers();
        UpdateProgress(0f, 0f);
    }

    private void CreateMarkers()
    {
        var redMarker = new ColorRect { Color = Colors.White };
        redMarker.Modulate = Energy.A.Line;
        redMarker.SetAnchorsPreset(LayoutPreset.LeftWide);
        redMarker.AnchorLeft = GoalPercentage;
        redMarker.AnchorRight = GoalPercentage;
        redMarker.OffsetLeft = -2;
        redMarker.OffsetRight = 2;
        AddChild(redMarker);

        var blueMarker = new ColorRect { Color = Colors.White };
        blueMarker.Modulate = Energy.B.Line;
        blueMarker.SetAnchorsPreset(LayoutPreset.RightWide);
        blueMarker.AnchorLeft = 1.0f - GoalPercentage;
        blueMarker.AnchorRight = 1.0f - GoalPercentage;
        blueMarker.OffsetLeft = -2;
        blueMarker.OffsetRight = 2;
        AddChild(blueMarker);
    }

    public void UpdateProgress(float redPercent, float bluePercent)
    {
        _targetRed = redPercent;
        _targetBlue = bluePercent;
    }

    public override void _Process(double delta)
    {
        float currentRed = _redFill.AnchorRight;
        float currentBlue = 1.0f - _blueFill.AnchorLeft;

        if (!Mathf.IsEqualApprox(currentRed, _targetRed))
        {
            currentRed = Mathf.Lerp(currentRed, _targetRed, 5f * (float)delta);
            _redFill.AnchorRight = currentRed;
            _redFill.OffsetRight = 0;
        }

        if (!Mathf.IsEqualApprox(currentBlue, _targetBlue))
        {
            currentBlue = Mathf.Lerp(currentBlue, _targetBlue, 5f * (float)delta);
            _blueFill.AnchorLeft = 1.0f - currentBlue;
            _blueFill.OffsetLeft = 0;
        }
    }
}