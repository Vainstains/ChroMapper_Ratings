using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class TriangleVisualizer : Graphic
{
    [Header("Ratings")]
    [Range(0, 15)] public float techRating = 0f;
    [Range(0, 15)] public float passRating = 0f;
    [Range(0, 15)] public float accRating = 0f;
    [Range(0, 20)] public float maxRating = 15f;
    
    [Header("Colors")]
    public Color techColor = Color.red;
    public Color passColor = Color.green;
    public Color accColor = Color.blue;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
    
    [Header("Visual Settings")]
    public float borderWidth = 1.5f;
    public bool showDashedBorder = true;
    public float dashLength = 5f;
    public float dashGap = 5f;
    
    private readonly float gypL = 57.74f;
    
    [Header("Rating Labels")]
    public TextMeshProUGUI techLabel;
    public TextMeshProUGUI accLabel;
    public TextMeshProUGUI passLabel;
    public TextMeshProUGUI starLabel;
    
    public void SetupLabels()
    {
        TextMeshProUGUI CreateLabel(string name, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 12;
            tmp.color = new Color(1f, 1f, 1f, 0.6f);
            tmp.alignment = alignment;
            tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = Vector2.zero;
            return tmp;
        }

        techLabel = CreateLabel("TechLabel", TextAlignmentOptions.BottomLeft);
        accLabel  = CreateLabel("AccLabel", TextAlignmentOptions.BottomRight);
        passLabel = CreateLabel("PassLabel", TextAlignmentOptions.Top);
        starLabel = CreateLabel("StarLabel");
        starLabel.color = Color.yellow;
        
        techLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
        techLabel.rectTransform.anchorMax = new Vector2(0f, 1f);

        accLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
        accLabel.rectTransform.anchorMax = new Vector2(1f, 1f);

        passLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        passLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);

        starLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.65f);
        starLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.65f);
        starLabel.rectTransform.anchoredPosition = Vector2.zero;
    }
    
    public override Material material
    {
        get
        {
            if (base.material == null || base.material == defaultMaterial)
            {
                return Canvas.GetDefaultCanvasMaterial();
            }
            return base.material;
        }
    }
    
    public override Material materialForRendering
    {
        get
        {
            Material result = base.materialForRendering;
            if (result == null)
            {
                return Canvas.GetDefaultCanvasMaterial();
            }
            return result;
        }
    }
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        
        Rect rect = GetPixelAdjustedRect();
        float width = rect.width;
        float height = rect.height;
        
        Vector2 center = rect.center;
        
        float scaleX = width / 100f;
        float scaleY = height / 86.6f;
        
        Vector2 corner1 = CalculateCorner1(scaleX, scaleY);
        Vector2 corner2 = CalculateCorner2(scaleX, scaleY);
        Vector2 corner3 = CalculateCorner3(scaleX, scaleY);
        
        Vector2 offset = new Vector2(-50f * scaleX, -43.3f * scaleY);
        corner1 += offset;
        corner2 += offset;
        corner3 += offset;
        
        DrawBackgroundTriangle(vh, rect, scaleX, scaleY, offset);
        
        DrawInnerTriangle(vh, corner1, corner2, corner3);
        
        if (showDashedBorder)
        {
            DrawDashedBorder(vh, rect, scaleX, scaleY, offset);
        }
    }
    
    private Vector2 CalculateCorner1(float scaleX, float scaleY)
    {
        float normalizedTech = Mathf.Clamp01(techRating / maxRating);
        float x = (gypL - normalizedTech * gypL) * 0.866f;
        float y = 86.6f - (gypL - normalizedTech * gypL) / 2f;
        
        return new Vector2(x * scaleX, y * scaleY);
    }
    
    private Vector2 CalculateCorner2(float scaleX, float scaleY)
    {
        float normalizedAcc = Mathf.Clamp01(accRating / maxRating);
        float x = 100f - (gypL - normalizedAcc * gypL) * 0.866f;
        float y = 86.6f - (gypL - normalizedAcc * gypL) / 2f;
        
        return new Vector2(x * scaleX, y * scaleY);
    }
    
    private Vector2 CalculateCorner3(float scaleX, float scaleY)
    {
        float normalizedPass = Mathf.Clamp01(passRating / maxRating);
        float x = 50f;
        float y = (86.6f - gypL / 2f) * (1f - normalizedPass);
        
        return new Vector2(x * scaleX, y * scaleY);
    }
    
    private void DrawBackgroundTriangle(VertexHelper vh, Rect rect, float scaleX, float scaleY, Vector2 offset)
    {
        Vector2 top = new Vector2(50f * scaleX, 0f) + offset;
        Vector2 bottomLeft = new Vector2(0f, 86.6f * scaleY) + offset;
        Vector2 bottomRight = new Vector2(100f * scaleX, 86.6f * scaleY) + offset;
        
        int startIndex = vh.currentVertCount;
        vh.AddVert(top, backgroundColor, Vector2.zero);
        vh.AddVert(bottomLeft, backgroundColor, Vector2.zero);
        vh.AddVert(bottomRight, backgroundColor, Vector2.zero);
        
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
    }
    
    private void DrawInnerTriangle(VertexHelper vh, Vector2 corner1, Vector2 corner2, Vector2 corner3)
    {
        float normalizedTech = Mathf.Clamp01(techRating / maxRating);
        float normalizedAcc = Mathf.Clamp01(accRating / maxRating);
        float normalizedPass = Mathf.Clamp01(passRating / maxRating);
        
        var avgCol = (techColor + accColor + passColor) / 3f;
        
        var techBlend = Color.Lerp(avgCol, techColor, normalizedTech);
        var accBlend =  Color.Lerp(avgCol, accColor, normalizedAcc);
        var passBlend = Color.Lerp(avgCol, passColor, normalizedPass);
        
        int startIndex = vh.currentVertCount;
        vh.AddVert(corner1, techBlend, Vector2.zero);
        vh.AddVert(corner2, accBlend, Vector2.zero);
        vh.AddVert(corner3, passBlend, Vector2.zero);
        
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
    }
    
    private void DrawDashedBorder(VertexHelper vh, Rect rect, float scaleX, float scaleY, Vector2 offset)
    {
        Vector2 top = new Vector2(50f * scaleX, 0f) + offset;
        Vector2 bottomLeft = new Vector2(0f, 86.6f * scaleY) + offset;
        Vector2 bottomRight = new Vector2(100f * scaleX, 86.6f * scaleY) + offset;
        
        Color borderColor = Color.white;
        
        DrawDashedLine(vh, top, bottomLeft, borderColor);
        DrawDashedLine(vh, bottomLeft, bottomRight, borderColor);
        DrawDashedLine(vh, bottomRight, top, borderColor);
    }
    
    private void DrawDashedLine(VertexHelper vh, Vector2 start, Vector2 end, Color color)
    {
        Vector2 direction = (end - start).normalized;
        float length = Vector2.Distance(start, end);
        float segmentLength = dashLength + dashGap;
        int segments = Mathf.CeilToInt(length / segmentLength);
        
        for (int i = 0; i < segments; i++)
        {
            float t1 = i * segmentLength / length;
            float t2 = Mathf.Min((i * segmentLength + dashLength) / length, 1f);
            
            if (t2 <= t1) break;
            
            Vector2 segmentStart = Vector2.Lerp(start, end, t1);
            Vector2 segmentEnd = Vector2.Lerp(start, end, t2);
            
            DrawLine(vh, segmentStart, segmentEnd, color);
        }
    }
    
    private void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, Color color)
    {
        Vector2 perpendicular = new Vector2(end.y - start.y, start.x - end.x).normalized * borderWidth * 0.5f;
        
        int startIndex = vh.currentVertCount;
        vh.AddVert(start - perpendicular, color, Vector2.zero);
        vh.AddVert(start + perpendicular, color, Vector2.zero);
        vh.AddVert(end + perpendicular, color, Vector2.zero);
        vh.AddVert(end - perpendicular, color, Vector2.zero);
        
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }
    
    public void UpdateRatings(float tech, float pass, float acc, float star = -1f)
    {
        techRating = tech;
        passRating = pass;
        accRating = acc;

        SetVerticesDirty();
        
        if (techLabel != null) techLabel.text = tech.ToString("F1");
        if (accLabel  != null) accLabel.text  = acc.ToString("F1");
        if (passLabel != null) passLabel.text = pass.ToString("F1");
        if (starLabel != null) starLabel.text = star >= 0 ? star.ToString("F1") : "";
    }
    
    public void UpdateMaxRating(float max)
    {
        maxRating = max;
        SetVerticesDirty();
    }
    
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}