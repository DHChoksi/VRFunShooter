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
            Hurt,
            Chase,
            Attack,
            Dead
        }
    }

    public enum CactusState { Idle, Patrol, Chase, Attack, Dead }
    public enum AnimationName { Cactus_IdleBattle, Cactus_IdleNormal, Cactus_IdlePlant, Cactus_Attack01, Cactus_Attack02, Cactus_RunFWD, Cactus_WalkFWD, Cactus_Die }
}
