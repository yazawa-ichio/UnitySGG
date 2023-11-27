using UnityEditor;

namespace UnitySGG.Editor
{
	static class ToolMenu
	{
		[MenuItem("Assets/Create/UnitySGG/CodeAnalysisDLL")]
		static void ImportDLL()
		{
			var path = AssetDatabase.GUIDToAssetPath("1f06787d94120734ca23bed6c0c2f1cc");
			AssetDatabase.ImportPackage(path, true);
		}
	}
}