using UnityEngine;

namespace AssemblyDefinitionViewer
{
	[System.Serializable]
	public class AssamblyAssetData
	{
		[SerializeField]
		public string name = default;

		[SerializeField]
		public string[] references = default;

		public AssamblyAssetData(string json)
		{
			UnityEngine.JsonUtility.FromJsonOverwrite(json, this);
		}
	}
}