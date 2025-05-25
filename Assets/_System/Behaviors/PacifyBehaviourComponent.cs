using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine.ProBuilder.MeshOperations;
using DG.Tweening;
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
                targetCreature.gameObject.GetComponent<NEW_IAController>().canWander = false;
                targetCreature.transform.LookAt(this.transform);
                
            }
        }
    }

    public void ClosePacifyUI()
    {
        canPacifyUI.SetActive(false);
    }

    public void OnPacifyHold()
    {
        var targetCreature = creaturesCanBePacified[0];
        var IAController = targetCreature.GetComponent<NEW_IAController>();
        float duration = 5;
        float minY = 5;
        float maxY;
        if (_isInPacifyMode && !targetCreature.GetComponent<NEW_IAController>().isPacified )
        {
            Debug.Log("IN PACIFY MOD");
            targetCreature.transform.DOLocalMoveY(minY,duration);
            IAController.isBeingPacified = true;
            StartCoroutine(IAController.OnEndPacify(targetCreature));
            IAController.StartPacifyEffects();
            canPacifyUI.GetComponent<TextMeshProUGUI>().text = "PACIFYING CREATURE...";
            


        }

       
    }
    private void OnTriggerStay(Collider other)
    {
      
        if (other.CompareTag("Creature") && !creaturesCanBePacified.Contains(other.gameObject) && !other.gameObject.GetComponent<NEW_IAController>().isPacified)
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
