using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class RazorScript : MonoBehaviour
{
    [SerializeField] private GameObject razorBlade;
    [SerializeField] private Vector3 shownBladeEulerRotation;
    [SerializeField] private AudioClip showBladeClip;
    [SerializeField] private Dialogue firstCuttingDialogue;
    [SerializeField] private AudioClip cutBladeClip;
    public float lerpSpeed;
    public float showBladeDelay;

    public bool startedCuttingSequence;
    private bool bladeShowing;
    private PlayerInputActions playerInputActions;
    private bool isCutting;
    private Transform childRazorTransform;
    private Vector3 downPosition;
    private AudioSource source;
    // Start is called before the first frame update
    void Start()
    {
        source = GetComponent<AudioSource>();
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();
        childRazorTransform = transform.GetChild(0);
        Vector3 camPos = FindObjectOfType<LookAroundCamera>().transform.position;
        downPosition = new Vector3(0, -25, 3);
    }

    // Update is called once per frame
    void Update()
    {
        LerpBladeRotation(bladeShowing);
        if (isCutting)
        {
            LerpDown();
        }
    }

    private void LerpDown()
    {
        childRazorTransform.position = Vector3.Lerp(childRazorTransform.position, downPosition, (lerpSpeed / 10) * Time.deltaTime);
    }

    public void ShowBlade()
    {
        bladeShowing = true;
    }

    public void HideBlade()
    {
        bladeShowing = false;
    }

    private void LerpBladeRotation(bool bladeShowing)
    {
        if (bladeShowing)
        {
            startedCuttingSequence = true;
            if (Quaternion.Angle(razorBlade.transform.localRotation, Quaternion.Euler(shownBladeEulerRotation)) < 3) return;
            razorBlade.transform.localRotation = Quaternion.Slerp(razorBlade.transform.localRotation, Quaternion.Euler(shownBladeEulerRotation), (lerpSpeed * Time.deltaTime));
        }
        else
        {
            razorBlade.transform.localRotation = Quaternion.Lerp(razorBlade.transform.localRotation, Quaternion.identity, lerpSpeed * Time.deltaTime);
        }
        
    }

    public void UseBladeToCut()
    {
        StartCoroutine(ShowBladeAnimation(showBladeDelay));
    }

    private IEnumerator ShowBladeAnimation(float delay)
    {
        ShowBlade();
        source.PlayOneShot(showBladeClip);
        yield return new WaitForSeconds(delay);
        DialogueManager.instance.StartDialogue(firstCuttingDialogue);
        yield return new WaitForSeconds(3f);
        isCutting = true;
        yield return new WaitForSeconds(delay);
        source.PlayOneShot(cutBladeClip);
        GameManager.Instance.isBleeding = true;
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    //Replace Vector3.Lerp because that one cant cross an angle of 0, so for instance lerp from 10 to -10 is only possible with this method without weird behaviour
    private Vector3 AngleLerp(Vector3 StartAngle, Vector3 FinishAngle, float t)
    {
        float xLerp = Mathf.LerpAngle(StartAngle.x, FinishAngle.x, t);
        float yLerp = Mathf.LerpAngle(StartAngle.y, FinishAngle.y, t);
        float zLerp = Mathf.LerpAngle(StartAngle.z, FinishAngle.z, t);
        Vector3 Lerped = new Vector3(xLerp, yLerp, zLerp);
        return Lerped;
    }
}
