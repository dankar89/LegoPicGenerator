using SFB;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System;
using TMPro;


public class ColorHSV {
    public Color32 color;
    public float h, s, v, r, g, b;

    public ColorHSV(Color32 c) {
        color = c;
        r = c.r;
        g = c.g;
        b = c.b;
        Color.RGBToHSV(c, out h, out s, out v);
    }

    public ColorHSV(float h, float s, float v) {
        this.h = h;
        this.s = s;
        this.v = v;
    }
}

public class Brick {
    public string name, type;
    public int width, height, area, id;
    public bool isSquare;
    public int quantity, index;
    public Color32 color;
    public string colorName;

    private string url = "https://www.bricklink.com/v2/catalog/catalogitem.page?P={0}&idColor={1}#T=C&C=1";

    private static Dictionary<int, Vector2> idMap = new Dictionary<int, Vector2>() {
        { 3024, new Vector2(1, 1) },
        { 3023, new Vector2(1, 2) },
        { 3623, new Vector2(1, 3) },
        { 3710, new Vector2(1, 4) },
        { 3666, new Vector2(1, 6) },
        { 3460, new Vector2(1, 8) },
        { 4477, new Vector2(1, 10) },

        { 3022, new Vector2(2, 2) },
        { 3021, new Vector2(2, 3) },
        { 3020, new Vector2(2, 4) },
        { 3795, new Vector2(2, 6) },
        { 3034, new Vector2(2, 8) },
        { 3832, new Vector2(2, 10) },

        //{ 11212, new Vector2(3, 3) },

        { 3031, new Vector2(4, 4) },
        { 3032, new Vector2(4, 6) },
        { 3035, new Vector2(4, 8) },

        { 3030, new Vector2(6, 8) },
    };

    public static int GetArticleId(int w, int h) {
        KeyValuePair<int, Vector2> res = idMap.FirstOrDefault(kvp => ((int)kvp.Value.x == w && (int)kvp.Value.y == h) || ((int)kvp.Value.x == h && (int)kvp.Value.y == w));
        return res.Key;
    }

    public string GetUrl(int colorId = 0) {
        if (id == 0) {
            id = GetArticleId(width, height);
        }

        return string.Format(url, id.ToString(), colorId.ToString());
    }

    public Brick(string type, int width, int height, int quantity = 0) {
        this.type = type;
        this.name = type + " " + width.ToString() + " x " + height.ToString();
        this.width = width;
        this.height = height;
        this.quantity = quantity;
        area = width * height;
        isSquare = width == height;

        id = GetArticleId(width, height);
    }

    public Brick(string type, int width, int height, Color32 color, int quantity = 0) {
        this.type = type;
        this.name = type + " " + width.ToString() + " x " + height.ToString();
        this.width = width;
        this.height = height;
        this.quantity = quantity;
        this.color = color;
        area = width * height;
        isSquare = width == height;

        id = GetArticleId(width, height);
    }
}

public class LegoPicCalculator : MonoBehaviour {

    private enum ColorAlgorithms {
        RGB, HUE, HSV
    }
    private ColorAlgorithms currentColorAlgo;

    private enum SortType {
        SIZE,
        COLOR,
        QUANTITY
    }
    private SortType currentSorting;

    public GameObject brickItemPrefab, overlayRectPrefab;

    Text titleText, pixelSizeText, worldSizeText, numberOfBricksText, numberOfColorsText;
    RawImage image;
    GameObject blocker, saveImageButton, brickGridOverlay;
    Toggle optimizedBlocksToggle, legoColorsToggle;
    ScrollRect scrollRect;


    Texture2D texture;
    Vector2 maxImageSize = new Vector2(500, 500);

    Color32[] originalPixels;
    Vector2 originalSize;

    List<Vector2> processedPixels = new List<Vector2>();
    Dictionary<Color32, List<int>> colorGroups = new Dictionary<Color32, List<int>>();

    public int maxPixels = 48;

    private bool processing = false;
    private bool optimizedBlocks = true;
    private bool useLegoColors = true;
    private bool imageLoaded = false;
    private bool showGrid = false;
    private bool isAnimatingScrollView = false;

