using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterHours.EditorTools
{
    /// <summary>
    /// After Hours 테스트 씬을 생성하는 에디터 전용 도구입니다.
    /// </summary>
    public static class AfterHoursSceneSetup
    {
        private const string ScenePath = "Assets/AfterHours/Scenes/PlayerMovementTest.unity";
        private const string AstronautPrefabPath = "Assets/Asset/Stylized_Astronaut/Stylized Astronaut.prefab";

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
            cameraObject.transform.position = new Vector3(0f, 2.4f, -5f);
            cameraObject.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            cameraObject.AddComponent<AudioListener>();

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

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"PlayerMovement 테스트 씬 생성 완료: {ScenePath}");
        }

        public static void CreatePlayerMovementTestSceneAndValidate()
        {
            CreatePlayerMovementTestScene();

            GameObject player = GameObject.Find("Player_Astronaut");
            GameObject cameraObject = GameObject.Find("Main Camera");
            GameObject visual = GameObject.Find("Astronaut Visual");

            if (player == null || cameraObject == null || visual == null)
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

            Debug.Log("PlayerMovement 테스트 씬 검증 완료: Astronaut, CharacterController, PlayerMovement, Main Camera 연결 확인");
            EditorApplication.Exit(0);
        }
    }
}
