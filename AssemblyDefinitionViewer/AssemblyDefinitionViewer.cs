using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Piggest
{
	/// <summary>
	/// Asmdefの依存関係を可視化するツール
	/// </summary>
	public class AssemblyDefinitionViewer : EditorWindow
	{
		[System.Serializable]
		class AssamblyAssetData
		{
			[SerializeField]
			public string name;

			[SerializeField]
			public string[] references;

			public AssamblyAssetData(string json)
			{
				UnityEngine.JsonUtility.FromJsonOverwrite(json, this);
			}
		}

		class AssemblyDataNode
		{
			public string name => data.name;

			public AssemblyDataNode[] referenceList;

			public bool isDraw { get; private set; }

			public Rect rect { get; private set; }

			public AssemblyDefinitionAsset textAsset { get; private set; }

			public int refCount { get; private set; } = 0;

			public float stability => (float)referenceList.Length / (refCount + referenceList.Length);

			private AssamblyAssetData data;

			public AssemblyDataNode(AssemblyDefinitionAsset textAsset)
			{
				this.textAsset = textAsset;
				this.data = new AssamblyAssetData(this.textAsset.text);
			}

			public void ResolveRefarence(AssemblyDataNode[] assemblyDatasNode)
			{
				if (data.references == null)
				{
					referenceList = new AssemblyDataNode[0];
					return;
				}

				referenceList = new AssemblyDataNode[data.references.Length];

				for (int i = 0; i < referenceList.Length; i++)
				{
					referenceList[i] = Array.Find(assemblyDatasNode, a => a.name == data.references[i]);
					if (referenceList[i] != null)
					{
						referenceList[i].refCount++;
					}
				}

				isDraw = false;
			}

			public void Draw(int nodeId, Rect rect, Action<AssemblyDataNode> windowCallback)
			{
				GUI.Window(nodeId, rect, i => windowCallback(this), name, "flow node 1");
				this.isDraw = true;
				this.rect = rect;
			}
		}

		[MenuItem("Window/AssemblyDefinitionViewer")]
		private static void Open()
		{
			GetWindow<AssemblyDefinitionViewer>();
		}

		private Vector2 scrollView;
		private Rect drawSize;

		private int drawNodeY = 0;
		private int nodeId = 0;

		//汎用Libraryとかの線を消せるように
		private List<string> ignoreNodeList = new List<string>();

		private void OnGUI()
		{
			var asmdefGuidList = AssetDatabase.FindAssets("t:asmdef");

			scrollView = GUI.BeginScrollView(
				new Rect(
					0f, 0f,
					position.width,
					position.height),
				scrollView,
				drawSize);

			AssemblyDataNode[] assemblyDatasNode = Array.ConvertAll(
				asmdefGuidList,
				guid => CreateNodeFromGuid(guid)
			);

			//リファレンスの解決
			foreach (var assemblyData in assemblyDatasNode)
			{
				assemblyData.ResolveRefarence(assemblyDatasNode);
			}

			//安定度が高い順
			Array.Sort(assemblyDatasNode, (l, r) => (int)Mathf.Sign(r.stability - l.stability));

			drawNodeY = 0;
			nodeId = 0;
			BeginWindows();

			OnDrawNode(null, assemblyDatasNode, 0);

			EndWindows();

			GUI.EndScrollView();
		}

		private void OnDrawNode(AssemblyDataNode parent, AssemblyDataNode[] assemblyDatasNode, int depth)
		{
			foreach (var assemblyData in assemblyDatasNode)
			{
				if (assemblyData == null)
				{
					continue;
				}

				if (ignoreNodeList.Find(n => n == assemblyData.name) != null)
				{
					continue;
				}

				if (!assemblyData.isDraw)
				{
					Rect rect = new Rect(
						100f + (200f + 100f) * depth,
						10f + (60f + 10f) * drawNodeY,
						200f,
						60f);

					drawSize.width = Mathf.Max(rect.xMax, drawSize.width);
					drawSize.height = Mathf.Max(rect.yMax, drawSize.height);

					assemblyData.Draw(nodeId, rect, WindowCallback);
					nodeId++;

					//線を書く
					Connect(parent, assemblyData);

					OnDrawNode(assemblyData, assemblyData.referenceList, depth + 1);
					drawNodeY++;
				}
				else
				{
					//線を書く
					Connect(parent, assemblyData);
					OnDrawNode(assemblyData, assemblyData.referenceList, depth + 1);
				}
			}
		}

		private void Connect(AssemblyDataNode node1, AssemblyDataNode node2)
		{
			if (node1 == null) return;
			if (node2 == null) return;

			var start = new Vector3(node1.rect.x + node1.rect.width, node1.rect.y + node1.rect.height / 2f, 0f);
			var startTan = start + new Vector3(100f, 0f, 0f);

			var end = new Vector3(node2.rect.x, node2.rect.y + node2.rect.height / 2f, 0f);
			var endTan = end + new Vector3(-100f, 0f, 0f);

			Handles.DrawBezier(start, end, startTan, endTan, Color.gray, null, 3f);

			float texSize = 8f;
			Texture tex = EditorGUIUtility.Load("icons/curvekeyframesemiselectedoverlay.png") as Texture2D;
			EditorGUI.DrawPreviewTexture(
				new Rect(
					node2.rect.x - texSize,
					node2.rect.y + node2.rect.height / 2f - texSize / 2f,
					texSize, texSize),
				tex);
		}

		private AssemblyDataNode CreateNodeFromGuid(string guid)
		{
			var textAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(
				AssetDatabase.GUIDToAssetPath(guid)
			);

			return new AssemblyDataNode(textAsset);
		}

		private void WindowCallback(AssemblyDataNode dataNode)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("ignore"))
			{
				ignoreNodeList.Add(dataNode.name);
			}

			if (GUILayout.Button("ping"))
			{
				Selection.activeObject = dataNode.textAsset;
				EditorGUIUtility.PingObject(dataNode.textAsset);
			}

			GUILayout.EndHorizontal();
		}

	}
}