    const float WORLD_UNIT_SIZE = .8f; //centimeters


    /* BRICKS */
    int brickCount = 0;
    List<Brick> bricks;

    /**********/

    /* COLORS (http://lego.wikia.com/wiki/Colour_Palette, http://www.peeron.com/cgi-bin/invcgis/colorguide.cgi/) */
    public Dictionary<string, Color32> colorPalette1, colorPalette2, colorPalette3, currentPalette;
    /**********/


    // Use this for initialization
    void Start() {
        texture = new Texture2D(maxPixels, maxPixels, TextureFormat.RGBA32, true);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        image = GameObject.Find("RawImage").GetComponent<RawImage>();
        maxImageSize = image.rectTransform.sizeDelta;

        brickGridOverlay = image.transform.Find("BrickGridOverlay").gameObject;
        brickGridOverlay.SetActive(false);

        titleText = GameObject.Find("TitleText").GetComponent<Text>();
        pixelSizeText = GameObject.Find("PixelSizeText").GetComponent<Text>();
        worldSizeText = GameObject.Find("WorldSizeText").GetComponent<Text>();
        numberOfBricksText = GameObject.Find("NumberOfBricksText").GetComponent<Text>();
        numberOfColorsText = GameObject.Find("NumberOfColorsText").GetComponent<Text>();

        optimizedBlocksToggle = GameObject.Find("OptimizedBlocksToggle").GetComponent<Toggle>();
        optimizedBlocksToggle.isOn = optimizedBlocks;
        legoColorsToggle = GameObject.Find("LegoColorsToggle").GetComponent<Toggle>();
        legoColorsToggle.isOn = useLegoColors;

        saveImageButton = GameObject.Find("SaveImageButton");
        saveImageButton.SetActive(false);

        scrollRect = GameObject.Find("BrickScrollView").GetComponentInChildren<ScrollRect>();

        blocker = GameObject.Find("Blocker");
        blocker.SetActive(false);

        //LEGO Shop colors
        colorPalette1 = new Dictionary<string, Color32>() {
            { "White", Color.white },

            { "Black", Color.black },

            { "Brigt Red", Color.red },
            { "Dark Red", new Color32(128, 8, 27, 255) },
            { "Reddish Brown", new Color32(91, 28, 12, 255) },

            { "Bright Blue", Color.blue },
            { "Medium Blue", new Color32(71, 140, 198, 255) },
            { "Earth Blue", new Color32(0, 37, 65, 255) },

            { "Bright Yellow", new Color32(255, 255, 0, 255) },
            { "Brick Yellow", new Color32(217, 187, 123, 255) },

            { "Bright Orange", new Color32(231, 99, 24, 255) },
            { "Flame Yel. Orange", new Color32(244, 155, 0, 255) },

            { "Bright Yel. Green", new Color32(149, 185, 11, 255) },
            { "Dark Green", new Color32(0, 153, 0, 255) },
            { "Medium Yel. Green", new Color32(150, 185, 59, 255) },
            { "Earth Green", new Color32(0, 51, 0, 255) },

            { "Medium Stone Grey", new Color32(156, 146, 145, 255) },
            { "Dark Stone Grey", new Color32(76, 81, 86, 255) },

            { "Dark Azur", new Color32(70, 155, 195, 255) },
            { "Medium Azur", new Color32(104, 195, 226, 255) },

            { "Medium Lavender", new Color32(160, 110, 185, 255) },
        };

        colorPalette2 = new Dictionary<string, Color32>() {
            { "white", Color.white },
            { "black", Color.black },
            { "Red", Color.red },
            { "Blue", Color.blue },
            { "Yellow", new Color32(255, 255, 0, 255) },
            { "Brick Yellow", new Color32(214, 114, 64, 255) },
            { "Nougat", new Color32(0, 0, 255, 255) },
            { "Dark Green", new Color32(0, 153, 0, 255) },
            { "Bright Green", new Color32(0, 204, 0, 255) },
            { "Dark Orange", new Color32(168, 61, 21, 255) },
            { "Medium Blue", new Color32(71, 140, 198, 255) },
            { "Bright Orange", new Color32(231, 99, 24, 255) },
            { "Bright Blu. Green", new Color32(5, 157, 158, 255) },
            { "Bright Yel. Green", new Color32(149, 185, 11, 255) },
            { "Bright Red. Violet", new Color32(153, 0, 102, 255) },
            { "Sand Blue", new Color32(94, 116, 140, 255) },
            { "Sand Yellow", new Color32(141, 116, 82, 255) },
            { "Earth Blue", new Color32(0, 37, 65, 255) },
            { "Earth Green", new Color32(0, 51, 0, 255) },
            { "Sand Green", new Color32(95, 130, 101, 255) },
            { "Dark Red", new Color32(128, 8, 27, 255) },
            { "Flame Yel. Orange", new Color32(244, 155, 0, 255) },
            { "Reddish Brown", new Color32(91, 28, 12, 255) },
            { "Medium Stone Grey", new Color32(156, 146, 145, 255) },
            { "Dark Stone Grey", new Color32(76, 81, 86, 255) },
            { "Light Royal Blue", new Color32(135, 192, 234, 255) },
            { "Bright Purple", new Color32(222, 55, 139, 255) },
            { "Light Purple", new Color32(238, 157, 195, 255) },
            { "Cool Yellow", new Color32(255, 255, 153, 255) },
            { "Medium Lilac", new Color32(44, 21, 119, 255) },
            { "Light Nougat", new Color32(245, 193, 137, 255) },
            { "Dark Brown", new Color32(48, 15, 6, 255) },
            { "Medium Nougat", new Color32(170, 125, 85, 255) },
            { "Dark Azur", new Color32(70, 155, 195, 255) },
            { "Medium Azur", new Color32(104, 195, 226, 255) },
            { "Aqua", new Color32(211, 242, 234, 255) },
            { "Medium Lavender", new Color32(160, 110, 185, 255) },
            { "Lavender", new Color32(205, 164, 222, 255) },
            { "White Glow", new Color32(245, 243, 215, 255) },
            { "Spring Yel. Green", new Color32(226, 249, 154, 255) },
            { "Olive Green", new Color32(119, 119, 78, 255) },
            { "Medium Yel. Green", new Color32(150, 185, 59, 255) },
        };

        colorPalette3 = new Dictionary<string, Color32>() {
            { "White", Color.white },

            { "Black", Color.black },

            { "Brigt Red", Color.red },
            { "Dark Red", new Color32(128, 8, 27, 255) },
            { "Reddish Brown", new Color32(91, 28, 12, 255) },

            { "Bright Blue", Color.blue },

            { "Bright Yellow", new Color32(255, 255, 0, 255) },

            { "Bright Orange", new Color32(231, 99, 24, 255) },
            { "Flame Yel. Orange", new Color32(244, 155, 0, 255) },


            { "Dark Green", new Color32(0, 153, 0, 255) },
            { "Earth Green", new Color32(0, 51, 0, 255) },
            { "Bright Green", new Color32(0, 204, 0, 255) },

            { "Medium Stone Grey", new Color32(156, 146, 145, 255) },
            { "Dark Stone Grey", new Color32(76, 81, 86, 255) },

            { "Dark Azur", new Color32(70, 155, 195, 255) },
            { "Medium Azur", new Color32(104, 195, 226, 255) },

            { "Medium Lavender", new Color32(160, 110, 185, 255) },
        };

        currentPalette = colorPalette1;



        bricks = new List<Brick>() {
            new Brick("Plate", 1, 1),
            new Brick("Plate", 1, 2),
            new Brick("Plate", 1, 3),
            new Brick("Plate", 1, 4),
            new Brick("Plate", 1, 6),
            new Brick("Plate", 1, 8),
            new Brick("Plate", 1, 10),

            new Brick("Plate", 2, 2),
            new Brick("Plate", 2, 3),
            new Brick("Plate", 2, 4),
            new Brick("Plate", 2, 6),
            new Brick("Plate", 2, 8),
            new Brick("Plate", 2, 10),

            //new Brick("Plate", 3, 3),

            new Brick("Plate", 4, 4),
            new Brick("Plate", 4, 6),
            new Brick("Plate", 4, 8),

            new Brick("Plate", 6, 8),
        };

        //Sort bricks by area, with the largest area first
        bricks = bricks.OrderByDescending(b => b.area).ToList();
    }

