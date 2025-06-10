using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using DG.Tweening;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

public class PacifyBehaviourComponent : MonoBehaviour
{

    public delegate void PacifyDelegate(GameObject creature);
    public event PacifyDelegate OnPacify;
    [SerializeField] private GameObject pacifyUI;

    public  List<GameObject> creaturesCanBePacified = new List<GameObject>();
    public bool canStartPacify;
    public bool isInPacifyMode;
    public bool isPacifyCanceled = false;
    
   
    
    #region PACIFY INTERACTIONS

    public GameObject TargetCreature()
    {
        return creaturesCanBePacified[0];
    }
    
    void Update() 
    {
        if (creaturesCanBePacified.Count != 0)
        {
           
            if (canStartPacify || TargetCreature().GetComponent<NEW_IAController>().isBeingPacified)
            {
                TargetCreature().transform.LookAt(this.gameObject.transform);
            
                TargetCreature().GetComponent<NavMeshAgent>().speed = 0;
            }
            else
            {
                TargetCreature().GetComponent<NavMeshAgent>().speed = 4 ;
            }
            
        }
        
    }
    
    public void OnPacifyStarted()
    {
        if (canStartPacify == true)
        {  
            isInPacifyMode = true;
            TargetCreature().GetComponent<NEW_IAController>().canWander = false;
            TargetCreature().transform.LookAt(this.transform);
            pacifyUI.GetComponent<TextMeshProUGUI>().text = "Hold V to pacify";
            
        }
    }

    
    public Sequence sequence;
    public void OnPacifyHold()
    {
       
        var targetController = TargetCreature().GetComponent<NEW_IAController>();
       
        if (isInPacifyMode && !TargetCreature().GetComponent<NEW_IAController>().isPacified )
        {
            
            sequence?.Kill();
            sequence = DOTween.Sequence();
            
            IsInPacifyMode(targetController);
            Debug.Log(TargetCreature().name  + " can wander: " + targetController.canWander);
            
           
            sequence.Append(TargetCreature().transform.DOLocalMoveY(4,3).SetEase(Ease.OutQuad));
            sequence.Append(TargetCreature().transform.DOLocalMoveY(0,2).SetEase(Ease.OutSine));

            if (TargetCreature().transform.position.y >= 4f)
            {
                targetController.isPacified = true;
            }
            
            targetController.StartPacifyEffects(true);
            
            pacifyUI.GetComponent<TextMeshProUGUI>().text = "PACIFYING CREATURE...";

            OnPacify?.Invoke(TargetCreature());
            
        }
        
    }
    
    public void CancelPacify()
    {
        
        TargetCreature().GetComponent<NEW_IAController>().StartPacifyEffects(false);
        
        StartCoroutine(StopAnim());
               
        sequence.Append(TargetCreature().transform.DOLocalMoveY(0,1).SetEase(Ease.OutQuad));
        
        isPacifyCanceled = true;
        canStartPacify = true;
        isInPacifyMode = false;
        
        TargetCreature().GetComponent<NEW_IAController>().isBeingPacified = false;
    }


    public IEnumerator StopAnim()
    {
        yield return new WaitForSeconds(0.1f);
        sequence.Kill();
        sequence = null;
    }
        
    public void OnEndPacify()
    {
        
        var targetController = TargetCreature().GetComponent<NEW_IAController>();
             
        targetController.canWander = true;
        targetController.isPacified = true;
        targetController.isBeingPacified = false;
                    
        foreach (var orb in targetController.orbsEaten)
        { 
            orb.SetActive(true);
            orb.transform.position = this.transform.position;
            Debug.Log("creature pacified : orb given back !");
        }
                    
        targetController.orbsEaten.Clear();
                        
        PetManager.Instance.AddCreature(this.gameObject);
             
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
            canStartPacify = true;
            creaturesCanBePacified.Add(other.gameObject);
            ShowPacifyUI();
            
        }
        OnPacifyStarted();
        
    }

    
    private void OnTriggerExit(Collider other)
    {
        if (creaturesCanBePacified.Count != 0)
        {
            IsNotInPacifyMode(TargetCreature().GetComponent<NEW_IAController>());
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
        canStartPacify = false;
        isInPacifyMode = false; 
        targetController.isBeingPacified = false;
        targetController.canWander = true;
        targetController.GetComponent<NavMeshAgent>().speed = 4;

    }


    private void IsInPacifyMode(NEW_IAController targetController)
    {
        targetController.isBeingPacified = true;
        targetController.canWander = false;
        canStartPacify = false;
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
