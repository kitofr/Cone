﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cone.Expectations
{
    class ExpectedNull
    {
        public static readonly ExpectedNull IsNull = new ExpectedNull();
        
        public override bool Equals(object obj) {
            return obj == null;
        }

        public override int GetHashCode() {
            return 0;
        }

        public override string ToString() {
            return "null";
        }
    }
}
