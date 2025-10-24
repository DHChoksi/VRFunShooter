using UnityEngine;

namespace NoClue.Constancts
{
    public static class Constants
    {
        public enum EnemyState
        {
            Idle,
            Walk,
            SensePlayer,
            Chase,
            Attack,
            Hurt,
            Dead
        }
    }
}
