// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	/// <summary>
	/// Represents the state of the current object within the current transaction.
	/// </summary>
	[Flags]
	public enum ObjectState
	{
		/// <summary>
		/// The object is unchanged.
		/// </summary>
		Unchanged = 0,

		/// <summary>
		/// The object has changed.
		/// </summary>
		Changed = 1,

		/// <summary>
		/// The object is new.
		/// </summary>
		New = 2,

		/// <summary>
		/// The object is new and has changed.
		/// </summary>
		NewChanged = New | Changed,

		/// <summary>
		/// The object has just been commited.
		/// </summary>
		ServerSidePropertiesHydrated = 4,

		/// <summary>
		/// The object is missing some contrained foreign keys and cannot be
		/// persisted until those foreign keys have been realised.
		/// </summary>
		MissingConstrainedForeignKeys = 8,

		/// <summary>
		/// The object is missing some unconstrained foreign keys and may be
		/// persisted and will need to be updated with the foreign keys once
		/// they are realised.
		/// </summary>
		MissingUnconstrainedForeignKeys = 16,

		/// <summary>
		/// The object is missing server generated primary keys
		/// </summary>
		MissingServerGeneratedForeignPrimaryKeys = 32,

		/// <summary>
		/// The object has been inserted in the transaction but may or may not be completely fulfilled.s
		/// </summary>
		ObjectInsertedWithinTransaction = 64,

		/// <summary>
		/// The object is not a member of any transaction
		/// </summary>
		Transient = 128,

		/// <summary>
		/// The object has been deleted
		/// </summary>
		Deleted = 4096
	}
}
