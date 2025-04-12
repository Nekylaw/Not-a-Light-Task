using System.Collections;

namespace Game.Services
{
    public abstract class Service
    {

        #region Delegates

        public delegate void ServiceInitializedDelegate(Service service);

        #endregion 


        #region Fields

        public event ServiceInitializedDelegate OnServiceInitialized = null;

        private bool _isServiceInitialized = false;

        #endregion


        #region Public API

        public bool IsServiceInitialized
        {
            get => _isServiceInitialized;
            set
            {
                _isServiceInitialized = value;

                if (_isServiceInitialized)
                    OnServiceInitialized?.Invoke(this);
            }
        }

        public abstract void Tick(float delta);

        public abstract IEnumerator Init();

        #endregion

    }
}