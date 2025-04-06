using System.Collections;
using UnityEngine;

public interface IService
{
    delegate void ServiceInitializedDelegate(IService service);

    bool IsServiceInitialized { get; set; }

    void Tick(float delta);

    IEnumerator Init();
}
