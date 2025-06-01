using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PetManager : MonoBehaviour
{
    public static PetManager Instance { get; private set; }
    [SerializeField] public TextMeshProUGUI debugList;

    private List<GameObject> PacifiedCreatures = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        //Debug.Log(PacifiedCreatures.Count);
        debugList.text = "pacified : \n";
        foreach (GameObject p in PacifiedCreatures)
        {
            debugList.text += p.name;
            if (p.GetComponent<NEW_IAController>().CanBePet)
            {
                debugList.text += "OUI \n";
            }
        }
    }
    public void AddCreature(GameObject creature)
    {
        PacifiedCreatures.Add(creature);
        ChoosePet(true);
    }

    public void ChoosePet(bool IsAdding)
    {
        int NbCreatures = PacifiedCreatures.Count;

        if (IsAdding)
        {
            if (NbCreatures > 2)
                return;
            else
            {
                if (!PacifiedCreatures[0].GetComponent<NEW_IAController>().CanBePet)
                {
                    PacifiedCreatures[NbCreatures - 1].GetComponent<NEW_IAController>().CanBePet = true;
                    
                    GameObject temp = PacifiedCreatures[NbCreatures -1].gameObject;
                    PacifiedCreatures.RemoveAt(NbCreatures - 1);
                    PacifiedCreatures.Insert(0, temp);
                }
                
            }
        }
        else
        {
            if (NbCreatures == 1)
                return;
            else
            {
                int choice = Random.Range(1, NbCreatures);
                PacifiedCreatures[choice].GetComponent<NEW_IAController>().CanBePet = true;
                GameObject temp = PacifiedCreatures[choice];
                PacifiedCreatures.RemoveAt(choice);
                PacifiedCreatures.Insert(0, temp);
                Debug.Log(choice);
            }
        }
    }
}