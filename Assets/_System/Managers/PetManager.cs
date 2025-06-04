using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PetManager : MonoBehaviour
{
    public static PetManager Instance { get; private set; }
    [SerializeField] public TextMeshProUGUI debugList;

    private List<GameObject> pacifiedCreatures = new List<GameObject>();

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
        //Debug.Log(pacifiedCreatures.Count);
        /*debugList.text = "pacified : \n";
        if (pacifiedCreatures.Count != 0)
        {
            foreach (GameObject p in pacifiedCreatures)
            {
                debugList.text += p.name;
                if (p.GetComponent<NEW_IAController>().canBePet)
                {
                    debugList.text += "OUI \n";
                }
            }
        }*/

    }
    public void AddCreature(GameObject creature)
    {
        pacifiedCreatures.Add(creature);
        ChoosePet(true);
    }

    public void ChoosePet(bool IsAdding)
    {
        int NbCreatures = pacifiedCreatures.Count;

        if (IsAdding)
        {
            if (NbCreatures > 2)
                return;
            else
            {
                if (!pacifiedCreatures[0].GetComponent<NEW_IAController>().canBePet)
                {
                    SetChoice(NbCreatures - 1);
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
                SetChoice(choice);
            }
        }
    }

    private void SetChoice(int chosenCreature)
    {
        pacifiedCreatures[chosenCreature].GetComponent<NEW_IAController>().canBePet = true;
        GameObject temp = pacifiedCreatures[chosenCreature].gameObject;
        pacifiedCreatures.RemoveAt(chosenCreature);
        pacifiedCreatures.Insert(0, temp);
        StartCoroutine(pacifiedCreatures[0].GetComponent<NEW_IAController>().Scale(pacifiedCreatures[0].transform, true));
    }
}