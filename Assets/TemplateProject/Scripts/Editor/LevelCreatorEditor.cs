using System;
using System.Collections.Generic;
using System.Linq;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.LevelCreation;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Editor
{
    [CustomEditor(typeof(LevelCreator))]
    public class LevelCreatorEditor : UnityEditor.Editor
    {
        private LevelCreator _levelCreator;

        private void OnEnable()
        {
            _levelCreator = (LevelCreator)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            _levelCreator.GenerateLevel();
            DrawGridProperties();

            if (!IsLevelDataAvailable())
            {
                EditorGUILayout.HelpBox("Please regenerate the Grid!", MessageType.Error);
                return;
            }

            DrawGrid(); 
            DrawSaveLoadButtons(DisplayColorStatus());
            DrawTestButton();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_levelCreator, "Change Level Goals");
                EditorUtility.SetDirty(_levelCreator);
                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool IsLevelDataAvailable()
        {
            var isLevelDataExist = _levelCreator.GetLevelData() != null;
            if (!isLevelDataExist)
                return false;

            var isLevelGridExist = _levelCreator.GetLevelData().GetGrid() != null;
            var isGridBoundsCorrect = isLevelGridExist ? (_levelCreator.gridWidth * _levelCreator.gridHeight) ==
                                      _levelCreator.GetLevelData().GetGrid().Length : false;
            return isLevelGridExist && isGridBoundsCorrect;
        }

        private void DrawGridProperties()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Grid Width");
            _levelCreator.gridWidth = EditorGUILayout.IntField(_levelCreator.gridWidth);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Grid Height");
            _levelCreator.gridHeight = EditorGUILayout.IntField(_levelCreator.gridHeight);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Level Index");
            _levelCreator.levelIndex = EditorGUILayout.IntField(_levelCreator.levelIndex);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Grid"))
            {
                _levelCreator.SpawnGrid();
            }

            if (GUILayout.Button("Reset"))
            {
                _levelCreator.ResetLevel();
            }

            if (GUILayout.Button("Load"))
            {
                _levelCreator.LoadLevel();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            var style = new GUIStyle(GUI.skin.button) { fontSize = 64 };
            var grid = _levelCreator.GetLevelData().GetGrid();
            if (ReferenceEquals(grid, null) || grid.Length.Equals(0))
            {
                return;
            }

            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

            for (var y = 0; y < _levelCreator.gridHeight; y++)
            {
                EditorGUILayout.BeginHorizontal();

                for (var x = _levelCreator.gridWidth - 1; x >= 0; x--)
                {
                    EditorGUILayout.BeginVertical();

                    var cell = _levelCreator.GetLevelData().GetGridCell(x, y);

                    var subColors = new List<Color>
                        { _levelCreator.GetGameColors().activeColors[(int)cell.stackData.stickmanColorType] };
                    var text = "";

                    if (cell.stackData.isSecret)
                    {
                        if (text == "")
                        {
                            text += "S";
                        }
                        else
                        {
                            text += "," + "S";
                        }
                    }

                    if (cell.stackData.isReserved)
                    {
                        if (text == "")
                        {
                            text += "R";
                        }
                        else
                        {
                            text += "," + "R";
                        }
                    }

                    var buttonRect = GUILayoutUtility.GetRect(new GUIContent(text), style, GUILayout.Width(75),
                        GUILayout.Height(75));

                    var textStyle = new GUIStyle
                    {
                        fontSize = 16,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.black },
                    };


                    Rect[] subRects =
                    {
                        new Rect(buttonRect.x, buttonRect.y, buttonRect.width, buttonRect.height)
                    };

                    for (var t = 0; t < subRects.Length; t++)
                    {
                        EditorGUI.DrawRect(subRects[t],
                            subColors[t] == Color.black
                                ? Color.black + new Color(0.1f * t, 0.1f * t, 0.1f * t)
                                : subColors[t]);
                    }

                    Handles.Label(
                        new Vector3(buttonRect.x, buttonRect.y, 0),
                        text, textStyle);


                    for (var o = subRects.Length - 1; o >= 0; o--)
                    {
                        if ((Event.current.type != EventType.MouseDrag && Event.current.type != EventType.MouseDown) ||
                            !subRects[o].Contains(Event.current.mousePosition)) continue;

                        Event.current.Use();

                        switch (Event.current.button)
                        {
                            case 0:
                                _levelCreator.GridButtonAction(x, y);
                                break;
                            case 1:
                                _levelCreator.GridRemoveButtonAction(x, y);
                                break;
                        }
                    }


                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = Color.white;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5f);
            }
        }

        private void DrawSaveLoadButtons(bool canInteractable)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Don't forget the save grid!", MessageType.Warning);
            EditorGUILayout.LabelField("Save/Load", EditorStyles.boldLabel);

            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = canInteractable;
            if (GUILayout.Button("Save"))
            {
                _levelCreator.SaveLevel();
            }


            EditorGUILayout.EndHorizontal();
        }

        

        private void DrawTestButton()
        { 
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Test Before Forward!", MessageType.Warning);
            EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test"))
            {
                _levelCreator.TestLevel();
            }

            EditorGUILayout.EndHorizontal();
            
        }
        private bool DisplayColorStatus()
        {
            var grid = _levelCreator.GetLevelData().GetGrid();
            var errorCount = 0;
            foreach (LevelData.GridColorType colorType in Enum.GetValues(typeof(LevelData.GridColorType)))
            {
                if (colorType is LevelData.GridColorType.None or LevelData.GridColorType.Close)
                    continue;

                var colorCount = grid.Cast<GridCell>().Count(cell => cell.stackData.stickmanColorType == colorType);
                var reservedColorCount = grid.Cast<GridCell>().Count(cell =>
                    cell.stackData.stickmanColorType == colorType && cell.stackData.isReserved);
                if (colorCount == 0)
                    continue;

                var colorName = colorType.ToString();
                var goalCount = DisplayGoalStatus(colorType);
                var reservedGoalCount = DisplayReservedGoalCount(colorType);
                if (colorCount % 3 == 0)
                {
                    if (goalCount == colorCount / 3)
                    {
                        if (reservedColorCount == reservedGoalCount)
                        {
                            EditorGUILayout.HelpBox(
                                $"{colorName} Color: OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
                                MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(
                                $"{colorName} Color: NOT OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
                                MessageType.Error);
                            errorCount++;
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"{colorName} Color: NOT OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
                            MessageType.Error);
                        errorCount++;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"{colorName} Color: NOT OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
                        MessageType.Error);
                    errorCount++;
                }
            }

            return errorCount == 0;
        }

        private int DisplayReservedGoalCount(LevelData.GridColorType colorType)
        {
            if (_levelCreator.levelGoals == null || _levelCreator.levelGoals.Count == 0)
                return 0;

            var sameColorGoals = _levelCreator.levelGoals.Where(goal => colorType == goal.colorType);
            return sameColorGoals.Sum(colorGoal => colorGoal.reservedCount);
        }

        private int DisplayGoalStatus(LevelData.GridColorType gridColorType)
        {
            if (_levelCreator.levelGoals == null || _levelCreator.levelGoals.Count == 0)
                return 0;

            return _levelCreator.levelGoals.Count(goal => gridColorType == goal.colorType);
        }
    }
}