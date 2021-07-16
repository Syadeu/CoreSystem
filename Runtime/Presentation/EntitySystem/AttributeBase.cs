﻿using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public abstract class AttributeBase : IAttribute, ICloneable
    {
        public string Name { get; set; } = "New Attribute";
        public Hash Hash { get; set; }

        public virtual object Clone()
        {
            AttributeBase att = (AttributeBase)MemberwiseClone();
            att.Name = string.Copy(Name);

            return att;
        }

        public override string ToString() => Name;
    }
}
