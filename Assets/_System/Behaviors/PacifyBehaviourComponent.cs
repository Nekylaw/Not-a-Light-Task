using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine.ProBuilder.MeshOperations;

public class PacifyBehaviourComponent : MonoBehaviour
{
    [SerializeField] private GameObject canPacifyUI;

    public List<GameObject> creaturesCanBePacified = new List<GameObject>();
    public bool _canStartPacify;
    public bool _isInPacifyMode;

    public void OnPacifyStarted()
    {
        if (_canStartPacify = true)
        {  
            var targetCreature = creaturesCanBePacified[0];
            if (Input.GetKeyDown(KeyCode.F))
            {   canPacifyUI.GetComponent<TextMeshProUGUI>().text = "Hold V to pacify";
                _isInPacifyMode = true;
                targetCreature.transform.LookAt(this.transform);
                targetCreature.gameObject.GetComponent<NEW_IAController>().canWander = false;
            }
        }
    }

    public void ClosePacifyUI()
    {
        canPacifyUI.SetActive(false);
    }

    public void OnPacifyHold()
    {
        if (_isInPacifyMode)
        {
            Debug.Log("IN PACIFY MOD");
        }
        canPacifyUI.GetComponent<TextMeshProUGUI>().text = "PACIFYING CREATURE...";
    }
    private void OnTriggerStay(Collider other)
    {
      
        if (other.CompareTag("Creature") && !creaturesCanBePacified.Contains(other.gameObject))
        {
            _canStartPacify = true;
            creaturesCanBePacified.Add(other.gameObject);
            canPacifyUI.SetActive(true);
            canPacifyUI.GetComponent<TextMeshProUGUI>().text = "PACIFY [F]";
        }
        OnPacifyStarted();
        
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

    

}
