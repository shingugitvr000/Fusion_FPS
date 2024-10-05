using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target; // ī�޶� ���� ���
    public float distance = 5.0f; // Ÿ�����κ����� �Ÿ�
    public float sensitivity = 3.0f; // ���콺 ȸ�� ����
    public Vector2 rotationLimits = new Vector2(-40, 80); // ī�޶� ���� ȸ�� ���� (�ּҰ�, �ִ밪)

    private float _currentX = 0.0f;
    private float _currentY = 0.0f;

    void Update()
    {
        if (target == null) return;

        // ���콺 �̵��� ���� ȸ�� ó��
        _currentX += Input.GetAxis("Mouse X") * sensitivity;
        _currentY -= Input.GetAxis("Mouse Y") * sensitivity;

        // ���� ȸ�� ������ �����Ͽ� ī�޶� �� ������ �������� ���� ����
        _currentY = Mathf.Clamp(_currentY, rotationLimits.x, rotationLimits.y);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ī�޶��� ��ġ�� ȸ�� ���
        Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
        Vector3 direction = new Vector3(0, 0, -distance);
        Vector3 position = target.position + rotation * direction;

        // ī�޶� �� ������ �������� ���� �����ϱ� ���� Ÿ���� ���̺��� ī�޶� �Ʒ��� �������� �ʰ� ����
        position.y = Mathf.Max(position.y, target.position.y + 1.0f); // ī�޶� ���̸� Ÿ���� y�ຸ�� �׻� ���� ����

        // ī�޶��� ��ġ�� Ÿ���� �ٶ󺸴� ������ ����
        transform.position = position;
        transform.LookAt(target.position);
    }
}
