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

//$Authors = Jiri Cincura (jiri@cincura.net)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version12
{
	internal class GdsStatement : Version11.GdsStatement
	{
		#region Constructors

		public GdsStatement(IDatabase db)
			: base(db)
		{ }

		public GdsStatement(IDatabase db, TransactionBase transaction)
			: base(db, transaction)
		{ }

		#endregion

		#region Overriden Methods

		public override void Execute()
		{
			if (State == StatementState.Deallocated)
			{
				throw new InvalidOperationException("Statement is not correctly created.");
			}

			Clear();

			try
			{
				RecordsAffected = -1;

				SendExecuteToBuffer();

				_database.Xdr.Flush();

				var numberOfResponses = (StatementType == DbStatementType.StoredProcedure ? 1 : 0) + 1;
				try
				{
					SqlResponse sqlStoredProcedureResponse = null;
					if (StatementType == DbStatementType.StoredProcedure)
					{
						numberOfResponses--;
						sqlStoredProcedureResponse = _database.ReadResponse<SqlResponse>();
						ProcessStoredProcedureExecuteResponse(sqlStoredProcedureResponse);
					}

					numberOfResponses--;
					var executeResponse = _database.ReadResponse<GenericResponse>();
					ProcessExecuteResponse(executeResponse);
				}
				finally
				{
					SafeFinishFetching(ref numberOfResponses);
				}

				// we need to split this in two, to allow server handle op_cancel properly

				if (DoRecordsAffected)
				{
					SendInfoSqlToBuffer(RowsAffectedInfoItems, IscCodes.ROWS_AFFECTED_BUFFER_SIZE);

					_database.Xdr.Flush();

					numberOfResponses = 1;
					try
					{
						numberOfResponses--;
						var rowsAffectedResponse = _database.ReadResponse<GenericResponse>();
						RecordsAffected = ProcessRecordsAffectedBuffer(ProcessInfoSqlResponse(rowsAffectedResponse));
					}
					finally
					{
						SafeFinishFetching(ref numberOfResponses);
					}
				}

				State = StatementState.Executed;
			}
			catch (IOException ex)
			{
				State = StatementState.Error;
				throw IscException.ForErrorCode(IscCodes.isc_network_error, ex);
			}
		}

		#endregion
	}
}
