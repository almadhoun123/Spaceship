using System;
using UnityEngine;

namespace Spaceship
{
    /// <summary>
    /// Level flow and scoring (GDD 2.2): win by reaching the end of the rail, lose when the ship is destroyed.
    /// Tracks score per player and combined.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public enum State { Playing, Won, Lost }

        [SerializeField] RailFollower rail;
        [SerializeField] ShipStatus ship;

        public State Current { get; private set; } = State.Playing;
        public int LeftScore { get; private set; }
        public int RightScore { get; private set; }
        public int TeamScore { get; private set; }
        public int Kills { get; private set; }

        public event Action<State> StateChanged;
        public event Action ScoreChanged;

        void OnEnable()
        {
            Damageable.AnyDestroyed += OnTargetDestroyed;
            if (rail) rail.ReachedEndOfRail += OnReachedEnd;
            if (ship) ship.ShipDestroyed += OnShipDestroyed;
        }

        void OnDisable()
        {
            Damageable.AnyDestroyed -= OnTargetDestroyed;
            if (rail) rail.ReachedEndOfRail -= OnReachedEnd;
            if (ship) ship.ShipDestroyed -= OnShipDestroyed;
        }

        void OnTargetDestroyed(Damageable d, PlayerSide? side)
        {
            if (Current != State.Playing) return;
            Kills++;
            TeamScore += d.ScoreValue;
            if (side == PlayerSide.Left) LeftScore += d.ScoreValue;
            else if (side == PlayerSide.Right) RightScore += d.ScoreValue;
            ScoreChanged?.Invoke();
        }

        void OnReachedEnd() => SetState(State.Won);

        void OnShipDestroyed()
        {
            if (rail) rail.IsMoving = false;
            SetState(State.Lost);
        }

        void SetState(State s)
        {
            if (Current != State.Playing) return;
            Current = s;
            StateChanged?.Invoke(s);
        }
    }
}
