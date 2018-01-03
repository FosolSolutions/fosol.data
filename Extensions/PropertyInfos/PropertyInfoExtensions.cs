using Fosol.Core.Extensions.Generics;
using Fosol.Data.Extensions.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Fosol.Data.Extensions.PropertyInfos
{
    public static class PropertyInfoExtensions
    {
        public static string GetColumnName(this PropertyInfo property)
        {
            return property.GetColumn()?.Name ?? property.Name;
        }

        public static IEnumerable<string> GetForeignKeyColumnNames(this PropertyInfo property)
        {
            var fkey = property.GetForeignKey();

            if (fkey == null)
                return null;

            if (fkey.Columns != null && fkey.Columns.Length != 0)
                return fkey.Columns;

            return property.PropertyType.GetPrimaryKeys().Select(p => $"{property.PropertyType.GetTableName()}_{p.GetColumnName()}");
        }

        public static IgnoreAttribute GetIgnore(this PropertyInfo property)
        {
            return property.GetCustomAttribute<IgnoreAttribute>();
        }

        public static ColumnAttribute GetColumn(this PropertyInfo property)
        {
            return property.GetCustomAttribute<ColumnAttribute>();
        }

        public static PrimaryKeyAttribute GetPrimaryKey(this PropertyInfo property)
        {
            return property.GetCustomAttribute<PrimaryKeyAttribute>();
        }

        public static ForeignKeyAttribute GetForeignKey(this PropertyInfo property)
        {
            return property.GetCustomAttribute<ForeignKeyAttribute>();
        }

        public static IndexAttribute GetIndex(this PropertyInfo property)
        {
            return property.GetCustomAttribute<IndexAttribute>();
        }

        public static IdentityAttribute GetIdentity(this PropertyInfo property)
        {
            return property.GetCustomAttribute<IdentityAttribute>();
        }

        public static RowVersionAttribute GetRowVersion(this PropertyInfo property)
        {
            return property.GetCustomAttribute<RowVersionAttribute>();
        }

        public static ComputedAttribute GetComputed(this PropertyInfo property)
        {
            return property.GetCustomAttributes<ComputedAttribute>().FirstOrDefault(c => typeof(ComputedAttribute).IsAssignableFrom(c.GetType()));
        }

        public static RequiredAttribute GetRequired(this PropertyInfo property)
        {
            return property.GetCustomAttribute<RequiredAttribute>();
        }

        public static MinLengthAttribute GetMinLength(this PropertyInfo property)
        {
            return property.GetCustomAttribute<MinLengthAttribute>();
        }

        public static MaxLengthAttribute GetMaxLength(this PropertyInfo property)
        {
            return property.GetCustomAttribute<MaxLengthAttribute>();
        }

        public static DefaultValueAttribute GetDefaultValue(this PropertyInfo property)
        {
            return property.GetCustomAttribute<DefaultValueAttribute>();
        }

        public static bool IsNullable(this PropertyInfo property)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        internal static string GetDataTypeForAction(this PropertyInfo property)
        {
            if (property.GetRowVersion() != null)
                return "BINARY(8)";

            return property.GetDataType();
        }

        public static string GetDataType(this PropertyInfo property)
        {
            var cattr = property.GetColumn();
            var maxattr = property.GetMaxLength();

            if (!String.IsNullOrWhiteSpace(cattr?.DataType))
            {
                return cattr.DataType;
            }

            switch (Type.GetTypeCode(property.PropertyType))
            {
                case (TypeCode.Boolean):
                    return "BIT";
                case (TypeCode.Byte):
                    return "BINARY";
                case (TypeCode.Char):
                    return "CHAR";
                case (TypeCode.DateTime):
                    return "DATETIME2";
                case (TypeCode.Decimal):
                    return "DECIMAL";
                case (TypeCode.Double):
                    return "FLOAT";
                case (TypeCode.Int16):
                    return "SMALLINT";
                case (TypeCode.Int32):
                    return "INT";
                case (TypeCode.Int64):
                    return "BIGINT";
                case (TypeCode.Single):
                    return "REAL";
                case (TypeCode.UInt16):
                case (TypeCode.UInt32):
                case (TypeCode.UInt64):
                case (TypeCode.Object):
                    if (property.PropertyType == typeof(Guid))
                    {
                        return "UNIQUEIDENTIFIER";
                    }
                    else if (property.PropertyType == typeof(byte[]))
                    {
                        if (property.GetRowVersion() != null)
                        {
                            return "ROWVERSION";
                        }
                        if (maxattr != null)
                        {
                            return $"VARBINARY({maxattr.Length})";
                        }

                        return "VARBINARY";
                    }
                    else if (property.PropertyType == typeof(TimeSpan))
                    {
                        return "TIME";
                    }
                    else if (property.PropertyType == typeof(System.Xml.XmlDocument))
                    {
                        return "XML";
                    }
                    return "BINARY";
                case (TypeCode.SByte):
                case (TypeCode.String):
                    if (maxattr != null)
                    {
                        return $"NVARCHAR({maxattr.Length})";
                    }
                    return "NVARCHAR(MAX)";
                default:
                    return "NVARCHAR(MAX)";
            }
        }
    }
}
