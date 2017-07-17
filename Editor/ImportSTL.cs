using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DefaultAsset))]
public class ImportSTL : Editor
{
    private string _currentClassify = "Model";
    private string _fileType = "Binary";
    private string _fileName = "";
    private string _trianglescount = "";
    private string _meshCompression = "Off";
    private int _singleTrianglesNumber = 15000;
    private bool _isSaveMesh = true;

    private int _total;
    private int _number;
    private BinaryReader _binaryReader;
    private List<Vector3> _vertices;
    private List<Vector3> _normals;
    private List<int> _triangles;

    public override void OnInspectorGUI()
    {
        //目标为STL格式
        if (AssetDatabase.GetAssetPath(target).IsStl())
        {
            ShowUI();
        }
    }

    private void ShowUI()
    {
        GUI.enabled = true;

        #region Classify
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Model", "ButtonLeft", GUILayout.Width(80)))
        {
            _currentClassify = "Model";
        }
        if (GUILayout.Button("Rig", "ButtonMid", GUILayout.Width(80)))
        {
            _currentClassify = "Rig";
        }
        if (GUILayout.Button("Animations", "ButtonRight", GUILayout.Width(80)))
        {
            _currentClassify = "Animations";
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        #endregion

        if (_currentClassify == "Model")
        {
            #region STL Meshes
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("STL Meshes", "BoldLabel");
            GUILayout.Label("(File Type:Binary)", "BoldLabel");
            EditorGUILayout.EndHorizontal();

            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Scale Factor", GUILayout.Width(120));
            GUILayout.TextField("1");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("File Scale", GUILayout.Width(120));
            GUILayout.TextField("1");
            EditorGUILayout.EndHorizontal();

            if (_fileName == "" && _trianglescount == "")
            {
                GetFileNameAndTrianglesCount();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("File Name", GUILayout.Width(120));
            _fileName = GUILayout.TextField(_fileName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Triangles Count", GUILayout.Width(120));
            _trianglescount = GUILayout.TextField(_trianglescount);
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Mesh Compression", GUILayout.Width(120));
            if (GUILayout.Button(_meshCompression, "MiniPopup"))
            {
                GenericMenu gm = new GenericMenu();
                gm.AddItem(new GUIContent("Off"), "Off" == _meshCompression, delegate () { _meshCompression = "Off"; });
                gm.AddItem(new GUIContent("On"), "On" == _meshCompression, delegate () { _meshCompression = "On"; });
                gm.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Single Triangles Number", GUILayout.Width(120));
            _singleTrianglesNumber = EditorGUILayout.IntField(_singleTrianglesNumber);
            EditorGUILayout.EndHorizontal();

            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Read/Write Enabled", GUILayout.Width(120));
            GUILayout.Toggle(true, "");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Optimize Mesh", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Import BlendShapes", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Generate Colliders", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Keep Quads", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Swap UVs", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Generate Lightmap UVs", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            #endregion

            #region Normals & Tangents
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Normals & Tangents", "BoldLabel");
            EditorGUILayout.EndHorizontal();

            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Normals", GUILayout.Width(120));
            GUILayout.Button("None", "MiniPopup");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Smoothing Angle", GUILayout.Width(120));
            GUILayout.HorizontalSlider(0.3f, 0, 1);
            GUILayout.TextField("60");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tangents", GUILayout.Width(120));
            GUILayout.Button("None - (Normals required)", "MiniPopup");
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            #endregion

            #region Materials
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Materials", "BoldLabel");
            EditorGUILayout.EndHorizontal();

            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Import Materials", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            #endregion
        }
        else if (_currentClassify == "Rig")
        {
            #region Rig
            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Animation Type", GUILayout.Width(120));
            GUILayout.Button("Legacy", "MiniPopup");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Generation", GUILayout.Width(120));
            GUILayout.Button("Don't Import", "MiniPopup");
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            #endregion
        }
        else if (_currentClassify == "Animations")
        {
            #region Animations
            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Import Animation", GUILayout.Width(120));
            GUILayout.Toggle(false, "");
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("No animation data available in this STL model.", MessageType.Info);
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        #region Apply
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("");
        EditorGUILayout.EndHorizontal();

        GUI.enabled = false;
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Button("Revert");
        GUILayout.Button("Apply");
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;
        #endregion

        #region CreateInstance
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("CreateInstance", GUILayout.Width(160)))
        {
            CreateInstance();
        }
        _isSaveMesh = GUILayout.Toggle(_isSaveMesh, "Save Mesh");
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        #endregion
    }

    /// <summary>
    /// 获取STL模型的文件名及三角面数量
    /// </summary>
    private void GetFileNameAndTrianglesCount()
    {
        string fullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(target));
        
        using (BinaryReader br = new BinaryReader(File.Open(fullPath, FileMode.Open)))
        {
            _fileName = Encoding.UTF8.GetString(br.ReadBytes(80));
            _trianglescount = BitConverter.ToInt32(br.ReadBytes(4), 0).ToString();
        }
    }

    /// <summary>
    /// 创建STL模型实例
    /// </summary>
    private void CreateInstance()
    {
        if (_singleTrianglesNumber < 1000 || _singleTrianglesNumber > 20000)
        {
            Debug.LogError("Single Triangles Number: this value is unreasonable!");
            return;
        }
        if (int.Parse(_trianglescount) > 200000)
        {
            Debug.LogError("Triangles Count: this value is too much!");
            return;
        }

        string fullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(target));

        _total = int.Parse(_trianglescount);
        _number = 0;
        _binaryReader = new BinaryReader(File.Open(fullPath, FileMode.Open));

        //抛弃前84个字节
        _binaryReader.ReadBytes(84);

        _vertices = new List<Vector3>();
        _normals = new List<Vector3>();
        _triangles = new List<int>();

        //读取顶点信息
        Thread t = new Thread(ReadVertex);
        t.Start();

        while (_number < _total)
        {
            EditorUtility.DisplayProgressBar("读取信息", "正在读取顶点信息（" + _number + "/" + _total + "）......", (float)_number / _total);
        }

        CreateGameObject();

        _binaryReader.Close();
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 读取顶点信息
    /// </summary>
    private void ReadVertex()
    {
        while (_number < _total)
        {
            byte[] bytes;
            bytes = _binaryReader.ReadBytes(50);

            if (bytes.Length < 50)
            {
                _number += 1;
                continue;
            }

            Vector3 vec0 = new Vector3(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8));
            Vector3 vec1 = new Vector3(BitConverter.ToSingle(bytes, 12), BitConverter.ToSingle(bytes, 16), BitConverter.ToSingle(bytes, 20));
            Vector3 vec2 = new Vector3(BitConverter.ToSingle(bytes, 24), BitConverter.ToSingle(bytes, 28), BitConverter.ToSingle(bytes, 32));
            Vector3 vec3 = new Vector3(BitConverter.ToSingle(bytes, 36), BitConverter.ToSingle(bytes, 40), BitConverter.ToSingle(bytes, 44));

            _normals.AddNormal(vec0);
            _triangles.AddTriangle(_vertices.AddGetIndex(vec1), _vertices.AddGetIndex(vec2), _vertices.AddGetIndex(vec3));

            _number += 1;
        }
    }

    /// <summary>
    /// 创建GameObject
    /// </summary>
    private void CreateGameObject()
    {
        string path = AssetDatabase.GetAssetPath(target);
        string fullPath = Path.GetFullPath(path);
        string assetPath = path.Substring(0, path.LastIndexOf("/")) + "/";

        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(fullPath));
        root.transform.localPosition = Vector3.zero;
        root.transform.localScale = Vector3.one;

        int count = _total / _singleTrianglesNumber;
        count += (_total % _singleTrianglesNumber > 0) ? 1 : 0;

        for (int i = 0; i < count; i++)
        {
            GameObject tem = new GameObject(Path.GetFileNameWithoutExtension(fullPath) + "Sub" + i);
            tem.transform.SetParent(root.transform);
            tem.transform.localPosition = Vector3.zero;
            tem.transform.localScale = Vector3.one;

            MeshFilter mf = tem.AddComponent<MeshFilter>();
            MeshRenderer mr = tem.AddComponent<MeshRenderer>();

            int startIndex = i * _singleTrianglesNumber * 3;
            int length = _singleTrianglesNumber * 3;
            if ((startIndex + length) > _vertices.Count)
            {
                length = _vertices.Count - startIndex;
            }

            List<Vector3> vertices = _vertices.GetRange(startIndex, length);
            List<Vector3> normals = _normals.GetRange(startIndex, length);
            List<int> triangles = _triangles.GetRange(0, length);

            //压缩网格
            if (_meshCompression.IsOn())
            {
                MeshCompression(tem.name, vertices, normals, triangles);
            }

            Mesh m = new Mesh();
            m.name = tem.name;
            m.vertices = vertices.ToArray();
            m.normals = normals.ToArray();
            m.triangles = triangles.ToArray();
            m.RecalculateNormals();
            mf.mesh = m;
            mr.material = new Material(Shader.Find("Standard"));

            //保存网格
            if (_isSaveMesh)
            {
                AssetDatabase.CreateAsset(mf.sharedMesh, assetPath + mf.sharedMesh.name + ".asset");
                AssetDatabase.SaveAssets();
            }

            Debug.Log("Create done! " + tem.name + ": Vertex Number " + m.vertices.Length);
        }
    }

    /// <summary>
    /// 压缩网格
    /// </summary>
    /// <param name="meshName">网格名称</param>
    /// <param name="vertices">需要压缩的网格顶点数组</param>
    /// <param name="normals">与之对应的法线数组</param>
    /// <param name="triangles">与之对应的三角面数组</param>
    private void MeshCompression(string meshName, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        //移位补偿，当顶点被标记为待删除顶点时
        int offset = 0;
        //需要删除的顶点索引集合
        List<int> removes = new List<int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            EditorUtility.DisplayProgressBar("压缩网格", "正在压缩网格[ " + meshName + " ]（" + i + "/" + vertices.Count + "）......", (float)i / vertices.Count);
            if (removes.Contains(i))
            {
                offset += 1;
                continue;
            }

            triangles[i] = i - offset;
            for (int j = i + 1; j < vertices.Count; j++)
            {
                if (vertices[i] == vertices[j])
                {
                    removes.Add(j);
                    triangles[j] = triangles[i];
                }
            }
        }

        removes.Sort();
        removes.Reverse();

        for (int i = 0; i < removes.Count; i++)
        {
            vertices.RemoveAt(removes[i]);
            normals.RemoveAt(removes[i]);
        }
    }
}

public static class Extension
{
    public static bool IsStl(this string path)
    {
        string extension = Path.GetExtension(path);

        if (extension == ".stl" || extension == ".STL")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool IsOn(this string value)
    {
        return value == "On";
    }

    public static int AddGetIndex(this List<Vector3> vertices, Vector3 vec)
    {
        vertices.Add(vec);
        return vertices.Count - 1;
    }

    public static void AddNormal(this List<Vector3> normals, Vector3 vec)
    {
        normals.Add(vec);
        normals.Add(vec);
        normals.Add(vec);
    }

    public static void AddTriangle(this List<int> triangles, int vertex1, int vertex2, int vertex3)
    {
        triangles.Add(vertex1);
        triangles.Add(vertex2);
        triangles.Add(vertex3);
    }
}
