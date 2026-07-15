// SPDX-FileCopyrightText: 2026 Juan Medina
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Chromonia.Enemies;
using Chromonia.Main;
using Chromonia.PlayerSystem;
using Godot;

namespace Chromonia.CollisionSystem;

public class CollisionSystem(Node2D playfield, Arrow.Arrow arrow)
{
    private readonly List<BlobEnemy> _activeBlobs = [];

    public void AddBlobs(IEnumerable<BlobEnemy> blobs) => _activeBlobs.AddRange(blobs);

    public void UpdateBlobMergeStates()
    {
        for (int i = _activeBlobs.Count - 1; i >= 0; i--)
        {
            if (_activeBlobs[i].IsDissolving || !GodotObject.IsInstanceValid(_activeBlobs[i])) _activeBlobs.RemoveAt(i);
        }

        for (int i = 0; i < _activeBlobs.Count; i++) _activeBlobs[i].SetMerged(false);

        for (int i = 0; i < _activeBlobs.Count; i++)
        {
            for (int j = i + 1; j < _activeBlobs.Count; j++)
            {
                if (_activeBlobs[i].BaseEnergy == _activeBlobs[j].BaseEnergy) continue;

                float dynamicMergeDistance = _activeBlobs[i].Radius + _activeBlobs[j].Radius + 15f;
                if (!(_activeBlobs[i].GlobalPosition.DistanceTo(_activeBlobs[j].GlobalPosition) <
                      dynamicMergeDistance)) continue;
                _activeBlobs[i].SetMerged(true);
                _activeBlobs[j].SetMerged(true);
            }
        }
    }

    public bool CheckCollisions(PlayerState playerState, List<Vector2> activeLine)
    {
        if (arrow.State != Arrow.ArrowState.Normal || playerState == PlayerState.Won) return false;

        const float arrowRadius = 15f;
        const float lineThicknessRadius = 4f;

        for (int i = 0; i < _activeBlobs.Count; i++)
        {
            var blob = _activeBlobs[i];

            if (blob.CurrentEnergy == arrow.CurrentEnergy &&
                blob.CurrentEnergy != Energy.Energy.Combined)
                continue;

            if (blob.GlobalPosition.DistanceTo(arrow.GlobalPosition) < blob.Radius + arrowRadius)
                if (arrow.State == Arrow.ArrowState.Normal)
                    return true;

            if (playerState != PlayerState.Drawing || activeLine.Count < 2) continue;

            Vector2 localBlobPos = playfield.ToLocal(blob.GlobalPosition);
            for (int j = 0; j < activeLine.Count - 1; j++)
            {
                if (!(GeometryUtils.GeometryUtils.DistanceToSegment(localBlobPos, activeLine[j], activeLine[j + 1]) <
                      blob.Radius + lineThicknessRadius)) continue;
                if (arrow.State == Arrow.ArrowState.Normal) return true;
            }
        }

        return false;
    }

    public void GetTrappedBlobs(Vector2[] claimedPoly, List<BlobEnemy> resultsBuffer)
    {
        resultsBuffer.Clear();
        for (int i = 0; i < _activeBlobs.Count; i++)
        {
            var blob = _activeBlobs[i];
            Vector2 localBlobPos = playfield.ToLocal(blob.GlobalPosition);
            if (Geometry2D.IsPointInPolygon(localBlobPos, claimedPoly)) resultsBuffer.Add(blob);
        }
    }

    public bool IsLethalTrap(List<BlobEnemy> trappedBlobs)
    {
        for (int i = 0; i < trappedBlobs.Count; i++)
        {
            var blob = trappedBlobs[i];
            if (blob.CurrentEnergy == arrow.CurrentEnergy ||
                blob.CurrentEnergy == Energy.Energy.Combined)
                return true;
        }

        return false;
    }

    public void DestroyAllBlobs()
    {
        for (int i = 0; i < _activeBlobs.Count; i++)
        {
            var blob = _activeBlobs[i];
            if (blob.GetParent() is BlobCluster cluster)
                cluster.Dissolve();
            else
                blob.Dissolve();
        }

        _activeBlobs.Clear();
    }
}