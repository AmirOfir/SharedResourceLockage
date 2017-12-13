using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;

namespace AmirOfir.SharedResourceTools
{
	public class SharedResourceLockage
	{
		public class LockingInfo
		{
			public LockingInfo(SemaphoreSlim item) : this()
			{
				semaphores = new List<SemaphoreSlim>();
				semaphores.Add(item);
			}
			public LockingInfo()
			{
			}

			internal List<SemaphoreSlim> semaphores { get; set; }
			public void Release()
			{
				foreach (var semaphore in semaphores)
				{
					semaphore.Release();
				}

			}
		}

		int _maxCOncurrent = 500;
		readonly IReadOnlyList<SemaphoreSlim> lockItems;

		public SharedResourceLockage(int maxCOncurrent)
		{
			_maxCOncurrent = maxCOncurrent;
			List<SemaphoreSlim> lst = new List<SemaphoreSlim>(_maxCOncurrent);
			for (int i = 0; i < _maxCOncurrent; i++)
			{
				lst.Add(new SemaphoreSlim(i));
			}
			lockItems = lst;
		}

		public async Task<LockingInfo> Lock(int index)
		{
			SemaphoreSlim semaphore = lockItems.ElementAt(index);
			await semaphore.WaitAsync().ConfigureAwait(false);
			return new LockingInfo(semaphore);
		}

		public async Task<LockingInfo> LockMultiple(params int[] indexes)
		{
			List<SemaphoreSlim> list = new List<SemaphoreSlim>();

			int mutual = indexes[0];
			for (int i = 1; i < indexes.Length; i++)
			{
				mutual ^= indexes[i];
			}
			SemaphoreSlim mutualSemaphore = lockItems.ElementAt(mutual % _maxCOncurrent);
			list.Add(mutualSemaphore);

			foreach (var index in indexes)
			{
				SemaphoreSlim semaphore = lockItems.ElementAt(index % _maxCOncurrent);
				await semaphore.WaitAsync(500).ConfigureAwait(false);
				list.Add(semaphore);
			}
			
			var lockingInfo = new LockingInfo() { semaphores = list };
			return lockingInfo;
		}

		public async Task LockIntransaction(int index)
		{
			if (Transaction.Current == null)
				throw new ApplicationException("Must be in transaction scope");
			var x = await Lock(index);
			Transaction.Current.EnlistVolatile(new SharedResourceTransactionItem(x), EnlistmentOptions.None);
		}

		class SharedResourceTransactionItem : IEnlistmentNotification
		{
			LockingInfo lockingInfo;
			public SharedResourceTransactionItem(LockingInfo lockingInfo)
			{
				this.lockingInfo = lockingInfo;
			}

			public void Commit(Enlistment enlistment)
			{
				lockingInfo.Release();
			}

			public void InDoubt(Enlistment enlistment)
			{
				lockingInfo.Release();
			}

			public void Prepare(PreparingEnlistment preparingEnlistment)
			{
				lockingInfo.Release();
			}

			public void Rollback(Enlistment enlistment)
			{
				lockingInfo.Release();
			}
		}
	}
}
