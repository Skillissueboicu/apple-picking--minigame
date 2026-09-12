using UnityEngine;

public class FarmBuildingPlacer : MonoBehaviour
{
    public GameObject barnPrefab;
    public GameObject fieldPrefab;

    private GameObject selectedBuilding;

    void Start()
    {
        selectedBuilding = null;
        RestoreBuildings();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuilding();
        }
    }

    public void SelectBarn()
    {
        selectedBuilding = barnPrefab;

        Debug.Log("Barn selected");
    }

    public void SelectField()
    {
        selectedBuilding = fieldPrefab;

        Debug.Log("Field selected");
    }

    void TryPlaceBuilding()
    {
        if (selectedBuilding == null)
        {
            return;
        }
        Ray ray =
            Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            FarmGridCell cell =
                hit.collider.GetComponent<FarmGridCell>();

            if (cell == null)
            {
                return;
            }

            if (cell.occupied)
            {
                Debug.Log(
                    $"Cell ({cell.x}, {cell.y}) is already occupied."
                );

                return;
            }
            GameObject building = Instantiate(
                selectedBuilding,
                cell.transform.position,
                Quaternion.identity
            );

            cell.placedBuilding = building;
            cell.occupied = true;

            FarmBuilding farmBuilding =
                building.GetComponent<FarmBuilding>();

            if (farmBuilding != null)
            {
                FarmStateManager.Instance.AddBuilding(
                    cell.x,
                    cell.y,
                    farmBuilding.buildingType
                );
            }
            selectedBuilding = null;

            Debug.Log(
                $"Placed {selectedBuilding.name} at ({cell.x}, {cell.y})"
            );
        }
    }
    void RestoreBuildings()
    {
        if (FarmStateManager.Instance == null)
        {
            return;
        }

        foreach (var data in FarmStateManager.Instance.placedBuildings)
        {
            FarmGridCell[] cells =
                FindObjectsByType<FarmGridCell>(
                    FindObjectsSortMode.None
                );

            foreach (FarmGridCell cell in cells)
            {
                if (cell.x == data.x && cell.y == data.y)
                {
                    GameObject prefab = null;

                    if (data.buildingType ==
                        FarmBuilding.BuildingType.Barn)
                    {
                        prefab = barnPrefab;
                    }
                    else if (data.buildingType ==
                             FarmBuilding.BuildingType.Field)
                    {
                        prefab = fieldPrefab;
                    }

                    if (prefab != null)
                    {
                        GameObject building = Instantiate(
                            prefab,
                            cell.transform.position,
                            Quaternion.identity
                        );

                        cell.placedBuilding = building;
                        cell.occupied = true;
                    }
                }
            }
        }
    }
}