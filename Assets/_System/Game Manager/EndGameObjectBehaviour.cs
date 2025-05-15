using System;
using UnityEngine;

public class EndGameObjectBehaviour : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("ENDGAME TRIGGERED");
        if (other.GetComponent<PlayerController>())
        {
            Debug.Log("PLAYER DETECTE");
            GameManager.Instance.EndGame();
        }
    }
}
