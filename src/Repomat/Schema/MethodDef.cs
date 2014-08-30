using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Repomat.Schema
{
    internal class MethodDef
    {
        private readonly MethodInfo _methodInfo;
        private readonly string _methodName;
        private readonly IReadOnlyList<ParameterDetails> _parameters;
        private readonly IReadOnlyList<ParameterDetails> _properties;
        private readonly Type _returnType;
        
        private readonly bool _isTryGet;
        private readonly bool _isSingleton;
        private readonly ParameterDetails _outParameterOrNull;
        private readonly ParameterDetails _connectionOrTransactionOrNull;
        private readonly ParameterDetails _dtoParameterOrNull;
        private readonly bool _returnsInt;
        private string _customSqlOrNull;
        private bool _customSqlIsStoredProcedure = false;

        private SingletonGetMethodBehavior _singletonGetMethodBehavior;
        private EntityDef _entityDefOrUndefined;

        private MethodType? _methodTypeOverrideOrNull = null;

        public MethodDef(MethodInfo methodInfo, EntityDef entityDef) : this(methodInfo, entityDef, methodInfo.Name, null)
        {
        }

        internal MethodDef(MethodInfo methodInfo, EntityDef entityDef, string methodName, string customSqlOrNull)
        {
            _methodInfo = methodInfo;
            _entityDefOrUndefined = entityDef;
            _methodName = methodName;
            _parameters = GetParameters(methodInfo).ToList();
            _properties = GetProperties();
            _returnType = methodInfo.ReturnType;
            _outParameterOrNull = _parameters.FirstOrDefault(p => p.IsOut);
            _isTryGet = methodInfo.ReturnType == typeof (bool) && _outParameterOrNull != null;
            _connectionOrTransactionOrNull = _parameters.FirstOrDefault(p => p.IsTransaction || p.IsConnection);
            _dtoParameterOrNull = _parameters.FirstOrDefault(p => p.IsSimpleArgument && !p.IsPrimitiveType && !p.IsTransaction && !p.IsConnection);
            _isSingleton = _isTryGet || ReturnType.GetInterfaces().All(i => !i.Name.StartsWith("IEnumerable"));
            _returnsInt = methodInfo.ReturnType == typeof(int);
            _singletonGetMethodBehavior = SingletonGetMethodBehavior.Strict;
            _customSqlOrNull = customSqlOrNull;
        }

        internal MethodDef CloneWithNewName(string newName)
        {
            if (_entityDefOrUndefined == null)
            {
                throw new RepomatException("Can't clone a method to a new name unless the entity type is defined");
            }
            return new MethodDef(_methodInfo, _entityDefOrUndefined, newName, null);
        }

        internal MethodDef CloneToCustomQuery(string sql)
        {
            if (_entityDefOrUndefined == null)
            {
                throw new RepomatException("Can't clone a method to a new name unless the entity type is defined");
            }
            return new MethodDef(_methodInfo, _entityDefOrUndefined, _methodInfo.Name, sql);
        }

        internal EntityDef EntityDef
        {
            get
            {
                if (MethodType != MethodType.Custom && _entityDefOrUndefined == null)
                {
                    throw new RepomatException("Can't clone a method to a new name unless the entity type is defined");
                }
                return _entityDefOrUndefined;
            }
            set
            {
                _entityDefOrUndefined = value;
            }
        }

        public string MethodName { get { return _methodName; } }
        public IReadOnlyList<ParameterDetails> Parameters { get { return _parameters; } }
        public Type ReturnType { get { return _returnType; } }
        public bool IsTryGet { get { return _isTryGet; } }
        public ParameterDetails OutParameterOrNull { get { return _outParameterOrNull; } }
        public ParameterDetails ConnectionOrTransactionOrNull { get { return _connectionOrTransactionOrNull; } }
        public ParameterDetails DtoParameterOrNull { get { return _dtoParameterOrNull; } }
        public bool IsSingleton { get { return _isSingleton; } }
        public IEnumerable<ParameterDetails> Properties { get { return _properties; } }
        public bool ReturnsInt { get { return _returnsInt; } }

        private IEnumerable<ParameterDetails> GetParameters(MethodInfo methodInfo)
        {
            foreach (var parm in methodInfo.GetParameters())
            {
                yield return new ParameterDetails(parm);
            }
        }

        internal MethodInfo MethodInfo { get { return _methodInfo; } }

        public string TableName
        {
            get { return _entityDefOrUndefined.TableName; }
            set { _entityDefOrUndefined.TableName = value; }
        }

        public string CustomSqlOrNull 
        {
            get { return _customSqlOrNull; }
            set { _customSqlOrNull = value; }
        }

        public bool CustomSqlIsStoredProcedure
        {
            get { return _customSqlIsStoredProcedure; }
            set { _customSqlIsStoredProcedure = value; }
        }

        public SingletonGetMethodBehavior SingletonGetMethodBehavior
        {
            get { return _singletonGetMethodBehavior; }
            set { _singletonGetMethodBehavior = value; }
        }

        private IReadOnlyList<ParameterDetails> GetProperties()
        {
            return _parameters.Where(p => p.IsSimpleArgument && p.IsPrimitiveType).ToList();
        }

        public override string ToString()
        {
            return string.Format("public {0} {1}", _returnType.ToCSharp(), NameAndArgumentList);
        }

        public string NameAndArgumentList
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("{0}(", _methodName);

                List<String> args = new List<string>();
                foreach (var parm in _parameters)
                {
                    args.Add(parm.ToString());
                }

                builder.Append(string.Join(", ", args));

                builder.Append(")");
                return builder.ToString();
            }
        }

        public bool IsScalarQuery
        {
            get { return !IsTryGet && ReturnType.IsDatabaseType(); }
        }

        internal static MethodType GetMethodTypeFromName(string methodName)
        {
            if (methodName.StartsWith("CreateTable"))
            {
                return MethodType.CreateTable;
            }
            else if (methodName.StartsWith("DropTable"))
            {
                return MethodType.DropTable;
            }
            else if (methodName.StartsWith("TableExists"))
            {
                return MethodType.TableExists;
            }
            else if (methodName.StartsWith("GetCount"))
            {
                return MethodType.GetCount;
            }
            else if (methodName.Contains("Exists"))
            {
                return MethodType.Exists;
            }
            else if (methodName.StartsWith("Get") || methodName.StartsWith("TryGet") || methodName.StartsWith("Find") || methodName.StartsWith("TryFind"))
            {
                return MethodType.Get;
            }
            else if (methodName.StartsWith("Insert"))
            {
                return MethodType.Insert;
            }
            else if (methodName.StartsWith("Delete"))
            {
                return MethodType.Delete;
            }
            else if (methodName.StartsWith("Update"))
            {
                return MethodType.Update;
            }
            else if (methodName.StartsWith("Upsert"))
            {
                return MethodType.Upsert;
            }
            else if (methodName.StartsWith("Create"))
            {
                return MethodType.Create;
            }

            // For now, we'll assume it's Custom. Later, come validation time, if the custom SQL
            // isn't set, then we should fail.
            return MethodType.Custom;
        }

        internal static bool MethodIsPrimaryKeyGet(MethodInfo methodInfo, Type entityType)
        {
            if (methodInfo.Name.StartsWith("TryGet"))
            {
                return true;
            }
            else if (GetMethodTypeFromName(methodInfo.Name) == MethodType.Get && methodInfo.ReturnType == entityType)
            {
                return true;
            }

            return false;
        }

        public MethodType MethodType
        {
            get
            {
                if (_methodTypeOverrideOrNull != null)
                {
                    return _methodTypeOverrideOrNull.Value;
                }

                if (_customSqlOrNull != null)
                {
                    return MethodType.Custom;
                }
                else
                {
                    return GetMethodTypeFromName(_methodName);
                }
            }
            internal set
            {
                _methodTypeOverrideOrNull = value;
            }
        }
    }
}
