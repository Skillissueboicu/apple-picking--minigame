using System.Collections.Generic;
using UnityEngine;

public class FarmStateManager : MonoBehaviour
{
    public static FarmStateManager Instance;

    public class PlacedBuildingData
    {
        public int x;
        public int y;
        public FarmBuilding.BuildingType buildingType;

        public PlacedBuildingData(
            int x,
            int y,
            FarmBuilding.BuildingType buildingType)
        {
            this.x = x;
            this.y = y;
            this.buildingType = buildingType;
        }
    }

    public List<PlacedBuildingData> placedBuildings =
        new List<PlacedBuildingData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void AddBuilding(
        int x,
        int y,
        FarmBuilding.BuildingType buildingType)
    {
        placedBuildings.Add(
            new PlacedBuildingData(x, y, buildingType)
        );
    }
}