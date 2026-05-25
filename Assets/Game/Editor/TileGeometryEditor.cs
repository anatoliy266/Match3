using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TileGeometryEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset;

    private ObjectField TileSpawnRuleField;
    private Button clearButton;
    private VisualElement gridContainer;
    private Label infoLabel;

    private TileSpawnRuleBase currentShape; 

    // Динамический запас пустых клеток вокруг краев фигуры для рисования кликабельной зоны
    private const int GridPadding = 2;

    [MenuItem("Window/UI Toolkit/TileGeometryEditor")]
    public static void ShowExample()
    {
        TileGeometryEditor wnd = GetWindow<TileGeometryEditor>();
        wnd.titleContent = new GUIContent("Tile Geometry Editor");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        m_VisualTreeAsset.CloneTree(root);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Game/Editor/TileGeometryEditor.uss");
        if (styleSheet != null)
            root.styleSheets.Add(styleSheet);
        else
            Debug.LogError("USS not found at path!");

        // Находим элементы (поле gridSizeField убрано из UXML/кода за ненадобностью)
        TileSpawnRuleField = root.Q<ObjectField>("bonusShapeField");
        clearButton = root.Q<Button>("clearButton");
        gridContainer = root.Q<VisualElement>("gridContainer");
        infoLabel = root.Q<Label>("infoLabel");

        // Подписки
        TileSpawnRuleField.RegisterValueChangedCallback(OnBonusShapeChanged);
        clearButton.clicked += OnClearClicked;

        if (currentShape != null)
        {
            TileSpawnRuleField.value = currentShape;
            RefreshGrid();
        }
    }

    private void OnBonusShapeChanged(ChangeEvent<Object> evt)
    {
        currentShape = evt.newValue as TileSpawnRuleBase;
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
    /// Перерисовать динамическую сетку на основе имеющихся точек фигуры
    /// </summary>
    private void RefreshGrid()
    {
        gridContainer.Clear();
        if (currentShape == null)
        {
            infoLabel.text = "No shape selected";
            return;
        }

        // Если фигура пустая, рисуем базовый дефолтный квадрат 5х5 вокруг нуля
        int minX = -2, maxX = 2;
        int minY = -2, maxY = 2;

        // Если в фигуре уже есть точки, считаем ее реальные границы (Bounding Box)
        if (currentShape.activeCells != null && currentShape.activeCells.Count > 0)
        {
            minX = int.MaxValue; maxX = int.MinValue;
            minY = int.MaxValue; maxY = int.MinValue;

            for (int i = 0; i < currentShape.activeCells.Count; i++)
            {
                var pos = currentShape.activeCells[i];
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            // Добавляем пустые поля по краям холста, чтобы было куда кликнуть для расширения
            minX -= GridPadding; maxX += GridPadding;
            minY -= GridPadding; maxY += GridPadding;
        }

        // Рисуем строки сверху вниз (от maxY до minY)
        for (int row = maxY; row >= minY; row--)
        {
            var rowElement = new VisualElement();
            rowElement.AddToClassList("grid-row");

            for (int col = minX; col <= maxX; col++)
            {
                var currentCoord = new Vector2Int(col, row);
                var cell = new VisualElement();
                cell.AddToClassList("grid-cell");

                // Подсвечиваем центр координат (0,0) другим цветом/стилем для удобства привязки
                if (col == 0 && row == 0)
                    cell.AddToClassList("grid-cell--center");

                if (currentShape.activeCells.Contains(currentCoord))
                    cell.AddToClassList("grid-cell--active");

                cell.userData = currentCoord;
                cell.RegisterCallback<MouseDownEvent>(OnCellClicked);

                // Инлайновые базовые стили (лучше перенести в USS)
                cell.style.width = 24;
                cell.style.height = 24;
                cell.style.marginLeft = 2;
                cell.style.marginTop = 2;

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

        // Перерисовываем ВСЮ сетку, так как при клике на край холст должен расшириться
        RefreshGrid();
    }
}
