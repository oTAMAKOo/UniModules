
using Extensions;

namespace Modules.Devkit.AssetBundleViewer
{
	public sealed class AssetBundleInfo
	{
		//----- params -----

		//----- field -----

		private int? contentsCount = null;

		private string labelsText = null;

		private int? dependenciesCount = null;

        //----- property -----

		public int Id { get; private set; }

		public string AssetBundleName { get; set; }

		public string Group { get; set; }

		public string[] AssetGuids { get; set; }

		public string[] Labels { get; set; }

		public long FileSize { get; set; }

		public string FileName { get; set; }

		public string[] Dependencies { get; set; }

        public long LoadFileSize { get; set; }

		//----- method -----

		public AssetBundleInfo(int id)
		{
			Id = id;
		}

		public int GetAssetCount()
		{
			if (!contentsCount.HasValue)
			{
				contentsCount = AssetGuids.Length;
			}

			return contentsCount.Value;
		}

		public string GetLabelsText()
		{
			if (Labels.IsEmpty()){ return "---"; }

			if (string.IsNullOrEmpty(labelsText))
			{
				labelsText = string.Join("'", Labels);
			}

			return labelsText;
		}

		public int GetDependenciesCount()
		{
			if (!dependenciesCount.HasValue)
			{
				dependenciesCount = Dependencies.Length;
			}

			return dependenciesCount.Value;
		}
    }
}