using System.Text;

namespace D64Reader.Renderers
{
    public class D64StringRenderer : ID64Renderer<string>
    {
        public string Render(D64Directory directory)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"0 \"{directory.DiskName}\" {directory.DiskId}");

            foreach (var item in directory.DirectoryItems)
            {
                var entry = (item.Blocks.ToString().PadRight(5) + $"\"{item.Name}\"").PadRight(24) + $"{item.Type}";
                sb.AppendLine(entry);
            }

            sb.AppendLine($"{directory.FreeBlocks} BLOCKS FREE.");

            return sb.ToString();
        }
    }
}