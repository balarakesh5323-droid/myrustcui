using System;
using System.Text;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Runtime.Core.Serialization
{
    /// <summary>
    /// Generates Oxide C# code with uMod/Oxide ImageLibrary plugin integration
    /// for loading and displaying cached web images and custom PNG assets.
    /// </summary>
    public static class ImageLibraryHelper
    {
        public static string GenerateImageLibraryHook(CuiDocument doc, string pluginName = "MyCuiPlugin")
        {
            var sb = new StringBuilder();
            sb.AppendLine("// [PluginReference] Plugin ImageLibrary;");
            sb.AppendLine();
            sb.AppendLine("private void LoadImages()");
            sb.AppendLine("{");
            sb.AppendLine("    if (ImageLibrary == null || !ImageLibrary.IsLoaded)");
            sb.AppendLine("    {");
            sb.AppendLine("        PrintError(\"ImageLibrary plugin is not loaded!\");");
            sb.AppendLine("        return;");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (doc != null && doc.Elements != null)
            {
                foreach (var elem in doc.Elements)
                {
                    var raw = elem.GetComponent<CuiRawImageComponent>();
                    if (raw != null && !string.IsNullOrEmpty(raw.Url))
                    {
                        sb.AppendLine($"    ImageLibrary.Call(\"AddImage\", \"{raw.Url}\", \"{elem.Name}_icon\", 0UL);");
                    }
                }
            }

            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("private string GetImage(string imageName)");
            sb.AppendLine("{");
            sb.AppendLine("    return (string)ImageLibrary?.Call(\"GetImage\", imageName) ?? \"\";");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
