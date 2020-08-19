using System;
using System.Linq;
using Base;
using Boo.Lang;
using DanielLochner.Assets.SimpleSideMenu;
using IO.Swagger.Model;
using Michsky.UI.ModernUIPack;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SimpleSideMenu))]
public class ActionPointAimingMenu : MonoBehaviour, IMenu {
    public Base.ActionPoint CurrentActionPoint;

    public GameObject PositionBlock, JointsBlock;

    [SerializeField]
    private TMPro.TMP_Text ActionPointName, OrientationsListLabel, JointsListLabel;

    [SerializeField]
    private ActionButton OrientationManualDefaultButton;

    [SerializeField]
    private Button AddOrientationUsingRobotButton;

    public DropdownParameter PositionRobotsList, JointsRobotsList, PositionEndEffectorList;

    public GameObject OrientationsDynamicList, JointsDynamicList;

    [SerializeField]
    private ConfirmationDialog ConfirmationDialog;

    [SerializeField]
    private AddOrientationMenu AddOrientationMenu;

    [SerializeField]
    private AddJointsMenu AddJointsMenu;

    [SerializeField]
    private OrientationJointsDetailMenu OrientationJointsDetailMenu;

    private SimpleSideMenu SideMenu;

    private void Start() {
        SideMenu = GetComponent<SimpleSideMenu>();
        ProjectManager.Instance.OnActionPointUpdated += OnActionPointUpdated;
        ProjectManager.Instance.OnActionPointOrientationAdded += OnOrientationAdded;
        ProjectManager.Instance.OnActionPointJointsAdded += OnJointsAdded;
    }

    private void OnJointsAdded(object sender, ActionPointJointsAddedEventArgs args) {
        if (CurrentActionPoint != null && CurrentActionPoint.GetId() == args.ActionPointID) {
            AddToJointsDynamicList(args.Joints);
        }
    }

    private void OnOrientationAdded(object sender, ActionPointOrientationAddedEventArgs args) {
        if (CurrentActionPoint != null && CurrentActionPoint.GetId() == args.ActionPointID) {
            AddToOrientationsDynamicList(args.Orientation);
        }
    }

    private void OnActionPointUpdated(object sender, ActionPointUpdatedEventArgs args) {
        if (CurrentActionPoint != null && CurrentActionPoint.Equals(args.Data)) {
            UpdateMenu();
            
        }
    }
    
    public void UpdateMenu() {
        ActionPointName.text = CurrentActionPoint.Data.Name;

        CustomDropdown positionRobotsListDropdown = PositionRobotsList.Dropdown;
        positionRobotsListDropdown.dropdownItems.Clear();
        PositionRobotsList.gameObject.GetComponent<DropdownRobots>().Init(OnRobotChanged, true);
        if (positionRobotsListDropdown.dropdownItems.Count == 0) {
            PositionBlock.SetActive(false);
            AddOrientationUsingRobotButton.interactable = false;
        } else {
            PositionBlock.SetActive(true);
            OnRobotChanged((string) PositionRobotsList.GetValue());
            AddOrientationUsingRobotButton.interactable = true;
        }

        JointsRobotsList.Dropdown.dropdownItems.Clear();
        JointsRobotsList.gameObject.GetComponent<DropdownRobots>().Init(UpdateJointsDynamicList, false);
        if (JointsRobotsList.Dropdown.dropdownItems.Count > 0) {
            JointsBlock.SetActive(true);
            UpdateJointsDynamicList((string) JointsRobotsList.GetValue());
        } else {
            JointsBlock.SetActive(false);
        }

        UpdateOrientationsDynamicList();
    }

    private void OnRobotChanged(string robot_name) {
        PositionEndEffectorList.Dropdown.dropdownItems.Clear();

        try {
            string robotId = SceneManager.Instance.RobotNameToId(robot_name);
            PositionEndEffectorList.gameObject.GetComponent<DropdownEndEffectors>().Init(robotId, null);

        } catch (ItemNotFoundException ex) {
            Debug.LogError(ex);
            Notifications.Instance.ShowNotification("Failed to load end effectors", "");
        }

    }

    public void ShowUpdatePositionConfirmationDialog() {
        ConfirmationDialog.Open("Update position",
                                "Do you want to update position of action point " + CurrentActionPoint.Data.Name,
                                () => UpdateActionPointPosition(),
                                () => ConfirmationDialog.Close());
    }

    private async void UpdateActionPointPosition() {
        try {
            string robotId = SceneManager.Instance.RobotNameToId(PositionRobotsList.Dropdown.selectedText.text);
            string endEffectorId = PositionEndEffectorList.Dropdown.selectedText.text;

            await WebsocketManager.Instance.UpdateActionPointUsingRobot(CurrentActionPoint.Data.Id, robotId, endEffectorId);
            ConfirmationDialog.Close();
        } catch (ItemNotFoundException ex) {
            Debug.LogError(ex);
            Notifications.Instance.ShowNotification("Update position failed", "");
        }
    }

    public void ShowMenu(Base.ActionPoint actionPoint) {
        CurrentActionPoint = actionPoint;
        OrientationManualDefaultButton.SetLabel(GameManager.Instance.ExpertMode ? "Manual" : "Default");
        UpdateMenu();
        SideMenu.Open();
    }

