// =====================================================================
// Copyright � 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator.Modules;
using UnityEditor;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ModifierTRSPath))]
    public class ModifierTRSPathEditor : CGModuleEditor<ModifierTRSPath> { }
}