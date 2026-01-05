using NoClue.Constancts;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Cactuse
{
    [SerializeField]
    private List<CastuseBehaviour> m_CastuseBehaviour;

    public List<CastuseBehaviour> _CastuseBehaviour
    {
        get => m_CastuseBehaviour;
        set => m_CastuseBehaviour = value;
    }

    public List<AnimationName> GetCastuseAnumations(CactusState cactusState)
    {
        for (int i = 0; i < m_CastuseBehaviour.Count; i++)
        {
            List<AnimationName> animationStates = (m_CastuseBehaviour[i].CactusStates == cactusState) ? m_CastuseBehaviour[i]._AnimationNames : null;
            return (m_CastuseBehaviour[i].CactusStates == cactusState) ? m_CastuseBehaviour[i]._AnimationNames : null;
        }

        return null;
    }
}

[Serializable]
public class CastuseBehaviour
{
    [SerializeField]
    private List<AnimationName> m_AnimationNames = new List<AnimationName>();

    [SerializeField]
    private CactusState m_CactusStates;

    public List<AnimationName> _AnimationNames
    {
        get => m_AnimationNames;
        set => m_AnimationNames = value;
    }
    public CactusState CactusStates
    {
        get => m_CactusStates;
        set => m_CactusStates = value;
    }
}