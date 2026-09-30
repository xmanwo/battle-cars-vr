using System.Collections.Generic;
using UnityEngine;

public class FlamethrowerTriggerZone : MonoBehaviour
{
    public readonly HashSet<Collider> targets = new HashSet<Collider>();

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        targets.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        targets.Remove(other);
    }

    public List<Collider> GetValidTargets()
    {
        List<Collider> result = new List<Collider>();

        foreach (var col in targets)
        {
            if (col != null)
                result.Add(col);
        }

        return result;
    }

    public void ClearNulls()
    {
        targets.RemoveWhere(c => c == null);
    }
}