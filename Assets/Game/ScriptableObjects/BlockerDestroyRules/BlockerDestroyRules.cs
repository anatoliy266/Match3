using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BlockerRuleMapping
{
    public BlockerType blockerType;
    public BlockerDestroyRuleBase rule;
}

[CreateAssetMenu(fileName = "BlockerDestroyRules", menuName = "Scriptable Objects/BlockerDestroyRules/Mapper")]
public class BlockerDestroyRules : ScriptableObject
{
    [SerializeField]
    private List<BlockerRuleMapping> mappings;

    private Dictionary<BlockerType, BlockerDestroyRuleBase> _lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<BlockerType, BlockerDestroyRuleBase>();
        if (mappings is null) return;
        for (int i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            if (mapping.rule != null)
            {
                _lookup[mapping.blockerType] = mapping.rule;
            }
        }
    }

    public BlockerDestroyRuleBase GetRule(BlockerType type)
    {
        if (_lookup == null) BuildLookup();
        if (_lookup.TryGetValue(type, out var rule))
            return rule;
        return null;
    }
}
