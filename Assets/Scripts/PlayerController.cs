using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 720f;
    
    [Header("Object Spawning")]
    public GameObject[] spawnableObjects;  // 생성 가능한 오브젝트들의 배열
    public Transform handPosition;         // 손 위치 (캐릭터 앞쪽)
    public float handDistance = 2f;        // 캐릭터로부터 손까지의 거리
    
    private Rigidbody rb;
    private Camera playerCamera;
    private GameObject currentHeldObject;  // 현재 들고 있는 오브젝트
    private int currentObjectIndex = 0;    // 현재 선택된 오브젝트 인덱스
    private bool isHoldingObject = false;  // 오브젝트를 들고 있는지 여부
    
    // Input System 변수들
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction spawnAction;
    private InputAction switchUpAction;
    private InputAction switchDownAction;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = Camera.main;
        
        // Rigidbody가 없다면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 기본 스폰 가능한 오브젝트들 생성 (프리팹이 없을 경우)
        if (spawnableObjects == null || spawnableObjects.Length == 0)
        {
            CreateDefaultObjects();
        }
        
        // 손 위치 설정
        if (handPosition == null)
        {
            GameObject handObj = new GameObject("HandPosition");
            handObj.transform.SetParent(transform);
            handObj.transform.localPosition = Vector3.forward * handDistance;
            handPosition = handObj.transform;
        }
        
        // Input System 설정
        SetupInputSystem();
    }
    
    void SetupInputSystem()
    {
        // PlayerInput 컴포넌트 추가 또는 가져오기
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
        
        // 입력 액션들 설정
        moveAction = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        
        spawnAction = new InputAction("Spawn", InputActionType.Button, "<Keyboard>/space");
        switchUpAction = new InputAction("SwitchUp", InputActionType.Button, "<Keyboard>/upArrow");
        switchDownAction = new InputAction("SwitchDown", InputActionType.Button, "<Keyboard>/downArrow");
        
        // 액션들 활성화
        moveAction.Enable();
        spawnAction.Enable();
        switchUpAction.Enable();
        switchDownAction.Enable();
        
        // 이벤트 연결
        spawnAction.performed += OnSpawnPressed;
        switchUpAction.performed += OnSwitchUp;
        switchDownAction.performed += OnSwitchDown;
    }
    
    void Update()
    {
        HandleMovement();
        UpdateHeldObjectPosition();
    }
    
    void OnDestroy()
    {
        // 메모리 누수 방지를 위해 액션들 비활성화
        if (moveAction != null) moveAction.Disable();
        if (spawnAction != null) spawnAction.Disable();
        if (switchUpAction != null) switchUpAction.Disable();
        if (switchDownAction != null) switchDownAction.Disable();
    }
    
    void HandleMovement()
    {
        // 새로운 Input System으로 입력 받기
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        
        if (movement.magnitude > 0.1f)
        {
            // 이동 - Rigidbody velocity 사용 (더 자연스러운 움직임)
            Vector3 moveDirection = movement * moveSpeed;
            rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
            
            // 회전 (이동 방향으로)
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // 입력이 없을 때는 수평 이동 정지
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }
    
    // 새로운 Input System 이벤트 메서드들
    void OnSpawnPressed(InputAction.CallbackContext context)
    {
        if (!isHoldingObject)
        {
            SpawnObject();
        }
        else
        {
            DestroyHeldObject();
        }
    }
    
    void OnSwitchUp(InputAction.CallbackContext context)
    {
        if (isHoldingObject && spawnableObjects.Length > 1)
        {
            SwitchToNextObject();
        }
    }
    
    void OnSwitchDown(InputAction.CallbackContext context)
    {
        if (isHoldingObject && spawnableObjects.Length > 1)
        {
            SwitchToPreviousObject();
        }
    }
    
    void SpawnObject()
    {
        if (spawnableObjects.Length > 0)
        {
            // 현재 선택된 오브젝트 생성
            GameObject prefab = spawnableObjects[currentObjectIndex];
            currentHeldObject = Instantiate(prefab, handPosition.position, handPosition.rotation);
            
            // 물리 시뮬레이션 비활성화 (손에 들고 있는 동안)
            Rigidbody objRb = currentHeldObject.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                objRb.isKinematic = true;
            }
            
            isHoldingObject = true;
            Debug.Log($"오브젝트 생성: {prefab.name}");
        }
    }
    
    void DestroyHeldObject()
    {
        if (currentHeldObject != null)
        {
            Destroy(currentHeldObject);
            currentHeldObject = null;
            isHoldingObject = false;
            Debug.Log("오브젝트 삭제");
        }
    }
    
    void SwitchToNextObject()
    {
        currentObjectIndex = (currentObjectIndex + 1) % spawnableObjects.Length;
        ReplaceHeldObject();
    }
    
    void SwitchToPreviousObject()
    {
        currentObjectIndex = (currentObjectIndex - 1 + spawnableObjects.Length) % spawnableObjects.Length;
        ReplaceHeldObject();
    }
    
    void ReplaceHeldObject()
    {
        if (isHoldingObject)
        {
            // 기존 오브젝트 삭제
            DestroyHeldObject();
            
            // 새 오브젝트 생성
            SpawnObject();
            
            Debug.Log($"오브젝트 변경: {spawnableObjects[currentObjectIndex].name}");
        }
    }
    
    void UpdateHeldObjectPosition()
    {
        if (isHoldingObject && currentHeldObject != null)
        {
            // 오브젝트가 손 위치를 따라다니도록
            currentHeldObject.transform.position = handPosition.position;
            currentHeldObject.transform.rotation = handPosition.rotation;
        }
    }
    
    void CreateDefaultObjects()
    {
        // 기본 프리팹들이 없을 경우 런타임에 생성
        spawnableObjects = new GameObject[3];
        
        // 큐브
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "DefaultCube";
        spawnableObjects[0] = cube;
        
        // 구
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "DefaultSphere";
        spawnableObjects[1] = sphere;
        
        // 실린더
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = "DefaultCylinder";
        spawnableObjects[2] = cylinder;
        
        // 프리팹들을 비활성화 (템플릿으로만 사용)
        cube.SetActive(false);
        sphere.SetActive(false);
        cylinder.SetActive(false);
        
        Debug.Log("기본 오브젝트들이 생성되었습니다.");
    }
}
