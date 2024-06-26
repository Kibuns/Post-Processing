using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class TarotCardScript : MonoBehaviour
{
    private Item attachedItem;
    [SerializeField] private AudioClip flipClip;
    private bool turned;
    public bool pickUpTurned;
    private Vector3 startingOffsetRotation;
    private AudioSource source;
    // Start is called before the first frame update
    void Start()
    {
        source = GetComponent<AudioSource>();
        attachedItem = GetComponent<Item>();
        startingOffsetRotation = attachedItem.offsetRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if(!attachedItem.isBeingHeld)
        {
            attachedItem.offsetRotation = startingOffsetRotation;
            turned = pickUpTurned;
        }
        else if (turned)
        {
            attachedItem.offsetRotation = new Vector3(startingOffsetRotation.x, startingOffsetRotation.y - 180, startingOffsetRotation.z);
        }
        else
        {
            attachedItem.offsetRotation = startingOffsetRotation;
        }

        //if(!attachedItem.isBeingHeld && !turned)
        //{
        //    turned = true;
        //    attachedItem.offsetRotation = new Vector3(attachedItem.offsetRotation.x, 0, attachedItem.offsetRotation.z);
        //}
    }

    private void OnMouseEnter()
    {
        CursorManager.instance.EnablePointCursor();
    }

    private void OnMouseExit()
    {
        CursorManager.instance.EnableDefaultCursor();
    }

    private void OnMouseDown()
    {
        turned = !turned;
        source.PlayOneShot(flipClip);
    }
}
