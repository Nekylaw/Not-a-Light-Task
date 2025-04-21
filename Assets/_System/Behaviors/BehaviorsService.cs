using Game.Services;
using System.Collections;
using UnityEngine;

namespace Services.Behaviors
{

    public static class BehaviorsService
    {

        #region Delegates

        public delegate void OnWalkDelegate(Vector3 direction, float speed);
        public delegate void OnWalkStopDelegate();
        public delegate void OnShootDelegate();
        public delegate void OnAimDelegate();
        public delegate void OnPacifyDelegate();
        public delegate void OnPickupDelegate(PickableComponent pickable);

        #endregion


        #region Fields

        public static event OnWalkDelegate OnWalk = null;
        public static event OnWalkStopDelegate OnWalkStop = null;
        public static event OnShootDelegate OnShoot = null;
        public static event OnAimDelegate OnAim = null;
        public static event OnPacifyDelegate OnPacify = null;
        public static event OnPickupDelegate OnPickup = null;

        #endregion


        public static void Move(Vector3 dir, float speed)
        {
            OnWalk?.Invoke(dir, speed);

            if (speed <= 0)
                OnWalkStop?.Invoke();
        }

        public static void Aim() => OnAim?.Invoke();

        public static void Shoot() => OnShoot?.Invoke();

        public static void Pacify() => OnPacify?.Invoke();
        public static void Pickup(PickableComponent pickable) => OnPickup?.Invoke(pickable);

    }
}
