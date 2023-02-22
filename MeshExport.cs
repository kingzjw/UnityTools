#define ENABLE_DEBUG_MENU
using System;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using BuildCompression = UnityEngine.BuildCompression;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Collections;
using System.Text;


namespace JNGameBuild
{
    public class MeshExport
    {
        public static string[] GetSelectedPaths()
        {
            List<string> ret = new List<string>();
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (System.IO.File.Exists(path))
                    ret.Add(path);
            }

            return ret.ToArray();
        }

        public enum MeshType
        {
            Obj,
            Off,
        }

#if ENABLE_DEBUG_MENU
        [MenuItem("Mesh/FbxLodsToMeshOff")]
#endif
        public static void FbxLodsToMeshOff()
        {
            FbxLodsToMesh(MeshType.Off);
        }

#if ENABLE_DEBUG_MENU
        [MenuItem("Mesh/FbxLodsToMeshObj")]
#endif
        public static void FbxLodsToMeshObj()
        {
            FbxLodsToMesh(MeshType.Obj);
        }

        public static void FbxLodsToMesh(MeshType meshType)
        {
            var paths = GetSelectedPaths();
            foreach (var path in paths)
            {
                if(!path.EndsWith(".fbx"))
                {
                    return;
                }

                Debug.Log($"fj: {path}");

                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                var objlist = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach(var tempObj in objlist)
                {
                    if(tempObj is Mesh)
                    {
                        var tempMesh = (Mesh)tempObj;
                        Debug.Log($"fj: {tempMesh.name}");
                        //var objFileContent = MeshToStringForObj(tempMesh);
                        DoExport(meshType, tempMesh, path);
                    }
                }
            }
        }

        private static void DoExport(MeshType meshType, Mesh mesh, string path)
        {
            if (!mesh)
            {
                Debug.Log("mesh is null");
                return;
            }

            StartIndex = 0;
            string meshName = mesh.name;
            if(meshType == MeshType.Obj)
            {
                string fileName = Path.GetDirectoryName(path) + "/" + meshName + ".obj";
                StringBuilder meshString = new StringBuilder();
                meshString.Append("#" + meshName + ".obj"
                                  + "\n#" + System.DateTime.Now.ToLongDateString()
                                  + "\n#" + System.DateTime.Now.ToLongTimeString()
                                  + "\n#-------"
                                  + "\n\n");

                meshString.Append(MeshToStringForObj(mesh));
                WriteToFile(meshString.ToString(), fileName, meshType);
                Debug.Log("Exported Mesh: " + fileName);
            }
            else if(meshType == MeshType.Off)
            {
                string ext = ".off";
                string fileName = Path.GetDirectoryName(path) + "/" + meshName + ext;
                StringBuilder meshString = new StringBuilder();
                meshString.Append(MeshToStringForOff(mesh));
                WriteToFile(meshString.ToString(), fileName, meshType);
                Debug.Log("Exported Mesh: " + fileName);
            }
            else
            {
                Debug.LogError("mesh export error");
            }

            StartIndex = 0;
        }

        private static int StartIndex = 0;
        private static string MeshToStringForObj(Mesh mesh)
        {
            int numVertices = 0;
            Mesh m = mesh;
            if (!m)
            {
                return "####Error####";
            }
            StringBuilder sb = new StringBuilder();

            foreach (Vector3 v in m.vertices)
            {
                numVertices++;
                sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
            }
            sb.Append("\n");

            foreach (Vector3 v in m.normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
            }
            sb.Append("\n");

            foreach (Vector3 v in m.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }

            for (int subMesh = 0; subMesh < m.subMeshCount; subMesh++)
            {
                sb.Append("\n");
                int[] triangles = m.GetTriangles(subMesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {

                    //sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i] + 1 + StartIndex, triangles[i + 1] + 1 + StartIndex, triangles[i + 2] + 1 + StartIndex));
                    
                    //换一个面的顺序
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i+2] + 1 + StartIndex, triangles[i + 1] + 1 + StartIndex, triangles[i] + 1 + StartIndex));
                }
            }
            StartIndex += numVertices;
            return sb.ToString();
        }

        private static string MeshToStringForOff(Mesh mesh)
        {
            //https://blog.csdn.net/whl0071/article/details/128320021

            if(!mesh)
            {
                return "mesh is null";
            }

            int numVertices = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("OFF\n"));

            //顶点数 面数 边数
            sb.Append(string.Format("{0} {1} {2}\n", mesh.vertices.Length, mesh.triangles.Length/3, 0));

            //顶点位置：  x y z
            foreach (Vector3 v in mesh.vertices)
            {
                numVertices++;
                sb.Append(string.Format("{0} {1} {2}\n", v.x, v.y, -v.z));
            }

            //面片信息： n个顶点 顶点1的索引 顶点2的索引 … 顶点n的索引
            //off文件的顶点序号从0开始
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] triangles = mesh.GetTriangles(subMesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    //sb.Append(string.Format("3 {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i] + 1 + StartIndex, triangles[i + 1] + 1 + StartIndex, triangles[i + 2] + 1 + StartIndex));

                    //换一个面的顺序
                    sb.Append(string.Format("3 {0} {1} {2}\n", triangles[i + 2] + StartIndex, triangles[i + 1] + StartIndex, triangles[i] + StartIndex));
                }
            }
            StartIndex += numVertices;
            return sb.ToString();
        }

        private static void WriteToFile(string s, string filename, MeshType type = MeshType.Obj)
        {
            if (type == MeshType.Off)
            {
                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.ASCII))
                {
                    sw.Write(s);
                }
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    sw.Write(s);
                }
            }
        }
    }
}