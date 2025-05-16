using System;
using System.Collections.Generic;
using Game.Services.LightSources;
using UnityEngine;

namespace _System.Game_Manager
{
    public class EndLevelManager : MonoBehaviour
    {
        public static EndLevelManager instance;

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

        
        private List<LightSourceComponent> LightSourcesToCompleteFirstCheckPoint = new();
        private List<LightSourceComponent> LightSourcesToCompleteSecondCheckPoint = new();
        private List<LightSourceComponent> LightSourcesToCompleteThirdCheckPoint = new();
        private List<LightSourceComponent> LightSourcesToCompleteFourthCheckPoint = new();
        private List<LightSourceComponent> LightSourcesToCompleteFifthCheckPoint = new();

        public List<int> CheckPointsCompleted = new();
        
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
        }
        #endregion
        
        #region PRIVATE METHODS

        private void ListsLoader()
        {
            var _lightSources = GameObject.FindGameObjectsWithTag("LightSource");

            foreach (var VARIABLE in _lightSources)
            {
                var comp = VARIABLE.GetComponent<LightSourceComponent>();

                if (comp.LightGroupId == 0)
                {
                    Debug.Log("light source sans ID : " + VARIABLE.name);
                    return;
                }
                
                SortLightSources(comp.LightGroupId, comp);
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
            }
        }
        
        private bool CheckingEachSource(List<LightSourceComponent> lightSources)
        {
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
            Debug.Log("First Check Point : Activation");
        }

        private void SecondCheckPoint()
        {
            Debug.Log("Second Check Point : Activation");
        }

        private void ThirdCheckPoint()
        {
            Debug.Log("Third Check Point : Activation");
        }

        private void FourthCheckPoint()
        {
            Debug.Log("Fourth Check Point : Activation");
        }

        private void FifthCheckPoint()
        {
            Debug.Log("Fifth Check Point : Activation");
        }
        
        #endregion
    }
}
