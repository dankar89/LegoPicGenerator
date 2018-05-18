using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class OverlayRectangleController : MonoBehaviour, IPointerClickHandler {

    public delegate void OnClick(Brick brick);
    public OnClick onClick;

    Brick brick;

    // Use this for initialization
    void Start() {
    }

    // Update is called once per frame
    void Update() {

    }

    public void Init(Brick brick, Vector2 position, Vector2 brickUnitSize, Vector2 offset) {
        this.brick = brick;
        GetComponentInChildren<TextMeshProUGUI>().text = brick.index.ToString();

        ((RectTransform)transform).sizeDelta = new Vector2(
            brickUnitSize.x * brick.width,
            brickUnitSize.y * brick.height);

        transform.localPosition = (brickUnitSize * position) - offset;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (onClick != null) {
            onClick(brick);
        }
    }

    public void OnDestroy() {
        onClick = null;
    }
}
