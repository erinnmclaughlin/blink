using System.Data;
using Blink.Database;
using Blink.Videos;
using Dapper;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DatabaseConfiguration
{
    public static void AddBlinkDatabase<T>(this T builder) where T : IHostApplicationBuilder
    {
        builder.AddNpgsqlDataSource("blink-db");
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        
        builder.AddBlinkVideosCore();
        builder.Services.TryAddSingleton<IBlinkUnitOfWorkFactory, BlinkUnitOfWorkFactory>();
        builder.Services.TryAddTransient<IBlinkUnitOfWork>(sp => sp.GetRequiredService<IBlinkUnitOfWorkFactory>().CreateUnitOfWork());
    }

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
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
