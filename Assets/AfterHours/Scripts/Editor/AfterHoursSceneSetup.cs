using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace AfterHours.EditorTools
{
    /// <summary>
    /// After Hours 테스트 씬을 생성하는 에디터 전용 도구입니다.
    /// </summary>
    public static class AfterHoursSceneSetup
    {
        private const string ScenePath = "Assets/AfterHours/Scenes/PlayerMovementTest.unity";
        private const string AstronautPrefabPath = "Assets/Asset/Stylized_Astronaut/Stylized Astronaut.prefab";
        private const string GrabTargetModelPath = "Assets/Asset/kenney_blocky-characters_20/Models/FBX format/character-g.fbx";

        [MenuItem("After Hours/Setup/Create Player Movement Test Scene")]
        public static void CreatePlayerMovementTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PlayerMovementTest";

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Test Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f);

            GameObject player = new GameObject("Player_Astronaut");
            player.transform.position = new Vector3(0f, 1.05f, 0f);

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0f, 0f);
            characterController.stepOffset = 0.3f;

            PlayerMovement playerMovement = player.AddComponent<PlayerMovement>();

            GameObject astronautPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AstronautPrefabPath);
            if (astronautPrefab == null)
            {
                Debug.LogError($"Astronaut 프리팹을 찾을 수 없습니다: {AstronautPrefabPath}");
                return;
            }

            GameObject astronaut = (GameObject)PrefabUtility.InstantiatePrefab(astronautPrefab);
            astronaut.name = "Astronaut Visual";
            astronaut.transform.SetParent(player.transform);
            astronaut.transform.localPosition = new Vector3(0f, -1.05f, 0f);
            astronaut.transform.localRotation = Quaternion.identity;
            astronaut.transform.localScale = Vector3.one;

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

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            SerializedObject serializedMovement = new SerializedObject(playerMovement);
            serializedMovement.FindProperty("characterController").objectReferenceValue = characterController;
            serializedMovement.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            serializedMovement.FindProperty("walkSpeed").floatValue = 4f;
            serializedMovement.FindProperty("runSpeed").floatValue = 7f;
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
            GameObject grabTarget = GameObject.Find("GrabTarget_CharacterG");
            GameObject energyCore = GameObject.Find("EnergyCore_Test");
            GameObject coreStation = GameObject.Find("CoreStation_Test");
            GameObject securityDoor = GameObject.Find("SecurityDoor_Core_Test");
            GameObject grabMuzzle = GameObject.Find("GrabPack_Muzzle");
            GameObject grabHoldPoint = GameObject.Find("GrabHoldPoint");

            if (player == null || cameraObject == null || visual == null || grabTarget == null || energyCore == null || coreStation == null || securityDoor == null || grabMuzzle == null || grabHoldPoint == null)
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

            Debug.Log("PlayerMovement 테스트 씬 검증 완료: Astronaut, CharacterController, PlayerMovement, ThirdPersonCamera, GrabTarget, EnergyCore, CoreStation, SecurityDoor, GrabPackController, Main Camera 연결 확인");
            EditorApplication.Exit(0);
        }

        private static void CreateGrabTargetTestObject()
        {
            GameObject targetRoot = new GameObject("GrabTarget_CharacterG");
            targetRoot.transform.position = new Vector3(2f, 1f, 3f);
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
            core.transform.position = new Vector3(-2f, 1f, 3f);
            core.transform.localScale = Vector3.one * 0.7f;

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
            door.transform.position = new Vector3(0f, 1.5f, 6f);
            door.transform.localScale = new Vector3(3f, 3f, 0.25f);
            door.GetComponent<Renderer>().sharedMaterial = coreDoorMaterial;

            Component securityDoor = door.AddComponent(securityDoorType);
            SerializedObject serializedDoor = new SerializedObject(securityDoor);
            serializedDoor.FindProperty("doorTransform").objectReferenceValue = door.transform;
            serializedDoor.FindProperty("openAudio").objectReferenceValue = null;
            serializedDoor.FindProperty("openOffset").vector3Value = new Vector3(0f, 3f, 0f);
            serializedDoor.FindProperty("openDuration").floatValue = 1.5f;
            serializedDoor.ApplyModifiedPropertiesWithoutUndo();

            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = "CoreStation_Test";
            station.transform.position = new Vector3(-2f, 0.15f, 5f);
            station.transform.localScale = new Vector3(1.5f, 0.3f, 1.5f);
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

            GameObject muzzle = new GameObject("GrabPack_Muzzle");
            muzzle.transform.SetParent(cameraObject.transform);
            muzzle.transform.localPosition = new Vector3(0.25f, -0.2f, 0.35f);
            muzzle.transform.localRotation = Quaternion.identity;

            GameObject holdPoint = new GameObject("GrabHoldPoint");
            holdPoint.transform.SetParent(cameraObject.transform);
            holdPoint.transform.localPosition = new Vector3(0f, 0f, 2.5f);
            holdPoint.transform.localRotation = Quaternion.identity;

            GameObject lineObject = new GameObject("GrabPack_Line");
            lineObject.transform.SetParent(cameraObject.transform);
            lineObject.transform.localPosition = Vector3.zero;
            lineObject.transform.localRotation = Quaternion.identity;

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 0.04f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.white;

            Component grabPackController = player.AddComponent(grabPackControllerType);
            SerializedObject serializedGrabPack = new SerializedObject(grabPackController);
            serializedGrabPack.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            serializedGrabPack.FindProperty("muzzleTransform").objectReferenceValue = muzzle.transform;
            serializedGrabPack.FindProperty("grabHoldPoint").objectReferenceValue = holdPoint.transform;
            serializedGrabPack.FindProperty("grabRange").floatValue = 8f;
            serializedGrabPack.FindProperty("pullForce").floatValue = 35f;
            serializedGrabPack.FindProperty("breakDistance").floatValue = 12f;
            serializedGrabPack.FindProperty("cooldown").floatValue = 0.25f;
            serializedGrabPack.FindProperty("lineRenderer").objectReferenceValue = lineRenderer;
            serializedGrabPack.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
