// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	[Flags]
	public enum IndexType
	{
		Unique,
		Hash,
		BTree,
		RTree
	}
}
