﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Platform;
using Platform.Reflection;
using Platform.Validation;

namespace Shaolinq.Persistence
{
	public class PropertyDescriptor
	{
		public TypeDescriptor DeclaringTypeDescriptor
		{
			get;
			private set;
		}

		public Type OwnerType
		{
			get;
			private set;
		}

		public PropertyInfo PropertyInfo
		{
			get;
			private set;
		}

		public string PropertyName
		{
			get
			{
				return this.PropertyInfo.Name;
			}
		}

		public UniqueAttribute UniqueAttribute
		{
			get;
			private set;
		}

		public bool HasUniqueAttribute
		{
			get
			{
				return this.UniqueAttribute != null;
			}
		}

		public PersistedMemberAttribute PersistedMemberAttribute
		{
			get;
			private set;
		}

		public BackReferenceAttribute BackReferenceAttribute
		{
			get;
			private set;
		}

		public ReadOnlyCollection<IndexAttribute> IndexAttributes
		{
			get;
			private set;
		}

		public ReferencedObjectPrimaryKeyPropertyAttribute ReferencedObjectPrimaryKeyPropertyAttribute
		{
			get;
			private set;
		}

		public PropertyDescriptor ReferencedObjectPrimaryKeyPropertyDescriptor
		{
			get
			{
				var typeDescriptor = TypeDescriptorProvider.GetProvider(referencedObjectPrimaryKeyPropertyInfo.ReflectedType.Assembly).GetTypeDescriptor(referencedObjectPrimaryKeyPropertyInfo.ReflectedType);

				return typeDescriptor.GetPropertyDescriptorByPropertyName(referencedObjectPrimaryKeyPropertyInfo.Name);
			}
		}
		internal PropertyInfo referencedObjectPrimaryKeyPropertyInfo;

		public RelatedDataAccessObjectsAttribute RelatedDataAccessObjectsAttribute
		{
			get;
			private set;
		}

		public AutoIncrementAttribute AutoIncrementAttribute
		{
			get;
			private set;
		}

		public bool IsReferencedObjectPrimaryKeyProperty
		{
			get
			{
				return this.ReferencedObjectPrimaryKeyPropertyAttribute != null;
			}
		}

		public bool IsRelatedDataAccessObjectsProperty
		{
			get
			{
				return this.RelatedDataAccessObjectsAttribute != null;
			}
		}

		public bool IsBackReferenceProperty
		{
			get
			{
				return this.BackReferenceAttribute != null;
			}
		}
        
		public bool IsAutoIncrement
		{
			get
			{
				return this.AutoIncrementAttribute != null
					&& this.AutoIncrementAttribute.AutoIncrement;
			}
		}

		public bool IsPrimaryKey
		{
			get;
			private set;
		}

		public string PersistedName
		{
			get;
			private set;
		}

		public string PersistedShortName
		{
			get;
			private set;
		}
		
		public Type PropertyType
		{
			get
			{
				return this.PropertyInfo.PropertyType;
			}
		}

		public bool IsComputedTextMember
		{
			get
			{
				return this.ComputedTextMemberAttribute != null;
			}
		}

		public ComputedTextMemberAttribute ComputedTextMemberAttribute
		{
			get;
			private set;
		}

		public PropertyDescriptor(TypeDescriptor declaringTypeDescriptor, Type ownerType, PropertyInfo propertyInfo)
		{
			this.OwnerType = ownerType;
			this.PropertyInfo = propertyInfo;
			this.DeclaringTypeDescriptor = declaringTypeDescriptor;

			this.BackReferenceAttribute = propertyInfo.GetFirstCustomAttribute<BackReferenceAttribute>(true);
			this.RelatedDataAccessObjectsAttribute = propertyInfo.GetFirstCustomAttribute<RelatedDataAccessObjectsAttribute>(true);
			this.PersistedMemberAttribute = propertyInfo.GetFirstCustomAttribute<PersistedMemberAttribute>(true);
			this.ReferencedObjectPrimaryKeyPropertyAttribute = propertyInfo.GetFirstCustomAttribute<ReferencedObjectPrimaryKeyPropertyAttribute>(true);
			this.ComputedTextMemberAttribute = propertyInfo.GetFirstCustomAttribute<ComputedTextMemberAttribute>(true);

			if (this.PropertyType.IsIntegerType() || this.PropertyType == typeof(Guid))
			{
				this.AutoIncrementAttribute = propertyInfo.GetFirstCustomAttribute<AutoIncrementAttribute>(true);
			}

			var attribute = this.PropertyInfo.GetFirstCustomAttribute<PrimaryKeyAttribute>(true);

			this.IsPrimaryKey = attribute != null && attribute.IsPrimaryKey;

			if (this.PersistedMemberAttribute != null)
			{
				this.PersistedName = this.PersistedMemberAttribute.GetName(this.PropertyInfo);
				this.PersistedShortName = this.PersistedMemberAttribute.GetShortName(this.PropertyInfo);
			}
			else if (this.BackReferenceAttribute != null)
			{
				this.PersistedName = propertyInfo.Name;
				this.PersistedShortName = propertyInfo.Name;
			}
			else if (this.RelatedDataAccessObjectsAttribute != null)
			{
				this.PersistedName = propertyInfo.Name;
				this.PersistedShortName = propertyInfo.Name;
			}

			var indexAttributes = new List<IndexAttribute>();

			foreach (IndexAttribute indexAttribute in this.PropertyInfo.GetCustomAttributes(typeof(IndexAttribute), true))
			{
				if (indexAttribute.IndexName == null)
				{
					if (indexAttribute.LowercaseIndex)
					{
						indexAttribute.IndexName = this.PersistedName + "ToLower";
					}
					else
					{
						indexAttribute.IndexName = this.PersistedName;
					}
				}

				indexAttributes.Add(indexAttribute);
			}

			this.IndexAttributes = new ReadOnlyCollection<IndexAttribute>(indexAttributes);
            
			this.UniqueAttribute = this.PropertyInfo.GetFirstCustomAttribute<UniqueAttribute>(true);
		}
	}
}