﻿using System;
using System.Linq.Expressions;

namespace Cone
{
    public class NullSubexpressionException : ArgumentNullException 
    {
        readonly Expression context;
        readonly Expression expression;

        public NullSubexpressionException(Expression expression, Expression context) {
            this.context = expression;
            this.expression = context;
        }

        public Expression Context { get { return context; } }
        public Expression Expression { get { return expression; } }
    }
}
