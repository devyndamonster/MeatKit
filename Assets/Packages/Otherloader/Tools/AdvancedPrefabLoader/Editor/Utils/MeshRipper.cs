using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MeshRipper
{
    public static void RipAndReplaceMeshes(GameObject spawned, string outputFolderPath)
    {
        foreach (MeshFilter meshFilter in spawned.GetComponentsInChildren<MeshFilter>())
        {
            try
            {
                string meshPath = outputFolderPath + "/" + meshFilter.sharedMesh.name + ".asset";

                Debug.Log("Mesh: " + meshFilter.sharedMesh.name);

                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) == null)
                {
                    Debug.Log("Mesh does not exist!");
                    Mesh copiedMesh = CopyMesh(meshFilter.sharedMesh);
                    AssetDatabase.CreateAsset(copiedMesh, meshPath);
                    AssetDatabase.Refresh();
                }

                meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to rip mesh for mesh filter: " + meshFilter.gameObject.name);
                Debug.LogError(ex.ToString());
            }
        }

        foreach (MeshRenderer meshRenderer in spawned.GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.materials = new Material[] { AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat") };
        }
    }

    private static Mesh CopyMesh(Mesh originalMesh)
    {
        Mesh copiedMesh = new Mesh();

        copiedMesh.vertices = originalMesh.vertices;
        copiedMesh.triangles = originalMesh.triangles;
        copiedMesh.uv = originalMesh.uv;
        copiedMesh.normals = originalMesh.normals;
        copiedMesh.colors = originalMesh.colors;
        copiedMesh.tangents = originalMesh.tangents;

        return copiedMesh;
    }
}
