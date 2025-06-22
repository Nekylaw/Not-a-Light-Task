using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class MusicStopperComponent : MonoBehaviour
{
    [SerializeField] private List<EventReference> _excludedStems = new();
}
