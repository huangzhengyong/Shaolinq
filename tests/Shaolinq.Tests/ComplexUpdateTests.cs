﻿// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.ComplexPrimaryKeyModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("SqlServer")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class ComplexUpdateTests
		: BaseTests<ComplexPrimaryKeyDataAccessModel>
	{
		public class DataModelHook : DataAccessModelHookBase
		{
			public int UpdateCount { get; set; }
		
			public override void AfterSubmit(DataAccessModelHookSubmitContext context)
			{
				this.UpdateCount += context.Updated.Count();

				base.AfterSubmit(context);
			}
		}

		private readonly DataModelHook hook = new DataModelHook();

		public ComplexUpdateTests(string providerName)
			: base(providerName)
		{
			this.hook = new DataModelHook();

			this.model.AddHook(this.hook);
		}

		[Test]
		public void Test_Set_Object_Property_To_Null()
		{
			long regionId;
			long addressId;
			
			using (var scope = this.NewTransactionScope())
			{
				var address = this.model.Addresses.Create();

				address.Region = this.model.Regions.Create();
				address.Region.Name = "RegionName";
				address.Region2 = this.model.Regions.Create();
				address.Region2.Name = "RegionName2";

				this.model.Flush();

				addressId = address.Id;
				regionId = address.Region.Id;

				var addresses = this.model.Addresses.ToList();

				scope.Complete();
			}

			var addresses1 = this.model.Addresses.ToList();

			using (var scope = this.NewTransactionScope())
			{
				var addresses = this.model.Addresses.ToList();

				var address = this.model.Addresses.GetByPrimaryKey(this.model.Addresses.GetReference(new { Id = addressId, Region = this.model.Regions.GetReference(new { Id = regionId, Name = "RegionName"})}));

				address.Region = null;

				var changedProperties = address.GetChangedProperties();
				var changedPropertiesFlattened = address.GetAdvanced().GetChangedPropertiesFlattened();

				Assert.AreEqual(1, changedProperties.Count);
				Assert.AreEqual(this.model.TypeDescriptorProvider.GetTypeDescriptor(typeof(Region)).PrimaryKeyCount, changedPropertiesFlattened.Count);
			}

			addresses1 = this.model.Addresses.ToList();

			var oldCount = hook.UpdateCount;

			using (var scope = this.NewTransactionScope())
			{
				var addresses = this.model.Addresses.ToList();

				var address = this.model.Addresses.GetByPrimaryKey(this.model.Addresses.GetReference(new { Id = addressId, Region = this.model.Regions.GetReference(new { Id = regionId, Name = "RegionName" }) }));

				Assert.IsNotNull(address.Region2);
				address.Region2 = null;

				var changedProperties = address.GetChangedProperties();
				var changedPropertiesFlattened = address.GetAdvanced().GetChangedPropertiesFlattened();

				Assert.AreEqual(1, changedProperties.Count);
				Assert.AreEqual(this.model.TypeDescriptorProvider.GetTypeDescriptor(typeof(Region)).PrimaryKeyCount, changedPropertiesFlattened.Count);

				scope.Complete();
			}

			Assert.AreEqual(oldCount + 1, hook.UpdateCount);

			using (var scope = this.NewTransactionScope())
			{
				var address = this.model.Addresses.GetByPrimaryKey(this.model.Addresses.GetReference(new { Id = addressId, Region = this.model.Regions.GetReference(new { Id = regionId, Name = "RegionName" }) }));

				Assert.IsNull(address.Region2);
			}
		}

		[Test]
		public void Test_Create_Object_With_Incomplete_Complex_Primary_Key()
		{
			Assert.Throws<MissingOrInvalidPrimaryKeyException>(() =>
			{
				try
				{
					using (var scope = this.NewTransactionScope())
					{
						var address = this.model.Addresses.Create();

						address.Region = this.model.Regions.Create();
						address.Region2 = address.Region;
						address.Region2 = null;

						var changedProperties = address.GetChangedProperties();
					
						Assert.IsTrue(address.GetAdvanced().IsMissingAnyDirectOrIndirectServerSideGeneratedPrimaryKeys);
						Assert.IsFalse(address.GetAdvanced().PrimaryKeyIsCommitReady);
						Assert.AreEqual(2, changedProperties.Count);

						scope.Complete();
					}
				}
				catch (TransactionAbortedException e)
				{
					throw e.InnerException;
				}
			});
		}

		[Test]
		public void Test_Create_Incomplete_Objects_Then_Delete()
		{
			using (var scope = this.NewTransactionScope())
			{
				var shop = this.model.Shops.Create();
				var address = this.model.Addresses.Create();
				var region = this.model.Regions.Create();

				address.Delete();
				shop.Delete();
				region.Delete();
				
				scope.Complete();
			}
		}

		[Test]
		public void Test_Nested_Scope_Update()
		{
			var e = new ManualResetEvent(false);

			var task = this.Test_Nested_Scope_Update_Async(e).ContinueOnAnyContext();

			if (task.GetAwaiter().IsCompleted)
			{
				this.Test_Set_Object_Property_To_Null();

				return;
			}

			while (!task.GetAwaiter().IsCompleted)
			{
				e.WaitOne(TimeSpan.FromSeconds(1));
			}

			Assert.IsTrue(task.GetAwaiter().IsCompleted);

			task.GetAwaiter().GetResult();

			this.Test_Set_Object_Property_To_Null();
		}

		private async Task Test_Nested_Scope_Update_Async(ManualResetEvent e)
		{
			Guid id;
			var methodName = MethodBase.GetCurrentMethod().Name;

			using (var scope = new DataAccessScope())
			{
				var child = this.model.Children.Create();

				await scope.FlushAsync().ContinueOnAnyContext();

				id = child.Id;

				using (var inner = new DataAccessScope())
				{
					child.Nickname = methodName;

					await inner.CompleteAsync().ContinueOnAnyContext();
				}

				await scope.FlushAsync();
				
				Assert.AreEqual(child.Id, this.model.Children.Single(c => c.Nickname == methodName).Id);

				await scope.CompleteAsync().ContinueOnAnyContext();
			}
			
			Assert.AreEqual(id, this.model.Children.Single(c => c.Nickname == methodName).Id);

			e.Set();
		}

		[Test, Category("IgnoreOnMono" /* No support for AsyncFlowOption */)]
		public void Test_Nested_Scope_Update2()
		{
			if (typeof(Transaction).Assembly.GetType("System.Transactions.TransactionScopeAsyncFlowOption") == null)
			{
				return;
			}

			var e = new ManualResetEvent(false);

			var task = this.Test_Nested_Scope_Update_Async2(e).ContinueOnAnyContext();

			if (task.GetAwaiter().IsCompleted)
			{
				this.Test_Set_Object_Property_To_Null();

				return;
			}

			while (!task.GetAwaiter().IsCompleted)
			{
				e.WaitOne(TimeSpan.FromSeconds(1));
			}

			Assert.IsTrue(task.GetAwaiter().IsCompleted);

			this.Test_Set_Object_Property_To_Null();
		}

		private async Task Test_Nested_Scope_Update_Async2(ManualResetEvent e)
		{
			Guid id;
			var methodName = MethodBase.GetCurrentMethod().Name;

			using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			{
				var child = this.model.Children.Create();

				await scope.FlushAsync().ContinueOnAnyContext();

				id = child.Id;

				using (var inner = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
				{
					child.Nickname = methodName;

					inner.Complete();
				}

				await scope.FlushAsync().ContinueOnAnyContext();

				Assert.AreEqual(child.Id, this.model.Children.Single(c => c.Nickname == methodName).Id);

				scope.Complete();
			}

			Assert.AreEqual(id, this.model.Children.Single(c => c.Nickname == methodName).Id);

			e.Set();
		}

		[Test]
		public void Test_Nested_Scope_Abort1()
		{
			Assert.Throws(Is.InstanceOf<TransactionAbortedException>().Or.InstanceOf<DataAccessTransactionAbortedException>(), () =>
			{
				var methodName = MethodBase.GetCurrentMethod().Name;

				using (var scope = this.NewTransactionScope())
				{
					var child = this.model.Children.Create();

					scope.Flush();

					using (var inner = this.NewTransactionScope())
					{
						child.Nickname = methodName;
					}

					scope.Flush();

					Assert.AreEqual(child.Id, this.model.Children.Single(c => c.Nickname == methodName).Id);

					scope.Complete();
				}
			});

			Assert.Throws(Is.InstanceOf<TransactionAbortedException>().Or.InstanceOf<DataAccessTransactionAbortedException>(), () =>
			{
				var methodName = MethodBase.GetCurrentMethod().Name;

				using (var scope = new DataAccessScope())
				{
					var child = this.model.Children.Create();

					scope.Flush();

					using (var inner = new DataAccessScope())
					{
						child.Nickname = methodName;
					}

					scope.Flush();

					Assert.AreEqual(child.Id, this.model.Children.Single(c => c.Nickname == methodName).Id);

					scope.Complete();
				}
			});

		}

		[Test]
		public void Test_Nested_Scope_Abort2()
		{
			Assert.Throws(Is.InstanceOf<TransactionAbortedException>().Or.InstanceOf<DataAccessTransactionAbortedException>(), () =>
			{
				var methodName = MethodBase.GetCurrentMethod().Name;

				using (var scope = new DataAccessScope())
				{
					var child = this.model.Children.Create();

					scope.Flush();

					using (var inner = new DataAccessScope())
					{
						child.Nickname = methodName;
					}

					scope.Flush();

					Assert.AreEqual(child.Id, this.model.Children.Single(c => c.Nickname == methodName).Id);

					scope.Complete();
				}
			});
		}
	}
}
