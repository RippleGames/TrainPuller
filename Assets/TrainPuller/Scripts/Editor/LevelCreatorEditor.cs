using System.Collections.Generic;
using System.Linq;
using TemplateProject.Scripts.Data;
using TrainPuller.Scripts.Runtime.LevelCreation;
using UnityEditor;
using UnityEngine;

namespace TrainPuller.Scripts.Editor
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
            DrawSaveLoadButtons(true);
            DrawTestButton();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_levelCreator, "Change Level");
                EditorUtility.SetDirty(_levelCreator);
                serializedObject.ApplyModifiedProperties();
                Repaint();
            }
        }


        private bool IsLevelDataAvailable()
        {
            var isLevelDataExist = _levelCreator.GetLevelData() != null;
            if (!isLevelDataExist)
                return false;

            var isLevelGridExist = _levelCreator.GetLevelData().GetGrid() != null;
            var isGridBoundsCorrect = isLevelGridExist && (_levelCreator.gridWidth * _levelCreator.gridHeight) ==
                _levelCreator.GetLevelData().GetGrid().Length;
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
                    List<Color> subColors = new List<Color>();

                    if (cell.stackData.colorTypes == null || cell.stackData.colorTypes.Count == 0)
                    {
                        cell.stackData.colorTypes = new List<LevelData.GridColorType>();
                    }


                    foreach (var stackColorType in cell.stackData.colorTypes)
                    {
                        if (_levelCreator.GetGameColors().activeColors.Length > (int)stackColorType)
                        {
                            subColors.Add(_levelCreator.GetGameColors().activeColors[(int)stackColorType]);
                        }
                        else
                        {
                            subColors.Add(Color.black); // Fallback color if out of range
                        }
                    }


                    var text = "";
                    if (cell.isExit)
                    {
                        if (text == "")
                        {
                            text += "E";
                        }
                        else
                        {
                            text += "," + "E";
                        }
                    }
                    else if (cell.isBarrier)
                    {
                        if (text == "")
                        {
                            text += "B";
                        }
                        else
                        {
                            text += "," + "B";
                        }
                    }else if (cell.isOneDirection)
                    {
                        if (text == "")
                        {
                            text += "OD";
                        }
                        else
                        {
                            text += "," + "OD";
                        }
                    }

                    var buttonRect = GUILayoutUtility.GetRect(new GUIContent(text), style, GUILayout.Width(50),
                        GUILayout.Height(50));

                    var textStyle = new GUIStyle
                    {
                        fontSize = 16,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.black },
                        alignment = TextAnchor.UpperLeft
                    };


                    Rect[] subRects;
                    if (cell.stackData.colorTypes.Contains(LevelData.GridColorType.Trail))
                    {
                        // Trail logic: Draw grayed-out rectangle with a small overlay
                        subRects = new Rect[]
                        {
                            new Rect(buttonRect.x, buttonRect.y, buttonRect.width, buttonRect.height), // Full gray
                            new Rect(buttonRect.x + buttonRect.width / 3, buttonRect.y + buttonRect.height / 3,
                                buttonRect.width / 3, buttonRect.height / 3) // Small overlay in center
                        };
                    }
                    else
                    {
                        // Non-Trail logic: Stack colors from bottom to top
                        subRects = new Rect[Mathf.Min(10, subColors.Count)]; // Limit to 10 layers max

                        float heightStep = buttonRect.height / Mathf.Max(1, subRects.Length); // Ensure valid height

                        for (int i = 0; i < subRects.Length; i++)
                        {
                            subRects[i] = new Rect(buttonRect.x, buttonRect.y + (subRects.Length - 1 - i) * heightStep,
                                buttonRect.width, heightStep);
                        }
                    }

                    for (int i = 0; i < subRects.Length && i < subColors.Count; i++)
                    {
                        EditorGUI.DrawRect(subRects[i], subColors[i]);
                        if (!cell.stackData.colorTypes.Contains(LevelData.GridColorType.Trail) &&
                            !cell.stackData.colorTypes.Contains(LevelData.GridColorType.None))
                        {
                            EditorGUI.LabelField(subRects[i], "-", textStyle = new GUIStyle
                            {
                                fontSize = 16,
                                fontStyle = FontStyle.Bold,
                                normal = { textColor = Color.black },
                                alignment = TextAnchor.MiddleRight
                            });
                        }
                    }

                    Handles.Label(
                        new Vector3(buttonRect.x, buttonRect.y, 0),
                        text, textStyle);


                    for (var o = subRects.Length - 1; o >= 0; o--)
                    {
                        if (Event.current.type != EventType.MouseDown ||
                            !subRects[o].Contains(Event.current.mousePosition)) continue;

                        Event.current.Use();

                        switch (Event.current.button)
                        {
                            case 0:
                                _levelCreator.GridButtonAction(x, y, o);
                                break;

                            case 1:
                                _levelCreator.GridRemoveButtonAction(x, y, o);
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

        // private bool DisplayColorStatus()
        // {
        //     var grid = _levelCreator.GetLevelData().GetGrid();
        //     var errorCount = 0;
        //     foreach (LevelData.GridColorType colorType in Enum.GetValues(typeof(LevelData.GridColorType)))
        //     {
        //         if (colorType is LevelData.GridColorType.None or LevelData.GridColorType.Close)
        //             continue;
        //
        //         var colorCount = grid.Cast<GridCell>().Count(cell => cell.stackData.gridColorType == colorType);
        //         var reservedColorCount = grid.Cast<GridCell>().Count(cell =>
        //             cell.stackData.gridColorType == colorType && cell.stackData.isReserved);
        //         if (colorCount == 0)
        //             continue;
        //
        //         var colorName = colorType.ToString();
        //         var goalCount = DisplayGoalStatus(colorType);
        //         var reservedGoalCount = DisplayReservedGoalCount(colorType);
        //         if (colorCount % 3 == 0)
        //         {
        //             if (goalCount == colorCount / 3)
        //             {
        //                 if (reservedColorCount == reservedGoalCount)
        //                 {
        //                     EditorGUILayout.HelpBox(
        //                         $"{colorName} Color: OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
        //                         MessageType.Info);
        //                 }
        //                 else
        //                 {
        //                     EditorGUILayout.HelpBox(
        //                         $"{colorName} Color: NOT OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
        //                         MessageType.Error);
        //                     errorCount++;
        //                 }
        //             }
        //             else
        //             {
        //                 EditorGUILayout.HelpBox(
        //                     $"{colorName} Color: NOT OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
        //                     MessageType.Error);
        //                 errorCount++;
        //             }
        //         }
        //         else
        //         {
        //             EditorGUILayout.HelpBox(
        //                 $"{colorName} Color: NOT OK ({colorCount} {colorName} Color with {goalCount} Goal Count that has {reservedColorCount} reserved.)",
        //                 MessageType.Error);
        //             errorCount++;
        //         }
        //     }
        //
        //     return errorCount == 0;
        // }
    }
}