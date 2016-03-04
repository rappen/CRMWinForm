using Microsoft.Xrm.Sdk;
using System;

namespace Cinteros.Xrm.CRMWinForm
{
    public class CRMRecordEventArgs : EventArgs
    {
        private Entity entity;
        private string attribute;

        public CRMRecordEventArgs(Entity entity, string attribute)
        {
            this.entity = entity;
            this.attribute = attribute;
        }

        public Entity Entity { get { return entity; } }

        public string Attribute { get { return attribute; } }

        public object Value { get { return entity != null && entity.Contains(attribute) ? entity[attribute] : null; } }
    }
}
