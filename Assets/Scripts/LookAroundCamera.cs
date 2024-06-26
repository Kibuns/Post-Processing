using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAroundCamera : MonoBehaviour
{
    [SerializeField] private Transform[] cameraPositions;

    [Header("Camera Positions")]
    [SerializeField] private Transform overTablePosition;
    [SerializeField] private Transform defaultPosition;
    [SerializeField] private Transform ritualPosition;

    public Vector3 mousInput;
    public Vector2 mousePercentagePosition;
    public float LookIntensityX;
    public float LookIntensityY;

    [SerializeField] private float bobbingSpeed = 1f;
    [SerializeField] private float bobbingAmount = 0.1f;
    [SerializeField] private float cooldown;

    private float bobbingOffset = 0f;

    public bool ignoreInput;
    public bool bentOverTable;
    public bool turnedAround;

    public float rotationLerpSpeed;
    public Vector3 backRotationOffset;
    private PlayerInputActions playerInputActions;
    private Transform currentTargetPosition;
    private int currentTargetIndex;

    private float triggeredSecondsAgo = 0f;
    private float cooldownTimer;
    // Start is called before the first frame update

    private void OnEnable()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }
    void Start()
    {
        cooldownTimer = 1f;
        mousePercentagePosition = Vector2.zero;
        currentTargetPosition = defaultPosition;
        GameManager.Instance.isTurnedAround = turnedAround;
    }


    // Update is called once per frame
    void Update()
    {
        cooldownTimer += Time.deltaTime;
        triggeredSecondsAgo += Time.deltaTime;
        mousInput = Input.mousePosition;
        mousePercentagePosition = new Vector2(Mathf.Clamp((mousInput.x / Screen.width) - 0.5f, -0.5f, 0.5f), Mathf.Clamp((mousInput.y / Screen.height) - 0.5f, -0.5f, 0.5f));


        Quaternion composedTargetRotation = currentTargetPosition.localRotation * Quaternion.Euler(MouseRotationOffset);
        bobbingOffset += Time.deltaTime * bobbingSpeed;
        float yOffset = Mathf.Sin(bobbingOffset) * bobbingAmount;
        float xOffset = Mathf.Cos(bobbingOffset + 3.1415f) * bobbingAmount * 1.3f;
        Vector3 bobbingOffsetVector = new Vector3(xOffset, yOffset, 0f);
        composedTargetRotation *= Quaternion.Euler(bobbingOffsetVector);

        //!!!!!!!!!JANK: to get the camera to rotate the nicest way around, without this it prettymuch always chooses the same side to turn.
        if(triggeredSecondsAgo < 0.2f)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, currentTargetPosition.localRotation, rotationLerpSpeed * Time.deltaTime);
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, composedTargetRotation, rotationLerpSpeed * Time.deltaTime);
        }
        ////Jank^^ (it does work rly well tho so this may as well just stay like this)
        
        transform.position = Vector3.Lerp(transform.position, currentTargetPosition.position, rotationLerpSpeed * Time.deltaTime);
        if (ignoreInput) return;

        if (playerInputActions.Player.Jump.triggered)
        {
            TurnAround();
        }

        if (turnedAround) { return; }

        if (playerInputActions.Player.MoveForward.triggered && !GameManager.Instance.isInGunSequence)
        {
            currentTargetPosition = overTablePosition;
        }

        if (playerInputActions.Player.MoveBack.triggered && !GameManager.Instance.isInGunSequence)
        {
            currentTargetPosition = defaultPosition;
        }
    }

    public void TurnAround()
    {
        if(cooldownTimer < cooldown) { return; }
        cooldownTimer = 0f;
        triggeredSecondsAgo = 0f;
        if (GameManager.Instance.isInGunSequence || FindObjectOfType<DrawWithMouse>().startedWriting)
        {
            GameManager.Instance.PlayErrorSound();
            return;
        }
        turnedAround = !turnedAround;
        if (ItemManager.Instance.currentlyHeldItem != null)
        {
            Debug.Log("has held item");
            ItemManager.Instance.currentlyHeldItem.GetComponentInParent<HoveringObject>().TurnRestAndHoverRotation();
        } //turn the front facing rotation with the player if they pick up item and turn around
        GameManager.Instance.isTurnedAround = turnedAround;
        if (turnedAround) { currentTargetPosition = ritualPosition; }
        else { currentTargetPosition = defaultPosition; }
    }

    public void MoveToDefaultPosition()
    {
        currentTargetPosition = defaultPosition;
        GameManager.Instance.isTurnedAround = false;
        turnedAround = false;
    }

    private Vector3 MouseRotationOffset => new Vector3(-mousePercentagePosition.y * LookIntensityY, mousePercentagePosition.x * LookIntensityX, 0f);

    private void NextCameraPosition()
    {
        currentTargetIndex++;
        if(currentTargetIndex > cameraPositions.Length - 1)
        {
            currentTargetIndex = 0;
        }
        currentTargetPosition = cameraPositions[currentTargetIndex];
    }

}
