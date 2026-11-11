
using System.Collections.Generic;
using Extensions;

namespace Modules.UI.ScreenRotation
{
	public enum RotateType
	{
		None = 0,
		DegreeMinus90,
		Degree90,
		Reverse,
	}

    public sealed class RotationManager : Singleton<RotationManager>
    {
        //----- params -----

        //----- field -----

		private RotateType rotateType = RotateType.None;

		private List<RotationRoot> targets = null;

        //----- property -----
		
		protected override void OnCreate()
		{
			targets = new List<RotationRoot>();
		}

		public RotateType RotateType
		{
			get { return rotateType; }

			set
			{
				rotateType = value;

				Apply();
			}
		}

        //----- method -----

		public void Add(RotationRoot rotationRoot)
		{
			if (!targets.Contains(rotationRoot))
			{
				targets.Add(rotationRoot);
			}
		}

		public void Remove(RotationRoot rotationRoot)
		{
			if (targets.Contains(rotationRoot))
			{
				targets.Remove(rotationRoot);
			}
		}

		private void Apply()
		{
			foreach (var target in targets)
			{
				target.RotateType = rotateType;
			}
		}
    }
}