    // Update is called once per frame
    void Update() {

    }

    public void ToggleColorPalette(bool b) {
        useLegoColors = b;
        StartCoroutine(ProcessTexture(texture));
    }

    public void ToggleOptimizedBlocks(bool b) {
        optimizedBlocks = b;
        StartCoroutine(ProcessTexture(texture));
    }

    public void ToggleShowGrid(bool b) {
        showGrid = b;
        brickGridOverlay.SetActive(b);
    }

    public void OpenFile() {
        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg")
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Browse Image", "", extensions, false);
        if (paths != null && paths.Length > 0) {
            titleText.text = Path.GetFileName(paths[0]);
        } else {
            return;
        }

        if (File.Exists(paths[0])) {
            byte[] fileData;
            fileData = File.ReadAllBytes(paths[0]);
            if (ImageConversion.LoadImage(texture, fileData)) {
                if (texture.width > maxPixels || texture.height > maxPixels) {
                    Debug.LogError("Image is too large!");
                    titleText.text = "Image is too large!";
                    return;
                }

                Debug.Log("Successfully loaded image!");
                saveImageButton.SetActive(true);
                imageLoaded = true;

                originalPixels = null;
                originalSize = new Vector2(texture.width, texture.height);

                SetTexture(texture);
                StartCoroutine(ProcessTexture(texture));
            } else {
                Debug.LogError("Failed to load image!");
                return;
            }
        }
    }

