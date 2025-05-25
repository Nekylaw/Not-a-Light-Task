using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine.ProBuilder.MeshOperations;
using DG.Tweening;
using UnityEngine.Serialization;

public class PacifyBehaviourComponent : MonoBehaviour
{
    [FormerlySerializedAs("canPacifyUI")] [SerializeField] private GameObject pacifyUI;

    public List<GameObject> creaturesCanBePacified = new List<GameObject>();
    public bool _canStartPacify;
    public bool _isInPacifyMode;

    
    #region PACIFY INTERACTIONS
    
    
    
    public void OnPacifyStarted()
    {
        if (_canStartPacify = true)
        {  
            var targetCreature = creaturesCanBePacified[0];
            if (Input.GetKeyDown(KeyCode.F))
            { 
                pacifyUI.GetComponent<TextMeshProUGUI>().text = "Hold V to pacify";
                _isInPacifyMode = true;
                targetCreature.gameObject.GetComponent<NEW_IAController>().canWander = false;
                targetCreature.transform.LookAt(this.transform);
                zooming = true;
                ZoomIn();
            }
        }
    }

    
    public void OnPacifyHold()
    {
        var targetCreature = creaturesCanBePacified[0];
        var IAController = targetCreature.GetComponent<NEW_IAController>();
        float duration = 5;
        float valueUpY = 5;
        if (_isInPacifyMode && !targetCreature.GetComponent<NEW_IAController>().isPacified )
        {
            targetCreature.transform.DOLocalMoveY(valueUpY,duration);
            IAController.isBeingPacified = true;
            
            StartCoroutine(IAController.OnEndPacify(targetCreature));
            IAController.StartPacifyEffects();
            pacifyUI.GetComponent<TextMeshProUGUI>().text = "PACIFYING CREATURE...";
        }

        
    }

    
    void ShowPacifyUI()
    {
        pacifyUI.SetActive(true);
    }
    
    
    public void HidePacifyUI()
    {
        pacifyUI.SetActive(false);
    }
    
    
    private void OnTriggerStay(Collider other)
    {
      
        if (other.CompareTag("Creature") && !creaturesCanBePacified.Contains(other.gameObject) && !other.gameObject.GetComponent<NEW_IAController>().isPacified)
        {
            _canStartPacify = true;
            creaturesCanBePacified.Add(other.gameObject);
            
            ShowPacifyUI();
            pacifyUI.GetComponent<TextMeshProUGUI>().text = "PACIFY [F]";
        }
        OnPacifyStarted();
        
    }

    
    private void OnTriggerExit(Collider other)
    {
        creaturesCanBePacified[0].gameObject.GetComponent<NEW_IAController>().canWander = true;
        _canStartPacify = false;
        _isInPacifyMode = false;
        if (creaturesCanBePacified.Contains(other.gameObject))
        {   
            creaturesCanBePacified.Remove(other.gameObject);
            HidePacifyUI(); 
        }
        ZoomOut();
    }

    
    #endregion
    
    
    #region  ZOOM EFFECT
    
    private int zoom = 30;
    bool zooming;
    int zoomOut  = 60;
    

    public void ZoomOut()
    {
        Camera.main.fieldOfView = zoomOut;
    }

    void ZoomIn()
    {
        Camera.main.fieldOfView = zoom;
    }
    
    #endregion
}
