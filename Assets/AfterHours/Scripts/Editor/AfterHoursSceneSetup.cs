using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

namespace AfterHours.EditorTools
{
    /// <summary>
    /// After Hours 테스트 씬을 생성하는 에디터 전용 도구입니다.
    /// </summary>
    public static class AfterHoursSceneSetup
    {
        private const string ScenePath = "Assets/AfterHours/Scenes/PlayerMovementTest.unity";
        private const string AstronautModelPath = "Assets/Asset/Stylized_Astronaut/Character/Astronaut.fbx";
        private const string AstronautAnimatorControllerPath = "Assets/Asset/Stylized_Astronaut/Character/AstronautCharacterController.controller";
        private const string GrabTargetModelPath = "Assets/Asset/kenney_blocky-characters_20/Models/FBX format/character-g.fbx";
        private const string GrabPackModelPath = "Assets/Asset/poppy-playtime-grabpack/source/GrabPack Wack A Wuggy/sourse/Grab Pack Rig by D1GQ.fbx";
        private const string SpaceStationModelRoot = "Assets/Asset/kenney_space-station-kit/Models/FBX format/";
        private const float OriginalTileSpacing = 2f;
        private const float FloorTileSpacing = 5f;
        private const float FloorTileSize = 5f;
        private const float FloorColliderThickness = 0.35f;
        private const float WallBlockHeight = 10f;
        private const float CeilingHeight = 10.2f;
        private const float LayoutScale = FloorTileSpacing / OriginalTileSpacing;
        private static readonly Vector3 LargeWallScale = new Vector3(5f, WallBlockHeight, 2.5f);

        [MenuItem("After Hours/Setup/Create Player Movement Test Scene")]
        public static void CreatePlayerMovementTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PlayerMovementTest";

            CreateEscapeMapLayout();

            GameObject player = new GameObject("Player_Astronaut");
            player.tag = "Player";
            player.transform.position = ScaleMapPosition(new Vector3(0f, 1.05f, -18f));

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0f, 0f);
            characterController.stepOffset = 0.3f;

            PlayerMovement playerMovement = player.AddComponent<PlayerMovement>();

            GameObject astronautModel = AssetDatabase.LoadAssetAtPath<GameObject>(AstronautModelPath);
            if (astronautModel == null)
            {
                Debug.LogError($"Astronaut 모델을 찾을 수 없습니다: {AstronautModelPath}");
                return;
            }

            GameObject astronaut = (GameObject)PrefabUtility.InstantiatePrefab(astronautModel);
            astronaut.name = "Astronaut Visual";
            astronaut.transform.SetParent(player.transform);
            astronaut.transform.localPosition = new Vector3(0f, -1.05f, 0f);
            astronaut.transform.localRotation = Quaternion.identity;
            astronaut.transform.localScale = Vector3.one;
            ApplyAstronautMaterials(astronaut);
            AssignAstronautAnimator(astronaut);