    public void SaveFile() {
        if (imageLoaded) {
            var extensions = new[] {
                new ExtensionFilter("Image Files", "png")
            };

            string path = StandaloneFileBrowser.SaveFilePanel("Save Image", "", "lego_image.png", extensions);
            if (path != null && path.Length > 0) {
                Debug.Log(path);
                // Encode texture into PNG
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
            }
        }
    }

    public void OnAlgoDrowdownChanged(int index) {
        switch (index) {
            case 0:
                currentColorAlgo = ColorAlgorithms.RGB;
                break;
            case 1:
                currentColorAlgo = ColorAlgorithms.HUE;
                break;
            case 2:
                currentColorAlgo = ColorAlgorithms.HSV;
                break;
            default:
                break;
        }

        if (useLegoColors) {
            StartCoroutine(ProcessTexture(texture));
        }
    }

    public void OnPaletteDrowdownChanged(int index) {
        switch (index) {
            case 0:
                currentPalette = colorPalette1;
                break;
            case 1:
                currentPalette = colorPalette2;
                break;
            case 2:
                currentPalette = colorPalette3;
                break;
            default:
                break;
        }

        if (useLegoColors) {
            StartCoroutine(ProcessTexture(texture));
        }
    }

    public void OnSortDrowdownChanged(int index) {
        switch (index) {
            case 0:
                currentSorting = SortType.SIZE;
                break;
            case 1:
                currentSorting = SortType.COLOR;
                break;
            case 2:
                currentSorting = SortType.QUANTITY;
                break;
            default:
                break;
        }

        if (scrollRect.content.childCount > 0) {
            StartCoroutine(ProcessTexture(texture));
        }
    }

    private void OnGridClicked(Brick brick) {
        Debug.Log(brick.index.ToString() + ": " + brick.name);

        Transform child = scrollRect.content.Find("BrickItem_" + brick.index.ToString());
        if (child != null) {
            float childY = -child.localPosition.y;
            Debug.Log(childY);
            Canvas.ForceUpdateCanvases();
            scrollRect.content.localPosition = new Vector2(scrollRect.content.localPosition.x, childY);
            //iTween.Stop(scrollRect.content.gameObject);
            //iTween.MoveTo(scrollRect.content.gameObject, iTween.Hash(
            //    "y", childY,
            //    "time", 0.2f
            //));
            //StartCoroutine(ScrollList((viewportLocalPosition.y + childLocalPosition.y), 0.3f));
        }
    }

    //private IEnumerator ScrollList(float y, float time) {
    //    while (scrollRect.content.localPosition.y != y) {
    //        scrollRect.content.localPosition = Vector2.Lerp(scrollRect.content.localPosition, new Vector2(scrollRect.content.localPosition.x, y), Time.deltaTime);
    //        yield return null;
    //    }
    //}

