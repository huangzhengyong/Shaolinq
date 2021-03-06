// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;

namespace Shaolinq.Persistence
{
	public partial interface IDataAccessModelInternal
	{
		IQueryable CreateDataAccessObjects(Type type);
		IQueryable CreateDataAccessObjects(RuntimeTypeHandle typeHandle);
		
		[RewriteAsync]
		void OnHookCreate(DataAccessObject obj);

		[RewriteAsync]
		void OnHookRead(DataAccessObject obj);

		[RewriteAsync]
		void OnHookBeforeSubmit(DataAccessModelHookSubmitContext context);

		[RewriteAsync]
		void OnHookAfterSubmit(DataAccessModelHookSubmitContext context);
	}
}
