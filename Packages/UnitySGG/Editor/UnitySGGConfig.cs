using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnitySGG.Editor
{

	[CreateAssetMenu(fileName = "UnitySGGConfig", menuName = "UnitySGG/Config")]
	class UnitySGGConfig : ScriptableObject
	{
		public string SourceAssembly;
		public string Name;
		public string OutputDir;

		string ProjectTemplate()
		{
			var path = AssetDatabase.GUIDToAssetPath("c6200179c19b8fb4cbc38378f09ea4d4");
			return File.ReadAllText(path);
		}

		string MetaTemplate()
		{
			var path = AssetDatabase.GUIDToAssetPath("681b6738364e0004199a8e00d2a2d5db");
			return File.ReadAllText(path);
		}

		public void Build()
		{
			if (string.IsNullOrEmpty(Name))
			{
				Debug.LogError("Name is empty");
				return;
			}
			var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(x => x.name == SourceAssembly);
			if (assembly == null)
			{
				Debug.LogError($"Assembly {SourceAssembly} not found");
				return;
			}
			var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(SourceAssembly);
			var assemblyData = JsonUtility.FromJson<AssemblyData>(File.ReadAllText(path));

			if (!assemblyData.overrideReferences)
			{
				Debug.LogError($"Assembly {SourceAssembly} not overrideReferences");
				return;
			}

			var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
			//var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

			var workspace = $"Library/UnitySGG/{Name}";
			if (Directory.Exists(workspace))
			{
				Directory.Delete(workspace, true);
			}

			foreach (var file in assembly.sourceFiles)
			{
				var relativePath = Path.GetRelativePath(dir, file);
				var outputPath = Path.Combine(workspace, relativePath);
				var outputDir = Path.GetDirectoryName(outputPath);
				if (!Directory.Exists(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}
				File.Copy(file, outputPath);
			}
			{

				var project = ProjectTemplate();
				StringBuilder sb = new();

				foreach (var dllName in assemblyData.precompiledReferences)
				{
					var r = assembly.compiledAssemblyReferences.FirstOrDefault(x => Path.GetFileName(x) == dllName);
					sb.AppendLine($"    <Reference Include=\"{Path.GetFileNameWithoutExtension(dllName)}\">");
					sb.AppendLine($"      <HintPath>{Path.GetRelativePath(workspace, r)}</HintPath>");
					sb.AppendLine($"    </Reference>");
				}
				project = project.Replace("{{REFERENCES}}", sb.ToString());

				File.WriteAllText(Path.Combine(workspace, $"{Name}.csproj"), project);

			}

			var args = $"build {workspace}";
			var process = System.Diagnostics.Process.Start("dotnet.exe", args);
			process.WaitForExit();

			{
				string outputDir = OutputDir;
				if (string.IsNullOrEmpty(outputDir))
				{
					outputDir = dir;
				}
				string outputPath = Path.Combine(outputDir, $"{Name}.dll");
				File.Copy(Path.Combine(workspace, "bin/Debug/netstandard2.0", $"{Name}.dll"), outputPath, true);

				if (!File.Exists(outputPath + ".meta"))
				{
					File.WriteAllText(outputPath + ".meta", MetaTemplate().Replace("{{GUID}}", GUID.Generate().ToString()));
				}


			}

			AssetDatabase.Refresh();

		}

		[System.Serializable]
		class AssemblyData
		{
			public bool overrideReferences;
			public string[] precompiledReferences;
			public bool autoReferenced;
			public string[] includePlatforms;
		}


		[CustomEditor(typeof(UnitySGGConfig))]
		class UnitySGGConfigEditor : UnityEditor.Editor
		{
			string[] m_AssemblyNames;

			private void OnEnable()
			{
				var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
				m_AssemblyNames = assemblies.Where(x => IsTargetAssembly(x)).Select(x => x.name).ToArray();
			}

			static bool IsTargetAssembly(Assembly assembly)
			{
				return assembly.compiledAssemblyReferences.Any(x => x.EndsWith("Microsoft.CodeAnalysis.dll"));
			}

			public override void OnInspectorGUI()
			{
				var obj = serializedObject;
				obj.UpdateIfRequiredOrScript();
				SerializedProperty iterator = obj.GetIterator();
				bool enterChildren = true;
				string sourceAssembly = "";
				while (iterator.NextVisible(enterChildren))
				{
					enterChildren = false;
					switch (iterator.propertyPath)
					{
						case "m_Script":
							using (new EditorGUI.DisabledScope(true))
							{
								EditorGUILayout.PropertyField(iterator, true);
							}
							continue;
						case nameof(UnitySGGConfig.SourceAssembly):
							{
								sourceAssembly = iterator.stringValue;
								var index = System.Array.IndexOf(m_AssemblyNames, iterator.stringValue);
								var ret = EditorGUILayout.Popup(iterator.displayName, index, m_AssemblyNames);
								if (index != ret)
								{
									iterator.stringValue = m_AssemblyNames[ret];
								}
							}
							break;
						case nameof(UnitySGGConfig.OutputDir):
							{
								using (new GUILayout.HorizontalScope())
								{
									EditorGUILayout.PropertyField(iterator, true);
									if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
									{
										var path = EditorUtility.OpenFolderPanel("Select Output Path", "Assets", "");
										iterator.stringValue = Path.GetRelativePath(".", path).Replace("\\", "/");
									}
								}
							}
							break;
						default:
							EditorGUILayout.PropertyField(iterator, true);
							break;
					}
				}

				if (GUILayout.Button("Build"))
				{
					(target as UnitySGGConfig).Build(); ;
				}

				obj.ApplyModifiedProperties();
			}
		}

	}


}