using UnityEditor;
using UnityEngine;
using System.IO;

public static class CreateSweepGlowMaterial
{
    [MenuItem("Game/Spark/Create SweepGlow Material", priority = 100)]
    private static void Create()
    {
        var shader = Shader.Find("Game/Spark/SweepGlow");
        if (!shader)
        {
            EditorUtility.DisplayDialog("Error", "Shader 'Game/Spark/SweepGlow' not found.\nMake sure the shader file has been imported.", "OK");
            return;
        }

        const string dir = "Assets/Game/Materials/Spark";
        const string path = dir + "/SweepGlowTile.mat";

        Directory.CreateDirectory(dir);

        if (File.Exists(path) && !EditorUtility.DisplayDialog("Overwrite?", "Material already exists. Overwrite?", "Yes", "No"))
            return;

        var mat = new Material(shader)
        {
            name = "SweepGlowTile",
        };

        mat.SetFloat("_SweepMin", -13f);
        mat.SetFloat("_SweepMax", 11f);
        mat.SetFloat("_SweepWidth", 4f);
        mat.SetFloat("_SweepSoftness", 3f);
        mat.SetFloat("_SweepSpeed", 0.3f);
        mat.SetColor("_GlowColor", Color.white);
        mat.SetFloat("_GlowIntensity", 4f);

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = mat;
        EditorGUIUtility.PingObject(mat);

        Debug.Log("SweepGlow material created at " + path);
    }
}
