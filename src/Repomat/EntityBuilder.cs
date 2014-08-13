using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Schema;

namespace Repomat
{
    public class EntityBuilder
    {
        private readonly EntityDef _entityDef;

        internal EntityBuilder(EntityDef entityDef)
        {
            _entityDef = entityDef;
        }

        public PropertyBuilder SetupProperty(string propertyName)
        {
            return new PropertyBuilder(_entityDef.Properties.First(p => p.PropertyName == propertyName));
        }
    }
}
