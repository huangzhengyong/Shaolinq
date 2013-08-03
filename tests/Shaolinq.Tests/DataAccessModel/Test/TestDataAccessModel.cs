﻿namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessModel]
	public abstract class TestDataAccessModel
		: BaseDataAccessModel
	{
		[DataAccessObjects]
		public abstract DataAccessObjects<Club> Club { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Paper> Papers { get; }
		
		[DataAccessObjects]
		public abstract DataAccessObjects<School> Schools { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Product> Products { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Student> Students { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Lecture> Lectures { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Lecturer> Lecturers { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Fraternity> Fraternity { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithCompositePrimaryKey> ObjectWithCompositePrimaryKeys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithGuidAutoIncrementPrimaryKey> ObjectWithGuidAutoIncrementPrimaryKeys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithGuidNonAutoIncrementPrimaryKey> ObjectWithGuidNonAutoIncrementPrimaryKeys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithLongAutoIncrementPrimaryKey> ObjectWithLongAutoIncrementPrimaryKeys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithLongNonAutoIncrementPrimaryKey> ObjectWithLongNonAutoIncrementPrimaryKeys { get; }
	}
}