    private void SetTexture(Texture2D tex, float scale = 1f) {
        image.texture = tex;
        Vector2 imageSize = maxImageSize;
        if (tex.width > tex.height) {
            imageSize.y = maxImageSize.y * (tex.height / (float)tex.width);
        } else if (tex.height > tex.width) {
            imageSize.x = maxImageSize.x * (tex.width / (float)tex.height);
        }
        image.rectTransform.sizeDelta = imageSize;
    }

    private void UpdateUI(Texture2D tex, float scale = 1f) {
        pixelSizeText.text = "Size in pixels: " + tex.width.ToString() + " X " + tex.height.ToString();
        worldSizeText.text = "Size in cm: " + (tex.width * WORLD_UNIT_SIZE).ToString() + " X " + (tex.height * WORLD_UNIT_SIZE).ToString();

        numberOfBricksText.text = "Number of bricks: " + brickCount.ToString();
    }

    private IEnumerator ProcessTexture(Texture2D tex) {
        if (!imageLoaded) {
            yield break;
        }

        GameObject clone;

        Color32[] pixels;
        Color32 currentColor;
        List<int> colorIndices = new List<int>();
        Dictionary<Vector2, Brick> overlayBricks = new Dictionary<Vector2, Brick>();
        List<Brick> allBricks = new List<Brick>();

        blocker.SetActive(true);

        colorGroups.Clear();
        processedPixels.Clear();
        brickCount = 0;

        foreach (Transform child in scrollRect.content) {
            Destroy(child.gameObject);
        }

        if (originalPixels == null) {
            pixels = texture.GetPixels32();
            originalPixels = pixels;
        } else {
            pixels = originalPixels;
        }

        if (useLegoColors) {
            ColorHSV[] colorPaletteHSV = currentPalette.Select(c => new ColorHSV(c.Value)).ToArray();
            ColorHSV[] pixelsHSV = pixels.Select(c => new ColorHSV(c)).ToArray();
            List<Color32> newPixels = new List<Color32>();

            for (int i = 0; i < pixelsHSV.Length; i++) {
                currentColor = ClosestColor(colorPaletteHSV, pixelsHSV[i], currentColorAlgo);
                colorGroups.TryGetValue(currentColor, out colorIndices);
                if (colorIndices != null && colorIndices.Count > 0) {
                    colorGroups[currentColor].Add(i);
                } else {
                    colorGroups.Add(currentColor, new List<int>() { i });
                }

                newPixels.Add(currentColor);
            }

            if (newPixels.Count > 0 && newPixels.Count == pixels.Length) {
                Debug.Log("Setting colors");
                tex.SetPixels32(newPixels.ToArray());
                tex.Apply();
            } else {
                Debug.LogError("Failed set colors!");
            }
        } else {
            //Count colors
            for (int i = 0; i < originalPixels.Length; i++) {
                currentColor = originalPixels[i];
                colorGroups.TryGetValue(currentColor, out colorIndices);
                if (colorIndices != null && colorIndices.Count > 0) {
                    colorGroups[currentColor].Add(i);
                } else {
                    colorGroups.Add(currentColor, new List<int>() { i });
                }
            }

            tex.SetPixels32(originalPixels);
            tex.Apply();
        }

        numberOfColorsText.text = "Number of colors: " + colorGroups.Count.ToString();

        if (optimizedBlocks) {
            Brick currentBrick;

            KeyValuePair<Brick, List<Vector2>> brickPixelList = new KeyValuePair<Brick, List<Vector2>>();
            Dictionary<Color32, Brick> brickDict = new Dictionary<Color32, Brick>();

            Vector2 step = Vector2.one;

            for (int i = 0; i < bricks.Count; i++) {
                brickDict.Clear();
                currentBrick = bricks[i];
                currentBrick.quantity = 0;

                step.x = currentBrick.width;
                step.y = currentBrick.height;

                for (int y = 0; y < tex.height;) {
                    if (y + Mathf.Min(currentBrick.width, currentBrick.height) > tex.height) {
                        break;
                    }

                    for (int x = 0; x < tex.width;) {
                        if (x + Mathf.Min(currentBrick.width, currentBrick.height) > tex.width) {
                            break;
                        }

                        brickPixelList = FindBrickInTexture(x, y, currentBrick, tex);

                        if (brickPixelList.Value.Count == currentBrick.area) {
                            overlayBricks.Add(brickPixelList.Value[0], brickPixelList.Key);

                            processedPixels.AddRange(brickPixelList.Value);

                            if (!brickDict.ContainsKey(brickPixelList.Key.color)) {
                                brickDict.Add(brickPixelList.Key.color, brickPixelList.Key);
                            }
                            brickDict[brickPixelList.Key.color].quantity++;
                        } else if (!currentBrick.isSquare) {
                            brickPixelList = FindBrickInTexture(x, y, currentBrick, tex, true);
                            if (brickPixelList.Value.Count == currentBrick.area) {
                                overlayBricks.Add(brickPixelList.Value[0], brickPixelList.Key);

                                processedPixels.AddRange(brickPixelList.Value);

                                if (!brickDict.ContainsKey(brickPixelList.Key.color)) {
                                    brickDict.Add(brickPixelList.Key.color, brickPixelList.Key);
                                }
                                brickDict[brickPixelList.Key.color].quantity++;
                            }
                        }

                        x += brickPixelList.Value.Count == currentBrick.area ? (int)step.x : 1;
                    }

                    y += brickPixelList.Value.Count == currentBrick.area ? (int)step.y : 1;
                }

                foreach (Brick brick in brickDict.Values) {
                    brickCount += brick.quantity;
                    brick.colorName = currentPalette.FirstOrDefault(kvp => kvp.Value.Equals(brick.color)).Key;
                    allBricks.Add(brick);
                }
                brickDict.Clear();


                //Add overlay
                ClearOverlay();
                foreach (Vector2 key in overlayBricks.Keys) {
                    if (allBricks.Count == 0) {
                        break;
                    }

                    int index = allBricks.FindIndex(b => {
                        return b.id == overlayBricks[key].id && b.color.Equals(overlayBricks[key].color);
                    });
                    allBricks[index].index = index + 1;
                    overlayBricks[key].index = index + 1;
                    AddBrickOverlay(overlayBricks[key], key);
                }
            }
        } else {
            Brick brick;
            int index = 1;
            foreach (Color32 key in colorGroups.Keys) {
                brickCount += colorGroups[key].Count;
                brick = new Brick("Plate", 1, 1, key, colorGroups[key].Count);
                brick.index = index++;
                brick.colorName = currentPalette.FirstOrDefault(kvp => kvp.Value.Equals(key)).Key;
                allBricks.Add(brick);
            }
        }

        switch (currentSorting) {
            case SortType.SIZE:
                if (optimizedBlocks) {
                    allBricks = allBricks.OrderByDescending(b => b.area).ToList();
                }
                break;
            case SortType.COLOR:
                if (optimizedBlocks) {
                    allBricks = allBricks.OrderBy(b => b.colorName).ToList();
                }
                break;
            case SortType.QUANTITY:
                allBricks = allBricks.OrderByDescending(b => b.quantity).ToList();
                break;
            default:
                break;
        }

        foreach (Brick b in allBricks) {
            clone = Instantiate(brickItemPrefab, scrollRect.content);
            clone.GetComponent<BrickItemController>().Init(b, b.quantity);
        }

        UpdateUI(texture);

        blocker.SetActive(false);
        yield return null;
    }

