using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Text.Std
{
	public class LargeFileLinesProcessorSettings
	{
		public static LargeFileLinesProcessorSettings Default => DefaultSettings.Instance;

		public class DefaultSettings : LargeFileLinesProcessorSettings
		{
			public static readonly DefaultSettings Instance = new DefaultSettings();
		}


		/// <summary>
		/// ファイルを "大まかには" 何バイト毎に分割するか
		/// </summary>
		/// <value></value>
		public long RoughChunkSize { get; set; } = 1 * 1024 * 1024;


		/// <summary>
		/// 分割されたタスクを実際にスレッドに割り当てるファクトリ。
		/// StartNew() メソッドは、スケジューリングのみ行い直ちにリターンする事が望ましい。
		/// </summary>
		/// <returns></returns>
		public TaskFactory TaskFactory { get; set; } = Task.Factory;
	}
}