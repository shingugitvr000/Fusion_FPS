using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


// NetworkManager 클래스
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private NetworkRunner _runner;
    private NetworkInputHandler _inputHandler;
    private string _roomName = "TestRoom"; // 기본 방 이름
    [SerializeField] private string _gameSceneName = "GameScene"; // 게임 씬 이름
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        _inputHandler = gameObject.AddComponent<NetworkInputHandler>();
    }

    private void OnGUI()
    {
        if (_runner == null)
        {
            // 방 이름 입력 필드
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            GUILayout.Label("방 번호:");
            _roomName = GUILayout.TextField(_roomName);

            // 호스트 버튼
            if (GUILayout.Button("호스트"))
            {
                StartGame(GameMode.Host);
            }
            // 참가 버튼
            if (GUILayout.Button("참가"))
            {
                StartGame(GameMode.Client);
            }
            GUILayout.EndArea();
        }
    }

    async void StartGame(GameMode mode)
    {
        // Fusion 러너를 생성하고 사용자 입력을 제공할 것임을 알림
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // 게임 씬의 빌드 인덱스 찾기
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(_gameSceneName);
        if (sceneIndex == -1)
        {
            Debug.LogError($"{_gameSceneName}을(를) 빌드 설정에서 찾을 수 없습니다!");
            return;
        }

        // Load the target scene
        var gameScene = SceneRef.FromIndex(sceneIndex);

        // Check if the scene was found
        if (gameScene == SceneRef.None)
        {
            Debug.LogError("GameScene could not be found in the available scenes!");
            return;
        }

        // 세션 시작 또는 참가
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = _roomName, // 사용자가 입력한 방 이름 사용
            Scene = gameScene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // 플레이어 스폰 위치 계산을 위한 새로운 로직
            Vector3 spawnPosition = GetNextSpawnPosition();
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
        Debug.Log($"플레이어 참가: {player}");
    }

    private Vector3 GetNextSpawnPosition()
    {
        // 간단한 예: 플레이어 수에 따라 원형으로 배치
        float angle = _spawnedCharacters.Count * (360f / 8); // 최대 8명의 플레이어 가정
        float radius = 5f; // 스폰 반경
        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
        return new Vector3(x, 1, z);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Find and remove the players avatar
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
        Debug.Log($"플레이어 퇴장: {player}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(_inputHandler.GetNetworkInput());
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"네트워크 셧다운: {shutdownReason}");
    }
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("서버에 연결됨");
    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"서버와의 연결이 끊김: {reason}");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log($"연결 실패: {reason}");
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("씬 로드 완료");
    }
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("씬 로드 시작");
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log($"오브젝트가 AOI를 벗어남: {obj.Id}");
    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log($"오브젝트가 AOI에 들어옴: {obj.Id}");
    }
}