            CreateNeckGrabPackVisual(astronaut.transform);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 2.7f, 0f);
            cameraObject.transform.rotation = Quaternion.identity;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            cameraObject.AddComponent<AudioListener>();
            Type thirdPersonCameraType = Type.GetType("ThirdPersonCamera, Assembly-CSharp");
            if (thirdPersonCameraType == null)
            {
                Debug.LogError("ThirdPersonCamera 타입을 찾을 수 없습니다.");
                return;
            }

            Component thirdPersonCamera = cameraObject.AddComponent(thirdPersonCameraType);

            CreateGrabTargetTestObject();
            CreateEnergyCoreTestObject();
            CreateCoreStationTestObjects();
            CreateGrabPackTestObjects(player, cameraObject);
            CreateMissionGuideObjects();
            CreateCheckpointRespawnObjects(player);

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            SerializedObject serializedMovement = new SerializedObject(playerMovement);
            serializedMovement.FindProperty("characterController").objectReferenceValue = characterController;
            serializedMovement.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            serializedMovement.FindProperty("characterAnimator").objectReferenceValue = astronaut.GetComponentInChildren<Animator>();
            serializedMovement.FindProperty("walkSpeed").floatValue = 5f;
            serializedMovement.FindProperty("runSpeed").floatValue = 9f;
            serializedMovement.FindProperty("jumpHeight").floatValue = 1.5f;
            serializedMovement.FindProperty("gravity").floatValue = -20f;
            serializedMovement.FindProperty("rotationSmoothTime").floatValue = 0.1f;
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedCamera = new SerializedObject(thirdPersonCamera);
            serializedCamera.FindProperty("target").objectReferenceValue = player.transform;
            serializedCamera.FindProperty("sensitivity").floatValue = 0.1f;
            serializedCamera.FindProperty("distance").floatValue = 0f;
            serializedCamera.FindProperty("height").floatValue = 1.65f;
            serializedCamera.FindProperty("minPitch").floatValue = -30f;
            serializedCamera.FindProperty("maxPitch").floatValue = 60f;
            serializedCamera.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"PlayerMovement 테스트 씬 생성 완료: {ScenePath}");
        }

        public static void CreatePlayerMovementTestSceneAndValidate()
        {
            CreatePlayerMovementTestScene();

            GameObject player = GameObject.Find("Player_Astronaut");
            GameObject cameraObject = GameObject.Find("Main Camera");
            GameObject visual = GameObject.Find("Astronaut Visual");
            GameObject grabPackVisual = GameObject.Find("Poppy GrabPack Visual");
            GameObject grabTarget = GameObject.Find("GrabTarget_CharacterG");
            GameObject energyCore = GameObject.Find("EnergyCore_Test");
            GameObject coreStation = GameObject.Find("CoreStation_Test");
            GameObject securityDoor = GameObject.Find("SecurityDoor_Core_Test");
            GameObject escapeMap = GameObject.Find("EscapeMap_Kenney");
            GameObject missionManager = GameObject.Find("MissionManager");
            GameObject missionCanvas = GameObject.Find("MissionCanvas");
            GameObject respawnManager = GameObject.Find("CheckpointRespawnManager");
            GameObject killZone = GameObject.Find("Map_KillZone");
            GameObject leftGrabMuzzle = GameObject.Find("LeftGrab_Muzzle");
            GameObject rightGrabMuzzle = GameObject.Find("RightGrab_Muzzle");
            GameObject leftGrabHoldPoint = GameObject.Find("LeftGrabHoldPoint");
            GameObject rightGrabHoldPoint = GameObject.Find("RightGrabHoldPoint");
            GameObject leftGrabArmVisual = FindSceneObjectIncludingInactive("LeftGrab_ArmVisual");
            GameObject rightGrabArmVisual = FindSceneObjectIncludingInactive("RightGrab_ArmVisual");
            GameObject leftGrabHandVisual = FindSceneObjectIncludingInactive("LeftGrab_HandVisual");
            GameObject rightGrabHandVisual = FindSceneObjectIncludingInactive("RightGrab_HandVisual");

            if (player == null || cameraObject == null || visual == null || grabPackVisual == null || grabTarget == null || energyCore == null || coreStation == null || securityDoor == null || escapeMap == null || missionManager == null || missionCanvas == null || respawnManager == null || killZone == null || leftGrabMuzzle == null || rightGrabMuzzle == null || leftGrabHoldPoint == null || rightGrabHoldPoint == null || leftGrabArmVisual == null || rightGrabArmVisual == null || leftGrabHandVisual == null || rightGrabHandVisual == null)
            {
                Debug.LogError("테스트 씬 필수 오브젝트 생성에 실패했습니다.");
                EditorApplication.Exit(1);
                return;
            }

            if (player.GetComponent<CharacterController>() == null || player.GetComponent<PlayerMovement>() == null)
            {
                Debug.LogError("Player_Astronaut에 이동 관련 컴포넌트가 없습니다.");
                EditorApplication.Exit(1);
                return;
            }

            Type thirdPersonCameraType = Type.GetType("ThirdPersonCamera, Assembly-CSharp");
            if (thirdPersonCameraType == null || cameraObject.GetComponent(thirdPersonCameraType) == null)
            {
                Debug.LogError("Main Camera에 ThirdPersonCamera 컴포넌트가 없습니다.");
                EditorApplication.Exit(1);
                return;
            }

            Type grabTargetType = Type.GetType("GrabTarget, Assembly-CSharp");
            if (grabTargetType == null || grabTarget.GetComponent<Rigidbody>() == null || grabTarget.GetComponent(grabTargetType) == null)
            {
                Debug.LogError("GrabTarget_CharacterG에 Rigidbody 또는 GrabTarget 컴포넌트가 없습니다.");
                EditorApplication.Exit(1);
                return;
            }

            Type energyCoreType = Type.GetType("EnergyCore, Assembly-CSharp");
            if (energyCoreType == null || energyCore.GetComponent<Rigidbody>() == null || energyCore.GetComponent(grabTargetType) == null || energyCore.GetComponent(energyCoreType) == null)
            {
                Debug.LogError("EnergyCore_Test에 Rigidbody, GrabTarget 또는 EnergyCore 컴포넌트가 없습니다.");
                EditorApplication.Exit(1);
                return;
            }

            Type coreStationType = Type.GetType("CoreStation, Assembly-CSharp");
            Type securityDoorType = Type.GetType("SecurityDoor, Assembly-CSharp");
            if (coreStationType == null || securityDoorType == null || coreStation.GetComponent(coreStationType) == null || securityDoor.GetComponent(securityDoorType) == null)
            {
                Debug.LogError("CoreStation_Test 또는 SecurityDoor_Test 컴포넌트가 없습니다.");
                EditorApplication.Exit(1);
                return;
            }

            Type grabPackControllerType = Type.GetType("GrabPackController, Assembly-CSharp");
            if (grabPackControllerType == null || player.GetComponent(grabPackControllerType) == null)
            {
                Debug.LogError("Player_Astronaut에 GrabPackController 컴포넌트가 없습니다.");
                EditorApplication.Exit(1);
                return;
            }

            if (missionManager.GetComponent<MissionManager>() == null || missionCanvas.GetComponentInChildren<UIManager>() == null)
            {
                Debug.LogError("미션 안내 UI 또는 MissionManager 생성에 실패했습니다.");
                EditorApplication.Exit(1);
                return;
            }

            if (respawnManager.GetComponent<CheckpointRespawnManager>() == null || killZone.GetComponent<KillZone>() == null)
            {
                Debug.LogError("체크포인트 리스폰 시스템 생성에 실패했습니다.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("PlayerMovement 테스트 씬 검증 완료: Astronaut, Animator, Poppy GrabPack Visual, Grab Arm Visuals, CharacterController, PlayerMovement, ThirdPersonCamera, GrabTarget, EnergyCore, CoreStation, SecurityDoor, GrabPackController, Mission UI, Main Camera 연결 확인");
            EditorApplication.Exit(0);
        }

        private static GameObject FindSceneObjectIncludingInactive(string objectName)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject sceneObject in objects)
            {
                if (sceneObject.name == objectName && sceneObject.scene.IsValid())
                {
                    return sceneObject;
                }
            }

            return null;
        }

        private static void CreateEscapeMapLayout()
        {
            GameObject mapRoot = new GameObject("EscapeMap_Kenney");
            bool useCompactLevelLayout = EditorPrefs.GetBool("AfterHours.UseCompactLevelLayout", true);
            if (useCompactLevelLayout)
            {
                CreateCompactEscapeMapLayout(mapRoot.transform);
                return;
            }

            // 레벨링 흐름: 시작실 -> 연습실 -> 긴 복도 -> 코어룸 -> 보안 복도 -> 최종 홀.
            CreateFloorGrid(mapRoot.transform, "StartRoomFloor", -4, 4, -8, -4, "floor-panel.fbx");
            CreateFloorGrid(mapRoot.transform, "TrainingRoomFloor", -5, 5, -3, 4, "floor-panel.fbx");
            CreateFloorGrid(mapRoot.transform, "NarrowCorridorFloor", -1, 1, 5, 15, "floor-panel-straight.fbx");
            CreateFloorGrid(mapRoot.transform, "CoreLabFloor", -6, 6, 16, 24, "floor-panel.fbx");
            CreateFloorGrid(mapRoot.transform, "SecurityCorridorFloor", -1, 1, 25, 32, "floor-panel-straight.fbx");
            CreateFloorGrid(mapRoot.transform, "FinalHallFloor", -7, 7, 33, 43, "floor-panel.fbx");

            // 천장을 추가해 외부 통로가 아니라 폐쇄된 실내 시설처럼 보이게 구성합니다.
            CreateCeilingGrid(mapRoot.transform, "StartRoomCeiling", -4, 4, -8, -4, "floor-panel.fbx");
            CreateCeilingGrid(mapRoot.transform, "TrainingRoomCeiling", -5, 5, -3, 4, "floor-panel.fbx");
            CreateCeilingGrid(mapRoot.transform, "NarrowCorridorCeiling", -1, 1, 5, 15, "floor-panel-straight.fbx");
            CreateCeilingGrid(mapRoot.transform, "CoreLabCeiling", -6, 6, 16, 24, "floor-panel.fbx");
            CreateCeilingGrid(mapRoot.transform, "SecurityCorridorCeiling", -1, 1, 25, 32, "floor-panel-straight.fbx");
            CreateCeilingGrid(mapRoot.transform, "FinalHallCeiling", -7, 7, 33, 43, "floor-panel.fbx");

            // 난이도 확장을 위한 추가 방 5개입니다. 지금은 배치용 빈 공간이고, 이후 퍼즐을 하나씩 넣습니다.
            CreateLeftSideRoom(mapRoot.transform, "StorageRoom_01", -11, -7, -1, 3);
            CreateLeftSideRoom(mapRoot.transform, "MaintenanceRoom_02", -10, -3, 9, 13);
            CreateRightSideRoom(mapRoot.transform, "LabRoom_03", 8, 12, 18, 22);
            CreateRightSideRoom(mapRoot.transform, "SecurityRoom_04", 3, 7, 26, 30);
            CreateLeftSideRoom(mapRoot.transform, "FinalPuzzleRoom_05", -13, -9, 35, 40);
            CreateInlineRoom(mapRoot.transform, "AirlockRoom_06", -5, 5, 44, 48);
            CreateInlineRoom(mapRoot.transform, "DecontaminationRoom_07", -5, 5, 49, 53);
            CreateInlineRoom(mapRoot.transform, "EscapeBay_08", -7, 7, 54, 60);

            // 레벨링 동선: 각 방을 하나의 흐름으로 이어주는 연결 복도입니다.
            CreateConnectorCorridor(mapRoot.transform, "Connector_Training_To_Storage", -6, -5, 0, 2);
            CreateConnectorCorridor(mapRoot.transform, "Connector_Storage_To_Maintenance", -9, -7, 4, 8);
            CreateConnectorCorridor(mapRoot.transform, "Connector_Maintenance_To_Main", -2, -1, 10, 13);
            CreateConnectorCorridor(mapRoot.transform, "Connector_CoreLab_To_Lab", 7, 8, 19, 22);
            CreateConnectorCorridor(mapRoot.transform, "Connector_Lab_To_SecurityRoom", 5, 7, 23, 25);
            CreateConnectorCorridor(mapRoot.transform, "Connector_SecurityRoom_To_Main", 1, 3, 27, 30);
            CreateConnectorCorridor(mapRoot.transform, "Connector_Final_To_Puzzle", -8, -7, 36, 40);
            CreateConnectorCorridor(mapRoot.transform, "Connector_Puzzle_To_Airlock", -8, -5, 41, 44);

            // 높은 벽을 두 줄로 쌓아 실내 시설처럼 보이게 구성합니다.
            CreateHighWallRun(mapRoot.transform, -9f, -17f, -7f, true, 6, "Start_Left_Wall");
            CreateHighWallRun(mapRoot.transform, 9f, -17f, -7f, true, 6, "Start_Right_Wall");
            CreateHighWallRun(mapRoot.transform, -8f, -17f, 8f, false, 9, "Start_Back_Wall");
            CreateHighWallRun(mapRoot.transform, -11f, -7f, 9f, true, 9, "Training_Left_Wall");
            CreateHighWallRun(mapRoot.transform, 11f, -7f, 9f, true, 9, "Training_Right_Wall");
            CreateHighWallRun(mapRoot.transform, -3f, 10f, 30f, true, 11, "Corridor_Left_Wall");
            CreateHighWallRun(mapRoot.transform, 3f, 10f, 30f, true, 11, "Corridor_Right_Wall");
            CreateHighWallRun(mapRoot.transform, -13f, 31f, 49f, true, 10, "CoreLab_Left_Wall");
            CreateHighWallRun(mapRoot.transform, 13f, 31f, 49f, true, 10, "CoreLab_Right_Wall");
            CreateHighWallRun(mapRoot.transform, -3f, 50f, 64f, true, 8, "Security_Left_Wall");
            CreateHighWallRun(mapRoot.transform, 3f, 50f, 64f, true, 8, "Security_Right_Wall");
            CreateHighWallRun(mapRoot.transform, -15f, 65f, 87f, true, 12, "Final_Left_Wall");
            CreateHighWallRun(mapRoot.transform, 15f, 65f, 87f, true, 12, "Final_Right_Wall");
            CreateHighWallRun(mapRoot.transform, -14f, 87f, 14f, false, 15, "Final_Exit_Wall");

            PlaceMapModel(mapRoot.transform, "wall-door-wide.fbx", "ExitDoorFrame", new Vector3(0f, 0f, 86f), Vector3.zero, Vector3.one * 2f);
            PlaceMapModel(mapRoot.transform, "computer-system.fbx", "StartRoom_Console", new Vector3(-5.8f, 0f, -12.5f), new Vector3(0f, 90f, 0f), Vector3.one * 1.6f);
            PlaceMapModel(mapRoot.transform, "container-wide.fbx", "TrainingRoom_Container_A", new Vector3(6.5f, 0f, -2f), new Vector3(0f, -90f, 0f), Vector3.one * 1.6f);
            PlaceMapModel(mapRoot.transform, "container-tall.fbx", "TrainingRoom_Container_B", new Vector3(-7f, 0f, 5f), new Vector3(0f, 90f, 0f), Vector3.one * 1.4f);
            PlaceMapModel(mapRoot.transform, "table-display.fbx", "CoreLab_DisplayTable", new Vector3(7.5f, 0f, 41f), new Vector3(0f, -90f, 0f), Vector3.one * 1.5f);
            PlaceMapModel(mapRoot.transform, "display-wall-wide.fbx", "CoreLab_StatusDisplay", new Vector3(-11.5f, 1f, 42f), new Vector3(0f, 90f, 0f), Vector3.one * 1.8f);
            PlaceMapModel(mapRoot.transform, "pipe.fbx", "LongCorridor_CeilingPipe_A", new Vector3(-2.6f, 3.1f, 20f), new Vector3(0f, 0f, 90f), Vector3.one * 2.5f);
            PlaceMapModel(mapRoot.transform, "pipe.fbx", "SecurityCorridor_CeilingPipe_B", new Vector3(2.6f, 3.1f, 58f), new Vector3(0f, 0f, 90f), Vector3.one * 2.5f);
            PlaceMapModel(mapRoot.transform, "wall-switch.fbx", "ExitWallSwitch_Deco", new Vector3(-2f, 1f, 85.7f), Vector3.zero, Vector3.one * 1.5f);

            // 각 방의 역할이 한눈에 보이도록 색상, 조명, 소품 밀도를 다르게 둡니다.
            CreateDistinctLevelZones(mapRoot.transform);

            // GrabPack 이동 퍼즐용 앵커 기둥입니다. 2초 이상 잡고 있으면 플레이어가 기둥 쪽으로 끌려갑니다.
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_Training_Left", new Vector3(-3.5f, 1.8f, -0.5f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_Training_Right", new Vector3(3.5f, 1.8f, 3.5f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_Corridor_01", new Vector3(0f, 1.8f, 13f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_Corridor_02", new Vector3(0f, 1.8f, 23f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_CoreLab_Left", new Vector3(-5f, 1.8f, 38f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_CoreLab_Right", new Vector3(5f, 1.8f, 44f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_Security", new Vector3(0f, 1.8f, 58f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_FinalHall", new Vector3(6f, 1.8f, 78f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_Airlock", new Vector3(-4f, 1.8f, 94f));
            CreateGrabAnchorPillar(mapRoot.transform, "GrabAnchor_EscapeBay", new Vector3(4f, 1.8f, 112f));

            // 모델과 별개로 테스트 플레이용 높은 충돌 경계를 배치합니다.
            CreateMapCollider(mapRoot.transform, "Start_Left_Blocker", new Vector3(-9.5f, 2.5f, -12f), new Vector3(0.4f, 5f, 10f));
            CreateMapCollider(mapRoot.transform, "Start_Right_Blocker", new Vector3(9.5f, 2.5f, -12f), new Vector3(0.4f, 5f, 10f));
            CreateMapCollider(mapRoot.transform, "Start_Back_Blocker", new Vector3(0f, 2.5f, -17.5f), new Vector3(18f, 5f, 0.4f));
            CreateMapCollider(mapRoot.transform, "Training_Left_Blocker_Back", new Vector3(-11.5f, 2.5f, -5.5f), new Vector3(0.4f, 5f, 3f));
            CreateMapCollider(mapRoot.transform, "Training_Left_Blocker_Front", new Vector3(-11.5f, 2.5f, 7.5f), new Vector3(0.4f, 5f, 3f));
            CreateMapCollider(mapRoot.transform, "Training_Right_Blocker", new Vector3(11.5f, 2.5f, 1f), new Vector3(0.4f, 5f, 16f));
            CreateMapCollider(mapRoot.transform, "Corridor_Left_Blocker_Back", new Vector3(-3.5f, 2.5f, 12.5f), new Vector3(0.4f, 5f, 7f));
            CreateMapCollider(mapRoot.transform, "Corridor_Left_Blocker_Front", new Vector3(-3.5f, 2.5f, 29f), new Vector3(0.4f, 5f, 4f));
            CreateMapCollider(mapRoot.transform, "Corridor_Right_Blocker", new Vector3(3.5f, 2.5f, 20f), new Vector3(0.4f, 5f, 22f));
            CreateMapCollider(mapRoot.transform, "CoreLab_Left_Blocker", new Vector3(-13.5f, 2.5f, 40f), new Vector3(0.4f, 5f, 18f));
            CreateMapCollider(mapRoot.transform, "CoreLab_Right_Blocker_Back", new Vector3(13.5f, 2.5f, 33.5f), new Vector3(0.4f, 5f, 5f));
            CreateMapCollider(mapRoot.transform, "CoreLab_Right_Blocker_Front", new Vector3(13.5f, 2.5f, 47f), new Vector3(0.4f, 5f, 4f));
            CreateMapCollider(mapRoot.transform, "Security_Left_Blocker", new Vector3(-3.5f, 2.5f, 57f), new Vector3(0.4f, 5f, 16f));
            CreateMapCollider(mapRoot.transform, "Security_Right_Blocker_Back", new Vector3(3.5f, 2.5f, 50f), new Vector3(0.4f, 5f, 2f));
            CreateMapCollider(mapRoot.transform, "Security_Right_Blocker_Front", new Vector3(3.5f, 2.5f, 63f), new Vector3(0.4f, 5f, 4f));
            CreateMapCollider(mapRoot.transform, "Final_Left_Blocker_Back", new Vector3(-15.5f, 2.5f, 66.5f), new Vector3(0.4f, 5f, 3f));
            CreateMapCollider(mapRoot.transform, "Final_Left_Blocker_Front", new Vector3(-15.5f, 2.5f, 83f), new Vector3(0.4f, 5f, 8f));
            CreateMapCollider(mapRoot.transform, "Final_Right_Blocker", new Vector3(15.5f, 2.5f, 76f), new Vector3(0.4f, 5f, 22f));
            CreateMapCollider(mapRoot.transform, "Final_Back_Left_Blocker", new Vector3(-8.5f, 2.5f, 87.5f), new Vector3(13f, 5f, 0.4f));
            CreateMapCollider(mapRoot.transform, "Final_Back_Right_Blocker", new Vector3(8.5f, 2.5f, 87.5f), new Vector3(13f, 5f, 0.4f));
            CreateMapCollider(mapRoot.transform, "Airlock_Left_Blocker", new Vector3(-11.5f, 5f, 92f), new Vector3(0.4f, 10f, 10f));
            CreateMapCollider(mapRoot.transform, "Airlock_Right_Blocker", new Vector3(11.5f, 5f, 92f), new Vector3(0.4f, 10f, 10f));
            CreateMapCollider(mapRoot.transform, "Decontamination_Left_Blocker", new Vector3(-11.5f, 5f, 102f), new Vector3(0.4f, 10f, 10f));
            CreateMapCollider(mapRoot.transform, "Decontamination_Right_Blocker", new Vector3(11.5f, 5f, 102f), new Vector3(0.4f, 10f, 10f));
            CreateMapCollider(mapRoot.transform, "EscapeBay_Left_Blocker", new Vector3(-15.5f, 5f, 114f), new Vector3(0.4f, 10f, 14f));
            CreateMapCollider(mapRoot.transform, "EscapeBay_Right_Blocker", new Vector3(15.5f, 5f, 114f), new Vector3(0.4f, 10f, 14f));
            CreateMapCollider(mapRoot.transform, "EscapeBay_Back_Blocker", new Vector3(0f, 5f, 121.5f), new Vector3(30f, 10f, 0.4f));

            // 혹시 벽 틈이나 방 연결부에서 빠지는 경우를 막기 위한 보이지 않는 안전 충돌 영역입니다.
            CreateMapCollider(mapRoot.transform, "MapSafetyFloor", new Vector3(0f, -0.35f, 52f), new Vector3(90f, 0.5f, 150f));
            CreateMapCollider(mapRoot.transform, "Outer_Left_Blocker", new Vector3(-36f, 5f, 52f), new Vector3(0.4f, 10f, 154f));
            CreateMapCollider(mapRoot.transform, "Outer_Right_Blocker", new Vector3(36f, 5f, 52f), new Vector3(0.4f, 10f, 154f));
            CreateMapCollider(mapRoot.transform, "Outer_Back_Blocker", new Vector3(0f, 5f, -25f), new Vector3(72f, 10f, 0.4f));
            CreateMapCollider(mapRoot.transform, "Outer_Front_Blocker", new Vector3(0f, 5f, 129f), new Vector3(72f, 10f, 0.4f));
        }

        private static void CreateCompactEscapeMapLayout(Transform mapRoot)
        {
            // 큰 홀을 없애고 작은 방을 하나씩 통과하는 탈출 코스로 재구성합니다.
            CreateCompactRoom(mapRoot, "Room01_CheckIn", "01 CHECK-IN", -4, 4, -10, -4, new Color(0.35f, 0.85f, 1f));
            CreateConnectorCorridor(mapRoot, "Corridor_01_To_02", -1, 1, -3, 1);
            CreateCompactRoom(mapRoot, "Room02_GrabPractice", "02 GRAB TEST", -4, 4, 2, 8, new Color(1f, 0.82f, 0.18f));
            CreateConnectorCorridor(mapRoot, "Corridor_02_To_03", -1, 1, 9, 13);
            CreateCompactRoom(mapRoot, "Room03_Storage", "03 STORAGE", -4, 4, 14, 20, new Color(1f, 0.48f, 0.12f));
            CreateConnectorCorridor(mapRoot, "Corridor_03_To_04", -1, 1, 21, 25);
            CreateCompactRoom(mapRoot, "Room04_AnchorShaft", "04 ANCHOR", -4, 4, 26, 32, new Color(0.25f, 1f, 0.45f));
            CreateConnectorCorridor(mapRoot, "Corridor_04_To_05", -1, 1, 33, 37);
            CreateCompactRoom(mapRoot, "Room05_CoreLab", "05 CORE", -4, 4, 38, 44, new Color(0.12f, 0.48f, 1f));
            CreateConnectorCorridor(mapRoot, "Corridor_05_To_06", -1, 1, 45, 49);
            CreateCompactRoom(mapRoot, "Room06_SecurityGate", "06 SECURITY", -4, 4, 50, 56, new Color(1f, 0.16f, 0.08f));
            CreateConnectorCorridor(mapRoot, "Corridor_06_To_07", -1, 1, 57, 61);
            CreateCompactRoom(mapRoot, "Room07_PuzzleCell", "07 PUZZLE", -4, 4, 62, 68, new Color(1f, 0.25f, 0.65f));
            CreateConnectorCorridor(mapRoot, "Corridor_07_To_08", -1, 1, 69, 73);
            CreateCompactRoom(mapRoot, "Room08_Airlock", "08 AIRLOCK", -4, 4, 74, 80, new Color(0.1f, 0.95f, 1f));
            CreateConnectorCorridor(mapRoot, "Corridor_08_To_09", -1, 1, 81, 85);
            CreateCompactRoom(mapRoot, "Room09_Decon", "09 DECON", -4, 4, 86, 92, new Color(0.1f, 1f, 0.55f));
            CreateConnectorCorridor(mapRoot, "Corridor_09_To_10", -1, 1, 93, 97);
            CreateCompactRoom(mapRoot, "Room10_EscapeBay", "10 ESCAPE", -4, 4, 98, 104, new Color(0.96f, 0.98f, 1f));

            PlaceCompactRoomProps(mapRoot);
            PlaceCompactDoorSequence(mapRoot);
            PlaceCompactObjectivePads(mapRoot);

            // GrabPack 이동 퍼즐용 앵커입니다. 움직일 수 없는 기둥이라 플레이어가 끌려갑니다.
            CreateGrabAnchorPillar(mapRoot, "GrabAnchor_Practice_Left", new Vector3(-5.8f, 1.8f, 8f));
            CreateGrabAnchorPillar(mapRoot, "GrabAnchor_Practice_Right", new Vector3(5.8f, 1.8f, 12f));
            CreateGrabAnchorPillar(mapRoot, "GrabAnchor_Shaft_A", new Vector3(0f, 1.8f, 58f));
            CreateGrabAnchorPillar(mapRoot, "GrabAnchor_Security_Exit", new Vector3(0f, 1.8f, 110f));
            CreateGrabAnchorPillar(mapRoot, "GrabAnchor_Puzzle_A", new Vector3(-5.8f, 1.8f, 128f));
            CreateGrabAnchorPillar(mapRoot, "GrabAnchor_Puzzle_B", new Vector3(5.8f, 1.8f, 136f));
            CreateGrabAnchorPillar(mapRoot, "GrabAnchor_Airlock_A", new Vector3(0f, 1.8f, 154f));

            // 바닥이 없는 곳은 실제로 떨어지도록 전체 안전 바닥은 만들지 않습니다.
            CreateMapCollider(mapRoot, "CompactMap_Left_Blocker", new Vector3(-13.2f, 5f, 94f), new Vector3(0.4f, 10f, 236f));
            CreateMapCollider(mapRoot, "CompactMap_Right_Blocker", new Vector3(13.2f, 5f, 94f), new Vector3(0.4f, 10f, 236f));
            CreateMapCollider(mapRoot, "CompactMap_Back_Blocker", new Vector3(0f, 5f, -30f), new Vector3(28f, 10f, 0.4f));
            CreateMapCollider(mapRoot, "CompactMap_Front_Blocker", new Vector3(0f, 5f, 218f), new Vector3(28f, 10f, 0.4f));
        }

        private static void CreateCompactRoom(Transform parent, string roomName, string labelText, int xMin, int xMax, int zMin, int zMax, Color accentColor)
        {
            CreateFloorGrid(parent, $"{roomName}_Floor", xMin, xMax, zMin, zMax, "floor-panel.fbx");
            CreateCeilingGrid(parent, $"{roomName}_Ceiling", xMin, xMax, zMin, zMax, "floor-panel.fbx");

            float leftX = xMin * 2f - 1f;
            float rightX = xMax * 2f + 1f;
            float backZ = zMin * 2f - 1f;
            float frontZ = zMax * 2f + 1f;
            float centerX = (leftX + rightX) * 0.5f;
            float centerZ = (backZ + frontZ) * 0.5f;
            float width = rightX - leftX;
            float depth = frontZ - backZ;

            CreateHighWallRun(parent, leftX, backZ, frontZ, true, Mathf.Max(3, zMax - zMin + 2), $"{roomName}_LeftWall");
            CreateHighWallRun(parent, rightX, backZ, frontZ, true, Mathf.Max(3, zMax - zMin + 2), $"{roomName}_RightWall");

            // 전후면은 완전한 벽 대신 문 프레임을 세워 작은 방이 계속 이어지는 느낌을 만듭니다.
            PlaceMapModel(parent, "wall-door-wide.fbx", $"{roomName}_EntryFrame", new Vector3(centerX, 0f, backZ), Vector3.zero, Vector3.one * 1.45f);
            PlaceMapModel(parent, "wall-door-wide.fbx", $"{roomName}_ExitFrame", new Vector3(centerX, 0f, frontZ), Vector3.zero, Vector3.one * 1.45f);
            CreateZoneIdentity(parent, roomName, labelText, new Vector3(centerX, 0f, centerZ), width, depth, accentColor);
        }

        private static void PlaceCompactRoomProps(Transform parent)
        {
            PlaceMapModel(parent, "computer-system.fbx", "Room01_CheckIn_Console", new Vector3(-6f, 0f, -17f), new Vector3(0f, 90f, 0f), Vector3.one * 2.8f);
            PlaceMapModel(parent, "chair.fbx", "Room01_CheckIn_Chair", new Vector3(-2.8f, 0f, -17f), new Vector3(0f, 90f, 0f), Vector3.one * 2.1f);

            PlaceMapModel(parent, "structure-barrier-high.fbx", "Room02_Practice_Barrier_A", new Vector3(-6f, 0f, 8f), new Vector3(0f, 90f, 0f), Vector3.one * 2.4f);
            PlaceMapModel(parent, "structure-barrier-high.fbx", "Room02_Practice_Barrier_B", new Vector3(6f, 0f, 12f), new Vector3(0f, -90f, 0f), Vector3.one * 2.4f);

            PlaceMapModel(parent, "container-wide.fbx", "Room03_Storage_Container_A", new Vector3(-6.2f, 0f, 33f), new Vector3(0f, 90f, 0f), Vector3.one * 2.7f);
            PlaceMapModel(parent, "container-tall.fbx", "Room03_Storage_Container_B", new Vector3(6.2f, 0f, 36f), new Vector3(0f, -90f, 0f), Vector3.one * 2.5f);

            PlaceMapModel(parent, "pipe-bend.fbx", "Room04_Anchor_Pipe_A", new Vector3(-6.6f, 1.8f, 58f), new Vector3(0f, 0f, 90f), Vector3.one * 2.8f);
            PlaceMapModel(parent, "wall-switch.fbx", "Room04_Anchor_Switch", new Vector3(7.4f, 1.35f, 58f), new Vector3(0f, -90f, 0f), Vector3.one * 2.4f);

            PlaceMapModel(parent, "table-display-planet.fbx", "Room05_Core_DisplayTable", new Vector3(-5.2f, 0f, 82f), Vector3.zero, Vector3.one * 2.6f);
            PlaceMapModel(parent, "display-wall-wide.fbx", "Room05_Core_StatusDisplay", new Vector3(7.4f, 1.45f, 82f), new Vector3(0f, -90f, 0f), Vector3.one * 2.4f);

            PlaceMapModel(parent, "door-double-closed.fbx", "Room06_Security_DoorProp", new Vector3(0f, 0f, 112f), Vector3.zero, Vector3.one * 2.5f);
            PlaceMapModel(parent, "display-wall.fbx", "Room06_Security_AlertDisplay", new Vector3(-7.4f, 1.45f, 106f), new Vector3(0f, 90f, 0f), Vector3.one * 2.4f);

            PlaceMapModel(parent, "structure-panel.fbx", "Room07_Puzzle_Panel_A", new Vector3(-6.4f, 0f, 128f), new Vector3(0f, 90f, 0f), Vector3.one * 2.6f);
            PlaceMapModel(parent, "structure-panel.fbx", "Room07_Puzzle_Panel_B", new Vector3(6.4f, 0f, 136f), new Vector3(0f, -90f, 0f), Vector3.one * 2.6f);
            PlaceMapModel(parent, "structure-barrier.fbx", "Room07_Puzzle_LowBarrier_A", new Vector3(-3.5f, 0f, 132f), new Vector3(0f, 90f, 0f), Vector3.one * 2.2f);
            PlaceMapModel(parent, "structure-barrier.fbx", "Room07_Puzzle_LowBarrier_B", new Vector3(3.5f, 0f, 132f), new Vector3(0f, -90f, 0f), Vector3.one * 2.2f);

            PlaceMapModel(parent, "wall-door-banner.fbx", "Room08_Airlock_Banner", new Vector3(0f, 0f, 160f), Vector3.zero, Vector3.one * 2.6f);
            PlaceMapModel(parent, "pipe-ring-colored.fbx", "Room09_Decon_Ring_A", new Vector3(-6.2f, 1.9f, 178f), new Vector3(0f, 0f, 90f), Vector3.one * 2.8f);
            PlaceMapModel(parent, "door-double.fbx", "Room10_Escape_FinalDoor", new Vector3(0f, 0f, 210f), Vector3.zero, Vector3.one * 3f);
        }

        private static void PlaceCompactDoorSequence(Transform parent)
        {
            // 일반 문은 통로를 막지 않는 얇은 시각 표시로 두고, 실제로 열리는 문은 Core Station과 연결합니다.
            CreateDoorPanel(parent, "Door_01_To_02_Normal", new Vector3(0f, 1.6f, -3f), new Color(0.25f, 0.32f, 0.38f), false);
            CreateDoorPanel(parent, "Door_02_To_03_Normal", new Vector3(0f, 1.6f, 21f), new Color(0.25f, 0.32f, 0.38f), false);
            CreateDoorPanel(parent, "Door_03_To_04_Normal", new Vector3(0f, 1.6f, 45f), new Color(0.25f, 0.32f, 0.38f), false);
            CreateDoorPanel(parent, "Door_04_To_05_Normal", new Vector3(0f, 1.6f, 69f), new Color(0.25f, 0.32f, 0.38f), false);

            CreateDoorPanel(parent, "Door_06_To_07_AfterCore", new Vector3(0f, 1.6f, 117f), new Color(0.42f, 0.08f, 0.1f), false);
            CreateDoorPanel(parent, "Door_07_To_08_Normal", new Vector3(0f, 1.6f, 141f), new Color(0.32f, 0.16f, 0.38f), false);
            CreateDoorPanel(parent, "Door_08_To_09_Normal", new Vector3(0f, 1.6f, 165f), new Color(0.08f, 0.38f, 0.42f), false);
            CreateDoorPanel(parent, "Door_09_To_10_Normal", new Vector3(0f, 1.6f, 189f), new Color(0.08f, 0.42f, 0.25f), false);
        }

        private static void PlaceCompactObjectivePads(Transform parent)
        {
            // 방 입구가 아니라 실제 목표 지점에 작은 발광 패드를 둡니다.
            CreateObjectivePad(parent, "ObjectivePad_CheckInConsole", new Vector3(-6f, 0.08f, -17f), new Color(0.35f, 0.85f, 1f));
            CreateObjectivePad(parent, "ObjectivePad_GrabTarget", new Vector3(4.8f, 0.08f, 8f), new Color(1f, 0.82f, 0.18f));
            CreateObjectivePad(parent, "ObjectivePad_StorageCrate", new Vector3(-6f, 0.08f, 33f), new Color(1f, 0.48f, 0.12f));
            CreateObjectivePad(parent, "ObjectivePad_AnchorExit", new Vector3(0f, 0.08f, 66f), new Color(0.25f, 1f, 0.45f));
            CreateObjectivePad(parent, "ObjectivePad_CorePickup", new Vector3(-5.2f, 0.08f, 82f), new Color(0.12f, 0.48f, 1f));
            CreateObjectivePad(parent, "ObjectivePad_SecurityConsole", new Vector3(-6.2f, 0.08f, 106f), new Color(1f, 0.16f, 0.08f));
            CreateObjectivePad(parent, "ObjectivePad_PuzzleExit", new Vector3(0f, 0.08f, 140f), new Color(1f, 0.25f, 0.65f));
            CreateObjectivePad(parent, "ObjectivePad_AirlockConsole", new Vector3(5.8f, 0.08f, 154f), new Color(0.1f, 0.95f, 1f));
            CreateObjectivePad(parent, "ObjectivePad_DeconRing", new Vector3(-6f, 0.08f, 178f), new Color(0.1f, 1f, 0.55f));
            CreateObjectivePad(parent, "ObjectivePad_EscapeDoor", new Vector3(0f, 0.08f, 206f), new Color(0.96f, 0.98f, 1f));
        }

        private static void CreateObjectivePad(Transform parent, string objectName, Vector3 position, Color color)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = objectName;
            pad.transform.SetParent(parent);
            pad.transform.position = ScaleMapPosition(position);
            pad.transform.localScale = new Vector3(2.8f, 0.08f, 2.8f);

            Renderer renderer = pad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSceneMaterial($"{objectName}_Material", color);
            }

            Collider collider = pad.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static GameObject CreateDoorPanel(Transform parent, string objectName, Vector3 position, Color color, bool addCollider)
        {
            GameObject doorRoot = new GameObject(objectName);
            doorRoot.transform.SetParent(parent);
            doorRoot.transform.position = ScaleMapPosition(position);

            Material doorMaterial = CreateSceneMaterial($"{objectName}_Material", color);
            CreateDoorLeaf(doorRoot.transform, $"{objectName}_LeftLeaf", new Vector3(-2.25f, 0f, 0f), new Vector3(1.05f, 4.1f, 0.22f), doorMaterial, addCollider);
            CreateDoorLeaf(doorRoot.transform, $"{objectName}_RightLeaf", new Vector3(2.25f, 0f, 0f), new Vector3(1.05f, 4.1f, 0.22f), doorMaterial, addCollider);
            CreateDoorLeaf(doorRoot.transform, $"{objectName}_Header", new Vector3(0f, 2f, 0f), new Vector3(5.6f, 0.35f, 0.22f), doorMaterial, addCollider);

            return doorRoot;
        }

        private static void CreateDoorLeaf(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, Material material, bool addCollider)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = objectName;
            leaf.transform.SetParent(parent);
            leaf.transform.localPosition = localPosition;
            leaf.transform.localRotation = Quaternion.identity;
            leaf.transform.localScale = localScale;

            Renderer renderer = leaf.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!addCollider)
            {
                Collider collider = leaf.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }

        private static void CreateFloorGrid(Transform parent, string prefix, int xMin, int xMax, int zMin, int zMax, string modelName)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    PlaceMapModel(parent, modelName, $"{prefix}_{x}_{z}", new Vector3(x * FloorTileSpacing, 0f, z * FloorTileSpacing), Vector3.zero, new Vector3(FloorTileSize, 1f, FloorTileSize));
                }
            }

            CreateFloorCollider(parent, $"{prefix}_Collider", xMin, xMax, zMin, zMax);
        }

        private static void CreateCeilingGrid(Transform parent, string prefix, int xMin, int xMax, int zMin, int zMax, string modelName)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    PlaceMapModel(parent, modelName, $"{prefix}_{x}_{z}", new Vector3(x * FloorTileSpacing, CeilingHeight, z * FloorTileSpacing), new Vector3(180f, 0f, 0f), new Vector3(FloorTileSize, 1f, FloorTileSize));
                }
            }
        }

        private static void CreateFloorCollider(Transform parent, string objectName, int xMin, int xMax, int zMin, int zMax)
        {
            float centerX = (xMin + xMax) * 0.5f * FloorTileSpacing;
            float centerZ = (zMin + zMax) * 0.5f * FloorTileSpacing;
            float sizeX = ((xMax - xMin) * FloorTileSpacing) + FloorTileSize;
            float sizeZ = ((zMax - zMin) * FloorTileSpacing) + FloorTileSize;

            GameObject floorColliderObject = new GameObject(objectName);
            floorColliderObject.transform.SetParent(parent);
            floorColliderObject.transform.position = new Vector3(centerX, -FloorColliderThickness * 0.5f, centerZ);

            BoxCollider boxCollider = floorColliderObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(sizeX, FloorColliderThickness, sizeZ);
        }

        private static void CreateLeftSideRoom(Transform parent, string roomName, int xMin, int xMax, int zMin, int zMax)
        {
            CreateSideRoom(parent, roomName, xMin, xMax, zMin, zMax, true);
        }

        private static void CreateRightSideRoom(Transform parent, string roomName, int xMin, int xMax, int zMin, int zMax)
        {
            CreateSideRoom(parent, roomName, xMin, xMax, zMin, zMax, false);
        }

        private static void CreateSideRoom(Transform parent, string roomName, int xMin, int xMax, int zMin, int zMax, bool isLeftRoom)
        {
            CreateFloorGrid(parent, $"{roomName}_Floor", xMin, xMax, zMin, zMax, "floor-panel.fbx");
            CreateCeilingGrid(parent, $"{roomName}_Ceiling", xMin, xMax, zMin, zMax, "floor-panel.fbx");

            float leftX = xMin * 2f - 1f;
            float rightX = xMax * 2f + 1f;
            float backZ = zMin * 2f - 1f;
            float frontZ = zMax * 2f + 1f;
            float centerX = (leftX + rightX) * 0.5f;
            float centerZ = (backZ + frontZ) * 0.5f;
            float width = rightX - leftX;
            float depth = frontZ - backZ;

            // 메인 동선과 이어지는 안쪽 벽은 열어두고, 바깥쪽 벽만 막아 방 입구가 자연스럽게 보이게 합니다.
            if (isLeftRoom)
            {
                CreateHighWallRun(parent, leftX, backZ, frontZ, true, Mathf.Max(3, zMax - zMin + 2), $"{roomName}_OuterLeftWall");
            }
            else
            {
                CreateHighWallRun(parent, rightX, backZ, frontZ, true, Mathf.Max(3, zMax - zMin + 2), $"{roomName}_OuterRightWall");
            }

            CreateHighWallRun(parent, leftX, backZ, rightX, false, Mathf.Max(3, xMax - xMin + 2), $"{roomName}_BackWall");
            CreateHighWallRun(parent, leftX, frontZ, rightX, false, Mathf.Max(3, xMax - xMin + 2), $"{roomName}_FrontWall");

            CreateMapCollider(parent, $"{roomName}_Back_Blocker", new Vector3(centerX, 2.5f, backZ - 0.5f), new Vector3(width, 5f, 0.4f));
            CreateMapCollider(parent, $"{roomName}_Front_Blocker", new Vector3(centerX, 2.5f, frontZ + 0.5f), new Vector3(width, 5f, 0.4f));

            if (isLeftRoom)
            {
                CreateMapCollider(parent, $"{roomName}_Outer_Blocker", new Vector3(leftX - 0.5f, 2.5f, centerZ), new Vector3(0.4f, 5f, depth));
            }
            else
            {
                CreateMapCollider(parent, $"{roomName}_Outer_Blocker", new Vector3(rightX + 0.5f, 2.5f, centerZ), new Vector3(0.4f, 5f, depth));
            }

            PlaceRoomDecoration(parent, roomName, new Vector3(centerX, 0f, centerZ), isLeftRoom);
        }

        private static void CreateConnectorCorridor(Transform parent, string corridorName, int xMin, int xMax, int zMin, int zMax)
        {
            CreateFloorGrid(parent, $"{corridorName}_Floor", xMin, xMax, zMin, zMax, "floor-panel-straight.fbx");
            CreateCeilingGrid(parent, $"{corridorName}_Ceiling", xMin, xMax, zMin, zMax, "floor-panel-straight.fbx");

            float centerX = (xMin + xMax) * 0.5f * OriginalTileSpacing;
            float centerZ = (zMin + zMax) * 0.5f * OriginalTileSpacing;
            bool horizontal = (xMax - xMin) >= (zMax - zMin);

            // 연결 지점을 눈으로 알아보기 쉽도록 문 프레임을 얹습니다.
            Vector3 doorRotation = horizontal ? new Vector3(0f, 90f, 0f) : Vector3.zero;
            PlaceMapModel(parent, "wall-door-wide.fbx", $"{corridorName}_EntryFrame", new Vector3(centerX, 0f, centerZ), doorRotation, Vector3.one * 1.4f);
        }

        private static void CreateInlineRoom(Transform parent, string roomName, int xMin, int xMax, int zMin, int zMax)
        {
            CreateFloorGrid(parent, $"{roomName}_Floor", xMin, xMax, zMin, zMax, "floor-panel.fbx");
            CreateCeilingGrid(parent, $"{roomName}_Ceiling", xMin, xMax, zMin, zMax, "floor-panel.fbx");

            float leftX = xMin * 2f - 1f;
            float rightX = xMax * 2f + 1f;
            float backZ = zMin * 2f - 1f;
            float frontZ = zMax * 2f + 1f;
            float centerX = (leftX + rightX) * 0.5f;
            float centerZ = (backZ + frontZ) * 0.5f;

            CreateHighWallRun(parent, leftX, backZ, frontZ, true, Mathf.Max(3, zMax - zMin + 2), $"{roomName}_LeftWall");
            CreateHighWallRun(parent, rightX, backZ, frontZ, true, Mathf.Max(3, zMax - zMin + 2), $"{roomName}_RightWall");

            // 방과 방 사이가 이어져 보이도록 전후 벽 대신 문 프레임과 장식만 배치합니다.
            PlaceMapModel(parent, "wall-door-wide.fbx", $"{roomName}_EntryFrame", new Vector3(centerX, 0f, backZ), Vector3.zero, Vector3.one * 1.8f);
            PlaceMapModel(parent, "wall-door-wide.fbx", $"{roomName}_ExitFrame", new Vector3(centerX, 0f, frontZ), Vector3.zero, Vector3.one * 1.8f);
            PlaceMapModel(parent, "display-wall-wide.fbx", $"{roomName}_StatusDisplay", new Vector3(leftX + 0.4f, 1f, centerZ), new Vector3(0f, 90f, 0f), Vector3.one * 1.6f);
            PlaceMapModel(parent, "container-flat.fbx", $"{roomName}_SupplyCrate", new Vector3(rightX - 2f, 0f, centerZ + 1.5f), new Vector3(0f, -90f, 0f), Vector3.one * 1.25f);
        }

        private static void PlaceRoomDecoration(Transform parent, string roomName, Vector3 center, bool isLeftRoom)
        {
            float yRotation = isLeftRoom ? 90f : -90f;
            PlaceMapModel(parent, "wall-door-wide.fbx", $"{roomName}_DoorFrame", center + new Vector3(isLeftRoom ? 4f : -4f, 0f, 0f), new Vector3(0f, yRotation, 0f), Vector3.one * 1.6f);
            PlaceMapModel(parent, "computer-wide.fbx", $"{roomName}_Terminal", center + new Vector3(isLeftRoom ? -2.5f : 2.5f, 0f, -2.5f), new Vector3(0f, yRotation, 0f), Vector3.one * 1.35f);
            PlaceMapModel(parent, "container.fbx", $"{roomName}_Container", center + new Vector3(isLeftRoom ? -2.5f : 2.5f, 0f, 2.5f), new Vector3(0f, -yRotation, 0f), Vector3.one * 1.25f);
        }

        private static void CreateGrabAnchorPillar(Transform parent, string objectName, Vector3 position)
        {
            Type grabTargetType = Type.GetType("GrabTarget, Assembly-CSharp");
            if (grabTargetType == null)
            {
                Debug.LogError("GrabTarget 타입을 찾을 수 없어 앵커 기둥을 만들 수 없습니다.");
                return;
            }

            GameObject anchorRoot = new GameObject(objectName);
            anchorRoot.transform.SetParent(parent);
            anchorRoot.transform.position = ScaleMapPosition(position);
            anchorRoot.transform.rotation = Quaternion.identity;

            GameObject pillarModel = AssetDatabase.LoadAssetAtPath<GameObject>(SpaceStationModelRoot + "wall-pillar.fbx");
            if (pillarModel == null)
            {
                Debug.LogWarning("앵커 기둥용 wall-pillar.fbx 모델을 찾을 수 없습니다.");
            }

            GameObject pillarVisual = pillarModel != null ? (GameObject)PrefabUtility.InstantiatePrefab(pillarModel) : null;
            if (pillarVisual != null)
            {
                pillarVisual.name = $"{objectName}_Visual";
                pillarVisual.transform.SetParent(anchorRoot.transform);
                pillarVisual.transform.localPosition = Vector3.zero;
                pillarVisual.transform.localRotation = Quaternion.identity;
                pillarVisual.transform.localScale = Vector3.one * 2.2f;
            }

            Rigidbody anchorRigidbody = anchorRoot.AddComponent<Rigidbody>();
            anchorRigidbody.mass = 1000f;
            anchorRigidbody.useGravity = false;
            anchorRigidbody.isKinematic = true;

            CapsuleCollider capsuleCollider = anchorRoot.AddComponent<CapsuleCollider>();
            capsuleCollider.center = new Vector3(0f, 1.2f, 0f);
            capsuleCollider.radius = 0.55f;
            capsuleCollider.height = 2.8f;

            Component grabTarget = anchorRoot.AddComponent(grabTargetType);
            SerializedObject serializedGrabTarget = new SerializedObject(grabTarget);
            serializedGrabTarget.FindProperty("targetRigidbody").objectReferenceValue = anchorRigidbody;
            serializedGrabTarget.FindProperty("canBePulled").boolValue = false;
            serializedGrabTarget.FindProperty("canPullPlayer").boolValue = true;
            serializedGrabTarget.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDistinctLevelZones(Transform parent)
        {
            // 01 시작 구역: 체크인 콘솔과 차가운 색 라이트로 안전한 출발 지점을 표시합니다.
            CreateZoneIdentity(parent, "Zone01_Start", "01 START", new Vector3(0f, 0f, -12f), 18f, 10f, new Color(0.3f, 0.85f, 1f));
            PlaceMapModel(parent, "computer-system.fbx", "Zone01_Start_CheckInConsole", new Vector3(4.5f, 0f, -15f), new Vector3(0f, -90f, 0f), Vector3.one * 1.4f);
            PlaceMapModel(parent, "chair.fbx", "Zone01_Start_OperatorChair", new Vector3(2.5f, 0f, -15f), new Vector3(0f, 90f, 0f), Vector3.one * 1.2f);

            // 02 훈련 구역: 넓고 밝은 노란색으로 Grab Pack 기본 조작 공간임을 드러냅니다.
            CreateZoneIdentity(parent, "Zone02_Training", "02 TRAINING", new Vector3(0f, 0f, 1f), 22f, 16f, new Color(1f, 0.85f, 0.18f));
            PlaceMapModel(parent, "structure-barrier-high.fbx", "Zone02_Training_PracticeBarrier_A", new Vector3(-5f, 0f, -1.5f), new Vector3(0f, 90f, 0f), Vector3.one * 1.6f);
            PlaceMapModel(parent, "structure-barrier-high.fbx", "Zone02_Training_PracticeBarrier_B", new Vector3(5f, 0f, 3f), new Vector3(0f, -90f, 0f), Vector3.one * 1.6f);

            // 03 보관실: 주황색과 컨테이너를 많이 배치해 자원 보관 공간처럼 보이게 합니다.
            CreateZoneIdentity(parent, "Zone03_Storage", "03 STORAGE", new Vector3(-18f, 0f, 2f), 10f, 10f, new Color(1f, 0.45f, 0.12f));
            PlaceMapModel(parent, "container-wide.fbx", "Zone03_Storage_CrateWall_A", new Vector3(-20.5f, 0f, 0f), new Vector3(0f, 90f, 0f), Vector3.one * 1.7f);
            PlaceMapModel(parent, "container-tall.fbx", "Zone03_Storage_CrateWall_B", new Vector3(-20.5f, 0f, 4f), new Vector3(0f, 90f, 0f), Vector3.one * 1.5f);
            PlaceMapModel(parent, "container-flat-open.fbx", "Zone03_Storage_OpenCrate", new Vector3(-15.5f, 0f, 3f), new Vector3(0f, -30f, 0f), Vector3.one * 1.35f);

            // 04 정비실: 초록색과 파이프/스위치로 설비 관리 구역을 구분합니다.
            CreateZoneIdentity(parent, "Zone04_Maintenance", "04 MAINT", new Vector3(-13f, 0f, 22f), 17f, 10f, new Color(0.25f, 1f, 0.45f));
            PlaceMapModel(parent, "pipe-bend.fbx", "Zone04_Maintenance_PipeBend_A", new Vector3(-16f, 1.5f, 20f), new Vector3(0f, 0f, 90f), Vector3.one * 2f);
            PlaceMapModel(parent, "pipe-ring-colored.fbx", "Zone04_Maintenance_PipeRing_A", new Vector3(-10f, 1.5f, 24f), new Vector3(0f, 0f, 90f), Vector3.one * 2f);
            PlaceMapModel(parent, "wall-switch.fbx", "Zone04_Maintenance_SwitchBank", new Vector3(-6.5f, 1f, 26.5f), Vector3.zero, Vector3.one * 1.4f);

            // 05 중앙 복도: 좁은 흰색 라인으로 다음 주요 실험실까지 압박감을 만듭니다.
            CreateZoneIdentity(parent, "Zone05_ServiceCorridor", "05 SERVICE", new Vector3(0f, 0f, 20f), 7f, 22f, new Color(0.85f, 0.9f, 1f));

            // 06 코어 연구실: 파란색 조명과 디스플레이를 집중 배치해 핵심 퍼즐 방으로 보이게 합니다.
            CreateZoneIdentity(parent, "Zone06_CoreLab", "06 CORE LAB", new Vector3(0f, 0f, 40f), 26f, 18f, new Color(0.1f, 0.45f, 1f));
            PlaceMapModel(parent, "table-display-planet.fbx", "Zone06_CoreLab_CoreDisplay_A", new Vector3(0f, 0f, 38f), Vector3.zero, Vector3.one * 1.7f);
            PlaceMapModel(parent, "display-wall.fbx", "Zone06_CoreLab_DisplayWall_A", new Vector3(10f, 1f, 34f), new Vector3(0f, -90f, 0f), Vector3.one * 1.7f);
            PlaceMapModel(parent, "computer-screen.fbx", "Zone06_CoreLab_Screen_A", new Vector3(-8f, 0.5f, 45f), new Vector3(0f, 35f, 0f), Vector3.one * 1.5f);

            // 07 실험실: 보라색과 책상형 구조물로 작은 보조 연구실처럼 구분합니다.
            CreateZoneIdentity(parent, "Zone07_Lab", "07 LAB", new Vector3(20f, 0f, 40f), 10f, 10f, new Color(0.75f, 0.35f, 1f));
            PlaceMapModel(parent, "table-large.fbx", "Zone07_Lab_WorkTable", new Vector3(20f, 0f, 39f), new Vector3(0f, 90f, 0f), Vector3.one * 1.4f);
            PlaceMapModel(parent, "computer-wide.fbx", "Zone07_Lab_ComputerBank", new Vector3(23f, 0f, 43f), new Vector3(0f, -90f, 0f), Vector3.one * 1.25f);

            // 08 보안실: 붉은 색상과 폐쇄된 느낌의 게이트로 위험 구역임을 보여줍니다.
            CreateZoneIdentity(parent, "Zone08_Security", "08 SECURITY", new Vector3(10f, 0f, 56f), 10f, 10f, new Color(1f, 0.16f, 0.08f));
            PlaceMapModel(parent, "door-double-closed.fbx", "Zone08_Security_LockedDoorProp", new Vector3(6.5f, 0f, 60f), new Vector3(0f, 90f, 0f), Vector3.one * 1.5f);
            PlaceMapModel(parent, "display-wall-wide.fbx", "Zone08_Security_AlertDisplay", new Vector3(13f, 1f, 56f), new Vector3(0f, -90f, 0f), Vector3.one * 1.6f);

            // 09 최종 홀: 넓은 붉은 오렌지 구역으로 최종 퍼즐 전 긴장감을 만듭니다.
            CreateZoneIdentity(parent, "Zone09_FinalHall", "09 FINAL", new Vector3(0f, 0f, 76f), 30f, 22f, new Color(1f, 0.32f, 0.05f));
            PlaceMapModel(parent, "wall-banner.fbx", "Zone09_FinalHall_Banner_Left", new Vector3(-12.5f, 1.2f, 76f), new Vector3(0f, 90f, 0f), Vector3.one * 1.8f);
            PlaceMapModel(parent, "wall-banner.fbx", "Zone09_FinalHall_Banner_Right", new Vector3(12.5f, 1.2f, 76f), new Vector3(0f, -90f, 0f), Vector3.one * 1.8f);

            // 10 퍼즐실: 분홍색 테두리와 촘촘한 구조물로 최종 조합 퍼즐 방을 표시합니다.
            CreateZoneIdentity(parent, "Zone10_Puzzle", "10 PUZZLE", new Vector3(-22f, 0f, 75f), 10f, 13f, new Color(1f, 0.25f, 0.65f));
            PlaceMapModel(parent, "structure-panel.fbx", "Zone10_Puzzle_Panel_A", new Vector3(-23.5f, 0f, 72f), new Vector3(0f, 90f, 0f), Vector3.one * 1.6f);
            PlaceMapModel(parent, "structure-panel.fbx", "Zone10_Puzzle_Panel_B", new Vector3(-20.5f, 0f, 79f), new Vector3(0f, -90f, 0f), Vector3.one * 1.6f);

            // 11 에어락/오염 제거/탈출 베이: 청록색, 녹색, 밝은 흰색으로 후반 3단계를 구분합니다.
            CreateZoneIdentity(parent, "Zone11_Airlock", "11 AIRLOCK", new Vector3(0f, 0f, 92f), 22f, 10f, new Color(0.1f, 0.95f, 1f));
            CreateZoneIdentity(parent, "Zone12_Decontamination", "12 DECON", new Vector3(0f, 0f, 102f), 22f, 10f, new Color(0.1f, 1f, 0.55f));
            CreateZoneIdentity(parent, "Zone13_EscapeBay", "13 ESCAPE", new Vector3(0f, 0f, 114f), 30f, 14f, new Color(0.95f, 0.98f, 1f));
            PlaceMapModel(parent, "door-double.fbx", "Zone13_EscapeBay_FinalDoorProp", new Vector3(0f, 0f, 120f), Vector3.zero, Vector3.one * 1.8f);
        }

        private static void CreateZoneIdentity(Transform parent, string zoneName, string labelText, Vector3 center, float width, float depth, Color color)
        {
            CreateZoneBorder(parent, $"{zoneName}_FloorBorder", center, width, depth, color);
            CreateAccentBox(parent, $"{zoneName}_CeilingLightBar", center + new Vector3(0f, CeilingHeight - 0.25f, 0f), new Vector3(width * 0.65f, 0.12f, 0.45f), color);
            CreateAccentBox(parent, $"{zoneName}_EntryColorBand", center + new Vector3(0f, 2.35f, -depth * 0.5f + 0.35f), new Vector3(width * 0.5f, 0.22f, 0.16f), color);
            CreateZoneLabel(parent, $"{zoneName}_Label", labelText, center + new Vector3(-width * 0.32f, 2.6f, -depth * 0.5f + 0.45f), color);
            CreatePointLight(parent, $"{zoneName}_AccentLight", center + new Vector3(0f, CeilingHeight - 1.4f, 0f), color, Mathf.Max(width, depth) * 0.65f);
        }

        private static void CreateZoneBorder(Transform parent, string objectName, Vector3 center, float width, float depth, Color color)
        {
            float y = 0.04f;
            float thickness = 0.12f;
            CreateAccentBox(parent, $"{objectName}_Back", center + new Vector3(0f, y, -depth * 0.5f), new Vector3(width, thickness, 0.16f), color);
            CreateAccentBox(parent, $"{objectName}_Front", center + new Vector3(0f, y, depth * 0.5f), new Vector3(width, thickness, 0.16f), color);
            CreateAccentBox(parent, $"{objectName}_Left", center + new Vector3(-width * 0.5f, y, 0f), new Vector3(0.16f, thickness, depth), color);
            CreateAccentBox(parent, $"{objectName}_Right", center + new Vector3(width * 0.5f, y, 0f), new Vector3(0.16f, thickness, depth), color);
        }

        private static void CreateAccentBox(Transform parent, string objectName, Vector3 position, Vector3 size, Color color)
        {
            GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accent.name = objectName;
            accent.transform.SetParent(parent);
            accent.transform.position = ScaleMapPosition(position);
            accent.transform.localScale = new Vector3(size.x * LayoutScale, size.y, size.z * LayoutScale);

            Renderer renderer = accent.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSceneMaterial($"{objectName}_Material", color);
            }

            Collider collider = accent.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void CreateZoneLabel(Transform parent, string objectName, string text, Vector3 position, Color color)
        {
            GameObject label = new GameObject(objectName);
            label.transform.SetParent(parent);
            label.transform.position = ScaleMapPosition(position);
            label.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            label.transform.localScale = Vector3.one * 0.35f;

            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.color = color;
            textMesh.characterSize = 1f;
            textMesh.anchor = TextAnchor.MiddleLeft;
            textMesh.alignment = TextAlignment.Left;
        }

        private static void CreatePointLight(Transform parent, string objectName, Vector3 position, Color color, float range)
        {
            GameObject lightObject = new GameObject(objectName);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = ScaleMapPosition(position);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 1.4f;
            light.range = Mathf.Max(6f, range * LayoutScale);
        }

        private static void CreateHighWallRun(Transform parent, float fixedXOrStartX, float startZOrFixedZ, float endZOrEndX, bool vertical, int count, string prefix)
        {
            for (int index = 0; index < count; index++)
            {
                float t = count <= 1 ? 0f : index / (float)(count - 1);
                Vector3 position;
                Vector3 rotation;

                if (vertical)
                {
                    float z = Mathf.Lerp(startZOrFixedZ, endZOrEndX, t);
                    position = new Vector3(fixedXOrStartX, 0f, z);
                    rotation = new Vector3(0f, fixedXOrStartX < 0f ? 90f : -90f, 0f);
                }
                else
                {
                    float x = Mathf.Lerp(fixedXOrStartX, endZOrEndX, t);
                    position = new Vector3(x, 0f, startZOrFixedZ);
                    rotation = Vector3.zero;
                }

                PlaceMapModel(parent, "wall.fbx", $"{prefix}_{index}", position, rotation, LargeWallScale);
            }
        }

        private static GameObject PlaceMapModel(Transform parent, string modelName, string objectName, Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(SpaceStationModelRoot + modelName);
            if (model == null)
            {
                Debug.LogWarning($"맵 모델을 찾을 수 없습니다: {modelName}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = objectName;
            instance.transform.SetParent(parent);
            instance.transform.position = ShouldKeepGridPosition(objectName) ? position : ScaleMapPosition(position);
            instance.transform.rotation = Quaternion.Euler(eulerAngles);
            instance.transform.localScale = scale;
            return instance;
        }

        private static void CreateMapCollider(Transform parent, string objectName, Vector3 center, Vector3 size)
        {
            bool isWallBlocker = objectName.Contains("Blocker");
            GameObject colliderObject = new GameObject(objectName);
            colliderObject.name = objectName;
            colliderObject.transform.SetParent(parent);

            if (isWallBlocker)
            {
                center.y = WallBlockHeight * 0.5f;
                size.y = Mathf.Max(size.y, WallBlockHeight);
                size.x = Mathf.Max(size.x, 0.8f);
                size.z = Mathf.Max(size.z, 0.8f);
            }

            colliderObject.transform.position = ScaleMapPosition(center);

            BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(size.x * LayoutScale, size.y, size.z * LayoutScale);
        }

        private static Vector3 ScaleMapPosition(Vector3 position)
        {
            return new Vector3(position.x * LayoutScale, position.y, position.z * LayoutScale);
        }

        private static bool ShouldKeepGridPosition(string objectName)
        {
            return objectName.Contains("Floor_") || objectName.Contains("Ceiling_");
        }

        private static void CreateGrabTargetTestObject()
        {
            GameObject targetRoot = new GameObject("GrabTarget_CharacterG");
            targetRoot.transform.position = ScaleMapPosition(new Vector3(4.8f, 1f, 8f));
            targetRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Rigidbody targetRigidbody = targetRoot.AddComponent<Rigidbody>();
            targetRigidbody.mass = 1f;
            targetRigidbody.useGravity = true;
            targetRigidbody.isKinematic = false;

            CapsuleCollider capsuleCollider = targetRoot.AddComponent<CapsuleCollider>();
            capsuleCollider.center = new Vector3(0f, 0f, 0f);
            capsuleCollider.radius = 0.35f;
            capsuleCollider.height = 2f;

            Type grabTargetType = Type.GetType("GrabTarget, Assembly-CSharp");
            if (grabTargetType == null)
            {
                Debug.LogError("GrabTarget 타입을 찾을 수 없습니다.");
                return;
            }

            Component grabTarget = targetRoot.AddComponent(grabTargetType);
            SerializedObject serializedGrabTarget = new SerializedObject(grabTarget);
            serializedGrabTarget.FindProperty("targetRigidbody").objectReferenceValue = targetRigidbody;
            serializedGrabTarget.ApplyModifiedPropertiesWithoutUndo();

            GameObject targetModel = AssetDatabase.LoadAssetAtPath<GameObject>(GrabTargetModelPath);
            if (targetModel == null)
            {
                Debug.LogError($"GrabTarget 테스트 모델을 찾을 수 없습니다: {GrabTargetModelPath}");
                return;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(targetModel);
            visual.name = "character-g Visual";
            visual.transform.SetParent(targetRoot.transform);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
        }

        private static void CreateNeckGrabPackVisual(Transform astronautTransform)
        {
            GameObject grabPackModel = AssetDatabase.LoadAssetAtPath<GameObject>(GrabPackModelPath);
            if (grabPackModel == null)
            {
                Debug.LogWarning($"GrabPack 모델을 찾을 수 없습니다: {GrabPackModelPath}");
                return;
            }

            Transform attachBone = FindChildRecursive(astronautTransform, "Body_Upper");
            if (attachBone == null)
            {
                attachBone = FindChildRecursive(astronautTransform, "Head");
            }

            if (attachBone == null)
            {
                Debug.LogWarning("GrabPack을 붙일 Astronaut 본을 찾지 못해 Astronaut 루트에 연결합니다.");
                attachBone = astronautTransform;
            }

            GameObject mount = new GameObject("NeckGrabPack_Mount");
            mount.transform.SetParent(attachBone);
            mount.transform.localPosition = new Vector3(0f, 0.15f, -0.28f);
            mount.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            mount.transform.localScale = Vector3.one;

            GameObject grabPackVisual = (GameObject)PrefabUtility.InstantiatePrefab(grabPackModel);
            grabPackVisual.name = "Poppy GrabPack Visual";
            grabPackVisual.transform.SetParent(mount.transform);
            grabPackVisual.transform.localPosition = Vector3.zero;
            grabPackVisual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            grabPackVisual.transform.localScale = Vector3.one;

            NormalizeVisualSize(grabPackVisual, 0.45f);
            ApplyGrabPackMaterials(grabPackVisual);
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform result = FindChildRecursive(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void NormalizeVisualSize(GameObject visualRoot, float targetMaxSize)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxSize <= 0.001f)
            {
                return;
            }

            float scaleMultiplier = targetMaxSize / maxSize;
            visualRoot.transform.localScale *= scaleMultiplier;
        }

        private static void ApplyAstronautMaterials(GameObject astronaut)
        {
            Material suitMaterial = CreateSceneMaterial("Astronaut_Suit_White_URP", new Color(0.92f, 0.94f, 0.9f));
            Material visorMaterial = CreateSceneMaterial("Astronaut_Visor_Dark_URP", new Color(0.05f, 0.08f, 0.12f));
            Material accentMaterial = CreateSceneMaterial("Astronaut_Accent_Yellow_URP", new Color(1f, 0.72f, 0.12f));

            Renderer[] renderers = astronaut.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i] != null ? materials[i].name.ToLowerInvariant() : string.Empty;

                    if (materialName.Contains("yellow"))
                    {
                        materials[i] = accentMaterial;
                    }
                    else if (materialName.Contains("grey") || materialName.Contains("gray") || materialName.Contains("visor"))
                    {
                        materials[i] = visorMaterial;
                    }
                    else
                    {
                        materials[i] = suitMaterial;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void AssignAstronautAnimator(GameObject astronaut)
        {
            RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AstronautAnimatorControllerPath);
            if (animatorController == null)
            {
                Debug.LogWarning($"Astronaut 애니메이터 컨트롤러를 찾을 수 없습니다: {AstronautAnimatorControllerPath}");
                return;
            }

            Animator animator = astronaut.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = astronaut.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
        }

        private static void ApplyGrabPackMaterials(GameObject grabPack)
        {
            Material bodyMaterial = CreateSceneMaterial("GrabPack_Body_Blue_URP", new Color(0.08f, 0.22f, 0.65f));
            Material handMaterial = CreateSceneMaterial("GrabPack_Hand_Red_URP", new Color(0.85f, 0.08f, 0.06f));
            Material darkMaterial = CreateSceneMaterial("GrabPack_Dark_URP", new Color(0.04f, 0.04f, 0.05f));

            Renderer[] renderers = grabPack.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                string rendererName = renderer.name.ToLowerInvariant();

                if (rendererName.Contains("hand") || rendererName.Contains("arm"))
                {
                    renderer.sharedMaterial = handMaterial;
                }
                else if (rendererName.Contains("strap") || rendererName.Contains("black") || rendererName.Contains("dark"))
                {
                    renderer.sharedMaterial = darkMaterial;
                }
                else
                {
                    renderer.sharedMaterial = bodyMaterial;
                }
            }
        }

        private static void CreateEnergyCoreTestObject()
        {
            Type grabTargetType = Type.GetType("GrabTarget, Assembly-CSharp");
            Type energyCoreType = Type.GetType("EnergyCore, Assembly-CSharp");
            if (grabTargetType == null || energyCoreType == null)
            {
                Debug.LogError("EnergyCore 테스트에 필요한 타입을 찾을 수 없습니다.");
                return;
            }

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "EnergyCore_Test";
            core.transform.position = ScaleMapPosition(new Vector3(-5.2f, 1.1f, 82f));
            core.transform.localScale = Vector3.one * 1.25f;

            Rigidbody coreRigidbody = core.AddComponent<Rigidbody>();
            coreRigidbody.mass = 0.75f;
            coreRigidbody.useGravity = true;
            coreRigidbody.isKinematic = false;

            Component grabTarget = core.AddComponent(grabTargetType);
            SerializedObject serializedGrabTarget = new SerializedObject(grabTarget);
            serializedGrabTarget.FindProperty("targetRigidbody").objectReferenceValue = coreRigidbody;
            serializedGrabTarget.ApplyModifiedPropertiesWithoutUndo();

            Renderer coreRenderer = core.GetComponent<Renderer>();
            Shader coreShader = Shader.Find("Universal Render Pipeline/Lit");
            if (coreShader == null)
            {
                coreShader = Shader.Find("Standard");
            }

            Material coreMaterial = new Material(coreShader);
            Color coreColor = new Color(0f, 0.7f, 1f);
            coreMaterial.EnableKeyword("_EMISSION");
            coreMaterial.SetColor("_EmissionColor", coreColor * 2f);
            if (coreMaterial.HasProperty("_BaseColor"))
            {
                coreMaterial.SetColor("_BaseColor", coreColor);
            }
            else if (coreMaterial.HasProperty("_Color"))
            {
                coreMaterial.SetColor("_Color", coreColor);
            }

            coreRenderer.sharedMaterial = coreMaterial;

            GameObject lightObject = new GameObject("EnergyCore_BlueLight");
            lightObject.transform.SetParent(core.transform);
            lightObject.transform.localPosition = Vector3.zero;
            Light coreLight = lightObject.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = coreColor;
            coreLight.intensity = 4f;
            coreLight.range = 4f;

            Component energyCore = core.AddComponent(energyCoreType);
            SerializedObject serializedEnergyCore = new SerializedObject(energyCore);
            serializedEnergyCore.FindProperty("emissionRenderer").objectReferenceValue = coreRenderer;
            serializedEnergyCore.FindProperty("coreLight").objectReferenceValue = coreLight;
            serializedEnergyCore.FindProperty("isActive").boolValue = true;
            serializedEnergyCore.FindProperty("activeEmissionColor").colorValue = coreColor;
            serializedEnergyCore.FindProperty("inactiveEmissionColor").colorValue = Color.black;
            serializedEnergyCore.FindProperty("activeLightIntensity").floatValue = 4f;
            serializedEnergyCore.FindProperty("inactiveLightIntensity").floatValue = 0f;
            serializedEnergyCore.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCoreStationTestObjects()
        {
            Type coreStationType = Type.GetType("CoreStation, Assembly-CSharp");
            Type securityDoorType = Type.GetType("SecurityDoor, Assembly-CSharp");
            if (coreStationType == null || securityDoorType == null)
            {
                Debug.LogError("CoreStation 테스트에 필요한 타입을 찾을 수 없습니다.");
                return;
            }

            Material stationMaterial = CreateSceneMaterial("Station_Grey_Material", Color.gray);
            Material coreDoorMaterial = CreateSceneMaterial("Core_Door_Blue_Material", new Color(0.15f, 0.35f, 0.8f));

            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "SecurityDoor_Core_Test";
            door.transform.position = ScaleMapPosition(new Vector3(0f, 1.7f, 93f));
            door.transform.localScale = new Vector3(6.4f, 4.2f, 0.35f);
            door.GetComponent<Renderer>().sharedMaterial = coreDoorMaterial;

            Component securityDoor = door.AddComponent(securityDoorType);
            SerializedObject serializedDoor = new SerializedObject(securityDoor);
            serializedDoor.FindProperty("doorTransform").objectReferenceValue = door.transform;
            serializedDoor.FindProperty("openAudio").objectReferenceValue = null;
            serializedDoor.FindProperty("openOffset").vector3Value = new Vector3(0f, 4f, 0f);
            serializedDoor.FindProperty("openDuration").floatValue = 1.5f;
            serializedDoor.ApplyModifiedPropertiesWithoutUndo();

            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = "CoreStation_Test";
            station.transform.position = ScaleMapPosition(new Vector3(5.2f, 0.15f, 82f));
            station.transform.localScale = new Vector3(3.6f, 0.35f, 3.6f);
            station.GetComponent<Renderer>().sharedMaterial = stationMaterial;

            BoxCollider stationCollider = station.GetComponent<BoxCollider>();
            stationCollider.isTrigger = true;
            stationCollider.size = new Vector3(1f, 2f, 1f);
            stationCollider.center = new Vector3(0f, 0.75f, 0f);

            GameObject holdPoint = new GameObject("StationHoldPoint");
            holdPoint.transform.SetParent(station.transform);
            holdPoint.transform.localPosition = new Vector3(0f, 1f, 0f);
            holdPoint.transform.localRotation = Quaternion.identity;

            Component coreStation = station.AddComponent(coreStationType);
            SerializedObject serializedStation = new SerializedObject(coreStation);
            serializedStation.FindProperty("stationHoldPoint").objectReferenceValue = holdPoint.transform;
            serializedStation.FindProperty("linkedDoor").objectReferenceValue = securityDoor;
            serializedStation.FindProperty("chargeTime").floatValue = 3f;
            serializedStation.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMissionGuideObjects()
        {
            GameObject canvasObject = new GameObject("MissionCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = CreateMissionUIRect(canvasObject.transform, "ObjectivePanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1120f, 176f), new Vector2(0f, 32f));
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.055f, 0.86f);

            GameObject statusObject = CreateMissionUIRect(panelObject.transform, "ObjectiveStatusText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220f, 26f), new Vector2(32f, -20f));
            Text statusText = CreateMissionText(statusObject, 18, FontStyle.Bold, new Color(0.35f, 0.9f, 1f), TextAnchor.UpperLeft);

            GameObject titleObject = CreateMissionUIRect(panelObject.transform, "ObjectiveTitleText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1050f, 42f), new Vector2(32f, -52f));
            Text titleText = CreateMissionText(titleObject, 28, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

            GameObject descriptionObject = CreateMissionUIRect(panelObject.transform, "ObjectiveDescriptionText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1050f, 70f), new Vector2(32f, -100f));
            Text descriptionText = CreateMissionText(descriptionObject, 21, FontStyle.Normal, new Color(0.86f, 0.9f, 0.95f), TextAnchor.UpperLeft);

            UIManager uiManager = canvasObject.AddComponent<UIManager>();
            SerializedObject serializedUi = new SerializedObject(uiManager);
            serializedUi.FindProperty("objectivePanel").objectReferenceValue = panelObject;
            serializedUi.FindProperty("objectiveTitleText").objectReferenceValue = titleText;
            serializedUi.FindProperty("objectiveDescriptionText").objectReferenceValue = descriptionText;
            serializedUi.FindProperty("objectiveStatusText").objectReferenceValue = statusText;
            serializedUi.FindProperty("completedStatusText").stringValue = "완료";
            serializedUi.FindProperty("activeStatusText").stringValue = "현재 목표";
            serializedUi.ApplyModifiedPropertiesWithoutUndo();

            GameObject missionObject = new GameObject("MissionManager");
            MissionManager missionManager = missionObject.AddComponent<MissionManager>();
            SerializedObject serializedMission = new SerializedObject(missionManager);
            serializedMission.FindProperty("uiManager").objectReferenceValue = uiManager;
            serializedMission.FindProperty("nextMissionDelay").floatValue = 1.5f;
            ConfigureMissionSteps(serializedMission.FindProperty("missionSteps"));
            serializedMission.ApplyModifiedPropertiesWithoutUndo();

            CreateMissionTrigger("MissionTrigger_CheckInConsolePad", "check_in_console", missionManager, new Vector3(-6f, 1.2f, -17f), new Vector3(4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_GrabTargetPad", "reach_grab_practice", missionManager, new Vector3(4.8f, 1.2f, 8f), new Vector3(4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_StorageCratePad", "reach_storage", missionManager, new Vector3(-6f, 1.2f, 33f), new Vector3(4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_AnchorExitPad", "reach_anchor_room", missionManager, new Vector3(0f, 1.2f, 66f), new Vector3(4.4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_CorePickupPad", "reach_core_lab", missionManager, new Vector3(-5.2f, 1.2f, 82f), new Vector3(4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_SecurityConsolePad", "reach_security", missionManager, new Vector3(-6.2f, 1.2f, 106f), new Vector3(4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_PuzzleExitPad", "reach_puzzle", missionManager, new Vector3(0f, 1.2f, 140f), new Vector3(4.4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_AirlockConsolePad", "reach_airlock", missionManager, new Vector3(5.8f, 1.2f, 154f), new Vector3(4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_DeconRingPad", "reach_decon", missionManager, new Vector3(-6f, 1.2f, 178f), new Vector3(4f, 2.4f, 4f));
            CreateMissionTrigger("MissionTrigger_EscapeDoorPad", "reach_escape", missionManager, new Vector3(0f, 1.2f, 206f), new Vector3(4.8f, 2.4f, 4f));

            GameObject coreStation = GameObject.Find("CoreStation_Test");
            CoreStation station = coreStation != null ? coreStation.GetComponent<CoreStation>() : null;
            if (station != null)
            {
                SerializedObject serializedStation = new SerializedObject(station);
                serializedStation.FindProperty("missionManager").objectReferenceValue = missionManager;
                serializedStation.FindProperty("completionObjectiveId").stringValue = "charge_core";
                serializedStation.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateCheckpointRespawnObjects(GameObject player)
        {
            GameObject canvasObject = GameObject.Find("MissionCanvas");
            if (canvasObject == null)
            {
                Debug.LogError("MissionCanvas를 찾을 수 없어 리스폰 연출 UI를 만들 수 없습니다.");
                return;
            }

            GameObject fadeObject = CreateMissionUIRect(canvasObject.transform, "RespawnFadeImage", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image fadeImage = fadeObject.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;
            fadeObject.transform.SetAsLastSibling();

            GameObject wakeTextObject = CreateMissionUIRect(canvasObject.transform, "RespawnWakeText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 70f), Vector2.zero);
            Text wakeText = CreateMissionText(wakeTextObject, 30, FontStyle.Bold, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleCenter);
            wakeText.enabled = false;
            wakeTextObject.transform.SetAsLastSibling();

            GameObject managerObject = new GameObject("CheckpointRespawnManager");
            CheckpointRespawnManager respawnManager = managerObject.AddComponent<CheckpointRespawnManager>();

            Transform firstSpawnPoint = CreateSpawnPoint("SpawnPoint_01_CheckIn", new Vector3(0f, 1.05f, -18f), Color.cyan);
            ConfigureRespawnManager(respawnManager, player, firstSpawnPoint, fadeImage, wakeText);

            CreateCheckpointTrigger("Checkpoint_01_CheckIn", "SpawnPoint_01_CheckIn", new Vector3(0f, 1.05f, -18f), respawnManager, firstSpawnPoint, new Color(0.35f, 0.85f, 1f));
            CreateCheckpointTrigger("Checkpoint_02_GrabTest", "SpawnPoint_02_GrabTest", new Vector3(0f, 1.05f, 4f), respawnManager, null, new Color(1f, 0.82f, 0.18f));
            CreateCheckpointTrigger("Checkpoint_03_Storage", "SpawnPoint_03_Storage", new Vector3(0f, 1.05f, 28f), respawnManager, null, new Color(1f, 0.48f, 0.12f));
            CreateCheckpointTrigger("Checkpoint_04_Anchor", "SpawnPoint_04_Anchor", new Vector3(0f, 1.05f, 52f), respawnManager, null, new Color(0.25f, 1f, 0.45f));
            CreateCheckpointTrigger("Checkpoint_05_Core", "SpawnPoint_05_Core", new Vector3(0f, 1.05f, 76f), respawnManager, null, new Color(0.12f, 0.48f, 1f));
            CreateCheckpointTrigger("Checkpoint_06_Security", "SpawnPoint_06_Security", new Vector3(0f, 1.05f, 100f), respawnManager, null, new Color(1f, 0.16f, 0.08f));
            CreateCheckpointTrigger("Checkpoint_07_Puzzle", "SpawnPoint_07_Puzzle", new Vector3(0f, 1.05f, 124f), respawnManager, null, new Color(1f, 0.25f, 0.65f));
            CreateCheckpointTrigger("Checkpoint_08_Airlock", "SpawnPoint_08_Airlock", new Vector3(0f, 1.05f, 148f), respawnManager, null, new Color(0.1f, 0.95f, 1f));
            CreateCheckpointTrigger("Checkpoint_09_Decon", "SpawnPoint_09_Decon", new Vector3(0f, 1.05f, 172f), respawnManager, null, new Color(0.1f, 1f, 0.55f));
            CreateCheckpointTrigger("Checkpoint_10_Escape", "SpawnPoint_10_Escape", new Vector3(0f, 1.05f, 196f), respawnManager, null, new Color(0.96f, 0.98f, 1f));

            CreateKillZone(respawnManager);
        }

        private static void ConfigureRespawnManager(CheckpointRespawnManager respawnManager, GameObject player, Transform firstSpawnPoint, Image fadeImage, Text wakeText)
        {
            SerializedObject serializedRespawnManager = new SerializedObject(respawnManager);
            serializedRespawnManager.FindProperty("playerTransform").objectReferenceValue = player.transform;
            serializedRespawnManager.FindProperty("playerController").objectReferenceValue = player.GetComponent<CharacterController>();
            serializedRespawnManager.FindProperty("playerMovement").objectReferenceValue = player.GetComponent<PlayerMovement>();
            serializedRespawnManager.FindProperty("fadeImage").objectReferenceValue = fadeImage;
            serializedRespawnManager.FindProperty("wakeUpText").objectReferenceValue = wakeText;
            serializedRespawnManager.FindProperty("startingSpawnPoint").objectReferenceValue = firstSpawnPoint;
            serializedRespawnManager.FindProperty("fadeOutDuration").floatValue = 0.45f;
            serializedRespawnManager.FindProperty("sleepDuration").floatValue = 0.65f;
            serializedRespawnManager.FindProperty("fadeInDuration").floatValue = 1.15f;
            serializedRespawnManager.FindProperty("wakeUpMessage").stringValue = "눈을 뜨는 중...";
            serializedRespawnManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCheckpointTrigger(string checkpointName, string spawnPointName, Vector3 position, CheckpointRespawnManager respawnManager, Transform existingSpawnPoint, Color color)
        {
            Transform spawnPoint = existingSpawnPoint != null ? existingSpawnPoint : CreateSpawnPoint(spawnPointName, position, color);

            GameObject triggerObject = new GameObject(checkpointName);
            triggerObject.transform.position = ScaleMapPosition(position);

            BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(7f * LayoutScale, 3f, 5f * LayoutScale);

            CheckpointTrigger checkpointTrigger = triggerObject.AddComponent<CheckpointTrigger>();
            SerializedObject serializedTrigger = new SerializedObject(checkpointTrigger);
            serializedTrigger.FindProperty("respawnManager").objectReferenceValue = respawnManager;
            serializedTrigger.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateSpawnPoint(string objectName, Vector3 position, Color color)
        {
            GameObject spawnPoint = new GameObject(objectName);
            spawnPoint.transform.position = ScaleMapPosition(position);
            spawnPoint.transform.rotation = Quaternion.identity;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"{objectName}_Marker";
            marker.transform.SetParent(spawnPoint.transform);
            marker.transform.localPosition = new Vector3(0f, -0.95f, 0f);
            marker.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSceneMaterial($"{objectName}_Marker_Material", color);
            }

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(markerCollider);
            }

            return spawnPoint.transform;
        }

        private static void CreateKillZone(CheckpointRespawnManager respawnManager)
        {
            GameObject killZoneObject = new GameObject("Map_KillZone");
            killZoneObject.transform.position = ScaleMapPosition(new Vector3(0f, -8f, 94f));

            BoxCollider killZoneCollider = killZoneObject.AddComponent<BoxCollider>();
            killZoneCollider.isTrigger = true;
            killZoneCollider.size = new Vector3(70f * LayoutScale, 4f, 260f * LayoutScale);

            KillZone killZone = killZoneObject.AddComponent<KillZone>();
            SerializedObject serializedKillZone = new SerializedObject(killZone);
            serializedKillZone.FindProperty("respawnManager").objectReferenceValue = respawnManager;
            serializedKillZone.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateMissionUIRect(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject uiObject = new GameObject(objectName);
            uiObject.transform.SetParent(parent);

            RectTransform rectTransform = uiObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;

            return uiObject;
        }

        private static Text CreateMissionText(GameObject textObject, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            Text text = textObject.AddComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", fontSize);
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureMissionSteps(SerializedProperty missionSteps)
        {
            string[,] data =
            {
                { "check_in_console", "01 CHECK-IN: 근무 종료 확인", "시작 방 왼쪽 콘솔 앞 목표 패드로 이동하세요. 폐쇄된 시설에 갇혔다는 상황을 확인합니다.", "체크인 콘솔 확인 완료. 다음 방으로 이동하세요." },
                { "reach_grab_practice", "02 GRAB TEST: 타겟 확인", "노란 방 오른쪽의 목표 패드까지 이동하세요. 앞의 캐릭터 타겟에 Grab Pack 손을 발사해 봅니다.", "Grab Pack 테스트 지점을 확인했습니다." },
                { "reach_storage", "03 STORAGE: 보관함 확인", "주황색 보관실 왼쪽 컨테이너 앞의 목표 패드까지 이동하세요.", "보관함 확인 완료." },
                { "reach_anchor_room", "04 ANCHOR: 출구까지 끌려가기", "초록색 방의 고정 기둥을 잡고 방 앞쪽 목표 패드까지 이동하세요.", "앵커 이동 구간을 통과했습니다." },
                { "reach_core_lab", "05 CORE: Energy Core 위치 확인", "파란 방 왼쪽의 Energy Core 옆 목표 패드까지 이동하세요.", "Energy Core 위치를 확인했습니다." },
                { "charge_core", "Energy Core를 Core Station에 올리기", "Grab Pack으로 파란 Energy Core를 끌어 회색 Core Station 위에 올리세요.", "충전 완료. 다음 보안문이 열렸습니다." },
                { "reach_security", "06 SECURITY: 경보 콘솔 확인", "열린 보안문을 지나 붉은 방 왼쪽 경보 콘솔 앞 목표 패드로 이동하세요.", "경보 콘솔 확인 완료." },
                { "reach_puzzle", "07 PUZZLE: 출구 패드 도착", "분홍색 방의 좌우 앵커를 이용해 앞쪽 출구 목표 패드까지 이동하세요.", "퍼즐 방 출구에 도착했습니다." },
                { "reach_airlock", "08 AIRLOCK: 콘솔 확인", "청록색 방 오른쪽 목표 패드로 이동해 Airlock 콘솔을 확인하세요.", "Airlock 콘솔 확인 완료." },
                { "reach_decon", "09 DECON: 오염 제거 링 통과", "녹색 방 왼쪽 파이프 링 아래 목표 패드까지 이동하세요.", "오염 제거 링 통과 완료." },
                { "reach_escape", "10 ESCAPE: 최종 문 도착", "밝은 마지막 방의 큰 출구 문 앞 목표 패드까지 이동하세요.", "Escape Bay에 도착했습니다. 탈출 성공!" }
            };

            missionSteps.arraySize = data.GetLength(0);
            for (int i = 0; i < data.GetLength(0); i++)
            {
                SerializedProperty step = missionSteps.GetArrayElementAtIndex(i);
                step.FindPropertyRelative("objectiveId").stringValue = data[i, 0];
                step.FindPropertyRelative("title").stringValue = data[i, 1];
                step.FindPropertyRelative("description").stringValue = data[i, 2];
                step.FindPropertyRelative("completionMessage").stringValue = data[i, 3];
            }
        }

        private static void CreateMissionTrigger(string objectName, string objectiveId, MissionManager missionManager, Vector3 center, Vector3 size)
        {
            GameObject triggerObject = new GameObject(objectName);
            triggerObject.transform.position = ScaleMapPosition(center);

            BoxCollider boxCollider = triggerObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(size.x * LayoutScale, size.y, size.z * LayoutScale);

            MissionObjectiveTrigger trigger = triggerObject.AddComponent<MissionObjectiveTrigger>();
            SerializedObject serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedTrigger.FindProperty("objectiveId").stringValue = objectiveId;
            serializedTrigger.FindProperty("triggerOnce").boolValue = true;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateSceneMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = materialName;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private static void CreateGrabPackTestObjects(GameObject player, GameObject cameraObject)
        {
            Type grabPackControllerType = Type.GetType("GrabPackController, Assembly-CSharp");
            if (grabPackControllerType == null)
            {
                Debug.LogError("GrabPackController 타입을 찾을 수 없습니다.");
                return;
            }

            GameObject leftMuzzle = CreateChildPoint(cameraObject.transform, "LeftGrab_Muzzle", new Vector3(-0.25f, -0.2f, 0.35f));
            GameObject rightMuzzle = CreateChildPoint(cameraObject.transform, "RightGrab_Muzzle", new Vector3(0.25f, -0.2f, 0.35f));
            GameObject leftHoldPoint = CreateChildPoint(cameraObject.transform, "LeftGrabHoldPoint", new Vector3(-0.45f, -0.15f, 2.5f));
            GameObject rightHoldPoint = CreateChildPoint(cameraObject.transform, "RightGrabHoldPoint", new Vector3(0.45f, -0.15f, 2.5f));

            LineRenderer leftLineRenderer = CreateGrabLine(cameraObject.transform, "LeftGrab_Line", Color.cyan, Color.white);
            LineRenderer rightLineRenderer = CreateGrabLine(cameraObject.transform, "RightGrab_Line", new Color(1f, 0.45f, 0.1f), Color.white);
            Transform leftArmVisual = CreateGrabArmSegment(cameraObject.transform, "LeftGrab_ArmVisual", new Color(0.1f, 0.35f, 1f));
            Transform rightArmVisual = CreateGrabArmSegment(cameraObject.transform, "RightGrab_ArmVisual", new Color(1f, 0.12f, 0.08f));
            Transform leftHandVisual = CreateGrabHandVisual(cameraObject.transform, "LeftGrab_HandVisual", new Color(0.1f, 0.35f, 1f));
            Transform rightHandVisual = CreateGrabHandVisual(cameraObject.transform, "RightGrab_HandVisual", new Color(1f, 0.12f, 0.08f));

            Component grabPackController = player.AddComponent(grabPackControllerType);
            SerializedObject serializedGrabPack = new SerializedObject(grabPackController);
            serializedGrabPack.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            serializedGrabPack.FindProperty("playerController").objectReferenceValue = player.GetComponent<CharacterController>();
            serializedGrabPack.FindProperty("leftMuzzleTransform").objectReferenceValue = leftMuzzle.transform;
            serializedGrabPack.FindProperty("rightMuzzleTransform").objectReferenceValue = rightMuzzle.transform;
            serializedGrabPack.FindProperty("leftGrabHoldPoint").objectReferenceValue = leftHoldPoint.transform;
            serializedGrabPack.FindProperty("rightGrabHoldPoint").objectReferenceValue = rightHoldPoint.transform;
            serializedGrabPack.FindProperty("leftLineRenderer").objectReferenceValue = leftLineRenderer;
            serializedGrabPack.FindProperty("rightLineRenderer").objectReferenceValue = rightLineRenderer;
            serializedGrabPack.FindProperty("leftArmVisual").objectReferenceValue = leftArmVisual;
            serializedGrabPack.FindProperty("rightArmVisual").objectReferenceValue = rightArmVisual;
            serializedGrabPack.FindProperty("leftHandVisual").objectReferenceValue = leftHandVisual;
            serializedGrabPack.FindProperty("rightHandVisual").objectReferenceValue = rightHandVisual;
            serializedGrabPack.FindProperty("grabRange").floatValue = 18f;
            serializedGrabPack.FindProperty("pullForce").floatValue = 42f;
            serializedGrabPack.FindProperty("breakDistance").floatValue = 24f;
            serializedGrabPack.FindProperty("cooldown").floatValue = 0.25f;
            serializedGrabPack.FindProperty("armExtendSpeed").floatValue = 32f;
            serializedGrabPack.FindProperty("armRetractSpeed").floatValue = 34f;
            serializedGrabPack.FindProperty("armRadius").floatValue = 0.08f;
            serializedGrabPack.FindProperty("handVisualSize").floatValue = 0.32f;
            serializedGrabPack.FindProperty("enablePlayerPull").boolValue = true;
            serializedGrabPack.FindProperty("playerPullDelay").floatValue = 2f;
            serializedGrabPack.FindProperty("playerPullSpeed").floatValue = 10f;
            serializedGrabPack.FindProperty("playerPullStopDistance").floatValue = 2f;
            serializedGrabPack.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateGrabArmSegment(Transform parent, string objectName, Color color)
        {
            GameObject armObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armObject.name = objectName;
            armObject.transform.SetParent(parent);
            armObject.transform.localPosition = Vector3.zero;
            armObject.transform.localRotation = Quaternion.identity;
            armObject.transform.localScale = Vector3.one;
            armObject.SetActive(false);

            Renderer renderer = armObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSceneMaterial($"{objectName}_Material", color);
            }

            Collider collider = armObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return armObject.transform;
        }

        private static Transform CreateGrabHandVisual(Transform parent, string objectName, Color color)
        {
            GameObject handObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handObject.name = objectName;
            handObject.transform.SetParent(parent);
            handObject.transform.localPosition = Vector3.zero;
            handObject.transform.localRotation = Quaternion.identity;
            handObject.transform.localScale = Vector3.one;
            handObject.SetActive(false);

            Renderer renderer = handObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSceneMaterial($"{objectName}_Material", color);
            }

            Collider collider = handObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return handObject.transform;
        }

        private static GameObject CreateChildPoint(Transform parent, string objectName, Vector3 localPosition)
        {
            GameObject point = new GameObject(objectName);
            point.transform.SetParent(parent);
            point.transform.localPosition = localPosition;
            point.transform.localRotation = Quaternion.identity;
            return point;
        }

        private static LineRenderer CreateGrabLine(Transform parent, string objectName, Color startColor, Color endColor)
        {
            GameObject lineObject = CreateChildPoint(parent, objectName, Vector3.zero);
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 0.04f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
            return lineRenderer;
        }
    }
}