    private KeyValuePair<Brick, List<Vector2>> tmpBrickPixelList;
    private KeyValuePair<Brick, List<Vector2>> FindBrickInTexture(int tx, int ty, Brick brick, Texture2D tex, bool rotated = false) {
        Color32 color = tex.GetPixel(tx, ty);
        Color32 tmpColor = Color.clear;

        tmpBrickPixelList = new KeyValuePair<Brick, List<Vector2>>(new Brick(brick.type, (rotated ? brick.height : brick.width), (rotated ? brick.width : brick.height), color), new List<Vector2>() { new Vector2(tx, ty) });

        float endX = tx + (rotated ? brick.height : brick.width);
        float endY = ty + (rotated ? brick.width : brick.height);

        for (int x = tx; x < endX; x++) {
            for (int y = ty; y < endY; y++) {
                //Outside of texture! No room for block
                if (endX > tex.width || endY > tex.height) {
                    tmpBrickPixelList.Value.Clear();
                    return tmpBrickPixelList;
                }

                if (processedPixels.Exists(v => (int)v.x == x && (int)v.y == y)) {
                    tmpBrickPixelList.Value.Clear();
                    return tmpBrickPixelList;
                } else if (brick.area == 1) {
                    return tmpBrickPixelList;
                }

                if (x != tx || y != ty) {
                    tmpColor = tex.GetPixel(x, y);
                    if (tmpColor.Equals(color)) {
                        tmpBrickPixelList.Value.Add(new Vector2(x, y));
                    } else {
                        //No room for block
                        tmpBrickPixelList.Value.Clear();
                        return tmpBrickPixelList;
                    }
                }
            }
        }

        return tmpBrickPixelList;
    }

