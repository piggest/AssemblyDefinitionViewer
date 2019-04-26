using System;
using UnityEditorInternal;
using UnityEngine;

namespace AssemblyDefinitionViewer
{
	public class AssemblyDataNode
	{
		public string name => data.name;

		public AssemblyDataNode[] referenceList;

		public bool isDraw { get; private set; }

		public Rect rect { get; private set; }

		public AssemblyDefinitionAsset textAsset { get; private set; }

		public int refCount { get; private set; } = 0;

		public float stability => (float) referenceList.Length / (refCount + referenceList.Length);

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
}
