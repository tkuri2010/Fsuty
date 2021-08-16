#define ENABLE_GREATGRANDPARENT

using System;
using System.Collections;
using System.Collections.Generic;

namespace Tkuri2010.Fsuty
{
	public class LinkedCollection<E> : IReadOnlyList<E>
	{
		internal class Payload
		{
			internal Payload? Parent = null;

#if ENABLE_GREATGRANDPARENT
			internal Payload? GreatGrandParent = null;
#endif

			internal E Value;

			internal Payload(E value, Payload? parent)
			{
				Value = value;
				Parent = parent;
#if ENABLE_GREATGRANDPARENT
				GreatGrandParent = parent?.Parent?.Parent;
#endif
			}


			/// <summary>
			/// FindAncestor(0) == this,
			/// FindAncestor(1) == this.Parent (meybe null)
			/// FindAncestor(int.MaxValue) == maybe null
			/// </summary>
			internal Payload? FindAncestor(int generation)
			{
				if (generation <= 0)
				{
					return this;
				}

				var rv = Parent;
				generation--;

#if ENABLE_GREATGRANDPARENT
				while (3 <= generation && rv is not null)
				{
					rv = rv.GreatGrandParent;
					generation -= 3;
				}
#endif
				while (1 <= generation && rv is not null)
				{
					rv = rv.Parent;
					generation--;
				}

				return rv;
			}


			internal Payload FindAncestorX(int generation)
			{
				var rv = FindAncestor(generation);
				if (rv is null)
				{
					throw new Exception($"{this.GetType().FullName}: FindAncestor({generation}) returns null. Counting elements error?");
				}
				else
				{
					return rv;
				}
			}
		}


		Payload? mPayload = null;


		public int Count { get; private set; } = 0;


		/// <summary>
		/// Gets the element at the specified index in the read-only list.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get.</param>
		/// <returns>The element at the specified index in the read-only list.</returns>
		public E this[int index]
		{
			get
			{
				if (mPayload is null || index < 0 || Count <= index)
				{
					throw new IndexOutOfRangeException($"{this.GetType().FullName}: this[index]: must be 0 <= index({index}) < Count({Count})");
				}

				return mPayload.FindAncestorX(Count - index - 1).Value;
			}
		}


		public LinkedCollection()
		{
		}


		public LinkedCollection(IEnumerable<E> elements)
		{
			foreach (var e in elements)
			{
				mPayload = new Payload(e, mPayload);
				Count++;
			}
		}


		public LinkedCollection<E> AppendElement(E element)
		{
			var newOne = new LinkedCollection<E>();
			newOne.mPayload = new Payload(element, mPayload);
			newOne.Count = this.Count + 1;
			return newOne;
		}


		public LinkedCollection<E> AppendRange(IEnumerable<E> elements)
		{
			var newOne = new LinkedCollection<E>();
			newOne.mPayload = this.mPayload;
			newOne.Count = this.Count;
			foreach (var e in elements)
			{
				newOne.mPayload = new Payload(e, newOne.mPayload);
				newOne.Count++;
			}
			return newOne;
		}


		public LinkedCollection<E> Slice(int skip, int take)
		{
			if (mPayload is null || Count == 0 || (skip <= 0 && Count <= take))
			{
				return this;
			}

			var newOne = new LinkedCollection<E>();
			if (Count <= skip || take <= 0)
			{
				return newOne; // empty tree
			}

			newOne.Count = Count - Math.Max(0, skip);
			newOne.mPayload = mPayload.FindAncestor(newOne.Count - take);
			newOne.Count = Math.Min(newOne.Count, take);

			return newOne;
		}


		public LinkedCollection<E> SkipItems(int n) => Slice(n, Count - n);


		public LinkedCollection<E> TakeItems(int n) => Slice(0, n);


		#region enumeration

		class EmptyEnumerator : IEnumerator<E>
		{
			public E Current => throw new InvalidOperationException("empty collection");

			object IEnumerator.Current => throw new InvalidOperationException("empty collection");

			public void Dispose() { }

			public bool MoveNext() => false;

			public void Reset() { }
		}


		class Enumerator : IEnumerator<E>
		{
			private Payload mOriginPayload;

			private Payload mCurrentPayload;

			private int mTotalCount;

			private int mLeftCount;


			public E Current => mCurrentPayload.Value;


			object? IEnumerator.Current => mCurrentPayload.Value;
			// https://github.com/dotnet/roslyn/issues/31867


			internal Enumerator(Payload payload, int count)
			{
				mOriginPayload = mCurrentPayload = payload;
				mTotalCount = mLeftCount = count;
			}


			public void Dispose()
			{
			}


			public bool MoveNext()
			{
				if (mLeftCount <= 0)
				{
					return false;
				}
				else
				{
					mCurrentPayload = mOriginPayload.FindAncestorX(--mLeftCount);
					return true;
				}
			}


			public void Reset()
			{
				mLeftCount = mTotalCount;
			}
		}


		public IEnumerator<E> GetEnumerator()
		{
			return (mPayload is null) ? new EmptyEnumerator() : new Enumerator(mPayload, Count);
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return (mPayload is null) ? new EmptyEnumerator() : new Enumerator(mPayload, Count);
		}

		#endregion
	}
}
