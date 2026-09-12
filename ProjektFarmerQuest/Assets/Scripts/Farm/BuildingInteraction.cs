using UnityEngine;

public class BuildingInteraction : MonoBehaviour
{
    public GameObject barnMinigameButton;
    public GameObject fieldMinigameButton;

    void Start()
    {
        HideMinigameButtons();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckForBuildingClick();
        }
    }

    void CheckForBuildingClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            FarmBuilding building =
                hit.collider.GetComponent<FarmBuilding>();

            if (building == null)
            {
                return;
            }

            HideMinigameButtons();

            if (building.buildingType == FarmBuilding.BuildingType.Barn)
            {
                barnMinigameButton.SetActive(true);
                Debug.Log("Barn clicked");
            }
            else if (building.buildingType == FarmBuilding.BuildingType.Field)
            {
                fieldMinigameButton.SetActive(true);
                Debug.Log("Field clicked");
            }
        }
    }

    void HideMinigameButtons()
    {
        barnMinigameButton.SetActive(false);
        fieldMinigameButton.SetActive(false);
    }
}