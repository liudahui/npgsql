﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql.BackendMessages;
using Npgsql.PostgresTypes;
using Npgsql.TypeMapping;

namespace Npgsql.Json.NET
{
    class JsonbHandlerFactory : TypeHandlerFactory
    {
        protected override TypeHandler Create(NpgsqlConnection conn)
            => new JsonbHandler(conn);
    }

    class JsonbHandler : Npgsql.TypeHandlers.JsonbHandler
    {
        public JsonbHandler(NpgsqlConnection connection)
            : base(connection) {}

        protected override async ValueTask<T> Read<T>(NpgsqlReadBuffer buf, int len, bool async, FieldDescription fieldDescription = null)
        {
            var s = await base.Read<string>(buf, len, async, fieldDescription);
            if (typeof(T) == typeof(string))
                return (T)(object)s;
            try
            {
                return JsonConvert.DeserializeObject<T>(s);
            }
            catch (Exception e)
            {
                throw new NpgsqlSafeReadException(e);
            }
        }

        protected override int ValidateAndGetLength(object value, ref NpgsqlLengthCache lengthCache, NpgsqlParameter parameter = null)
        {
            var s = value as string;
            if (s == null)
            {
                s = JsonConvert.SerializeObject(value);
                if (parameter != null)
                    parameter.ConvertedValue = s;
            }
            return base.ValidateAndGetLength(s, ref lengthCache, parameter);
        }

        protected override Task Write(object value, NpgsqlWriteBuffer buf, NpgsqlLengthCache lengthCache, NpgsqlParameter parameter,
            bool async, CancellationToken cancellationToken)
        {
            if (parameter?.ConvertedValue != null)
                value = parameter.ConvertedValue;
            var s = value as string ?? JsonConvert.SerializeObject(value);
            return base.Write(s, buf, lengthCache, parameter, async, cancellationToken);
        }
    }
}