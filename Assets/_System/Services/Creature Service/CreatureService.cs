using UnityEngine;

public class CreatureService : MonoBehaviour
{
    #region Delegates

    public delegate void CreatureMoveDelegate(CreatureController creature, float speed);
    public event CreatureMoveDelegate OnCreatureMoving;

    public delegate void StartCreatureDrainDelegate(CreatureController creature);
    public event StartCreatureDrainDelegate OnCreatureDrainingStart;

    public delegate void EndCreatureDrainDelegate(CreatureController creature);
    public event EndCreatureDrainDelegate OnCreatureDrainingEnd;

    public delegate void CreatureIdleDelegate(CreatureController creature);
    public event CreatureIdleDelegate OnCreatureIdle;

    public delegate void StartCreatureEatDelegate(CreatureController creature);
    public event StartCreatureEatDelegate OnCreatureBeginEat;

    public delegate void EndCreatureEatDelegate(CreatureController creature);
    public event EndCreatureEatDelegate OnCreatureEatEnd;

    public delegate void StartPacfyDelegate(CreatureController creature); 
    public event StartPacfyDelegate OnPacifyStart;

    public delegate void UpdatePacfyDelegate(CreatureController creature, float progress);
    public event UpdatePacfyDelegate OnPacifyUpdate;

    public delegate void EndPacfyDelegate(CreatureController creature, bool isCancelled);
    public event EndPacfyDelegate OnPacifyEnd;

    public delegate void StartPetDelegate(CreatureController creature);
    public event StartPetDelegate OnPetStart;

    public delegate void UpdatePetDelegate(CreatureController creature, float progress); 
    public event UpdatePetDelegate OnPetUpdate;

    public delegate void EndPetDelegate(CreatureController creature, bool success);
    public event EndPetDelegate OnPetEnd;

    public delegate void SeekDelegate(CreatureController creature, Vector3 position);
    public event SeekDelegate OnCreatureSeek;

    public delegate void StopSeekDelegate(CreatureController creature);
    public event StopSeekDelegate OnCreatureStopSeek;

    #endregion

    #region Handlers    

    public void HandleCreatureMoving(CreatureController creature, float speed)
    {
        OnCreatureMoving?.Invoke(creature, speed);
    }

    public void HandleCreatureDrainingStart(CreatureController creature)
    {
        OnCreatureDrainingStart?.Invoke(creature);
    }

    public void HandleCreatureDrainingEnd(CreatureController creature)
    {
        OnCreatureDrainingEnd?.Invoke(creature);
    }

    public void HandleCreatureIdle(CreatureController creature)
    {
        OnCreatureIdle?.Invoke(creature);
    }

    public void HandleCreatureBeginEat(CreatureController creature)
    {
        OnCreatureBeginEat?.Invoke(creature);
    }

    public void HandleCreatureEatEnd(CreatureController creature)
    {
        OnCreatureEatEnd?.Invoke(creature);
    }

    public void HandlePacifyStart(CreatureController creature) 
    {
        OnPacifyStart?.Invoke(creature);
    }

    public void HandlePacifyUpdate(CreatureController creature, float progress)
    {
        OnPacifyUpdate?.Invoke(creature, progress);
    }

    public void HandlePacifyEnd(CreatureController creature, bool isCancelled)
    {
        OnPacifyEnd?.Invoke(creature, isCancelled);
    }

    public void HandlePetStart(CreatureController creature)
    {
        OnPetStart?.Invoke(creature);
    }

    public void HandlePetUpdate(CreatureController creature, float progress)
    {
        OnPetUpdate?.Invoke(creature, progress);
    }

    public void HandlePetEnd(CreatureController creature, bool success)
    {
        OnPetEnd?.Invoke(creature, success);
    }

    public void HandleCreatureSeek(CreatureController creature, Vector3 position)
    {
        OnCreatureSeek?.Invoke(creature, position);
    }

    public void HandleCreatureStopSeek(CreatureController creature)
    {
        OnCreatureStopSeek?.Invoke(creature);
    }

    #endregion

}