using System;
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine.ProBuilder.MeshOperations;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

public class PacifyBehaviourComponent : MonoBehaviour
{
    [FormerlySerializedAs("canPacifyUI")] [SerializeField] private GameObject pacifyUI;

    public List<GameObject> creaturesCanBePacified = new List<GameObject>();
    public bool _canStartPacify;
    public bool _isInPacifyMode;
    
    #region PACIFY INTERACTIONS

    void Update()
    { 
        var targetCreature = creaturesCanBePacified[0];
        if (_canStartPacify || targetCreature.GetComponent<NEW_IAController>().isBeingPacified)
        {
            targetCreature.transform.LookAt(this.gameObject.transform);

            targetCreature.GetComponent<NavMeshAgent>().speed = 0;
        }
        else
        {
            targetCreature.GetComponent<NavMeshAgent>().speed = 4 ;
        }
    }
    public void OnPacifyStarted()
    {
        if (_canStartPacify == true)
        {  
            var targetCreature = creaturesCanBePacified[0];
            if (Input.GetKeyDown(KeyCode.F))
            { 
                pacifyUI.GetComponent<TextMeshProUGUI>().text = "Hold V to pacify";
                _isInPacifyMode = true;
                targetCreature.GetComponent<NEW_IAController>().canWander = false;
                targetCreature.transform.LookAt(this.transform);
            }
        }
    }

    
    public void OnPacifyHold()
    {
        var targetCreature = creaturesCanBePacified[0];
        var targetController = targetCreature.GetComponent<NEW_IAController>();
        
        if (_isInPacifyMode && !targetCreature.GetComponent<NEW_IAController>().isPacified )
        {   
            
            IsInPacifyMode(targetController);
            Debug.Log(targetCreature.name  + " can wander: " + targetController.canWander);
            
            Sequence sequence = DOTween.Sequence().SetEase(Ease.Linear);
            sequence.Append(targetCreature.transform.DOLocalMoveY(4,5).SetEase(Ease.OutQuad));
            sequence.Append(targetCreature.transform.DOLocalMoveY(0,3).SetEase(Ease.OutSine));
            
            StartCoroutine(targetController.OnEndPacify());
            targetController.StartPacifyEffects();
            
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
        IsNotInPacifyMode(creaturesCanBePacified[0].GetComponent<NEW_IAController>());
        if (creaturesCanBePacified.Contains(other.gameObject))
        {   
            creaturesCanBePacified.Remove(other.gameObject);
            HidePacifyUI(); 
        }

    }

    
    #endregion


    private void IsNotInPacifyMode(NEW_IAController targetController)
    {
        _canStartPacify = false;
        _isInPacifyMode = false;   
        targetController.isBeingPacified = false;
        
    }


    private void IsInPacifyMode(NEW_IAController targetController)
    {
        targetController.isBeingPacified = true;
        targetController.canWander = false;
        targetController.isPacified = true;
        _canStartPacify = false;
    }
    
    #region  ZOOM EFFECT
    
    private int zoom = 30;
    bool zooming;
    int zoomOut  = 60;
    

    // public void ZoomOut()
    // {
    //     Camera.main.fieldOfView = zoomOut;
    // }
    //
    // void ZoomIn()
    // {
    //     Camera.main.fieldOfView = zoom;
    // }
    
    #endregion
}
