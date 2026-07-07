using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AfterHours.EditorTools
{
    /// <summary>
    /// 기존 씬 배치를 유지한 채 퇴근 시나리오용 연결만 추가하는 에디터 패처입니다.
    /// </summary>
    public static class AfterHoursScenePatcher
    {
        private const string ScenePath = "Assets/AfterHours/Scenes/PlayerMovementTest.unity";
        private const string PatchRootName = "AfterHours_ClockOutPatch";
        private const string GrabTargetModelPath = "Assets/Asset/kenney_blocky-characters_20/Models/FBX format/character-g.fbx";
        private const float LayoutScale = 2.5f;

        [MenuItem("After Hours/Setup/Patch Clock-Out Scenario In Current Scene")]
        public static void PatchClockOutScenarioInCurrentScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            MissionManager missionManager = Object.FindAnyObjectByType<MissionManager>();
            if (missionManager == null)
            {
                Debug.LogError("MissionManager를 찾을 수 없어 퇴근 시나리오 패치를 중단합니다.");
                EditorApplication.Exit(1);
                return;
            }

            Transform patchRoot = GetOrCreateRoot(PatchRootName).transform;

            ConfigureMissionSteps(missionManager);
            ConfigureExistingMissionTriggers(missionManager);
            ConfigureAreaLabels();
            ConfigureQuestObjectPositions();
            ConfigureGrabPackPickup(missionManager);
            ConfigureCoreStation("CoreStation_Test", missionManager, "swap_battery_01");
            ConfigureMissionDoors(patchRoot, missionManager);
            CreateSignalLightRoom(patchRoot, missionManager);
            CreateSecondBatteryRoom(patchRoot, missionManager);
            CreateRobotCheckoutRoom(patchRoot, missionManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("기존 씬 보존 방식의 퇴근 시나리오 패치 완료");
        }

        private static Transform GetOrCreateRoot(string rootName)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                root = new GameObject(rootName);
            }

            return root.transform;
        }

        private static void ConfigureMissionSteps(MissionManager missionManager)
        {
            SerializedObject serializedMission = new SerializedObject(missionManager);
            SerializedProperty missionSteps = serializedMission.FindProperty("missionSteps");
            string[,] data =
            {
                { "check_in_console", "01 퇴근 점검 시작", "컴퓨터 앞 목표 패드에서 오늘의 퇴근 점검을 시작하세요.", "퇴근 점검 시작 확인 완료." },
                { "check_storage", "02 스토리지 확인", "왼쪽 점프 구간을 지나 스토리지 컨테이너를 확인하세요.", "스토리지 확인 완료." },
                { "test_grab_gear", "03 그랩 장비 획득", "Grab Pack 장비 구역에서 좌클릭/우클릭으로 앞의 테스트 오브젝트를 잡아보세요.", "그랩 장비 점검 완료." },
                { "use_anchor", "04 앵커 사용", "점프로 넘기 어려운 구역에서 앵커를 잡고 출구까지 이동하세요.", "앵커 이동 점검 완료." },
                { "swap_battery_01", "05 전력 낮은 방 배터리 교체", "푸른 Energy Core를 회색 Core Station에 넣어 방 전력을 복구하세요.", "첫 번째 배터리 교체 완료." },
                { "pass_warning_lights", "06 신호등 방 통과", "점검등이 꺼지는 타이밍에만 붉은 점검등 구역을 통과하세요.", "신호등 방 통과 완료." },
                { "swap_battery_02", "07 배터리 교체 2", "한 손은 코어, 한 손은 앵커를 쓰는 느낌으로 함정을 피해 두 번째 코어를 넣으세요.", "두 번째 배터리 교체 완료." },
                { "checkout_robot", "08 직원 퇴근 시키기", "퇴근하지 않은 로봇을 Grab Pack으로 잡아 창문 밖 처리 구역으로 보내세요.", "로봇 퇴근 처리 완료." },
                { "clear_final_mix", "09 종합 점검 방", "라이트, 점프, 그랩을 섞어 최종 점검 방 출구까지 이동하세요.", "종합 점검 통과." },
                { "clock_out", "10 퇴근 완료", "마지막 기계 앞 목표 패드에서 점검완을 찍고 퇴근하세요.", "점검완. 퇴근 완료!" }
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

            serializedMission.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureExistingMissionTriggers(MissionManager missionManager)
        {
            SetMissionTrigger("MissionTrigger_CheckInConsolePad", missionManager, "check_in_console");
            SetMissionTrigger("MissionTrigger_StorageCratePad", missionManager, "check_storage");
            SetMissionTrigger("MissionTrigger_GrabTargetPad", missionManager, "grab_test_after_pickup");
            SetMissionTrigger("MissionTrigger_AnchorExitPad", missionManager, "use_anchor");
            SetMissionTrigger("MissionTrigger_PuzzleExitPad", missionManager, "clear_final_mix");
            SetMissionTrigger("MissionTrigger_EscapeDoorPad", missionManager, "clock_out");
        }

        private static void ConfigureAreaLabels()
        {
            SetTextMeshLabel("Room01_CheckIn_Label", "01 CHECK-IN\nPC 점검");
            SetTextMeshLabel("Room02_GrabPractice_Label", "02 STORAGE\n좌측 점프");
            SetTextMeshLabel("Room03_Storage_Label", "03 GRAB PACK\n장비 착용");
            SetTextMeshLabel("Room04_AnchorShaft_Label", "04 ANCHOR\n2초 고정");
            SetTextMeshLabel("Room05_CoreLab_Label", "05 POWER\n코어 교체");
            SetTextMeshLabel("Room06_SecurityGate_Label", "06 SIGNAL\n소등 통과");
            SetTextMeshLabel("Room07_PuzzleCell_Label", "07 BATTERY\n코어 + 앵커");
            SetTextMeshLabel("Room08_Airlock_Label", "08 ROBOT\n퇴근 처리");
            SetTextMeshLabel("Room09_Decon_Label", "09 FINAL\n종합 점검");
            SetTextMeshLabel("Room10_EscapeBay_Label", "10 CLOCK OUT\n점검완");
        }

        private static void SetTextMeshLabel(string objectName, string labelText)
        {
            GameObject labelObject = GameObject.Find(objectName);
            TextMesh textMesh = labelObject != null ? labelObject.GetComponent<TextMesh>() : null;
            if (textMesh == null)
            {
                Debug.LogWarning($"{objectName} 라벨을 찾지 못했습니다.");
                return;
            }

            textMesh.text = labelText;
        }

        private static void ConfigureQuestObjectPositions()
        {
            // 02번 미션은 왼쪽 점프 동선 끝에서 스토리지를 확인하게 배치합니다.
            SetWorldPosition("MissionTrigger_StorageCratePad", new Vector3(-13.5f, 1.2f, 38f));
            SetWorldPosition("Room03_Storage_Container_A", new Vector3(-13.5f, 0f, 33f));
            SetWorldPosition("Room03_Storage_Container_B", new Vector3(-16.5f, 0f, 42f));

            // 03번 미션은 장비를 실제로 밟고 지나가며 획득한 뒤, 앞의 테스트 타겟을 잡게 만듭니다.
            SetWorldPosition("Grab Pack Rig by D1GQ", new Vector3(0f, 1.1f, 82f));
            SetWorldPosition("MissionTrigger_GrabTargetPad", new Vector3(0f, 1.2f, 101f));
            SetWorldPosition("GrabTarget_TestDummy", new Vector3(0f, 1.1f, 108f));
            SetWorldPosition("GrabTarget_Test", new Vector3(0f, 1.1f, 108f));

            // 05번 코어 교체는 코어와 스테이션을 한눈에 보이도록 좌우로 벌립니다.
            SetWorldPosition("EnergyCore_Test", new Vector3(-10f, 1.15f, 205f));
            SetWorldPosition("CoreStation_Test", new Vector3(10f, 0.15f, 205f));

            // 06번 신호등은 방 중앙 통로를 가로막도록 정렬합니다.
            SetWorldPosition("ClockOut_SignalLightGate", new Vector3(0f, 1.5f, 265f));
            SetWorldPosition("ClockOut_SignalLight_BlinkingBar", new Vector3(0f, 9.7f, 265f));
            SetWorldPosition("ClockOut_SignalLight_PointLight", new Vector3(0f, 8.9f, 265f));

            // 07번 두 번째 배터리는 코어와 스테이션을 대각선으로 배치해 앵커 활용 의도를 보이게 합니다.
            SetWorldPosition("ClockOut_EnergyCore_TrapRoom", new Vector3(-13f, 1.1f, 325f));
            SetWorldPosition("ClockOut_CoreStation_TrapRoom", new Vector3(13f, 0.15f, 338f));

            // 08번 로봇은 왼쪽, 처리 창문은 오른쪽으로 배치해 던지는 방향을 명확히 합니다.
            SetWorldPosition("ClockOut_Overtime_Robot", new Vector3(-12f, 1f, 385f));
            SetWorldPosition("ClockOut_RobotCheckout_WindowTrigger", new Vector3(18f, 1.5f, 385f));
        }

        private static void SetWorldPosition(string objectName, Vector3 position)
        {
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null)
            {
                return;
            }

            targetObject.transform.position = position;
        }

        private static void ConfigureGrabPackPickup(MissionManager missionManager)
        {
            GrabPackController grabPackController = Object.FindAnyObjectByType<GrabPackController>();
            if (grabPackController == null)
            {
                Debug.LogWarning("GrabPackController를 찾지 못해 장비 획득 설정을 건너뜁니다.");
                return;
            }

            SerializedObject serializedController = new SerializedObject(grabPackController);
            serializedController.FindProperty("isGrabPackUsable").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            GameObject pickupObject = GameObject.Find("Grab Pack Rig by D1GQ");
            if (pickupObject == null)
            {
                Debug.LogWarning("Grab Pack Rig by D1GQ 오브젝트를 찾지 못했습니다.");
                return;
            }

            BoxCollider pickupCollider = pickupObject.GetComponent<BoxCollider>();
            if (pickupCollider == null)
            {
                pickupCollider = pickupObject.AddComponent<BoxCollider>();
            }

            pickupCollider.isTrigger = true;
            pickupCollider.size = new Vector3(3f, 2.6f, 3f);
            pickupCollider.center = new Vector3(0f, 1.3f, 0f);

            Type pickupType = Type.GetType("GrabPackPickup, Assembly-CSharp");
            if (pickupType == null)
            {
                Debug.LogWarning("GrabPackPickup 타입을 찾지 못해 장비 획득 트리거 연결을 건너뜁니다.");
                return;
            }

            Component pickup = pickupObject.GetComponent(pickupType);
            if (pickup == null)
            {
                pickup = pickupObject.AddComponent(pickupType);
            }

            SerializedObject serializedPickup = new SerializedObject(pickup);
            serializedPickup.FindProperty("grabPackController").objectReferenceValue = grabPackController;
            serializedPickup.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedPickup.FindProperty("completionObjectiveId").stringValue = "test_grab_gear";
            serializedPickup.FindProperty("completeMissionOnPickup").boolValue = true;
            serializedPickup.FindProperty("hideAfterPickup").boolValue = false;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetMissionTrigger(string objectName, MissionManager missionManager, string objectiveId)
        {
            GameObject triggerObject = GameObject.Find(objectName);
            if (triggerObject == null)
            {
                Debug.LogWarning($"{objectName}을 찾지 못했습니다.");
                return;
            }

            MissionObjectiveTrigger trigger = triggerObject.GetComponent<MissionObjectiveTrigger>();
            if (trigger == null)
            {
                trigger = triggerObject.AddComponent<MissionObjectiveTrigger>();
            }

            SerializedObject serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedTrigger.FindProperty("objectiveId").stringValue = objectiveId;
            serializedTrigger.FindProperty("triggerOnce").boolValue = true;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCoreStation(string objectName, MissionManager missionManager, string objectiveId)
        {
            GameObject stationObject = GameObject.Find(objectName);
            CoreStation station = stationObject != null ? stationObject.GetComponent<CoreStation>() : null;
            if (station == null)
            {
                Debug.LogWarning($"{objectName} CoreStation을 찾지 못했습니다.");
                return;
            }

            SerializedObject serializedStation = new SerializedObject(station);
            serializedStation.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedStation.FindProperty("completionObjectiveId").stringValue = objectiveId;
            serializedStation.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMissionDoors(Transform patchRoot, MissionManager missionManager)
        {
            string[] objectiveIds =
            {
                "check_in_console",
                "check_storage",
                "test_grab_gear",
                "use_anchor",
                "swap_battery_01",
                "pass_warning_lights",
                "swap_battery_02",
                "checkout_robot",
                "clear_final_mix"
            };

            string[] openDoorNames =
            {
                "door-double-Open",
                "door-double-Open2",
                "door-double-Open3",
                "door-double-Open4",
                "door-double-Open5",
                "door-double-Open6",
                "door-double-Open7",
                "door-double-Open8",
                "Door_09_To_10_Normal"
            };

            string[] closeDoorNames =
            {
                "door-double-closed",
                "door-double-closed2",
                "door-double-closed3",
                "door-double-closed4",
                "door-double-closed5",
                "door-double-closed6",
                "door-double-closed7",
                "door-double-closed8",
                "door-double-closed9"
            };

            string[] closeSpawnNames =
            {
                "SpawnPoint_02_GrabTest",
                "SpawnPoint_03_Storage",
                "SpawnPoint_04_Anchor",
                "SpawnPoint_05_Core",
                "SpawnPoint_06_Security",
                "SpawnPoint_07_Puzzle",
                "SpawnPoint_08_Airlock",
                "SpawnPoint_09_Decon",
                "SpawnPoint_10_Escape"
            };

            for (int i = 0; i < objectiveIds.Length; i++)
            {
                GameObject openDoorObject = GameObject.Find(openDoorNames[i]);
                if (openDoorObject == null)
                {
                    Debug.LogWarning($"{openDoorNames[i]} 문을 찾지 못했습니다.");
                    continue;
                }

                SecurityDoor openDoor = ConfigureDoor(openDoorObject, false, 2.4f, 0.35f);
                CreateMissionDoorController(patchRoot, $"ClockOut_OpenDoor_{i + 1:00}", missionManager, openDoor, objectiveIds[i]);

                GameObject backLockObject = GameObject.Find(closeDoorNames[i]);
                if (backLockObject == null)
                {
                    backLockObject = GetOrCreateBackLockDoor(patchRoot, i + 1, openDoorObject.transform.position);
                    Debug.LogWarning($"{closeDoorNames[i]} 문을 찾지 못해 보조 뒤잠금 문을 생성했습니다.");
                }

                SecurityDoor backLockDoor = ConfigureDoor(backLockObject, true, 2.4f, 0.28f);

                GameObject spawnObject = GameObject.Find(closeSpawnNames[i]);
                Vector3 triggerPosition = spawnObject != null ? spawnObject.transform.position : openDoorObject.transform.position + Vector3.forward * (8f * LayoutScale);
                CreateDoorCloseTrigger(patchRoot, $"ClockOut_CloseBackDoor_{i + 1:00}", backLockDoor, triggerPosition);
            }
        }

        private static SecurityDoor ConfigureDoor(GameObject doorObject, bool startOpened, float openDuration, float closeDuration)
        {
            SecurityDoor securityDoor = doorObject.GetComponent<SecurityDoor>();
            if (securityDoor == null)
            {
                securityDoor = doorObject.AddComponent<SecurityDoor>();
            }

            EnsureDoorCollider(doorObject);

            SerializedObject serializedDoor = new SerializedObject(securityDoor);
            serializedDoor.FindProperty("doorTransform").objectReferenceValue = doorObject.transform;
            serializedDoor.FindProperty("openOffset").vector3Value = new Vector3(0f, 8f, 0f);
            serializedDoor.FindProperty("openDuration").floatValue = openDuration;
            serializedDoor.FindProperty("closeDuration").floatValue = closeDuration;
            serializedDoor.FindProperty("startOpened").boolValue = startOpened;
            serializedDoor.ApplyModifiedPropertiesWithoutUndo();

            return securityDoor;
        }

        private static void EnsureDoorCollider(GameObject doorObject)
        {
            BoxCollider collider = doorObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = doorObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(7.2f, 5.2f, 0.8f);
                collider.center = new Vector3(0f, 2.6f, 0f);
            }
        }

        private static GameObject GetOrCreateBackLockDoor(Transform parent, int index, Vector3 basePosition)
        {
            string objectName = $"ClockOut_BackLockDoor_{index:00}";
            GameObject door = GameObject.Find(objectName);
            if (door != null)
            {
                return door;
            }

            door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = objectName;
            door.transform.SetParent(parent);
            door.transform.position = basePosition + new Vector3(0f, 0f, -0.8f);
            door.transform.localScale = new Vector3(7.2f, 5.2f, 0.45f);

            Renderer renderer = door.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSceneMaterial($"{objectName}_Material", new Color(0.12f, 0.14f, 0.16f));
            }

            return door;
        }

        private static void CreateMissionDoorController(Transform parent, string objectName, MissionManager missionManager, SecurityDoor securityDoor, string objectiveId)
        {
            GameObject controllerObject = GameObject.Find(objectName);
            if (controllerObject == null)
            {
                controllerObject = new GameObject(objectName);
                controllerObject.transform.SetParent(parent);
            }

            MissionDoorController controller = controllerObject.GetComponent<MissionDoorController>();
            if (controller == null)
            {
                controller = controllerObject.AddComponent<MissionDoorController>();
            }

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedController.FindProperty("securityDoor").objectReferenceValue = securityDoor;
            serializedController.FindProperty("openObjectiveId").stringValue = objectiveId;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDoorCloseTrigger(Transform parent, string objectName, SecurityDoor doorToClose, Vector3 position)
        {
            GameObject triggerObject = GameObject.Find(objectName);
            if (triggerObject == null)
            {
                triggerObject = new GameObject(objectName);
                triggerObject.transform.SetParent(parent);
                BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.size = new Vector3(7f, 3f, 6f);
            }

            triggerObject.transform.position = position + Vector3.up * 0.35f;

            DoorCloseTrigger trigger = triggerObject.GetComponent<DoorCloseTrigger>();
            if (trigger == null)
            {
                trigger = triggerObject.AddComponent<DoorCloseTrigger>();
            }

            SerializedObject serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("doorToClose").objectReferenceValue = doorToClose;
            serializedTrigger.FindProperty("triggerOnce").boolValue = true;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSignalLightRoom(Transform parent, MissionManager missionManager)
        {
            if (GameObject.Find("ClockOut_SignalLightGate") != null)
            {
                return;
            }

            GameObject gateObject = new GameObject("ClockOut_SignalLightGate");
            gateObject.transform.SetParent(parent);
            gateObject.transform.position = new Vector3(0f, 1.5f, 112f * LayoutScale);

            BoxCollider triggerCollider = gateObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(20f, 3f, 10f);

            GameObject lightBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightBar.name = "ClockOut_SignalLight_BlinkingBar";
            lightBar.transform.SetParent(parent);
            lightBar.transform.position = new Vector3(0f, 9.7f, 112f * LayoutScale);
            lightBar.transform.localScale = new Vector3(10f, 0.25f, 0.6f);

            Renderer renderer = lightBar.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateSceneMaterial("ClockOut_SignalLight_Material", new Color(1f, 0.1f, 0.05f));
            }

            Collider lightCollider = lightBar.GetComponent<Collider>();
            if (lightCollider != null)
            {
                Object.DestroyImmediate(lightCollider);
            }

            GameObject lightObject = new GameObject("ClockOut_SignalLight_PointLight");
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = new Vector3(0f, 8.9f, 112f * LayoutScale);
            Light warningLight = lightObject.AddComponent<Light>();
            warningLight.type = LightType.Point;
            warningLight.color = new Color(1f, 0.1f, 0.05f);
            warningLight.range = 18f;
            warningLight.intensity = 3f;

            WarningLightGate gate = gateObject.AddComponent<WarningLightGate>();
            SerializedObject serializedGate = new SerializedObject(gate);
            serializedGate.FindProperty("warningLight").objectReferenceValue = warningLight;
            serializedGate.FindProperty("warningRenderer").objectReferenceValue = renderer;
            serializedGate.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedGate.FindProperty("respawnManager").objectReferenceValue = Object.FindAnyObjectByType<CheckpointRespawnManager>();
            serializedGate.FindProperty("completionObjectiveId").stringValue = "pass_warning_lights";
            serializedGate.FindProperty("lightOnDuration").floatValue = 1.4f;
            serializedGate.FindProperty("lightOffDuration").floatValue = 1.2f;
            serializedGate.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSecondBatteryRoom(Transform parent, MissionManager missionManager)
        {
            if (GameObject.Find("ClockOut_CoreStation_TrapRoom") != null)
            {
                ConfigureCoreStation("ClockOut_CoreStation_TrapRoom", missionManager, "swap_battery_02");
                return;
            }

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "ClockOut_EnergyCore_TrapRoom";
            core.transform.SetParent(parent);
            core.transform.position = new Vector3(-13f, 1.1f, 130f * LayoutScale);
            core.transform.localScale = Vector3.one * 1.25f;

            Rigidbody coreRigidbody = core.AddComponent<Rigidbody>();
            coreRigidbody.mass = 0.75f;

            GrabTarget grabTarget = core.AddComponent<GrabTarget>();
            SerializedObject serializedGrabTarget = new SerializedObject(grabTarget);
            serializedGrabTarget.FindProperty("targetRigidbody").objectReferenceValue = coreRigidbody;
            serializedGrabTarget.ApplyModifiedPropertiesWithoutUndo();

            Renderer coreRenderer = core.GetComponent<Renderer>();
            Color coreColor = new Color(0f, 0.7f, 1f);
            if (coreRenderer != null)
            {
                coreRenderer.sharedMaterial = CreateSceneMaterial("ClockOut_Core_Blue_Material", coreColor);
            }

            GameObject lightObject = new GameObject("ClockOut_EnergyCore_TrapRoom_BlueLight");
            lightObject.transform.SetParent(core.transform);
            lightObject.transform.localPosition = Vector3.zero;
            Light coreLight = lightObject.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = coreColor;
            coreLight.intensity = 4f;
            coreLight.range = 4f;

            EnergyCore energyCore = core.AddComponent<EnergyCore>();
            SerializedObject serializedCore = new SerializedObject(energyCore);
            serializedCore.FindProperty("emissionRenderer").objectReferenceValue = coreRenderer;
            serializedCore.FindProperty("coreLight").objectReferenceValue = coreLight;
            serializedCore.FindProperty("isActive").boolValue = true;
            serializedCore.ApplyModifiedPropertiesWithoutUndo();

            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = "ClockOut_CoreStation_TrapRoom";
            station.transform.SetParent(parent);
            station.transform.position = new Vector3(13f, 0.15f, 130f * LayoutScale);
            station.transform.localScale = new Vector3(3.6f, 0.35f, 3.6f);
            station.GetComponent<Renderer>().sharedMaterial = CreateSceneMaterial("ClockOut_Station_Grey_Material", Color.gray);

            BoxCollider stationCollider = station.GetComponent<BoxCollider>();
            stationCollider.isTrigger = true;
            stationCollider.size = new Vector3(1f, 2f, 1f);
            stationCollider.center = new Vector3(0f, 0.75f, 0f);

            GameObject holdPoint = new GameObject("ClockOut_TrapRoom_StationHoldPoint");
            holdPoint.transform.SetParent(station.transform);
            holdPoint.transform.localPosition = new Vector3(0f, 1f, 0f);
            holdPoint.transform.localRotation = Quaternion.identity;

            CoreStation coreStation = station.AddComponent<CoreStation>();
            SerializedObject serializedStation = new SerializedObject(coreStation);
            serializedStation.FindProperty("stationHoldPoint").objectReferenceValue = holdPoint.transform;
            serializedStation.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedStation.FindProperty("chargeTime").floatValue = 3f;
            serializedStation.FindProperty("completionObjectiveId").stringValue = "swap_battery_02";
            serializedStation.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateRobotCheckoutRoom(Transform parent, MissionManager missionManager)
        {
            if (GameObject.Find("ClockOut_Overtime_Robot") == null)
            {
                GameObject robot = new GameObject("ClockOut_Overtime_Robot");
                robot.transform.SetParent(parent);
                robot.transform.position = new Vector3(-12f, 1f, 154f * LayoutScale);
                robot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                Rigidbody robotRigidbody = robot.AddComponent<Rigidbody>();
                robotRigidbody.mass = 1.2f;

                CapsuleCollider robotCollider = robot.AddComponent<CapsuleCollider>();
                robotCollider.radius = 0.38f;
                robotCollider.height = 2f;

                GrabTarget robotGrabTarget = robot.AddComponent<GrabTarget>();
                SerializedObject serializedGrabTarget = new SerializedObject(robotGrabTarget);
                serializedGrabTarget.FindProperty("targetRigidbody").objectReferenceValue = robotRigidbody;
                serializedGrabTarget.FindProperty("canBePulled").boolValue = true;
                serializedGrabTarget.FindProperty("canPullPlayer").boolValue = false;
                serializedGrabTarget.ApplyModifiedPropertiesWithoutUndo();

                GameObject robotModel = AssetDatabase.LoadAssetAtPath<GameObject>(GrabTargetModelPath);
                if (robotModel != null)
                {
                    GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(robotModel);
                    visual.name = "ClockOut_Overtime_Robot_Visual";
                    visual.transform.SetParent(robot.transform);
                    visual.transform.localPosition = new Vector3(0f, -1f, 0f);
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one * 1.15f;
                }
            }

            GameObject triggerObject = GameObject.Find("ClockOut_RobotCheckout_WindowTrigger");
            if (triggerObject == null)
            {
                triggerObject = new GameObject("ClockOut_RobotCheckout_WindowTrigger");
                triggerObject.transform.SetParent(parent);
                triggerObject.transform.position = new Vector3(18f, 1.5f, 154f * LayoutScale);
                BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.size = new Vector3(7f, 3f, 12f);
            }

            RobotCheckoutTrigger checkoutTrigger = triggerObject.GetComponent<RobotCheckoutTrigger>();
            if (checkoutTrigger == null)
            {
                checkoutTrigger = triggerObject.AddComponent<RobotCheckoutTrigger>();
            }

            SerializedObject serializedTrigger = new SerializedObject(checkoutTrigger);
            serializedTrigger.FindProperty("missionManager").objectReferenceValue = missionManager;
            serializedTrigger.FindProperty("robotObjectName").stringValue = "ClockOut_Overtime_Robot";
            serializedTrigger.FindProperty("completionObjectiveId").stringValue = "checkout_robot";
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
    }
}
