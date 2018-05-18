using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BrickItemController : Button {

    public GameObject brickImagePrefab;

    GridLayoutGroup imageGridLayout;
    Text quantityText, nameText, colorText, idText;

    Brick brick;

    // Use this for initialization
    protected override void Awake() {
        base.Awake();

        imageGridLayout = transform.Find("ImageContainer").GetComponent<GridLayoutGroup>();
        idText = transform.Find("IdText").GetComponent<Text>();
        nameText = transform.Find("NameText").GetComponent<Text>();
        colorText = transform.Find("ColorText").GetComponent<Text>();
        quantityText = transform.Find("QuantityText").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update() {

    }

    public void Init(Brick brick, int quantity) {
        this.brick = brick;

        imageGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        imageGridLayout.constraintCount = Mathf.Min(brick.width, brick.height);

        float maxSize = Mathf.Max(brick.width, brick.height);
        if (maxSize > 5) {
            imageGridLayout.cellSize = new Vector2(((RectTransform)imageGridLayout.transform).sizeDelta.x / maxSize, ((RectTransform)imageGridLayout.transform).sizeDelta.y / maxSize);
        }

        name = "BrickItem_" + brick.index.ToString();

        idText.text = "ID: " + brick.index.ToString();
        nameText.text = "Name: " + brick.name;
        quantityText.text = "Quantity: x" + quantity.ToString();
        colorText.text = "Color: " + (brick.colorName != null && brick.colorName.Length > 0 ? brick.colorName : brick.color.ToString());

        for (int i = 0; i < brick.area; i++) {
            Instantiate(brickImagePrefab, imageGridLayout.transform);
        }

        foreach (var image in imageGridLayout.GetComponentsInChildren<RawImage>()) {
            if (brick.color.r == 0 && brick.color.g == 0 && brick.color.b == 0) {
                image.color = new Color32(30, 30, 30, 255);
            } else {
                image.color = brick.color;
            }
        }

        onClick.AddListener(() => { Application.OpenURL(brick.GetUrl()); });
    }
}
