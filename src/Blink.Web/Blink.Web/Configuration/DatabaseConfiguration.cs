using Dapper;
using System.Data;

namespace Blink.Web.Configuration;

public static class DatabaseConfiguration
{
    public static void AddAndConfigureDatabase(this WebApplicationBuilder builder)
    {
        builder.AddNpgsqlDataSource(ServiceNames.BlinkDatabase);
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value)
        {
            if (value is DateTime dateTime)
            {
                return DateOnly.FromDateTime(dateTime);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return DateOnly.FromDateTime(dateTimeOffset.DateTime);
            }

            throw new InvalidCastException($"Unable to cast object of type {value.GetType()} to DateOnly.");
        }

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.Value = value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            parameter.DbType = DbType.Date;
        }
    }
}
