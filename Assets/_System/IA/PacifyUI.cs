using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PacifyUI : MonoBehaviour
{
    [SerializeField] private GameObject canPacifyUI;

    public List<GameObject> creaturesCanBePacified = new List<GameObject>();


    public void OnPacifyStarted()
    { 
        Debug.Log("OnPacifyStarted called");
        canPacifyUI.GetComponentInChildren<TextMeshProUGUI>().text = "Hold P to pacify";
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Creature") && !creaturesCanBePacified.Contains(other.gameObject))
        {
            creaturesCanBePacified.Add(other.gameObject);
            canPacifyUI.SetActive(true);
            canPacifyUI.GetComponentInChildren<TextMeshProUGUI>().text = "PACIFY [P]";
            if (Input.GetKeyDown(KeyCode.P))
            {
                OnPacifyStarted();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (creaturesCanBePacified.Contains(other.gameObject))
        {   creaturesCanBePacified.Remove(other.gameObject);
            canPacifyUI.SetActive(false); 
        }
    }
}
