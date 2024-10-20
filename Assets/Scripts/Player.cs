using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private NetworkCharacterController _cc;
    public float moveSpeed = 5.0f;

    [SerializeField] private NetworkPrefabRef _prefabBall;
    [Networked] private TickTimer delay { get; set; }
    [Networked] private NetworkButtons _networkButtons { get; set; }


    //ī�޶� ���� ����
    public ThirdPersonCamera thirdPersonCamera;
    [Networked] private Vector3 _networkCameraForward { get; set; }
    [Networked] private Vector3 _networkCameraRight { get; set; }
    [Networked] private Vector3 _networkMoveDirection { get; set; }

    public float rotationSpeed = 720.0f;

    //�ִϸ��̼� ���� ����
    public NetworkMecanimAnimator _animator;

    [Networked] public string CurrentState { get; set; }

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
        CurrentState = "Waiting";
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

    public void SetPlayerState(string newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case "Playing":
                // ���� ���� �� �ʿ��� ����
                EnablePlayerControls();
                break;
            case "Finished":
                // ���� ���� �� �ʿ��� ����
                break;
            case "Spectating":
                DisablePlayerControls();
                break;
        }
    }

    private void EnablePlayerControls()
    {
        // �÷��̾� ��Ʈ�� ��Ȱ��ȭ ����
        _cc.enabled = true;
        _animator.enabled = true;
    }

    private void DisablePlayerControls()
    {
        // �÷��̾� ��Ʈ�� ��Ȱ��ȭ ����
        _cc.enabled = false;
        _animator.enabled = false;
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
        // �߷��� ������ �̵� ���� ���
        Vector3 movement = moveDirection * moveSpeed;

        if (moveDirection != Vector3.zero)
        {
            // NetworkCharacterController�� ����Ͽ� �̵� (�߷� ����)
            _cc.Move(movement);

            // �̵� �������� ȸ��
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Runner.DeltaTime);

            // �ִϸ��̼� �Ķ���� ����
            float currentMoveSpeed = moveDirection.magnitude * moveSpeed;
            _animator.Animator.SetFloat("MoveSpeed", currentMoveSpeed);

            if (Object.HasInputAuthority)
            {
                Debug.Log($"Moving: {moveDirection}, Speed: {currentMoveSpeed}, Position: {transform.position}");
            }
        }
        else
        {
            // �������� ���� ���� �߷��� ����
            _cc.Move(Vector3.zero);
            _animator.Animator.SetFloat("MoveSpeed", 0);
        }
    }


    public override void FixedUpdateNetwork()
    {
        if (CurrentState != "Playing")
        {
            return; // �÷��� ���� �ƴϸ� �Է� ó������ ����
        }

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

    private void CheckAndFireProjectile()                           //üũ�ϰ� ��� �Լ�
    {
        if (delay.ExpiredOrNotRunning(Runner))
        {
            if (_networkButtons.IsSet(NetworkInputData.MOUSEBUTTON0))    //��ư �����Ѱ� �����ͼ� �����Ѵ�. 
            {
                delay = TickTimer.CreateFromSeconds(Runner, 0.5f);      //0.5�� �������� ���. 
                FireProjectile();
            }
        }
    }

    private void FireProjectile()                                   //�߻�ü ���� �Լ� 
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