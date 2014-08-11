using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Repomat.Schema
{
    internal class RepositoryDefBuilder
    {
        public static RepositoryDef BuildRepositoryDef<TType, TRepo>(NamingConvention tableNamingConvention, NamingConvention columnNamingConvention)
        {
            var entityType = typeof(TType);
            var repoType = typeof(TRepo);

            string tableName = tableNamingConvention.Convert(entityType.Name);

            var repoDefBuilder = new RepositoryDefBuilder();

            IEnumerable<PropertyDef> columns = GetAssignableColumnsForType(columnNamingConvention, entityType).ToArray();

            // Get the primary columns that map to real columns. If there are any that don't map, ignore them.
            // The validation will figure out what to do with them.
            IEnumerable<PropertyDef> primaryKey = repoDefBuilder.GetPrimaryKeyColumns(entityType, repoType)
                .Select(pk => columns.FirstOrDefault(c => c.PropertyName == pk))
                .Where(pk => pk != null)
                .ToArray();


            var entityDef = new EntityDef(entityType, tableName, columns, primaryKey, GetHasIdentity(repoType), repoDefBuilder.GetCreateClassThroughConstructor(entityType));
            IEnumerable<MethodDef> implementationDetails = repoDefBuilder.GetImplementationDetails(repoType, entityDef);

            return new RepositoryDef(entityDef, typeof(TRepo), implementationDetails);
        }

        private RepositoryDefBuilder()
        {
        }

        private bool GetCreateClassThroughConstructor(Type type)
        {
            // If the class only has a default constructor, then the properties must be generated throuh
            // property injection. If the class has more than one constructor, then we use the constructor with the
            // most parameters.
            var ctors = type.GetConstructors();
            if (ctors.Length == 1 && ctors[0].GetParameters().Length == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool GetHasIdentity(Type repoType)
        {
            return repoType.GetMethod("Create") != null;
        }


        public static IEnumerable<PropertyDef> GetAssignableColumnsForType(NamingConvention columnNamingConvention, Type type)
        {
            var coreType = type.GetCoreType();

            var repoDefBuilder = new RepositoryDefBuilder();

            bool createClassThroughConstructor = repoDefBuilder.GetCreateClassThroughConstructor(coreType);
            if (createClassThroughConstructor)
            {
                // If we're using the constructor, then we find the longest constructor.
                ConstructorInfo maxLenCtor = null;
                int maxLength = 0;
                foreach (var ctor in coreType.GetConstructors())
                {
                    if (ctor.GetParameters().Length > maxLength)
                    {
                        maxLenCtor = ctor;
                    }
                }

                // Now, go through the parameters of the ctor. If there's a matching property, then 
                // this is a valid column. If there's no match, then that's an exception.
                foreach (var parm in maxLenCtor.GetParameters())
                {
                    var matchingProp = coreType.GetProperty(parm.Name.Capitalize());
                    if (matchingProp != null)
                    {
                        string propertyName = parm.Name.Capitalize();
                        yield return new PropertyDef(propertyName, columnNamingConvention.Convert(propertyName), parm.ParameterType);
                    }
                }
            }
            else
            {
                foreach (var prop in coreType.GetProperties())
                {
                    if (prop.GetGetMethod() != null && prop.GetSetMethod() != null)
                    {
                        string propertyName = prop.Name;
                        yield return new PropertyDef(propertyName, columnNamingConvention.Convert(propertyName), prop.PropertyType);
                    }
                }
            }
        }

        private IEnumerable<MethodDef> GetImplementationDetails(Type repoType, EntityDef entityDef)
        {
            foreach (var method in repoType.GetMethods())
            {
                yield return new MethodDef(method, entityDef);
            }
        }

        private IEnumerable<string> GetPrimaryKeyColumns(Type entityType, Type repoType)
        {
            // Get all Get or TryGet methods that are singleton methods that return the entityType.
            var getMethods = repoType
                .GetMethods()
                .Where(m => m.Name == "Get" || m.Name == "TryGet")
                .Where(m => m.ReturnType == entityType)
                .ToArray();
            MethodInfo longestGet = null;
            int longestGetCount = 0;

            if (getMethods.Length > 0)
            {
                foreach (var currentGet in getMethods)
                {
                    bool isTryMethod = currentGet.Name.StartsWith("Try");
                    int args = currentGet.GetParameters().Length - (isTryMethod ? 1 : 0);

                    if (args > longestGetCount)
                    {
                        longestGetCount = args;
                        longestGet = currentGet;
                    }
                }

                // The primary key is the arguments to this method.
                List<string> primaryKey = new List<string>();
                foreach (var arg in longestGet.GetParameters())
                {
                    if (!arg.IsOut)
                    {
                        primaryKey.Add(arg.Name.Capitalize());
                    }
                }

                return primaryKey;
            }

            // No Get method? then it's either the "Id" property or the "XxxId" property
            // First we'll get the Id property.
            var idProp = repoType.GetProperty("Id");
            if (idProp == null)
            {
                idProp = repoType.GetProperty(entityType.Name + "Id");
            }

            if (idProp != null)
            {
                return new[] { idProp.Name };
            }

            // No primary key.
            return Enumerable.Empty<string>();
        }
    }
}
