﻿using System;
using Platform.Xml.Serialization;

namespace Shaolinq.Persistence.Sql.Postgres
{
	[XmlElement]
	public class PostgresDatabaseConnectionInfo
		: DatabaseConnectionInfo
	{
		[XmlAttribute]
		public string ServerName { get; set; }

		[XmlAttribute]
		public string UserId { get; set; }

		[XmlAttribute]
		public int Port { get; set; }

		[XmlAttribute]
		public bool Pooling { get; set; }

		[XmlAttribute]
		public int MinPoolSize { get; set; }

		[XmlAttribute]
		public int MaxPoolSize { get; set; }

		[XmlAttribute]
		public string Password { get; set; }
		
		[XmlAttribute]
		public bool NativeUuids { get; set; }

		[XmlAttribute]
		public DateTimeKind DateTimeKindIfUnspecified { get; set; }

		public PostgresDatabaseConnectionInfo()
		{
			this.Port = 5432;
			this.Pooling = true;
			this.MaxPoolSize = 100;
			this.NativeUuids = true;
		}
	}
}
