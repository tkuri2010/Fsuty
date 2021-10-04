using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Detail
{
	public class SimpleYielder
	{
		private readonly int mYieldIntervalMask = 0b11111;

		private int mCount = 0;


		public SimpleYielder(int yieldIntervalLevel = 5)
		{
			if (yieldIntervalLevel < 0 || 31 < yieldIntervalLevel)
			{
				throw new System.ArgumentOutOfRangeException(nameof(yieldIntervalLevel), yieldIntervalLevel, "must br 0 <= value <= 31");
			}

			mYieldIntervalMask = (1 << yieldIntervalLevel) - 1;
		}


		public bool Countup()
		{
			var rv = ((mYieldIntervalMask != 0) && (mCount & mYieldIntervalMask) == mYieldIntervalMask);
			mCount++;
			return rv;
		}


		public System.Runtime.CompilerServices.YieldAwaitable YieldAsync() => Task.Yield();
	}
}