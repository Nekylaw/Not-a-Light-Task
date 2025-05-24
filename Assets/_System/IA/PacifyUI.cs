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
    private bool _canStartPacify;
    private bool _isInPacifyMode;

    public void OnPacifyStarted()
    {
        if (_canStartPacify == true)
        {
            var targetCreature = creaturesCanBePacified[0];
             if (Input.GetKeyDown(KeyCode.P))
             {    canPacifyUI.GetComponentInChildren<TextMeshProUGUI>().text = "Hold P to pacify";
                  _isInPacifyMode = true;
                  targetCreature.transform.LookAt(this.transform);
                  targetCreature.gameObject.GetComponent<NEW_IAController>().canWander = false;
             }
        }
    }


    public void OnPacifyHold()
    {
        
    }
    private void OnTriggerStay(Collider other)
    {
      
        if (other.CompareTag("Creature") && !creaturesCanBePacified.Contains(other.gameObject))
        {
            _canStartPacify = true;
            creaturesCanBePacified.Add(other.gameObject);
            canPacifyUI.SetActive(true);
            canPacifyUI.GetComponentInChildren<TextMeshProUGUI>().text = "PACIFY [P]";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        creaturesCanBePacified[0].gameObject.GetComponent<NEW_IAController>().canWander = true;
        _canStartPacify = false;
        _isInPacifyMode = false;
        if (creaturesCanBePacified.Contains(other.gameObject))
        {   creaturesCanBePacified.Remove(other.gameObject);
            canPacifyUI.SetActive(false); 
        }
    }

    private void Update()
    {
        OnPacifyStarted();
    }
}
