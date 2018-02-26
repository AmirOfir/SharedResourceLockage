# SharedResourceLockage
locking mechanism for multiple resources from several entries, and transaction support


Usage: 
int maxConcurrent = 500;
SharedResourceLockage sharedResourceLockage = new SharedResourceLockage();

private async Task DoSomething(long Id)
		{
			var x = await sharedResourceLockage.Lock((int)(Id % maxConcurrent));

			try
			{
           await BlaBla(Id);
			}
			finally
			{
				x.Release();
			}
		}