    public void Close() {
        SideMenu.Close();
    }

    public void UpdateOrientationsDynamicList() {
        foreach (RectTransform o in OrientationsDynamicList.GetComponentsInChildren<RectTransform>()) {
            if (o.gameObject.tag != "Persistent") {
                Destroy(o.gameObject);
            }
        }

        foreach (IO.Swagger.Model.NamedOrientation orientation in CurrentActionPoint.GetNamedOrientations()) {
            InstantiateActionButton(orientation);
        }
        if (CurrentActionPoint.GetNamedOrientations().Any()) {
            OrientationsListLabel.text = "List of orientations:";
        } else {
            OrientationsListLabel.text = "There is no orientation yet.";
        }
    }
    /// <summary>
    /// Adds and highlights new orientation button in dynamic list of orientations
    /// </summary>
    /// <param name="orientation">New orientation to add</param>
    private void AddToOrientationsDynamicList(NamedOrientation orientation) {
        ActionButton button = InstantiateActionButton(orientation);
        button.GetComponent<ActionButton>().Highlight(2f);
        OrientationsListLabel.text = "List of orientations:";
    }

    /// <summary>
    /// Instantiates button representing orientation in OrientationDynamicList
    /// </summary>
    /// <param name="orientation">Orientation to be represented by the button</param>
    /// <returns></returns>
    private ActionButton InstantiateActionButton(NamedOrientation orientation) {
        ActionButton btn = Instantiate(Base.GameManager.Instance.ButtonPrefab, OrientationsDynamicList.transform).GetComponent<ActionButton>();
        btn.transform.localScale = new Vector3(1, 1, 1);
        btn.SetLabel(orientation.Name);

        btn.Button.onClick.AddListener(() => OpenDetailMenu(orientation));
        return btn;
    }

    /// <summary>
    /// Instantiates button representing joints in JointsDynamicList
    /// </summary>
    /// <param name="joints">Joints to be represented by the button</param>
    /// <returns></returns>
    private ActionButton InstantiateActionButton(ProjectRobotJoints joints) {
        ActionButton btn = Instantiate(Base.GameManager.Instance.ButtonPrefab, JointsDynamicList.transform).GetComponent<ActionButton>();
        btn.transform.localScale = new Vector3(1, 1, 1);
        btn.SetLabel(joints.Name);

        btn.Button.onClick.AddListener(() => OpenDetailMenu(joints));
        return btn;
    }

    public void UpdateJointsDynamicList(string robotName) {
        if (robotName == null)
            return;

        try {
            string robotId = SceneManager.Instance.RobotNameToId(robotName);

            foreach (RectTransform o in JointsDynamicList.GetComponentsInChildren<RectTransform>()) {
                if (o.gameObject.tag != "Persistent") {
                    Destroy(o.gameObject);
                }
            }

            System.Collections.Generic.List<ProjectRobotJoints> joints = CurrentActionPoint.GetAllJoints(true, robotId).Values.ToList();
            foreach (IO.Swagger.Model.ProjectRobotJoints joint in joints) {
                InstantiateActionButton(joint);
            }
        } catch (ItemNotFoundException ex) {
            Debug.LogError(ex);
            Notifications.Instance.ShowNotification("Failed to get robot's ID", "");
            return;
        }
    }

    /// <summary>
    /// Adds and highlights new joints button in dynamic list of joints
    /// </summary>
    /// <param name="joints">New joints to add</param>
    private void AddToJointsDynamicList(ProjectRobotJoints joints) {
        ActionButton button = InstantiateActionButton(joints);
        button.GetComponent<ActionButton>().Highlight(2f);
        Notifications.Instance.ShowNotification("method addtojountsdynamiclist called", "");
    }


    private void OpenDetailMenu(ProjectRobotJoints joint) {
        OrientationJointsDetailMenu.ShowMenu(CurrentActionPoint, joint);
    }

    private void OpenDetailMenu(NamedOrientation orientation) {
        OrientationJointsDetailMenu.ShowMenu(CurrentActionPoint, orientation);
    }

    /// <summary>
    /// If expert mode is active - opens add orientation side menu in manual mode, otherwise adds default orientation (0,0,0,1)
    /// </summary>
    public void OpenAddOrientationMenuManualDefault() {
        if (GameManager.Instance.ExpertMode) {
            AddOrientationMenu.ShowMenu(CurrentActionPoint, true);
        } else {
            AddDefaultOrientation();
        }
    }

    public void OpenAddOrientationMenuUsingRobot() {
        AddOrientationMenu.ShowMenu(CurrentActionPoint, false);
    }

    public void OpenAddJointsMenu(bool manual) {
        AddJointsMenu.ShowMenu(CurrentActionPoint, manual);
    }

    public async void AddDefaultOrientation() {
        try {
            name = CurrentActionPoint.GetFreeOrientationName();
            await WebsocketManager.Instance.AddActionPointOrientation(CurrentActionPoint.Data.Id, new Orientation(), name);
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Failed to add new orientation", ex.Message);
        }
    }
}
