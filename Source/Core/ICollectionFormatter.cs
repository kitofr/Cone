﻿using System.Collections;
using System.Collections.Generic;

namespace Cone.Core
{
    interface ICollectionFormatter<T> : IFormatter<IEnumerable<T>>, IFormatter<IEnumerable>
    {
        string Format(IEnumerable<T> collection, IFormatter<T> itemFormatter);
        string Format(IEnumerable collection, IFormatter<T> itemFormatter);
    }
}
