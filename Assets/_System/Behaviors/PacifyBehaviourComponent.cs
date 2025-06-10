using UnityEngine;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

public class PacifyBehaviourComponent : MonoBehaviour
{

    public delegate void PacifyDelegate(GameObject creature);
    public event PacifyDelegate OnPacify;

    [FormerlySerializedAs("canPacifyUI")] [SerializeField] private GameObject pacifyUI;

    public List<GameObject> creaturesCanBePacified = new List<GameObject>();
    public bool _canStartPacify;
    public bool _isInPacifyMode;
    
    #region PACIFY INTERACTIONS

    void Update() 
    {
        if (creaturesCanBePacified.Count != 0)
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
        
    }
    
    public void OnPacifyStarted()
    {
        if (_canStartPacify == true)
        {  
            var targetCreature = creaturesCanBePacified[0];
            _isInPacifyMode = true;
            targetCreature.GetComponent<NEW_IAController>().canWander = false;
            targetCreature.transform.LookAt(this.transform);
            pacifyUI.GetComponent<TextMeshProUGUI>().text = "Hold V to pacify";
            
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
            
            
            StartCoroutine(targetController.OnEndPacify());
            Sequence sequence = DOTween.Sequence().SetEase(Ease.Linear);
            sequence.Append(targetCreature.transform.DOLocalMoveY(4,3).SetEase(Ease.OutQuad));
            sequence.Append(targetCreature.transform.DOLocalMoveY(0,2).SetEase(Ease.OutSine));
            
            
            targetController.StartPacifyEffects();
            
            pacifyUI.GetComponent<TextMeshProUGUI>().text = "PACIFYING CREATURE...";

            OnPacify?.Invoke(targetCreature);
        }
        
    }

    public void CancelPacify()
    {
        IsNotInPacifyMode(creaturesCanBePacified[0].GetComponent<NEW_IAController>());
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
            
        }
        OnPacifyStarted();
        
    }

    
    private void OnTriggerExit(Collider other)
    {
        if (creaturesCanBePacified.Count != 0)
        {
            IsNotInPacifyMode(creaturesCanBePacified[0].GetComponent<NEW_IAController>());
            if (creaturesCanBePacified.Contains(other.gameObject))
            {   
                creaturesCanBePacified.Remove(other.gameObject);
                HidePacifyUI(); 
            }
        }
        
    }

    
    #endregion


    private void IsNotInPacifyMode(NEW_IAController targetController)
    {
        _canStartPacify = false;
        _isInPacifyMode = false; 
        targetController.isBeingPacified = false;
        targetController.canWander = true;
        targetController.GetComponent<NavMeshAgent>().speed = 4;

    }


    private void IsInPacifyMode(NEW_IAController targetController)
    {
        targetController.isBeingPacified = true;
        targetController.canWander = false;
        targetController.isPacified = true;
        _canStartPacify = false;
        targetController.GetComponent<NavMeshAgent>().speed = 0;

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
