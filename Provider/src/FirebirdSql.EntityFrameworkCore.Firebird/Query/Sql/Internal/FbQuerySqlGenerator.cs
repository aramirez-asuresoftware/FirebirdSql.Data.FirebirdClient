﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/blob/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Jiri Cincura (jiri@cincura.net), Jean Ressouche, Rafael Almeida (ralms@ralms.net)

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FirebirdSql.EntityFrameworkCore.Firebird.Query.Expressions.Internal;
using FirebirdSql.EntityFrameworkCore.Firebird.Storage.Internal;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Query.Sql.Internal
{
	public class FbQuerySqlGenerator : DefaultQuerySqlGenerator, IFbExpressionVisitor
	{
		public FbQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies, SelectExpression selectExpression)
			: base(dependencies, selectExpression)
		{ }

		protected override string TypedTrueLiteral => FbBoolTypeMapping.TrueLiteral;
		protected override string TypedFalseLiteral => FbBoolTypeMapping.FalseLiteral;

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (binaryExpression.NodeType == ExpressionType.Modulo)
			{
				Sql.Append("MOD(");
				Visit(binaryExpression.Left);
				Sql.Append(", ");
				Visit(binaryExpression.Right);
				Sql.Append(")");
				return binaryExpression;
			}
			else
			{
				return base.VisitBinary(binaryExpression);
			}
		}

		protected override void GenerateTop(SelectExpression selectExpression)
		{
			if (selectExpression.Limit != null)
			{
				Sql.Append("FIRST ");
				Visit(selectExpression.Limit);
				Sql.Append(" ");
			}

			if (selectExpression.Offset != null)
			{
				Sql.Append("SKIP ");
				Visit(selectExpression.Offset);
				Sql.Append(" ");
			}
		}

		protected override void GenerateLimitOffset(SelectExpression selectExpression)
		{
			// handled by GenerateTop
		}

		protected override string GenerateOperator(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Add:
					return " || ";
				case ExpressionType.And:
					return " AND ";
				case ExpressionType.Or:
					return " OR ";
				default:
					return base.GenerateOperator(expression);
			}
		}

		public virtual Expression VisitSubstring(FbSubstringExpression substringExpression)
		{
			Sql.Append("SUBSTRING(");
			Visit(substringExpression.ValueExpression);
			Sql.Append(" FROM ");
			Visit(substringExpression.FromExpression);
			if (substringExpression.ForExpression != null)
			{
				Sql.Append(" FOR ");
				Visit(substringExpression.ForExpression);
			}
			Sql.Append(")");
			return substringExpression;
		}

		public virtual Expression VisitExtract(FbExtractExpression extractExpression)
		{
			Sql.Append("EXTRACT(");
			Sql.Append(extractExpression.Part);
			Sql.Append(" FROM ");
			Visit(extractExpression.ValueExpression);
			Sql.Append(")");
			return extractExpression;
		}

		public virtual Expression VisitDateMember(FbDateTimeDateMemberExpression dateTimeDateMemberExpression)
		{
			Sql.Append("CAST(");
			Visit(dateTimeDateMemberExpression.ValueExpression);
			Sql.Append(" AS DATE)");
			return dateTimeDateMemberExpression;
		}

		protected override void GeneratePseudoFromClause()
		{
			Sql.Append(" FROM RDB$DATABASE");
		}
	}
}
