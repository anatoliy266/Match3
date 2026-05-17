//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UIElements;

//public class TileGeometryEditor : EditorWindow
//{
//    [SerializeField]
//    private VisualTreeAsset m_VisualTreeAsset = default;

//    [MenuItem("Window/UI Toolkit/TileGeometryEditor")]
//    public static void ShowExample()
//    {
//        TileGeometryEditor wnd = GetWindow<TileGeometryEditor>();
//        wnd.titleContent = new GUIContent("TileGeometryEditor");
//    }

//    public void CreateGUI()
//    {
//        // Each editor window contains a root VisualElement object
//        VisualElement root = rootVisualElement;

//        // VisualElements objects can contain other VisualElement following a tree hierarchy.
//        VisualElement label = new Label("Hello World! From C#");
//        root.Add(label);

//        // Instantiate UXML
//        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
//        root.Add(labelFromUXML);
//    }
//}
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TileGeometryEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset; // Перетащи сюда UXML

    private ObjectField TileSpawnRuleField;
    private Vector2IntField gridSizeField;
    private Button clearButton;
    private VisualElement gridContainer;
    private Label infoLabel;

    private TileSpawnRuleBase currentShape; // Текущий редактируемый ассет

    private const int CellSize = 24;
    private const int CellMargin = 2;

    [MenuItem("Window/UI Toolkit/TileGeometryEditor")]
    public static void ShowExample()
    {
        TileGeometryEditor wnd = GetWindow<TileGeometryEditor>();
        wnd.titleContent = new GUIContent("Tile Geometry Editor");
    }

    public void CreateGUI()
    {
        // Загружаем UXML
        VisualElement root = rootVisualElement;
        m_VisualTreeAsset.CloneTree(root);

        // Стили
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Game/Editor/TileGeometryEditor.uss");
        if (styleSheet != null)
            root.styleSheets.Add(styleSheet);
        else
            Debug.LogError("USS not found at path!");

        // Находим элементы
        TileSpawnRuleField = root.Q<ObjectField>("bonusShapeField");
        gridSizeField = root.Q<Vector2IntField>("gridSizeField");
        clearButton = root.Q<Button>("clearButton");
        gridContainer = root.Q<VisualElement>("gridContainer");
        infoLabel = root.Q<Label>("infoLabel");

        // Подписки
        TileSpawnRuleField.RegisterValueChangedCallback(OnBonusShapeChanged);
        gridSizeField.RegisterValueChangedCallback(OnGridSizeChanged);
        clearButton.clicked += OnClearClicked;

        // Если окно открыто с уже выбранным ассетом (можно задать извне)
        if (currentShape != null)
        {
            TileSpawnRuleField.value = currentShape;
            RefreshAll();
        }
    }

    private void OnBonusShapeChanged(ChangeEvent<Object> evt)
    {
        currentShape = evt.newValue as TileSpawnRuleBase;
        if (currentShape != null)
        {
            // Обновляем поле gridSize из ассета
            gridSizeField.SetValueWithoutNotify(currentShape.gridSize);
        }
        RefreshGrid();
    }

    private void OnGridSizeChanged(ChangeEvent<Vector2Int> evt)
    {
        if (currentShape == null) return;

        Undo.RecordObject(currentShape, "Change Grid Size");
        currentShape.gridSize = evt.newValue;
        // Удаляем клетки за пределами
        currentShape.activeCells.RemoveAll(c =>
            c.x < 0 || c.x >= currentShape.gridSize.x ||
            c.y < 0 || c.y >= currentShape.gridSize.y);
        EditorUtility.SetDirty(currentShape);
        RefreshGrid();
    }

    private void OnClearClicked()
    {
        if (currentShape == null) return;
        Undo.RecordObject(currentShape, "Clear Cells");
        currentShape.activeCells.Clear();
        EditorUtility.SetDirty(currentShape);
        RefreshGrid();
    }

    /// <summary>
    /// Полная перестройка интерфейса при смене ассета
    /// </summary>
    private void RefreshAll()
    {
        if (currentShape == null)
        {
            gridSizeField.SetValueWithoutNotify(Vector2Int.one);
            gridContainer.Clear();
            infoLabel.text = "No shape selected";
            return;
        }
        gridSizeField.SetValueWithoutNotify(currentShape.gridSize);
        RefreshGrid();
    }

    /// <summary>
    /// Перерисовать только сетку
    /// </summary>
    private void RefreshGrid()
    {
        gridContainer.Clear();
        if (currentShape == null)
        {
            infoLabel.text = "No shape selected";
            return;
        }

        int w = currentShape.gridSize.x;
        int h = currentShape.gridSize.y;

        // Рисуем строки снизу вверх (чтобы (0,0) был левый нижний)
        for (int row = h - 1; row >= 0; row--)
        {
            var rowElement = new VisualElement();
            rowElement.AddToClassList("grid-row");
            for (int col = 0; col < w; col++)
            {
                var cell = new VisualElement();
                cell.AddToClassList("grid-cell");
                if (currentShape.activeCells.Contains(new Vector2Int(col, row)))
                    cell.AddToClassList("grid-cell--active");

                cell.userData = new Vector2Int(col, row);
                cell.RegisterCallback<MouseDownEvent>(OnCellClicked);
                cell.style.width = 24;
                cell.style.height = 24;
                cell.style.marginLeft = 2;
                cell.style.marginTop = 2;
                //cell.style.backgroundColor = new StyleColor(Color.gray);
                rowElement.Add(cell);
            }
            gridContainer.Add(rowElement);
        }

        infoLabel.text = $"Active Cells: {currentShape.activeCells.Count}";
    }

    private void OnCellClicked(MouseDownEvent evt)
    {
        if (currentShape == null) return;
        var cell = evt.currentTarget as VisualElement;
        Vector2Int coord = (Vector2Int)cell.userData;

        Undo.RecordObject(currentShape, "Toggle Cell");

        if (currentShape.activeCells.Contains(coord))
            currentShape.activeCells.Remove(coord);
        else
            currentShape.activeCells.Add(coord);

        EditorUtility.SetDirty(currentShape);

        // Обновляем визуал только этой клетки
        if (currentShape.activeCells.Contains(coord))
            cell.AddToClassList("grid-cell--active");
        else
            cell.RemoveFromClassList("grid-cell--active");

        infoLabel.text = $"Active Cells: {currentShape.activeCells.Count}";
    }
}