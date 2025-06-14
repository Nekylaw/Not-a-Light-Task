using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PetManager : MonoBehaviour
{
    public static PetManager Instance { get; private set; }

    [SerializeField] private float petRange = 50f;
    private GameObject player;

    private List<GameObject> pacifiedCreatures = new List<GameObject>();

    private void Start()
    {
        player = GameObject.Find("PF_Player");
        InvokeRepeating("ChoosePet", 10f, 10f);
    }
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    #region Debug list UI
    /*
    [SerializeField] public TextMeshProUGUI debugList;
    private void Update()
    {
        //Debug.Log(pacifiedCreatures.Count);
        debugList.text = "pacified : \n";
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
        }

    }*/
    #endregion
    
    public void AddCreature(GameObject creature)
    {
        pacifiedCreatures.Add(creature);
        if (pacifiedCreatures.Count == 1)
        {
            pacifiedCreatures[0].GetComponent<NEW_IAController>().canBePet = true;
            StartCoroutine(pacifiedCreatures[0].GetComponent<NEW_IAController>().Scale());
        }
    }


    public void ChoosePet()
    {
        int nbPacified = pacifiedCreatures.Count;

        if (nbPacified == 0) return;

        else
        {
            NEW_IAController topCreature = pacifiedCreatures[0].GetComponent<NEW_IAController>();

            Vector2 posPlayerXZ = new Vector2(player.transform.position.x, player.transform.position.z);
            Vector2 posTargetXZ = new Vector2(pacifiedCreatures[0].transform.position.x, pacifiedCreatures[0].transform.position.z);
            float creatureDistanceXZ = Vector2.Distance(posPlayerXZ, posTargetXZ);

            if (topCreature.canBePet && creatureDistanceXZ < petRange) return;
            else
            {
                if (topCreature.canBePet)
                {
                    topCreature.canBePet = false;
                    StartCoroutine(topCreature.Scale());
                }

                List<int> petCandidatesID = new List<int>();

                for (int i = 1; i < pacifiedCreatures.Count; i++)
                {
                    Vector2 posCreatureXZ = new Vector2(pacifiedCreatures[i].transform.position.x, pacifiedCreatures[i].transform.position.z);

                    if (Vector2.Distance(posPlayerXZ, posCreatureXZ) < petRange)
                    {
                        petCandidatesID.Add(i);
                    }
                }
                if (petCandidatesID.Count > 0)
                {
                    int chosenCreatureID = Random.Range(0, petCandidatesID.Count);

                    pacifiedCreatures[petCandidatesID[chosenCreatureID]].GetComponent<NEW_IAController>().canBePet = true;
                    StartCoroutine(pacifiedCreatures[petCandidatesID[chosenCreatureID]].GetComponent<NEW_IAController>().Scale());

                    GameObject tempCreature = pacifiedCreatures[petCandidatesID[chosenCreatureID]].gameObject;
                    pacifiedCreatures.RemoveAt(petCandidatesID[chosenCreatureID]);
                    pacifiedCreatures.Insert(0, tempCreature);
                }
            }
        }
    }
}