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

            if (player == null || cameraObject == null || visual == null || grabTarget == null)
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

            Debug.Log("PlayerMovement 테스트 씬 검증 완료: Astronaut, CharacterController, PlayerMovement, ThirdPersonCamera, GrabTarget, Main Camera 연결 확인");
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
    }
}
