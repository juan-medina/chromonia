// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using Chromonia.Scripts;
using Godot;

namespace Chromonia.Scenes;

public partial class SharedProgressBar : Control
{
    [Export] public float GoalPercentage { get; set; } = 0.35f;

    [Export] private Panel _fillA = null!;
    [Export] private Panel _fillB = null!;
    [Export] private ColorRect _markerA = null!;
    [Export] private ColorRect _markerB = null!;
    [Export] private Label _labelA = null!;
    [Export] private Label _labelB = null!;

    private float _targetA;
    private float _targetB;
    private int _lastPercentA = -1;
    private int _lastPercentB = -1;

    public override void _Ready()
    {
        UpdateProgress(0f, 0f);

        _fillA.Modulate = Energy.A.Fill;
        _fillB.Modulate = Energy.B.Fill;
        _markerA.Modulate = Energy.A.Marker;
        _markerB.Modulate = Energy.B.Marker;
    }

    public void UpdateProgress(float percentA, float percentB)
    {
        _targetA = percentA;
        _targetB = percentB;
    }

    public override void _Process(double delta)
    {
        float currentA = _fillA.AnchorRight;
        float currentB = 1.0f - _fillB.AnchorLeft;

        if (!Mathf.IsEqualApprox(currentA, _targetA))
        {
            currentA = Mathf.Lerp(currentA, _targetA, 5f * (float)delta);
            _fillA.AnchorRight = currentA;
            _fillA.OffsetRight = 0;
            int percentA = Mathf.RoundToInt(currentA * 100);
            if (percentA != _lastPercentA)
            {
                _labelA.Text = $"{percentA}%";
                _lastPercentA = percentA;
            }
        }

        if (Mathf.IsEqualApprox(currentB, _targetB)) return;
        currentB = Mathf.Lerp(currentB, _targetB, 5f * (float)delta);
        _fillB.AnchorLeft = 1.0f - currentB;
        _fillB.OffsetLeft = 0;
        int percentB = Mathf.RoundToInt(currentB * 100);
        if (percentB == _lastPercentB) return;
        _labelB.Text = $"{percentB}%";
        _lastPercentB = percentB;
    }
}