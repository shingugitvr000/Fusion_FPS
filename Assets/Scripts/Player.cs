using Fusion;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public class Player : NetworkBehaviour
{
    private NetworkCharacterController _cc;
    public float moveSpeed = 5.0f;

    [SerializeField] private NetworkPrefabRef _prefabBall;
    [Networked] private TickTimer delay { get; set; }
    [Networked] private NetworkButtons _networkButtons { get; set; }


    //카메라 관련 선언
    public ThirdPersonCamera thirdPersonCamera;
    [Networked] private Vector3 _networkCameraForward { get; set; }
    [Networked] private Vector3 _networkCameraRight { get; set; }
    [Networked] private Vector3 _networkMoveDirection { get; set; }

    public float rotationSpeed = 720.0f;

    //애니메이션 관련 선언
    public NetworkMecanimAnimator _animator;


    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        
        if (Object.HasInputAuthority)
        {
            SetupCamera();
        }
    }

    private void SetupCamera()
    {
        ThirdPersonCamera camera = FindObjectOfType<ThirdPersonCamera>();
        if (camera != null)
        {
            camera.target = transform;
            thirdPersonCamera = camera;
        }
        else
        {
            Debug.LogError("ThirdPersonCamera not found in the scene!");
        }
    }


    private void UpdateCameraDirection()
    {
        if (thirdPersonCamera != null)
        {
            _networkCameraForward = thirdPersonCamera.transform.forward;
            _networkCameraRight = thirdPersonCamera.transform.right;
        }
    }

    private void MovePlayer(Vector3 moveDirection)
    {
        // 중력을 포함한 이동 벡터 계산
        Vector3 movement = moveDirection * moveSpeed;

        if (moveDirection != Vector3.zero)
        {
            // NetworkCharacterController를 사용하여 이동 (중력 포함)
            _cc.Move(movement);

            // 이동 방향으로 회전
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Runner.DeltaTime);

            // 애니메이션 파라미터 설정
            float currentMoveSpeed = moveDirection.magnitude * moveSpeed;
            _animator.Animator.SetFloat("MoveSpeed", currentMoveSpeed);

            if (Object.HasInputAuthority)
            {
                Debug.Log($"Moving: {moveDirection}, Speed: {currentMoveSpeed}, Position: {transform.position}");
            }
        }
        else
        {
            // 움직이지 않을 때도 중력은 적용
            _cc.Move(Vector3.zero);
            _animator.Animator.SetFloat("MoveSpeed", 0);
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            _networkButtons = data.buttons;

            _networkMoveDirection = data.direction;

            if (Object.HasInputAuthority)
            {
                UpdateCameraDirection();
            }

            MovePlayer(data.direction);            
        }

        CheckAndFireProjectile();
    }

    private void CheckAndFireProjectile()                           //체크하고 쏘는 함수
    {
        if (delay.ExpiredOrNotRunning(Runner))
        {
            if (_networkButtons.IsSet(NetworkInputData.MOUSEBUTTON0))    //버튼 선언한것 가져와서 진행한다. 
            {
                delay = TickTimer.CreateFromSeconds(Runner, 0.5f);      //0.5초 간격으로 쏜다. 
                FireProjectile();
            }
        }
    }

    private void FireProjectile()                                   //발사체 생성 함수 
    {
        if (Object.HasStateAuthority)
        {
            Vector3 forward = transform.forward;
            Runner.Spawn(_prefabBall,
                transform.position + forward,
                Quaternion.LookRotation(forward),
                Object.InputAuthority,
                (runner, o) => o.GetComponent<Ball>().Init());
        }
    }



}