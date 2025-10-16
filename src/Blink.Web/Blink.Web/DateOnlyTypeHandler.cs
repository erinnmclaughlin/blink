using Dapper;
using System.Data;

namespace Blink.Web;

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
        parameter.Value = value.ToDateTime(TimeOnly.MinValue); // Convert DateOnly to DateTime for Dapper
        parameter.DbType = DbType.Date;
    }
}