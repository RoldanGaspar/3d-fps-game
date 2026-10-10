using System.Collections.Generic;
using UnityEngine;

/// <summary>Implemented by scripts that want OnAnimatorIK callbacks but don't sit on the Animator's GameObject.</summary>
public interface IAnimatorIKHandler
{
    void HandleAnimatorIK(int layerIndex);
}

/// <summary>
/// Unity only calls OnAnimatorIK on components that sit on the same GameObject as the Animator.
/// Gameplay scripts live elsewhere, so this relay on the model forwards the callback to any number of handlers.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorIKRelay : MonoBehaviour
{
    private struct Entry
    {
        public IAnimatorIKHandler handler;
        public int order;
    }

    private readonly List<Entry> _targets = new List<Entry>(4);

    /// <summary>Registers a handler (kept for compatibility with the old single-target API).</summary>
    public void SetTarget(IAnimatorIKHandler target)
    {
        AddTarget(target, 0);
    }

    /// <summary>
    /// Registers a handler. Handlers run in ascending order, so a handler with a higher order has the last
    /// word on IK goals (0 = base animation drivers, 10 = body lean, 20 = parkour poses).
    /// </summary>
    public void AddTarget(IAnimatorIKHandler target, int order = 0)
    {
        if (target == null) return;
        for (int i = 0; i < _targets.Count; i++)
            if (_targets[i].handler == target) return;

        int at = _targets.Count;
        while (at > 0 && _targets[at - 1].order > order) at--;
        _targets.Insert(at, new Entry { handler = target, order = order });
    }

    public void RemoveTarget(IAnimatorIKHandler target)
    {
        for (int i = _targets.Count - 1; i >= 0; i--)
            if (_targets[i].handler == target) _targets.RemoveAt(i);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        for (int i = 0; i < _targets.Count; i++)
            _targets[i].handler.HandleAnimatorIK(layerIndex);
    }
}
