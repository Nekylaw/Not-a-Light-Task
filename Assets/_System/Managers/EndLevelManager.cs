using System;
using System.Collections.Generic;
using Game.Services.LightSources;
using UnityEngine;

namespace _System.Game_Manager
{
    public class EndLevelManager : MonoBehaviour
    {
        public static EndLevelManager instance;
        private static readonly int Color1 = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ListsLoader();
        }

        #region Lists

        
        [SerializeField] private List<LightSourceComponent> LightSourcesToCompleteFirstCheckPoint = new();
        [SerializeField] private List<LightSourceComponent> LightSourcesToCompleteSecondCheckPoint = new();
        [SerializeField] private List<LightSourceComponent> LightSourcesToCompleteThirdCheckPoint = new();
        [SerializeField] private List<LightSourceComponent> LightSourcesToCompleteFourthCheckPoint = new();
        [SerializeField] private List<LightSourceComponent> LightSourcesToCompleteFifthCheckPoint = new();
        [SerializeField] private List<LightSourceComponent> LightSourcesToCompleteSixthCheckPoint = new();

        public List<int> CheckPointsCompleted = new();
        
        #endregion
        
        #region Serialize Fields

        [SerializeField] private GameObject firstCpToOpen;
        [SerializeField] private GameObject secondCpToOpen;
        [SerializeField] private GameObject thirdCpToOpen;
        [SerializeField] private GameObject fourthCpToOpen;
        [SerializeField] private GameObject fifthCpToOpen;
        [SerializeField] private GameObject sixthCpToOpen;
        [SerializeField] private GameObject seventhCpToOpen;
        
        #endregion
        #region PUBLIC METHODS

        public void CheckLightSources(LightSourceComponent lightSource)
        {
            if (LightSourcesToCompleteFirstCheckPoint.Contains(lightSource) && !CheckPointsCompleted.Contains(1))
            {
                var completed = CheckingEachSource(LightSourcesToCompleteFirstCheckPoint);

                if (!completed) return;

                    Check(1);
                    FirstCheckPoint();
                
            }
            else if (LightSourcesToCompleteSecondCheckPoint.Contains(lightSource) && !CheckPointsCompleted.Contains(2))
            {
                var completed = CheckingEachSource(LightSourcesToCompleteSecondCheckPoint);

                if (!completed) return;

                Check(2);
                SecondCheckPoint();
            }
            else if (LightSourcesToCompleteThirdCheckPoint.Contains(lightSource) && !CheckPointsCompleted.Contains(3))
            {
                var completed = CheckingEachSource(LightSourcesToCompleteThirdCheckPoint);

                if (!completed) return;

                Check(3);
                ThirdCheckPoint();
            }
            else if (LightSourcesToCompleteFourthCheckPoint.Contains(lightSource) && !CheckPointsCompleted.Contains(4))
            {
                var completed = CheckingEachSource(LightSourcesToCompleteFourthCheckPoint);

                if (!completed) return;

                Check(4);
                FourthCheckPoint();
            }
            else if (LightSourcesToCompleteFifthCheckPoint.Contains(lightSource) && !CheckPointsCompleted.Contains(5))
            {
                var completed = CheckingEachSource(LightSourcesToCompleteFifthCheckPoint);

                if (!completed) return;
                
                Check(5);
                FifthCheckPoint();
            }

            else if (LightSourcesToCompleteSixthCheckPoint.Contains(lightSource) && !CheckPointsCompleted.Contains(6))
            {
                 var completed = CheckingEachSource(LightSourcesToCompleteSixthCheckPoint);
                 
                 if (!completed) return;
                 Check(6);
                 SixthCheckPoint();
            }
        }
        #endregion
        
        #region PRIVATE METHODS

        private void ListsLoader()
        {
            Debug.Log(LightSourcesService.Instance.LightSources.Length);
            foreach (var VARIABLE in LightSourcesService.Instance.LightSources)
            {
                Debug.Log(VARIABLE.name);
                if (VARIABLE.LightGroupId != 0)
                {
                    SortLightSources(VARIABLE.LightGroupId, VARIABLE);
                }
                
                Debug.Log("light source sans ID : " + VARIABLE.gameObject.name);
            }
        }

        private void SortLightSources(int i, LightSourceComponent lightSource)
        {
            switch (i)
            {
                case 1:
                    LightSourcesToCompleteFirstCheckPoint.Add(lightSource);
                    break;
                case 2:
                    LightSourcesToCompleteSecondCheckPoint.Add(lightSource);
                    break;
                case 3:
                    LightSourcesToCompleteThirdCheckPoint.Add(lightSource);
                    break;
                case 4:
                    LightSourcesToCompleteFourthCheckPoint.Add(lightSource);
                    break;
                case 5:
                    LightSourcesToCompleteFifthCheckPoint.Add(lightSource);
                    break;
                case 6 :
                    LightSourcesToCompleteSixthCheckPoint.Add(lightSource);
                    break;
            }
        }
        
        private bool CheckingEachSource(List<LightSourceComponent> lightSources)
        {
            Debug.Log("checking");
            foreach (var VARIABLE in lightSources)
            {
                if (!VARIABLE.IsLightOn)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void Check(int i)
        {
            CheckPointsCompleted.Add(i);
        }

        private void FirstCheckPoint()
        {
            firstCpToOpen.GetComponent<GrillageBehaviour>().OpenGateProperly();
        }

        private void SecondCheckPoint()
        {
            secondCpToOpen.GetComponent<GrillageBehaviour>().OpenGateProperly();
        }

        private void ThirdCheckPoint()
        {
            thirdCpToOpen.GetComponent<GrillageBehaviour>().OpenGateProperly();
            fourthCpToOpen.GetComponent<GrillageBehaviour>().OpenGateProperly();
        }

        private void FourthCheckPoint()
        {
            fifthCpToOpen.GetComponent<GrillageBehaviour>().OpenGateProperly();
        }

        private void FifthCheckPoint()
        {
            sixthCpToOpen.GetComponent<GrillageBehaviour>().OpenGateProperly();
        }

        private void SixthCheckPoint()
        {
            seventhCpToOpen.GetComponent<GrillageBehaviour>().OpenGateProperly();
        }
        
        #endregion
    }
}