    private void ClearOverlay() {
        foreach (Transform child in brickGridOverlay.transform) {
            if (!child.name.Equals("OverlayBorder")) {
                Destroy(child.gameObject);
            }
        }
    }

    private void AddBrickOverlay(Brick brick, Vector2 position) {
        GameObject clone = Instantiate(overlayRectPrefab, brickGridOverlay.transform);
        Vector2 brickUnitSize = new Vector2(image.rectTransform.sizeDelta.x / texture.width, image.rectTransform.sizeDelta.y / texture.height);
        OverlayRectangleController overlay = clone.GetComponent<OverlayRectangleController>();
        overlay.Init(brick, position, brickUnitSize, image.rectTransform.sizeDelta / 2);
        overlay.onClick += OnGridClicked;
    }

    /*
     COLOR STUFF
    */

    Color32 ClosestColor(ColorHSV[] colors, ColorHSV target, ColorAlgorithms colorAlgo) {
        switch (colorAlgo) {
            case ColorAlgorithms.HUE:
                return ClosestColorHue(colors, target);
            case ColorAlgorithms.RGB:
                return ClosestColorRGB(colors, target);
            case ColorAlgorithms.HSV:
                return ClosestColorHSV(colors, target);
            default:
                return ClosestColorHSV(colors, target);
        }
    }

    // closed match for hues only:
    Color32 ClosestColorHue(ColorHSV[] colors, ColorHSV target) {
        var hue1 = target.h;
        var diffs = colors.Select(n => getHueDistance(n.h, hue1));
        var diffMin = diffs.Min(n => n);
        return colors[diffs.ToList().FindIndex(n => n == diffMin)].color;
    }

    // closed match in RGB space
    Color32 ClosestColorRGB(ColorHSV[] colors, ColorHSV target) {
        var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
        return colors.ToList().Find(n => ColorDiff(n, target) == colorDiffs).color;
    }

    // weighed distance using hue, saturation and brightness
    Color32 ClosestColorHSV(ColorHSV[] colors, ColorHSV target) {
        float hue1 = target.h;
        var num1 = ColorNum(target);
        var diffs = colors.Select(n => Math.Abs(ColorNum(n) - num1) +
                                       getHueDistance(n.h, hue1));
        var diffMin = diffs.Min(x => x);
        return colors[diffs.ToList().FindIndex(n => n == diffMin)].color;
    }

    // color brightness as perceived:
    float getBrightness(Color c) { return (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) / 256f; }

    // distance between two hues:
    float getHueDistance(float hue1, float hue2) {
        float d = Math.Abs(hue1 - hue2); return d > 180 ? 360 - d : d;
    }

    //  weighed only by saturation and brightness 
    float ColorNum(ColorHSV c) {
        return c.s + getBrightness(c.color);
    }

    // distance in RGB space
    int ColorDiff(ColorHSV c1, ColorHSV c2) {
        return (int)Math.Sqrt((c1.r - c2.r) * (c1.r - c2.r)
                               + (c1.g - c2.g) * (c1.g - c2.g)
                               + (c1.b - c2.b) * (c1.b - c2.b));
    }
}
