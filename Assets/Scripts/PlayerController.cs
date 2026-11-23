using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Weapon))]
public class PlayerController : MonoBehaviour
{
    public Transform cam;
    public float moveSpeed = 6f, lookSensitivity = 0.12f, jumpHeight = 1.2f, gravity = -22f;

    CharacterController cc;
    Weapon weapon;
    float pitch, velY;
    int moveId = -1, lookId = -1;
    Vector2 moveOrigin;
    Rect fireRect, jumpRect, reloadRect;
    GUIStyle btnStyle;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        weapon = GetComponent<Weapon>();
    }

    void OnEnable() { moveId = -1; lookId = -1; velY = 0f; }

    void Layout()
    {
        float s = Screen.height * 0.2f, W = Screen.width, H = Screen.height;
        fireRect = new Rect(W - 1.4f * s, H - 1.4f * s, s, s);
        reloadRect = new Rect(W - 1.3f * s, H - 2.5f * s, 0.8f * s, 0.8f * s);
        jumpRect = new Rect(W - 2.6f * s, H - 1.0f * s, 0.8f * s, 0.8f * s);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        Layout();

        Vector2 move = Vector2.zero, look = Vector2.zero;
        bool fire = false, jump = false, reload = false;

        foreach (Touch t in Input.touches)
        {
            Vector2 p = t.position;
            Vector2 g = new Vector2(p.x, Screen.height - p.y);
            bool ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

            if (t.phase == TouchPhase.Began)
            {
                bool onBtn = fireRect.Contains(g) || jumpRect.Contains(g) || reloadRect.Contains(g);
                if (!onBtn)
                {
                    if (p.x < Screen.width * 0.4f && moveId < 0) { moveId = t.fingerId; moveOrigin = p; }
                    else if (p.x >= Screen.width * 0.4f && lookId < 0) lookId = t.fingerId;
                }
            }

            if (t.fingerId == moveId)
            {
                if (ended) moveId = -1;
                else move = Vector2.ClampMagnitude((p - moveOrigin) / (Screen.height * 0.12f), 1f);
            }
            else if (t.fingerId == lookId)
            {
                if (ended) lookId = -1;
                else look += t.deltaPosition;
            }
            else if (!ended)
            {
                if (fireRect.Contains(g)) fire = true;
                if (t.phase == TouchPhase.Began)
                {
                    if (jumpRect.Contains(g)) jump = true;
                    if (reloadRect.Contains(g)) reload = true;
                }
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        move += new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 8f;
        fire |= Input.GetButton("Fire1");
        jump |= Input.GetButtonDown("Jump");
        reload |= Input.GetKeyDown(KeyCode.R);
#endif

        // Look
        transform.Rotate(0f, look.x * lookSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -80f, 80f);
        cam.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // Move
        bool grounded = cc.isGrounded;
        if (grounded && velY < 0f) velY = -2f;
        if (jump && grounded) velY = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velY += gravity * Time.deltaTime;

        Vector3 dir = transform.right * move.x + transform.forward * move.y;
        dir = Vector3.ClampMagnitude(dir, 1f) * moveSpeed;
        cc.Move((dir + Vector3.up * velY) * Time.deltaTime);

        if (fire) weapon.Fire();
        if (reload) weapon.Reload();
    }

    void OnGUI()
    {
        if (Time.timeScale == 0f) return;
        if (btnStyle == null)
        {
            btnStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.035f),
                alignment = TextAnchor.MiddleCenter
            };
        }
        GUI.Box(fireRect, "FIRE", btnStyle);
        GUI.Box(jumpRect, "JUMP", btnStyle);
        GUI.Box(reloadRect, "RELOAD", btnStyle);

        if (moveId >= 0)
        {
            float r = Screen.height * 0.08f;
            GUI.Box(new Rect(moveOrigin.x - r, Screen.height - moveOrigin.y - r, r * 2f, r * 2f), "");
        }
    }
}
